using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace gamelibrary4.Games.Snake
{
    public partial class SnakeView : UserControl
    {
        // ---- Настройки поля ----
        private const int Cols = 20;
        private const int Rows = 20;
        private const int CellSize = 24;

        // ---- Игровые данные ----
        private readonly List<Point> _snake = new List<Point>();   // [0] — голова
        private Point _food;
        private Direction _direction = Direction.Right;
        private Direction _nextDirection = Direction.Right;
        private int _score = 0;
        private int _best = 0;
        private int _baseTickMs = 160;      // стартовая скорость (мс на шаг)
        private int _speedLevel = 1;
        private bool _isPaused = false;
        private bool _isGameOver = false;
        private bool _isRunning = false;

        // ---- Таймер игрового цикла ----
        private DispatcherTimer _gameTimer;

        // ---- Ссылки на визуальные элементы змейки и еды ----
        private readonly Dictionary<Point, Rectangle> _snakeRects = new Dictionary<Point, Rectangle>();
        private Ellipse _foodEllipse;

        // ---- Random ----
        private readonly Random _rng = new Random();

        // ---- Конструктор ----
        public SnakeView()
        {
            InitializeComponent();
        }

        private void SnakeView_Loaded(object sender, RoutedEventArgs e)
        {
            // Фокус, чтобы ловить клавиши
            Dispatcher.BeginInvoke(new Action(() => Focus()));
            StartNewGame();
        }

        // ================================================================
        //  СТАРТ / РЕСТАРТ
        // ================================================================
        private void StartNewGame()
        {
            _snake.Clear();
            _snakeRects.Clear();
            BoardCanvas.Children.Clear();

            _isPaused = false;
            _isGameOver = false;
            _score = 0;
            _speedLevel = 1;
            _baseTickMs = 160;
            _direction = Direction.Right;
            _nextDirection = Direction.Right;

            // Стартовая змейка из 3 сегментов по центру
            int startX = Cols / 2;
            int startY = Rows / 2;
            _snake.Add(new Point(startX, startY)); // голова
            _snake.Add(new Point(startX - 1, startY));
            _snake.Add(new Point(startX - 2, startY));

            // Рисуем сегменты
            for (int i = 0; i < _snake.Count; i++)
                DrawSnakeSegment(_snake[i], i);

            SpawnFood();
            UpdateHud();

            StartGameLoop();
            _isRunning = true;
            PauseButton.Content = "⏸  Пауза";
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
            Focus();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePause();
            Focus();
        }

        private void TogglePause()
        {
            if (_isGameOver) return;

            _isPaused = !_isPaused;
            PauseButton.Content = _isPaused ? "▶  Продолжить" : "⏸  Пауза";
        }

        // ================================================================
        //  ИГРОВОЙ ЦИКЛ
        // ================================================================
        private void StartGameLoop()
        {
            _gameTimer?.Stop();
            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_baseTickMs)
            };
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();
        }

        private void RestartGameLoop()
        {
            _gameTimer?.Stop();
            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_baseTickMs)
            };
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (_isPaused || _isGameOver) return;
            Step();
        }

        // ================================================================
        //  ШАГ ИГРЫ
        // ================================================================
        private void Step()
        {
            // Применяем намерение поворота
            _direction = _nextDirection;

            // Новая голова
            var head = _snake[0];
            Point newHead = _direction switch
            {
                Direction.Up => new Point(head.X, head.Y - 1),
                Direction.Down => new Point(head.X, head.Y + 1),
                Direction.Left => new Point(head.X - 1, head.Y),
                Direction.Right => new Point(head.X + 1, head.Y),
                _ => head
            };

            // Проверка стен
            if (newHead.X < 0 || newHead.X >= Cols || newHead.Y < 0 || newHead.Y >= Rows)
            {
                GameOver("Вы врезались в стену!");
                return;
            }

            // Проверка столкновения с телом (не считая хвост, который уйдёт)
            bool willEat = (newHead == _food);
            int lastIndex = _snake.Count - 1;
            for (int i = 0; i < _snake.Count; i++)
            {
                if (!willEat && i == lastIndex) continue; // хвост сдвинется
                if (_snake[i] == newHead)
                {
                    GameOver("Вы врезались в себя!");
                    return;
                }
            }

            // Двигаем змейку
            if (willEat)
            {
                // Растём: добавляем голову, хвост остаётся
                _snake.Insert(0, newHead);
                DrawSnakeSegment(newHead, 0);

                _score++;
                _best = Math.Max(_best, _score);

                // Каждые 5 яблок — ускоряемся
                if (_score % 5 == 0 && _baseTickMs > 60)
                {
                    _baseTickMs -= 15;
                    _speedLevel++;
                    RestartGameLoop();
                }

                SpawnFood();
                UpdateHud();

                // Победа — змейка заняла всё поле
                if (_snake.Count >= Cols * Rows)
                {
                    WinGame();
                    return;
                }
            }
            else
            {
                // Обычный шаг: добавляем голову, убираем хвост
                var tail = _snake[_snake.Count - 1];
                _snake.RemoveAt(_snake.Count - 1);
                RemoveSnakeSegment(tail);

                _snake.Insert(0, newHead);
                DrawSnakeSegment(newHead, 0);
            }

            // Перекрашиваем сегменты (голова ярче, хвост темнее)
            RecolorSnake();
        }

        // ================================================================
        //  ОТРИСОВКА
        // ================================================================
        private void DrawSnakeSegment(Point p, int index)
        {
            var rect = new Rectangle
            {
                Width = CellSize - 2,
                Height = CellSize - 2,
                RadiusX = 5,
                RadiusY = 5,
                Fill = SnakeBrush(index)
            };

            Canvas.SetLeft(rect, p.X * CellSize + 1);
            Canvas.SetTop(rect, p.Y * CellSize + 1);

            BoardCanvas.Children.Add(rect);

            if (_snakeRects.ContainsKey(p))
                _snakeRects[p] = rect;
            else
                _snakeRects.Add(p, rect);
        }

        private void RemoveSnakeSegment(Point p)
        {
            if (_snakeRects.TryGetValue(p, out var rect))
            {
                BoardCanvas.Children.Remove(rect);
                _snakeRects.Remove(p);
            }
        }

        private void RecolorSnake()
        {
            for (int i = 0; i < _snake.Count; i++)
            {
                if (_snakeRects.TryGetValue(_snake[i], out var rect))
                    rect.Fill = SnakeBrush(i);
            }
        }

        private Brush SnakeBrush(int index)
        {
            // Голова — корал / индиго
            if (index == 0)
                return (Brush)FindResource("CoralBrush");

            // Хвост — градиент от акцента к фону
            var accentBrush = (SolidColorBrush)FindResource("CoralBrush");
            var tailBrush = (SolidColorBrush)FindResource("CrimsonBrush");

            double t = _snake.Count <= 2 ? 1.0 : (double)index / (_snake.Count - 1);
            var a = accentBrush.Color;
            var b = tailBrush.Color;

            return new SolidColorBrush(Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t)));
        }

        // ================================================================
        //  ЕДА
        // ================================================================
        private void SpawnFood()
        {
            // Ищем свободную клетку
            var free = new List<Point>();
            for (int x = 0; x < Cols; x++)
                for (int y = 0; y < Rows; y++)
                {
                    var p = new Point(x, y);
                    if (!_snake.Contains(p)) free.Add(p);
                }

            if (free.Count == 0) return;

            _food = free[_rng.Next(free.Count)];

            // Удаляем старое яблоко
            if (_foodEllipse != null)
                BoardCanvas.Children.Remove(_foodEllipse);

            _foodEllipse = new Ellipse
            {
                Width = CellSize - 6,
                Height = CellSize - 6,
                Fill = (Brush)FindResource("CrimsonBrush"),
                Stroke = (Brush)FindResource("CoralBrush"),
                StrokeThickness = 1.5
            };

            Canvas.SetLeft(_foodEllipse, _food.X * CellSize + 3);
            Canvas.SetTop(_foodEllipse, _food.Y * CellSize + 3);

            // Небольшая анимация появления
            _foodEllipse.RenderTransformOrigin = new Point(0.5, 0.5);
            _foodEllipse.RenderTransform = new ScaleTransform(0.5, 0.5);

            var scaleUp = new System.Windows.Media.Animation.DoubleAnimation(0.5, 1.0, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new System.Windows.Media.Animation.CubicEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
                }
            };
            var scale = (ScaleTransform)_foodEllipse.RenderTransform;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);

            BoardCanvas.Children.Add(_foodEllipse);
        }

        // ================================================================
        //  HUD
        // ================================================================
        private void UpdateHud()
        {
            ScoreText.Text = _score.ToString();
            LengthText.Text = _snake.Count.ToString();
            BestText.Text = _best.ToString();
            SpeedText.Text = _speedLevel.ToString();
        }

        // ================================================================
        //  УПРАВЛЕНИЕ
        // ================================================================
        private void SnakeView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    TryChangeDirection(Direction.Up);
                    break;
                case Key.Down:
                case Key.S:
                    TryChangeDirection(Direction.Down);
                    break;
                case Key.Left:
                case Key.A:
                    TryChangeDirection(Direction.Left);
                    break;
                case Key.Right:
                case Key.D:
                    TryChangeDirection(Direction.Right);
                    break;
                case Key.Space:
                case Key.P:
                    TogglePause();
                    break;
                case Key.R:
                    StartNewGame();
                    break;
            }
            e.Handled = true;
        }

        private void TryChangeDirection(Direction newDir)
        {
            // Нельзя развернуться на 180°
            if (_direction == Direction.Up && newDir == Direction.Down) return;
            if (_direction == Direction.Down && newDir == Direction.Up) return;
            if (_direction == Direction.Left && newDir == Direction.Right) return;
            if (_direction == Direction.Right && newDir == Direction.Left) return;

            _nextDirection = newDir;
        }

        // ================================================================
        //  КОНЦЫ ИГРЫ
        // ================================================================

        private void GameOver(string reason)
        {
            _isGameOver = true;
            _gameTimer?.Stop();

            // Подсветим голову
            if (_snakeRects.TryGetValue(_snake[0], out var headRect))
                headRect.Fill = (Brush)FindResource("CoralBrush");

            Services.ScoreService.AddScore("snake", _score, 0, _snake.Count);
            
            MessageBox.Show($"💥 {reason}\n\nВаш счёт: {_score}\nДлина змейки: {_snake.Count}",
                            "Игра окончена",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void WinGame()
        {
            _isGameOver = true;
            _gameTimer?.Stop();

            Services.ScoreService.AddScore("snake", _score, 0, _snake.Count);

            MessageBox.Show($"🏆 Невероятно! Вы заполнили всё поле!\n\nСчёт: {_score}",
                            "Победа!",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ================================================================
        //  ВСПОМОГАТЕЛЬНОЕ
        // ================================================================
        private enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}