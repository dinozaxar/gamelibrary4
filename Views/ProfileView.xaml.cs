using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using gamelibrary4.Contracts;
using gamelibrary4.Services;
using Microsoft.Win32;

namespace gamelibrary4.Views
{
    public partial class ProfileView : UserControl
    {
        /// <summary>Событие: логин пользователя изменился (для обновления верхнего меню).</summary>
        public event Action LoginChanged;

        public ProfileView()
        {
            InitializeComponent();
            LoadProfile();
        }

        private void LoadProfile()
        {
            var user = UserService.CurrentUser;
            if (user == null) return;

            LoginText.Text = user.Login;
            CreatedText.Text = $"Аккаунт создан {user.CreatedAt:dd.MM.yyyy}";
            NewLoginBox.Text = user.Login;

            // Аватар
            if (!string.IsNullOrEmpty(user.AvatarPath) && System.IO.File.Exists(user.AvatarPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(user.AvatarPath);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();

                    AvatarEllipse.Fill = new ImageBrush(bmp)
                    {
                        Stretch = Stretch.UniformToFill
                    };
                    AvatarPlaceholder.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    AvatarPlaceholder.Visibility = Visibility.Visible;
                }
            }
            else
            {
                AvatarPlaceholder.Visibility = Visibility.Visible;
            }

            // Статистика
            RecordsCountText.Text = ScoreService.CountForUser(user.Login).ToString();
            FavoritesCountText.Text = user.FavoriteGames.Count.ToString();

            string favKey = ScoreService.FavoriteGameForUser(user.Login);
            var module = GameRegistry.Find(favKey);
            FavoriteGameText.Text = module != null ? module.Title : "—";
        }

        // ================================================================
        //  АВАТАР
        // ================================================================
        private void UploadAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите картинку",
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            string error = UserService.SetAvatar(dlg.FileName);
            if (error != null)
            {
                MessageBox.Show(error, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadProfile();
        }

        // ================================================================
        //  ЛОГИН
        // ================================================================
        private void SaveLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginErrorText.Visibility = Visibility.Collapsed;

            string error = UserService.ChangeLogin(NewLoginBox.Text);
            if (error != null)
            {
                LoginErrorText.Text = error;
                LoginErrorText.Visibility = Visibility.Visible;
                return;
            }

            LoadProfile();
            LoginChanged?.Invoke();

            MessageBox.Show("Логин успешно изменён.", "Готово",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ================================================================
        //  ПАРОЛЬ
        // ================================================================
        private void SavePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            PasswordErrorText.Visibility = Visibility.Collapsed;

            string error = UserService.ChangePassword(
                OldPasswordBox.Password,
                NewPasswordBox.Password,
                ConfirmPasswordBox.Password);

            if (error != null)
            {
                PasswordErrorText.Text = error;
                PasswordErrorText.Visibility = Visibility.Visible;
                return;
            }

            OldPasswordBox.Password = "";
            NewPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";

            MessageBox.Show("Пароль успешно изменён.", "Готово",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}