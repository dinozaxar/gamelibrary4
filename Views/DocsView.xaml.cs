using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using gamelibrary4.Docs;

namespace gamelibrary4.Views
{
    public partial class DocsView : UserControl
    {
        private List<DocSection> _sections;

        public DocsView()
        {
            InitializeComponent();
            _sections = DocProvider.GetSections();
            BuildNavigation();

            if (_sections.Count > 0)
                RenderSection(_sections[0]);
        }

        private void BuildNavigation()
        {
            var items = new List<NavItem>();
            foreach (var s in _sections)
                items.Add(new NavItem { Key = s.Key, Display = $"{s.Emoji}  {s.Title}" });

            NavList.ItemsSource = items;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            string key = (string)btn.Tag;
            var section = _sections.Find(s => s.Key == key);
            if (section != null)
                RenderSection(section);
        }

        private void RenderSection(DocSection section)
        {
            ContentPanel.Children.Clear();

            foreach (var raw in section.Content)
            {
                var line = raw ?? "";

                if (line.StartsWith("# "))
                {
                    ContentPanel.Children.Add(new TextBlock
                    {
                        Text = line.Substring(2),
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 16)
                    });
                }
                else if (line.StartsWith("## "))
                {
                    ContentPanel.Children.Add(new TextBlock
                    {
                        Text = line.Substring(3),
                        FontSize = 17,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)Application.Current.FindResource("CoralBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 18, 0, 8)
                    });
                }
                else if (line.StartsWith("- "))
                {
                    ContentPanel.Children.Add(new TextBlock
                    {
                        Text = "•  " + line.Substring(2),
                        FontSize = 14,
                        Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(14, 2, 0, 2)
                    });
                }
                else if (line.StartsWith("! "))
                {
                    var border = new Border
                    {
                        Background = (Brush)Application.Current.FindResource("OverlayWeakBrush"),
                        BorderThickness = new Thickness(3, 0, 0, 0),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 10, 0, 10)
                    };
                    border.Child = new TextBlock
                    {
                        Text = line.Substring(2),
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                        TextWrapping = TextWrapping.Wrap
                    };
                    ContentPanel.Children.Add(border);
                }
                else if (line.StartsWith("> "))
                {
                    var border = new Border
                    {
                        Background = (Brush)Application.Current.FindResource("SurfaceDeepBrush"),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    border.Child = new TextBlock
                    {
                        Text = line.Substring(2),
                        FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New"),
                        FontSize = 13,
                        Foreground = (Brush)Application.Current.FindResource("CoralBrush"),
                        TextWrapping = TextWrapping.Wrap
                    };
                    ContentPanel.Children.Add(border);
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // Пустая строка — пропускаем (отступы задаются через Margin)
                }
                else
                {
                    ContentPanel.Children.Add(new TextBlock
                    {
                        Text = line,
                        FontSize = 14,
                        LineHeight = 22,
                        Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 8)
                    });
                }
            }
        }
    }

    public class NavItem
    {
        public string Key { get; set; }
        public string Display { get; set; }
    }
}