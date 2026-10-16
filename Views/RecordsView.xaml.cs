using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using gamelibrary4.Models;
using gamelibrary4.Services;

namespace gamelibrary4.Views
{
    public partial class RecordsView : UserControl
    {
        // Игры, для которых показываем вкладки
        private readonly (string Key, string Name, string Emoji)[] _games =
        {
            ("minesweeper", "Сапёр",   "💣"),
            ("snake",       "Змейка",  "🐍"),
            ("sudoku",      "Судоку",  "🧩"),
            ("2048",        "2048",    "🔢"),
            ("tictactoe",   "Крестики","❌"),
            ("hangman",     "Виселица","🎯"),
            ("sudoku",      "Судоку",  "🧩"),
        };

        private string _selectedGameKey;

        public RecordsView()
        {
            InitializeComponent();
            BuildGameTabs();
            _selectedGameKey = _games[0].Key;
            SortCombo.SelectedIndex = 0;
            Refresh();
        }

        private void BuildGameTabs()
        {
            GameTabsPanel.Children.Clear();

            foreach (var (key, name, emoji) in _games)
            {
                var btn = new Button
                {
                    Content = $"{emoji}  {name}",
                    Tag = key,
                    Margin = new Thickness(4, 0, 4, 0),
                    Padding = new Thickness(16, 10, 16, 10),
                    FontSize = 13,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Foreground = (Brush)FindResource("MutedTextBrush"),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0)
                };

                btn.Template = BuildTabTemplate();
                btn.Click += GameTab_Click;

                GameTabsPanel.Children.Add(btn);
            }

            UpdateTabsVisual();
        }

        private ControlTemplate BuildTabTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            template.VisualTree = border;

            var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty,
                (Brush)Application.Current.FindResource("OverlayWeakBrush"), "bd"));
            template.Triggers.Add(hover);

            return template;
        }

        private void GameTab_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            _selectedGameKey = (string)btn.Tag;
            UpdateTabsVisual();
            Refresh();
        }

        private void UpdateTabsVisual()
        {
            foreach (Button btn in GameTabsPanel.Children)
            {
                bool active = (string)btn.Tag == _selectedGameKey;
                if (active)
                {
                    btn.Background = (Brush)FindResource("AccentGradient");
                    btn.Foreground = Brushes.White;
                    btn.FontWeight = FontWeights.SemiBold;
                }
                else
                {
                    btn.Background = Brushes.Transparent;
                    btn.Foreground = (Brush)FindResource("MutedTextBrush");
                    btn.FontWeight = FontWeights.Normal;
                }
            }
        }

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void OnlyMineCheck_Changed(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_selectedGameKey == null) return;

            var sort = ScoreSort.ScoreDesc;
            if (SortCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (Enum.TryParse<ScoreSort>(tag, out var parsed))
                    sort = parsed;
            }

            bool onlyMine = OnlyMineCheck.IsChecked == true;

            var entries = ScoreService.GetForGame(_selectedGameKey, sort, onlyMine);

            var display = entries.Select((s, i) => new ScoreRow
            {
                Rank = (i + 1).ToString(),
                UserLogin = s.UserLogin,
                Score = s.Score,
                TimeDisplay = s.TimeSeconds > 0
                    ? TimeSpan.FromSeconds(s.TimeSeconds).ToString(@"mm\:ss")
                    : "—",
                DateDisplay = s.AchievedAt.ToString("dd.MM.yyyy HH:mm")
            }).ToList();

            RecordsList.ItemsSource = display;
            EmptyText.Visibility = display.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class ScoreRow
    {
        public string Rank { get; set; }
        public string UserLogin { get; set; }
        public int Score { get; set; }
        public string TimeDisplay { get; set; }
        public string DateDisplay { get; set; }
    }
}