using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace gamelibrary4.Games.Hangman
{
    public partial class HangmanView : UserControl
    {
        private const int MaxMistakes = 6;

        // Словарик: слово + подсказка
        private readonly (string Word, string Hint)[] _words =
        {
            ("КОШКА",       "животное"),
            ("СОБАКА",      "животное"),
            ("СЛОН",        "животное"),
            ("ЖИРАФ",       "животное"),
            ("ПРОГРАММА",   "IT"),
            ("КОМПЬЮТЕР",   "IT"),
            ("КЛАВИАТУРА",  "IT"),
            ("МОНИТОР",     "IT"),
            ("ЯБЛОКО",      "еда"),
            ("БАНАН",       "еда"),
            ("АПЕЛЬСИН",    "еда"),
            ("ШОКОЛАД",     "еда"),
            ("МОСКВА",      "город"),
            ("ПАРИЖ",       "город"),
            ("ТОКИО",       "город"),
            ("РОССИЯ",      "страна"),
            ("ГЕРМАНИЯ",    "страна"),
            ("КНИГА",       "предмет"),
            ("СТОЛ",        "предмет"),
            ("ОКНО",        "предмет"),
            ("МУЗЫКА",      "искусство"),
            ("КАРТИНА",     "искусство"),
            ("ФИЛЬМ",       "искусство"),
            ("СПОРТ",       "активность"),
            ("ФУТБОЛ",      "спорт"),
            ("ПЛАВАНИЕ",    "спорт"),
        };

        private string _word = "";
        private string _hint = "";
        private readonly HashSet<char> _guessed = new HashSet<char>();
        private int _mistakes = 0;
        private int _wins = 0;
        private bool _isOver = false;

        private readonly Random _rng = new Random();

        // Ссылки на элементы виселицы (чтобы показывать по частям)
        private readonly List<Shape> _hangmanParts = new List<Shape>();

        public HangmanView()
        {
            InitializeComponent();
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => Focus()));
            BuildKeyboard();
            StartNewWord();
        }

        // ================================================================
        //  НОВОЕ СЛОВО
        // ================================================================
        private void StartNewWord()
        {
            var pick = _words[_rng.Next(_words.Length)];
            _word = pick.Word;
            _hint = pick.Hint;
            _guessed.Clear();
            _mistakes = 0;
            _isOver = false;

            HintText.Text = $"Подсказка: {_hint}";
            LivesText.Text = MaxMistakes.ToString();

            DrawGallows();
            UpdateKeyboardState();
            UpdateWordDisplay();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewWord();
            Focus();
        }

        // ================================================================
        //  КЛАВИАТУРА
        // ================================================================
        private void BuildKeyboard()
        {
            var letters = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ".ToCharArray();
            var items = letters.Select(c => new LetterItem { Letter = c.ToString() }).ToList();
            KeyboardPanel.ItemsSource = items;
        }

        private void LetterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isOver) return;
            var btn = (Button)sender;
            char letter = btn.Content.ToString()[0];

            if (_guessed.Contains(letter)) return;
            _guessed.Add(letter);

            if (!_word.Contains(letter))
            {
                _mistakes++;
                LivesText.Text = (MaxMistakes - _mistakes).ToString();
                ShowNextHangmanPart();
            }

            UpdateKeyboardState();
            UpdateWordDisplay();
            CheckGameEnd();
            Focus();
        }

        private void View_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isOver) { e.Handled = true; return; }

            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                // Сопоставление латинской раскладки с русскими буквами — упрощённо:
                // A→Ф, B→И, C→С, D→В, E→У, F→А, G→П, H→Р, I→Ш, J→О, K→Л, L→Д, M→Ь,
                // N→Т, O→Щ, P→З, Q→Й, R→К, S→Ы, T→Е, U→Г, V→М, W→Ц, X→Ч, Y→Н, Z→Я
                string map = "ФИСВУАПРШОЛДЬТЩЗЙКЫЕГМЦЧНЯ";
                int idx = (int)e.Key - (int)Key.A;
                if (idx >= 0 && idx < map.Length)
                {
                    char letter = map[idx];
                    TryGuess(letter);
                }
            }
            e.Handled = true;
        }

        private void TryGuess(char letter)
        {
            if (_guessed.Contains(letter)) return;
            _guessed.Add(letter);

            if (!_word.Contains(letter))
            {
                _mistakes++;
                LivesText.Text = (MaxMistakes - _mistakes).ToString();
                ShowNextHangmanPart();
            }

            UpdateKeyboardState();
            UpdateWordDisplay();
            CheckGameEnd();
        }

        private void UpdateKeyboardState()
        {
            // Пробегаем по визуальным кнопкам и раскрашиваем
            foreach (var item in FindVisualChildren<Button>(KeyboardPanel))
            {
                if (item.Tag is string tag && tag.Length > 0)
                {
                    char c = tag[0];
                    if (_guessed.Contains(c))
                    {
                        item.IsEnabled = false;
                        if (_word.Contains(c))
                        {
                            item.Background = (Brush)Application.Current.FindResource("CoralBrush");
                            item.Foreground = Brushes.White;
                        }
                        else
                        {
                            item.Background = (Brush)Application.Current.FindResource("OverlayWeakBrush");
                            item.Foreground = (Brush)Application.Current.FindResource("MutedTextBrush");
                        }
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var c in FindVisualChildren<T>(child)) yield return c;
            }
        }

        // ================================================================
        //  ОТОБРАЖЕНИЕ СЛОВА
        // ================================================================
        private void UpdateWordDisplay()
        {
            var items = _word.Select(c => new WordLetter
            {
                Letter = _guessed.Contains(c) ? c.ToString() : ""
            }).ToList();

            WordPanel.ItemsSource = items;
        }

        // ================================================================
        //  ВИСЕЛИЦА
        // ================================================================
        private void DrawGallows()
        {
            GallowsCanvas.Children.Clear();
            _hangmanParts.Clear();

            var strokeBrush = (Brush)Application.Current.FindResource("CoralBrush");
            var groundBrush = (Brush)Application.Current.FindResource("MutedTextBrush");
            double stroke = 6;

            // Земля
            var ground = new Line
            {
                X1 = 20,
                Y1 = 340,
                X2 = 280,
                Y2 = 340,
                Stroke = groundBrush,
                StrokeThickness = 4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            GallowsCanvas.Children.Add(ground);

            // Вертикаль
            var vertical = new Line
            {
                X1 = 80,
                Y1 = 340,
                X2 = 80,
                Y2 = 40,
                Stroke = groundBrush,
                StrokeThickness = 4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            GallowsCanvas.Children.Add(vertical);

            // Горизонталь
            var horizontal = new Line
            {
                X1 = 80,
                Y1 = 40,
                X2 = 200,
                Y2 = 40,
                Stroke = groundBrush,
                StrokeThickness = 4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            GallowsCanvas.Children.Add(horizontal);

            // Верёвка
            var rope = new Line
            {
                X1 = 200,
                Y1 = 40,
                X2 = 200,
                Y2 = 80,
                Stroke = groundBrush,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            GallowsCanvas.Children.Add(rope);

            // 6 частей тела (скрыты по умолчанию)
            // 1. Голова
            var head = new Ellipse
            {
                Width = 50,
                Height = 50,
                Stroke = strokeBrush,
                StrokeThickness = stroke,
                Visibility = Visibility.Hidden
            };
            Canvas.SetLeft(head, 175);
            Canvas.SetTop(head, 80);
            GallowsCanvas.Children.Add(head);
            _hangmanParts.Add(head);

            // 2. Туловище
            var body = new Line
            {
                X1 = 200,
                Y1 = 130,
                X2 = 200,
                Y2 = 220,
                Stroke = strokeBrush,
                StrokeThickness = stroke,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Visibility = Visibility.Hidden
            };
            GallowsCanvas.Children.Add(body);
            _hangmanParts.Add(body);

            // 3. Левая рука
            var leftArm = new Line
            {
                X1 = 200,
                Y1 = 150,
                X2 = 160,
                Y2 = 190,
                Stroke = strokeBrush,
                StrokeThickness = stroke,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Visibility = Visibility.Hidden
            };
            GallowsCanvas.Children.Add(leftArm);
            _hangmanParts.Add(leftArm);

            // 4. Правая рука
            var rightArm = new Line
            {
                X1 = 200,
                Y1 = 150,
                X2 = 240,
                Y2 = 190,
                Stroke = strokeBrush,
                StrokeThickness = stroke,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Visibility = Visibility.Hidden
            };
            GallowsCanvas.Children.Add(rightArm);
            _hangmanParts.Add(rightArm);

            // 5. Левая нога
            var leftLeg = new Line
            {
                X1 = 200,
                Y1 = 220,
                X2 = 170,
                Y2 = 280,
                Stroke = strokeBrush,
                StrokeThickness = stroke,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Visibility = Visibility.Hidden
            };
            GallowsCanvas.Children.Add(leftLeg);
            _hangmanParts.Add(leftLeg);

            // 6. Правая нога
            var rightLeg = new Line
            {
                X1 = 200,
                Y1 = 220,
                X2 = 230,
                Y2 = 280,
                Stroke = strokeBrush,
                StrokeThickness = stroke,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Visibility = Visibility.Hidden
            };
            GallowsCanvas.Children.Add(rightLeg);
            _hangmanParts.Add(rightLeg);
        }

        private void ShowNextHangmanPart()
        {
            if (_mistakes - 1 < _hangmanParts.Count)
            {
                var part = _hangmanParts[_mistakes - 1];
                part.Visibility = Visibility.Visible;
                part.Opacity = 0;

                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                part.BeginAnimation(OpacityProperty, fade);
            }
        }

        // ================================================================
        //  КОНЕЦ ИГРЫ
        // ================================================================
        private void CheckGameEnd()
        {
            bool won = _word.All(c => _guessed.Contains(c));

            if (won)
            {
                _isOver = true;
                _wins++;
                WinsText.Text = _wins.ToString();
                Services.ScoreService.AddScore("hangman", _word.Length, 0, _wins);

                MessageBox.Show($"🎉 Вы угадали слово «{_word}»!",
                                "Победа!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_mistakes >= MaxMistakes)
            {
                _isOver = true;
                Services.ScoreService.AddScore("hangman", 0, 0, _wins);

                // Показать слово
                var items = _word.Select(c => new WordLetter { Letter = c.ToString() }).ToList();
                WordPanel.ItemsSource = items;

                MessageBox.Show($"😔 Вы проиграли.\n\nЗагаданное слово: {_word}",
                                "Игра окончена", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class LetterItem
    {
        public string Letter { get; set; }
    }

    public class WordLetter
    {
        public string Letter { get; set; }
    }
}