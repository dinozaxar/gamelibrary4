using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace gamelibrary4.Games.Sudoku
{
    public partial class SudokuView : UserControl
    {
        private const int N = 9;

        private int[,] _solution = new int[N, N];
        private int[,] _board = new int[N, N];
        private bool[,] _given = new bool[N, N];      // изначально заполненные
        private bool[,] _wrong = new bool[N, N];      // помечены как ошибочные

        private int _selX = 0, _selY = 0;
        private bool _isGameOver = false;

        private readonly Button[,] _cells = new Button[N, N];
        private readonly SudokuGenerator _gen = new SudokuGenerator();

        public SudokuView()
        {
            InitializeComponent();
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            BuildBoard();
            Dispatcher.BeginInvoke(new Action(() => Focus()));
            StartNewGame();
        }

        // ================================================================
        //  ПОСТРОЕНИЕ ПОЛЯ
        // ================================================================
        private void BuildBoard()
        {
            BoardGrid.Children.Clear();

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    var btn = new Button
                    {
                        FontSize = 24,
                        FontWeight = FontWeights.SemiBold,
                        Cursor = Cursors.Hand,
                        Tag = (x, y),
                        Focusable = false,
                        Margin = new Thickness(0),
                        BorderThickness = new Thickness(0)
                    };

                    btn.Template = BuildCellTemplate();
                    btn.Click += Cell_Click;

                    _cells[x, y] = btn;
                    BoardGrid.Children.Add(btn);
                }
            }
        }

        private ControlTemplate BuildCellTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            return template;
        }

        // ================================================================
        //  НОВАЯ ИГРА
        // ================================================================
        private void StartNewGame()
        {
            var diff = GetSelectedDifficulty();

            GeneratingOverlay.Visibility = Visibility.Visible;
            StatusText.Text = "";

            // Откладываем генерацию на отдельный кадр, чтобы оверлей успел показаться
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _gen.Generate(diff);
                _solution = (int[,])_gen.Solution.Clone();
                _board = (int[,])_gen.Puzzle.Clone();

                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        _given[x, y] = _board[x, y] != 0;
                        _wrong[x, y] = false;
                    }

                _selX = 0;
                _selY = 0;
                _isGameOver = false;

                RefreshAllCells();
                GeneratingOverlay.Visibility = Visibility.Collapsed;
                Focus();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private Difficulty GetSelectedDifficulty()
        {
            var item = DifficultyCombo.SelectedItem as ComboBoxItem;
            string tag = item?.Tag?.ToString() ?? "Medium";
            return Enum.TryParse<Difficulty>(tag, out var d) ? d : Difficulty.Medium;
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e) => StartNewGame();

        private void DifficultyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Автозапуск новой игры при смене сложности
            if (IsLoaded && _solution != null)
                StartNewGame();
        }

        // ================================================================
        //  КЛИКИ И ВВОД
        // ================================================================
        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (_isGameOver) return;
            var btn = (Button)sender;
            var (x, y) = ((int, int))btn.Tag;
            _selX = x;
            _selY = y;
            RefreshAllCells();
            Focus();
        }

        private void DigitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isGameOver) return;
            var btn = (Button)sender;
            if (int.TryParse(btn.Tag?.ToString(), out int digit))
                TryPlace(digit);
            Focus();
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isGameOver) return;
            TryPlace(0);
            Focus();
        }

        private void View_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isGameOver) { e.Handled = true; return; }

            switch (e.Key)
            {
                case Key.Left: if (_selX > 0) _selX--; RefreshAllCells(); break;
                case Key.Right: if (_selX < N - 1) _selX++; RefreshAllCells(); break;
                case Key.Up: if (_selY > 0) _selY--; RefreshAllCells(); break;
                case Key.Down: if (_selY < N - 1) _selY++; RefreshAllCells(); break;

                case Key.Back:
                case Key.Delete:
                    TryPlace(0);
                    break;

                case Key.D1: case Key.NumPad1: TryPlace(1); break;
                case Key.D2: case Key.NumPad2: TryPlace(2); break;
                case Key.D3: case Key.NumPad3: TryPlace(3); break;
                case Key.D4: case Key.NumPad4: TryPlace(4); break;
                case Key.D5: case Key.NumPad5: TryPlace(5); break;
                case Key.D6: case Key.NumPad6: TryPlace(6); break;
                case Key.D7: case Key.NumPad7: TryPlace(7); break;
                case Key.D8: case Key.NumPad8: TryPlace(8); break;
                case Key.D9: case Key.NumPad9: TryPlace(9); break;
            }
            e.Handled = true;
        }

        private void TryPlace(int digit)
        {
            if (_given[_selX, _selY]) return;     // нельзя менять исходные

            _board[_selX, _selY] = digit;
            _wrong[_selX, _selY] = digit != 0 && digit != _solution[_selX, _selY];

            RefreshAllCells();
            CheckWin();
        }

        // ================================================================
        //  ПРОВЕРКА / ПОДСКАЗКА / ОЧИСТКА
        // ================================================================
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            int errors = 0;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (_board[x, y] == 0) continue;
                    if (_board[x, y] != _solution[x, y])
                    {
                        _wrong[x, y] = true;
                        errors++;
                    }
                }

            RefreshAllCells();

            StatusText.Text = errors == 0
                ? "✔ Пока всё правильно"
                : $"✖ Найдено ошибок: {errors}";
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            // Ищем пустую или неправильную клетку и открываем одну случайную
            var candidates = new List<(int x, int y)>();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    if (_board[x, y] != _solution[x, y])
                        candidates.Add((x, y));

            if (candidates.Count == 0)
            {
                StatusText.Text = "💡 Подсказок не требуется";
                return;
            }

            var rnd = new Random();
            var (hx, hy) = candidates[rnd.Next(candidates.Count)];
            _board[hx, hy] = _solution[hx, hy];
            _wrong[hx, hy] = false;

            // Подсветим подсказанную клетку анимацией
            var cell = _cells[hx, hy];
            cell.Opacity = 0.3;
            var fade = new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(600))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            cell.BeginAnimation(OpacityProperty, fade);

            StatusText.Text = "💡 Подсказка использована";
            RefreshAllCells();
            CheckWin();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    if (!_given[x, y])
                    {
                        _board[x, y] = 0;
                        _wrong[x, y] = false;
                    }

            StatusText.Text = "🧹 Все ваши цифры стёрты";
            RefreshAllCells();
        }

        // ================================================================
        //  ОТРИСОВКА
        // ================================================================
        private void RefreshAllCells()
        {
            int selValue = _board[_selX, _selY];

            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    var btn = _cells[x, y];
                    int value = _board[x, y];

                    btn.Content = value == 0 ? "" : value.ToString();

                    // Цвет текста
                    if (_wrong[x, y])
                        btn.Foreground = (Brush)Application.Current.FindResource("CoralBrush");
                    else if (_given[x, y])
                        btn.Foreground = (Brush)Application.Current.FindResource("TextBrush");
                    else if (value != 0)
                        btn.Foreground = (Brush)Application.Current.FindResource("XBrush"); // акцентный (индиго/коралл)
                    else
                        btn.Foreground = (Brush)Application.Current.FindResource("TextBrush");

                    // Фон клетки
                    Brush bg;
                    bool sameRowOrColOrBox =
                        (x == _selX) || (y == _selY) ||
                        (x / 3 == _selX / 3 && y / 3 == _selY / 3);

                    if (x == _selX && y == _selY)
                        bg = (Brush)Application.Current.FindResource("OverlayMediumBrush");      // выделенная
                    else if (_wrong[x, y])
                        bg = (Brush)Application.Current.FindResource("OverlayMediumBrush");
                    else if (selValue != 0 && value == selValue)
                        bg = (Brush)Application.Current.FindResource("OverlayWeakBrush");        // та же цифра
                    else if (sameRowOrColOrBox)
                        bg = (Brush)Application.Current.FindResource("OverlayWeakBrush");        // строка/столбец/блок
                    else
                        bg = Brushes.Transparent;

                    btn.Background = bg;
                }
            }
        }

        // ================================================================
        //  ПОБЕДА
        // ================================================================
        private void CheckWin()
        {
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (_board[x, y] == 0) return;
                    if (_board[x, y] != _solution[x, y]) return;
                }

            _isGameOver = true;

            // Записываем рекорд: очки = уровень сложности × 1000, время = 0
            int diffScore = GetSelectedDifficulty() switch
            {
                Difficulty.Easy => 1000,
                Difficulty.Medium => 2000,
                Difficulty.Hard => 3000,
                _ => 1000
            };
            Services.ScoreService.AddScore("sudoku", diffScore, 0, diffScore / 100);

            MessageBox.Show($"🎉 Судоку решено!\n\nСложность: {GetSelectedDifficulty()}\nОчки: {diffScore}",
                            "Победа!", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}