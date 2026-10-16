using System.Windows;
using gamelibrary4.Contracts;
using gamelibrary4.Services;
using gamelibrary4.Views;

namespace gamelibrary4
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Services.ThemeService.InitializeFromSettings();

            Services.ThemeService.ThemeChanged += OnThemeChanged;
            // Восстановить последний трек (но не играть)
            Services.MusicService.RestoreLastTrack();
            ShowLogin();
        }

        // ============================================================
        //  ЭКРАНЫ
        // ============================================================
        private void ShowLogin()
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            UserPanel.Visibility = Visibility.Collapsed;

            var login = new LoginView();
            login.LoginSucceeded += () =>
            {
                MainMenuPanel.Visibility = Visibility.Visible;
                UserPanel.Visibility = Visibility.Visible;
                CurrentUserText.Text = UserService.CurrentUser.Login;
                RefreshAvatar();
                UpdateThemeIcon();
                ShowHome();
            };
            ContentHost.Content = login;
        }
		private void CloseCurrentGame()
		{
			if (ContentHost.Content is Games.TicTacToe.TicTacToeView ttt)
				ttt.Shutdown();
		}

        private void OnThemeChanged()
        {
            // Если пользователь уже залогинен — пересоздаём текущий экран,
            // чтобы все хардкод-элементы перерисовались.
            if (Services.UserService.CurrentUser != null)
            {
                // Не сбрасываем положение — просто идём на главную
                ShowHome();
            }
        }
        private void ShowHome()
        {
			CloseCurrentGame();
			var home = new HomeView();
            home.GameRequested += OnGameRequested;
            ContentHost.Content = home;
        }

        private void ShowFavorites()
        {
			CloseCurrentGame();
			var fav = new FavoritesView();
            fav.GameRequested += OnGameRequested;
            ContentHost.Content = fav;
        }

        private void ShowRecords()
        {
			CloseCurrentGame();
			ContentHost.Content = new RecordsView();
        }

		// ============================================================
		//  ЗАПУСК ИГРЫ ПО КЛЮЧУ
		// ============================================================
		private void OnGameRequested(string key)
		{
			// Если предыдущий экран — TicTacToeView, вежливо закрываем сеть
			if (ContentHost.Content is Games.TicTacToe.TicTacToeView oldTtt)
				oldTtt.Shutdown();

			var module = GameRegistry.Find(key);
			if (module == null)
			{
				MessageBox.Show($"Игра «{key}» не найдена.", "Ошибка",
								MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			ContentHost.Content = module.CreateView();
		}

		// ============================================================
		//  КНОПКИ МЕНЮ
		// ============================================================
		private void HomeButton_Click(object sender, RoutedEventArgs e) => ShowHome();
        private void FavoritesButton_Click(object sender, RoutedEventArgs e) => ShowFavorites();
        private void RecordsButton_Click(object sender, RoutedEventArgs e) => ShowRecords();
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            Services.ThemeService.Toggle();
            UpdateThemeIcon();
        }

        private void UpdateThemeIcon()
        {
            ThemeButton.Content = Services.ThemeService.Current == Services.AppTheme.Dark
                ? "🌙"
                : "☀";
        }
        private void ShowDocs()
        {
            CloseCurrentGame();
            ContentHost.Content = new DocsView();
        }

        private void DocsButton_Click(object sender, RoutedEventArgs e) => ShowDocs();
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UserService.Logout();
            ShowLogin();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Сохраняем позицию трека и громкость
            Services.MusicService.SaveState();
        }
        private void ShowProfile()
        {
			CloseCurrentGame();
			var profile = new ProfileView();
            profile.LoginChanged += () =>
            {
                // Обновить ник в верхнем меню
                CurrentUserText.Text = UserService.CurrentUser.Login;
                RefreshAvatar();
            };
            ContentHost.Content = profile;
        }
        private void ProfileButton_Click(object sender, RoutedEventArgs e) => ShowProfile();

        private void RefreshAvatar()
        {
            var user = UserService.CurrentUser;
            if (user == null) return;

            if (!string.IsNullOrEmpty(user.AvatarPath) && System.IO.File.Exists(user.AvatarPath))
            {
                try
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(user.AvatarPath);
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 64;
                    bmp.EndInit();
                    bmp.Freeze();

                    MiniAvatarEllipse.Fill = new System.Windows.Media.ImageBrush(bmp)
                    {
                        Stretch = System.Windows.Media.Stretch.UniformToFill
                    };
                    MiniAvatarPlaceholder.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    MiniAvatarPlaceholder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                MiniAvatarEllipse.Fill = (System.Windows.Media.Brush)FindResource("PurpleBrush");
                MiniAvatarPlaceholder.Visibility = Visibility.Visible;
            }
        }


    }


}