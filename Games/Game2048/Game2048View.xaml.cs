using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace gamelibrary4.Games.Game2048
{
    public partial class Game2048View : UserControl
    {
        private const int Size = 4;
        private const double CellSize = 100;
        private const double CellMargin = 6;

        private int[,] _board = new int[Size, Size];
        private int _score = 0;
        private int _best = 0;
        private bool _isGameOver = false;
        private bool _isWinShown = false;

        private readonly Random _rng = new Random();
        private readonly Dictionary<(int, int), Border> _tiles = new Dictionary<(int, int), Border>();

        public Game2048View()
        {
            InitializeComponent();
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => Focus()));
            StartNewGame();
        }

        // ================================================================
        //  СТАРТ ИГРЫ
        // ================================================================
        private void StartNewGame()
        {
            _board = new int[Size, Size];
            _score = 0;
            _isGameOver = false;
            _isWinShown = false;

            _tiles.Clear();
            BoardCanvas.Children.Clear();

            SpawnRandomTile();
            SpawnRandomTile();

            UpdateHud();
            RenderBoard();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
            Focus();
        }

        // ================================================================
        //  СПАВН
        // ================================================================
        private void SpawnRandomTile()
        {
            var free = new List<(int, int)>();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if (_board[x, y] == 0) free.Add((x, y));

            if (free.Count == 0) return;

            var (fx, fy) = free[_rng.Next(free.Count)];
            _board[fx, fy] = _rng.Next(10) < 9 ? 2 : 4;
        }

        // ================================================================
        //  УПРАВЛЕНИЕ
        // ================================================================
        private void View_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isGameOver) { e.Handled = true; return; }

            Direction? dir = null;
            switch (e.Key)
            {
                case Key.Up:
                case Key.W: dir = Direction.Up; break;
                case Key.Down:
                case Key.S: dir = Direction.Down; break;
                case Key.Left:
                case Key.A: dir = Direction.Left; break;
                case Key.Right:
                case Key.D: dir = Direction.Right; break;
                case Key.R: StartNewGame(); e.Handled = true; return;
            }

            if (dir.HasValue)
            {
                if (Move(dir.Value))
                {
                    SpawnRandomTile();
                    RenderBoard();
                    UpdateHud();
                    CheckWinLose();
                }
            }
            e.Handled = true;
        }

        private enum Direction { Up, Down, Left, Right }

        // ================================================================
        //  ЛОГИКА ХОДА
        // ================================================================
        private bool Move(Direction dir)
        {
            bool moved = false;

            switch (dir)
            {
                case Direction.Left:
                    for (int y = 0; y < Size; y++)
                        moved |= SlideRow(y, leftToRight: true);
                    break;
                case Direction.Right:
                    for (int y = 0; y < Size; y++)
                        moved |= SlideRow(y, leftToRight: false);
                    break;
                case Direction.Up:
                    for (int x = 0; x < Size; x++)
                        moved |= SlideColumn(x, topToBottom: true);
                    break;
                case Direction.Down:
                    for (int x = 0; x < Size; x++)
                        moved |= SlideColumn(x, topToBottom: false);
                    break;
            }

            return moved;
        }

        private bool SlideRow(int y, bool leftToRight)
        {
            int[] line = new int[Size];
            for (int x = 0; x < Size; x++) line[x] = _board[x, y];

            int[] merged = MergeLine(line, leftToRight);

            bool changed = false;
            for (int x = 0; x < Size; x++)
            {
                if (_board[x, y] != merged[x]) changed = true;
                _board[x, y] = merged[x];
            }
            return changed;
        }

        private bool SlideColumn(int x, bool topToBottom)
        {
            int[] line = new int[Size];
            for (int y = 0; y < Size; y++) line[y] = _board[x, y];

            int[] merged = MergeLine(line, topToBottom);

            bool changed = false;
            for (int y = 0; y < Size; y++)
            {
                if (_board[x, y] != merged[y]) changed = true;
                _board[x, y] = merged[y];
            }
            return changed;
        }

        private int[] MergeLine(int[] line, bool leftToRight)
        {
            // Берём только ненулевые в порядке движения
            var values = new List<int>();
            if (leftToRight)
            {
                foreach (var v in line) if (v != 0) values.Add(v);
            }
            else
            {
                for (int i = line.Length - 1; i >= 0; i--) if (line[i] != 0) values.Add(line[i]);
            }

            // Слияние
            var merged = new List<int>();
            int i2 = 0;
            while (i2 < values.Count)
            {
                if (i2 + 1 < values.Count && values[i2] == values[i2 + 1])
                {
                    int m = values[i2] * 2;
                    merged.Add(m);
                    _score += m;
                    i2 += 2;
                }
                else
                {
                    merged.Add(values[i2]);
                    i2++;
                }
            }

            while (merged.Count < Size) merged.Add(0);

            // Разворачиваем обратно
            int[] result = new int[Size];
            if (leftToRight)
            {
                for (int i = 0; i < Size; i++) result[i] = merged[i];
            }
            else
            {
                for (int i = 0; i < Size; i++) result[Size - 1 - i] = merged[i];
            }
            return result;
        }

        // ================================================================
        //  ОТРИСОВКА
        // ================================================================
        private void RenderBoard()
        {
            BoardCanvas.Children.Clear();
            _tiles.Clear();

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    if (_board[x, y] == 0) continue;

                    var tile = CreateTile(_board[x, y]);
                    Canvas.SetLeft(tile, x * CellSize + CellMargin);
                    Canvas.SetTop(tile, y * CellSize + CellMargin);
                    BoardCanvas.Children.Add(tile);
                    _tiles[(x, y)] = tile;

                    // Анимация появления
                    tile.Opacity = 0;
                    tile.RenderTransformOrigin = new Point(0.5, 0.5);
                    tile.RenderTransform = new ScaleTransform(0.7, 0.7);

                    var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
                    var scaleX = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(140))
                    { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 } };
                    var scaleY = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(140))
                    { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 } };

                    tile.BeginAnimation(OpacityProperty, fade);
                    var st = (ScaleTransform)tile.RenderTransform;
                    st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                    st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
                }
            }
        }

        private Border CreateTile(int value)
        {
            var border = new Border
            {
                Width = CellSize - CellMargin * 2,
                Height = CellSize - CellMargin * 2,
                CornerRadius = new CornerRadius(10),
                Background = TileBackground(value)
            };

            border.Child = new TextBlock
            {
                Text = value.ToString(),
                FontSize = value >= 1024 ? 28 : (value >= 128 ? 32 : 38),
                FontWeight = FontWeights.Bold,
                Foreground = TileForeground(value),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            return border;
        }

        private Brush TileBackground(int value)
        {
            // Цвета из палитры + промежуточные оттенки
            switch (value)
            {
                case 2: return new SolidColorBrush(Color.FromRgb(0xEE, 0xE4, 0xDA));
                case 4: return new SolidColorBrush(Color.FromRgb(0xED, 0xCF, 0x72));
                case 8: return (Brush)Application.Current.FindResource("CoralBrush");
                case 16: return (Brush)Application.Current.FindResource("CrimsonBrush");
                case 32: return (Brush)Application.Current.FindResource("BurgundyBrush");
                case 64: return (Brush)Application.Current.FindResource("PurpleBrush");
                case 128: return new SolidColorBrush(Color.FromRgb(0x9B, 0x30, 0x6B));
                case 256: return new SolidColorBrush(Color.FromRgb(0xB6, 0x3A, 0x81));
                case 512: return new SolidColorBrush(Color.FromRgb(0xD1, 0x4A, 0x99));
                case 1024: return new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0xB0));
                case 2048: return new SolidColorBrush(Color.FromRgb(0xF2, 0x7A, 0xC5));
                default: return new SolidColorBrush(Color.FromRgb(0xF7, 0x9E, 0xD6));
            }
        }

        private Brush TileForeground(int value)
        {
            // На 2 и 4 — тёмный текст, на остальных — светлый
            return value <= 4
                ? new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E))
                : Brushes.White;
        }

        // ================================================================
        //  HUD
        // ================================================================
        private void UpdateHud()
        {
            if (_score > _best) _best = _score;
            ScoreText.Text = _score.ToString();
            BestText.Text = _best.ToString();
        }

        // ================================================================
        //  ПОБЕДА / ПРОИГРЫШ
        // ================================================================
        private void CheckWinLose()
        {
            // Победа
            if (!_isWinShown)
            {
                for (int x = 0; x < Size; x++)
                    for (int y = 0; y < Size; y++)
                        if (_board[x, y] == 2048)
                        {
                            _isWinShown = true;
                            Services.ScoreService.AddScore("2048", _score, 0, 2048);
                            MessageBox.Show($"🎉 Вы собрали 2048!\n\nОчки: {_score}",
                                            "Победа!", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
            }

            // Проигрыш: нет ходов
            if (!HasMoves())
            {
                _isGameOver = true;
                Services.ScoreService.AddScore("2048", _score, 0, MaxTile());
                MessageBox.Show($"😔 Нет ходов.\n\nОчки: {_score}\nМаксимальная плитка: {MaxTile()}",
                                "Игра окончена", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool HasMoves()
        {
            // Есть пустые?
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if (_board[x, y] == 0) return true;

            // Есть соседи одинаковые?
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                {
                    if (x + 1 < Size && _board[x, y] == _board[x + 1, y]) return true;
                    if (y + 1 < Size && _board[x, y] == _board[x, y + 1]) return true;
                }

            return false;
        }

        private int MaxTile()
        {
            int max = 0;
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    if (_board[x, y] > max) max = _board[x, y];
            return max;
        }
    }
}