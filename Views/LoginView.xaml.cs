using System;
using System.Windows;
using System.Windows.Controls;
using gamelibrary4.Services;

namespace gamelibrary4.Views
{
    public partial class LoginView : UserControl
    {
        public event Action LoginSucceeded;

        private bool _isRegisterMode = false;

        public LoginView()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            string login = LoginBox.Text;
            string password = PasswordBox.Password;

            string error;
            if (_isRegisterMode)
                error = UserService.Register(login, password);
            else
                error = UserService.Login(login, password);

            if (error != null)
            {
                ErrorText.Text = error;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            LoginSucceeded?.Invoke();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            _isRegisterMode = !_isRegisterMode;

            if (_isRegisterMode)
            {
                SubtitleText.Text = "Создайте новый аккаунт";
                LoginButton.Content = "Зарегистрироваться";
                RegisterButton.Content = "У меня уже есть аккаунт";
            }
            else
            {
                SubtitleText.Text = "Войдите, чтобы продолжить";
                LoginButton.Content = "Войти";
                RegisterButton.Content = "Создать аккаунт";
            }

            ErrorText.Visibility = Visibility.Collapsed;
        }
    }
}