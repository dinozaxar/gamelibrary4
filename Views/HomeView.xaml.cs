using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace gamelibrary4.Views
{
    public partial class HomeView : UserControl
    {
        private readonly (string Name, string Emoji, string Key)[] _games =
        {
            ("Судоку",   "🧩", "sudoku"),
            ("Крестики", "❌", "tictactoe"),
            ("Виселица", "🎯", "hangman"),
            ("Сапёр",    "💣", "minesweeper"),
            ("Змейка",   "🐍", "snake"),
            ("2048",     "🔢", "2048"),
            ("Пасьянс",  "🃏", "solitaire"),
        };

        // Событие наружу — чтобы MainWindow знал, какую игру открыть
        public event Action<string> GameRequested;

        public HomeView()
        {
            InitializeComponent();
            Loaded += HomeView_Loaded;
        }

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            BuildRing();
            StartRotationAnimations();
        }

        private void BuildRing()
        {
            RingCanvas.Children.Clear();

            double radius = 210;
            double centerX = RingCanvas.Width / 2;
            double centerY = RingCanvas.Height / 2;

            var modules = Contracts.GameRegistry.Modules;
            int count = modules.Count;

            for (int i = 0; i < count; i++)
            {
                double angle = (-Math.PI / 2) + (2 * Math.PI * i / count);

                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);

                // Контейнер: кнопка игры + звёздочка поверх
                var container = new Grid
                {
                    Width = 110,
                    Height = 110
                };

                var btn = CreateGameButton(modules[i]);
                container.Children.Add(btn);

                // Звёздочка в правом верхнем углу
                var star = CreateStarButton(modules[i]);
                star.HorizontalAlignment = HorizontalAlignment.Right;
                star.VerticalAlignment = VerticalAlignment.Top;
                star.Margin = new Thickness(0, -4, -4, 0);
                container.Children.Add(star);

                Canvas.SetLeft(container, x - 55);
                Canvas.SetTop(container, y - 55);

                // Анимация появления
                container.Opacity = 0;
                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
                {
                    BeginTime = TimeSpan.FromMilliseconds(i * 80),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                container.BeginAnimation(OpacityProperty, fade);

                RingCanvas.Children.Add(container);
            }

            // Обновим подпись в центре
            CenterSubtitle.Text = $"{count} доступно";
        }

        private Button CreateGameButton(Contracts.IGameModule module)
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            stack.Children.Add(new TextBlock
            {
                Text = module.Emoji,
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = module.Title,
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)FindResource("TextBrush")
            });

            var btn = new Button
            {
                Style = (Style)FindResource("RingButton"),
                Content = stack,
                Tag = module.Key
            };

            btn.MouseEnter += (s, e) =>
            {
                CenterTitle.Text = module.Title;
                CenterSubtitle.Text = "Нажмите, чтобы играть";
            };

            btn.MouseLeave += (s, e) =>
            {
                CenterTitle.Text = "Игры";
                CenterSubtitle.Text = $"{Contracts.GameRegistry.Modules.Count} доступно";
            };

            btn.Click += (s, e) => GameRequested?.Invoke(module.Key);

            return btn;
        }

        private Button CreateStarButton(Contracts.IGameModule module)
        {
            bool isFav = Services.UserService.IsFavorite(module.Key);

            var star = new Button
            {
                Content = isFav ? "★" : "☆",
                FontSize = 18,
                Width = 30,
                Height = 30,
                Tag = module.Key,
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = isFav
                    ? (Brush)FindResource("CoralBrush")
                    : (Brush)FindResource("MutedTextBrush"),
                Background = new SolidColorBrush(Color.FromArgb(0xAA, 0x2D, 0x14, 0x2C)),
                BorderThickness = new Thickness(0),
                ToolTip = isFav ? "Убрать из избранного" : "Добавить в избранное"
            };

            // Круглая шаблонная кнопка
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(15));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;
            star.Template = template;

            star.Click += (s, e) =>
            {
                Services.UserService.ToggleFavorite(module.Key);
                bool nowFav = Services.UserService.IsFavorite(module.Key);
                star.Content = nowFav ? "★" : "☆";
                star.Foreground = nowFav
                    ? (Brush)FindResource("CoralBrush")
                    : (Brush)FindResource("MutedTextBrush");
                star.ToolTip = nowFav ? "Убрать из избранного" : "Добавить в избранное";

                e.Handled = true;   // не даём клику "провалиться" в кнопку игры
            };

            return star;
        }

        private void StartRotationAnimations()
        {
            var outer = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(60))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            OuterRotation.BeginAnimation(RotateTransform.AngleProperty, outer);

            var inner = new DoubleAnimation(360, 0, TimeSpan.FromSeconds(90))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            InnerRotation.BeginAnimation(RotateTransform.AngleProperty, inner);
        }
    }
}