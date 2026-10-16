using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using gamelibrary4.Contracts;
using gamelibrary4.Services;

namespace gamelibrary4.Views
{
    public partial class FavoritesView : UserControl
    {
        public event Action<string> GameRequested;

        public FavoritesView()
        {
            InitializeComponent();
            LoadFavorites();
        }

        private void LoadFavorites()
        {
            var favKeys = UserService.CurrentUser?.FavoriteGames ?? new System.Collections.Generic.List<string>();

            var items = GameRegistry.Modules
                .Where(m => favKeys.Contains(m.Key))
                .Select(m => new FavoriteRow
                {
                    Key = m.Key,
                    Title = m.Title,
                    Emoji = m.Emoji,
                    Description = m.Description
                })
                .ToList();

            FavoritesList.ItemsSource = items;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            string key = (string)btn.Tag;
            GameRequested?.Invoke(key);
        }

        private void Card_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is FavoriteRow row)
                GameRequested?.Invoke(row.Key);
        }
    }

    public class FavoriteRow
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Emoji { get; set; }
        public string Description { get; set; }
    }
}