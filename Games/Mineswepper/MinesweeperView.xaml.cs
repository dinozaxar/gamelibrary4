using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace gamelibrary4.Games.Minesweeper
{
    public partial class MinesweeperView : UserControl
    {
        // ---- Настройки игры ----
        private const int Cols = 10;
        private const int Rows = 10;
        private const int MineCount = 15;
        private const int CellSize = 40;

        // ---- Логика ----
        private readonly Random _rng = new Random();
        private CellState[,] _state = new CellState[Cols, Rows];
        private bool[,] _isMine = new bool[Cols, Rows];
        private bool _firstMove = true;
        private bool _gameOver = false;
        private int _flagsPlaced = 0;

        // ---- Таймер ----
        private DispatcherTimer _timer;
        private int _secondsElapsed = 0;

        // ---- Визуальные ссылки ----
        private Button[,] _buttons = new Button[Cols, Rows];

        public MinesweeperView()
        {
            InitializeComponent();
            Loaded += MinesweeperView_Loaded;
        }

        private void MinesweeperView_Loaded(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        // ================================================================
        //  УПРАВЛЕНИЕ ИГРОЙ
        // ================================================================
        private void StartNewGame()
        {
            _firstMove = true;
            _gameOver = false;
            _flagsPlaced = 0;
            _secondsElapsed = 0;
            _state = new CellState[Cols, Rows];
            _isMine = new bool[Cols, Rows];

            TimerText.Text = "00:00";
            MinesLeftText.Text = MineCount.ToString();

            StopTimer();
            BuildBoard();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private void StartTimer()
        {
            StopTimer();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _secondsElapsed++;
                TimerText.Text = TimeSpan.FromSeconds(_secondsElapsed).ToString(@"mm\:ss");
            };
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }

        // ================================================================
        //  ПОСТРОЕНИЕ ПОЛЯ
        // ================================================================
        private void BuildBoard()
        {
            BoardGrid.Children.Clear();
            BoardGrid.RowDefinitions.Clear();
            BoardGrid.ColumnDefinitions.Clear();

            for (int x = 0; x < Cols; x++)
                BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSize) });
            for (int y = 0; y < Rows; y++)
                BoardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSize) });

            for (int x = 0; x < Cols; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    var btn = new Button
                    {
                        Width = CellSize - 4,
                        Height = CellSize - 4,
                        Margin = new Thickness(2),
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Cursor = Cursors.Hand,
                        Tag = (x, y),
                        Focusable = false,
                        Template = BuildCellTemplate()
                    };

                    btn.PreviewMouseLeftButtonDown += Cell_LeftClick;
                    btn.PreviewMouseRightButtonDown += Cell_RightClick;

                    Grid.SetColumn(btn, x);
                    Grid.SetRow(btn, y);

                    _buttons[x, y] = btn;
                    BoardGrid.Children.Add(btn);

                    UpdateCellVisual(x, y);
                }
            }
        }

        private ControlTemplate BuildCellTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            template.VisualTree = border;

            var hoverTrigger = new Trigger
            {
                Property = Button.IsMouseOverProperty,
                Value = true
            };
            hoverTrigger.Setters.Add(new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(0x55, 0xEE, 0x45, 0x40)),
                "bd"));
            template.Triggers.Add(hoverTrigger);

            return template;
        }

        // ================================================================
        //  ГЕНЕРАЦИЯ МИН (после первого хода, чтобы первый клик был безопасным)
        // ================================================================
        private void PlaceMines(int safeX, int safeY)
        {
            int placed = 0;
            while (placed < MineCount)
            {
                int x = _rng.Next(Cols);
                int y = _rng.Next(Rows);

                // Клетка-цель и её соседи — не мины
                if (Math.Abs(x - safeX) <= 1 && Math.Abs(y - safeY) <= 1) continue;
                if (_isMine[x, y]) continue;

                _isMine[x, y] = true;
                placed++;
            }
        }

        private int CountMinesAround(int cx, int cy)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || nx >= Cols || ny < 0 || ny >= Rows) continue;
                    if (_isMine[nx, ny]) count++;
                }
            }
            return count;
        }

        // ================================================================
        //  КЛИКИ
        // ================================================================
        private void Cell_LeftClick(object sender, MouseButtonEventArgs e)
        {
            if (_gameOver) return;
            var btn = (Button)sender;
            var (x, y) = ((int, int))btn.Tag;

            if (_state[x, y] == CellState.Flagged) return;
            if (_state[x, y] == CellState.Open) return;

            // Первый ход — генерируем мины вокруг, безопасно
            if (_firstMove)
            {
                _firstMove = false;
                PlaceMines(x, y);
                StartTimer();
            }

            if (_isMine[x, y])
            {
                LoseGame(x, y);
                return;
            }

            RevealCell(x, y);
            CheckWin();
        }

        private void Cell_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (_gameOver) return;
            var btn = (Button)sender;
            var (x, y) = ((int, int))btn.Tag;

            if (_state[x, y] == CellState.Open) return;

            if (_state[x, y] == CellState.Hidden)
            {
                _state[x, y] = CellState.Flagged;
                _flagsPlaced++;
            }
            else if (_state[x, y] == CellState.Flagged)
            {
                _state[x, y] = CellState.Hidden;
                _flagsPlaced--;
            }

            MinesLeftText.Text = (MineCount - _flagsPlaced).ToString();
            UpdateCellVisual(x, y);
            e.Handled = true;
        }

        // ================================================================
        //  ОТКРЫТИЕ КЛЕТОК (с авто-раскрытием пустых зон)
        // ================================================================
        private void RevealCell(int x, int y)
        {
            if (x < 0 || x >= Cols || y < 0 || y >= Rows) return;
            if (_state[x, y] == CellState.Open) return;
            if (_state[x, y] == CellState.Flagged) return;

            _state[x, y] = CellState.Open;
            UpdateCellVisual(x, y);

            int mines = CountMinesAround(x, y);
            if (mines == 0)
            {
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        RevealCell(x + dx, y + dy);
                    }
            }
        }

        // ================================================================
        //  КОНЦЫ ИГРЫ
        // ================================================================
        private void LoseGame(int hitX, int hitY)
        {
            _gameOver = true;
            StopTimer();

            // Показываем все мины
            for (int x = 0; x < Cols; x++)
                for (int y = 0; y < Rows; y++)
                {
                    if (_isMine[x, y] && _state[x, y] != CellState.Flagged)
                    {
                        _state[x, y] = CellState.Open;
                        UpdateCellVisual(x, y);
                    }
                }

            // Подсветить мину, на которую нажал
            HighlightDeath(hitX, hitY);

            Services.ScoreService.AddScore("minesweeper", 0, _secondsElapsed, 0);

            MessageBox.Show("💥 Бум! Вы наступили на мину.",
                            "Игра окончена",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void HighlightDeath(int x, int y)
        {
            var btn = _buttons[x, y];
            btn.Background = (Brush)FindResource("CoralBrush");

            var shake = new DoubleAnimationUsingKeyFrames();
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(-6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));

            var transform = new TranslateTransform();
            btn.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        private void CheckWin()
        {
            int opened = 0;
            for (int x = 0; x < Cols; x++)
                for (int y = 0; y < Rows; y++)
                    if (_state[x, y] == CellState.Open) opened++;

            if (opened == Cols * Rows - MineCount)
            {
                _gameOver = true;
                StopTimer();

                Services.ScoreService.AddScore("minesweeper", 100, _secondsElapsed, 15);
                
                MessageBox.Show($"🎉 Победа! Время: {TimerText.Text}",
                                "Ура!",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ================================================================
        //  ВИЗУАЛИЗАЦИЯ КЛЕТКИ
        // ================================================================
        private void UpdateCellVisual(int x, int y)
        {
            var btn = _buttons[x, y];
            var state = _state[x, y];

            if (state == CellState.Hidden)
            {
                btn.Background = (Brush)FindResource("CellClosedBrush"); // #510A32
                btn.Content = "";
                btn.Foreground = Brushes.White;
            }
            else if (state == CellState.Flagged)
            {
                btn.Background = (Brush)FindResource("CellFlaggedBrush"); // #C72C41
                btn.Content = "🚩";
                btn.FontSize = 16;
            }
            else if (state == CellState.Open)
            {
                if (_isMine[x, y])
                {
                    btn.Background = (Brush)FindResource("CellOpenBrush");
                    btn.Content = "💣";
                    btn.FontSize = 18;
                }
                else
                {
                    int n = CountMinesAround(x, y);
                    btn.Background = new SolidColorBrush(Color.FromArgb(0x66, 0x2D, 0x14, 0x2C)); // полупрозрачный #2D142C
                    if (n == 0)
                    {
                        btn.Content = "";
                    }
                    else
                    {
                        btn.Content = n.ToString();
                        btn.FontSize = 15;
                        btn.Foreground = NumberColor(n);
                    }
                }
            }
        }

        private Brush NumberColor(int n)
        {
            // Возьмём цвета из темы для 1-2, остальные — универсальные
            switch (n)
            {
                case 1: return (Brush)FindResource("CoralBrush");
                case 2: return new SolidColorBrush(Color.FromRgb(0xFF, 0x9A, 0x7A));
                case 3: return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x8A));
                case 4: return new SolidColorBrush(Color.FromRgb(0xE0, 0xB0, 0xFF));
                case 5: return new SolidColorBrush(Color.FromRgb(0x9F, 0xE8, 0xFF));
                case 6: return new SolidColorBrush(Color.FromRgb(0xB8, 0xFF, 0xD0));
                case 7: return new SolidColorBrush(Color.FromRgb(0xFF, 0xE7, 0x8A));
                default: return (Brush)FindResource("TextBrush");
            }
        }

        // ================================================================
        //  СОСТОЯНИЕ КЛЕТКИ
        // ================================================================
        private enum CellState
        {
            Hidden,
            Open,
            Flagged
        }
    }
}