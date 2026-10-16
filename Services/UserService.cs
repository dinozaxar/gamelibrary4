using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using gamelibrary4.Models;

namespace gamelibrary4.Services
{
    public static class UserService
    {
        private static string DataDir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "gamelibrary4");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private static string UsersFile => Path.Combine(DataDir, "users.json");

        public static User CurrentUser { get; private set; }

        public static List<User> LoadUsers()
        {
            if (!File.Exists(UsersFile)) return new List<User>();
            try
            {
                var json = File.ReadAllText(UsersFile);
                return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
            catch
            {
                return new List<User>();
            }
        }

        private static void SaveUsers(List<User> users)
        {
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(UsersFile, json);
        }

        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Регистрирует нового пользователя. Возвращает null при успехе, иначе текст ошибки.
        /// </summary>
        public static string Register(string login, string password)
        {
            login = login.Trim();

            if (string.IsNullOrWhiteSpace(login)) return "Введите логин.";
            if (login.Length < 3) return "Логин минимум 3 символа.";
            if (string.IsNullOrWhiteSpace(password)) return "Введите пароль.";
            if (password.Length < 4) return "Пароль минимум 4 символа.";

            var users = LoadUsers();
            if (users.Any(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase)))
                return "Пользователь с таким логином уже существует.";

            var user = new User
            {
                Login = login,
                PasswordHash = HashPassword(password)
            };
            users.Add(user);
            SaveUsers(users);

            CurrentUser = user;
            return null; // успех
        }

        /// <summary>
        /// Вход. Возвращает null при успехе, иначе текст ошибки.
        /// </summary>
        public static string Login(string login, string password)
        {
            login = login.Trim();
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                return "Введите логин и пароль.";

            var users = LoadUsers();
            var user = users.FirstOrDefault(u =>
                string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase));

            if (user == null) return "Пользователь не найден.";
            if (user.PasswordHash != HashPassword(password)) return "Неверный пароль.";

            CurrentUser = user;
            return null;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        // ==================== ИЗБРАННОЕ ====================

        public static bool IsFavorite(string gameKey)
        {
            if (CurrentUser == null) return false;
            return CurrentUser.FavoriteGames.Contains(gameKey);
        }

        public static void AddFavorite(string gameKey)
        {
            if (CurrentUser == null) return;
            if (CurrentUser.FavoriteGames.Contains(gameKey)) return;

            CurrentUser.FavoriteGames.Add(gameKey);
            SaveCurrentUser();
        }

        public static void RemoveFavorite(string gameKey)
        {
            if (CurrentUser == null) return;
            if (!CurrentUser.FavoriteGames.Contains(gameKey)) return;

            CurrentUser.FavoriteGames.Remove(gameKey);
            SaveCurrentUser();
        }

        public static void ToggleFavorite(string gameKey)
        {
            if (IsFavorite(gameKey)) RemoveFavorite(gameKey);
            else AddFavorite(gameKey);
        }

        /// <summary>
        /// Сохраняет изменения текущего пользователя в файл.
        /// </summary>
        private static void SaveCurrentUser()
        {
            if (CurrentUser == null) return;

            var users = LoadUsers();
            var existing = users.FindIndex(u => u.Id == CurrentUser.Id);
            if (existing >= 0)
            {
                users[existing] = CurrentUser;
                SaveUsers(users);
            }
        }
        // ==================== АВАТАР ====================

        public static string AvatarsDir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "gamelibrary4", "avatars");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>
        /// Копирует выбранную картинку в папку аватаров, ресайзит до 256x256 и сохраняет путь.
        /// </summary>
        public static string SetAvatar(string sourcePath)
        {
            if (CurrentUser == null) return "Нет активного пользователя.";
            if (!File.Exists(sourcePath)) return "Файл не найден.";

            try
            {
                // Загружаем и ресайзим
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(sourcePath);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 256;  // ресайз по ширине
                bitmap.EndInit();
                bitmap.Freeze();

                // Определяем формат: png для png, jpg для остальных
                string ext = Path.GetExtension(sourcePath).ToLower();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".bmp")
                    ext = ".png";

                // Удаляем старый аватар (если был)
                if (!string.IsNullOrEmpty(CurrentUser.AvatarPath) && File.Exists(CurrentUser.AvatarPath))
                {
                    try { File.Delete(CurrentUser.AvatarPath); } catch { }
                }

                // Сохраняем под именем {UserId}{ext}
                string targetPath = Path.Combine(AvatarsDir, CurrentUser.Id + ext);

                System.Windows.Media.Imaging.BitmapEncoder encoder =
                    ext == ".png"
                        ? new System.Windows.Media.Imaging.PngBitmapEncoder()
                        : (System.Windows.Media.Imaging.BitmapEncoder)new System.Windows.Media.Imaging.JpegBitmapEncoder();

                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var fs = new FileStream(targetPath, FileMode.Create))
                    encoder.Save(fs);

                CurrentUser.AvatarPath = targetPath;
                SaveCurrentUser();
                return null; // успех
            }
            catch (Exception ex)
            {
                return "Ошибка: " + ex.Message;
            }
        }

        // ==================== СМЕНА ЛОГИНА / ПАРОЛЯ ====================

        public static string ChangeLogin(string newLogin)
        {
            if (CurrentUser == null) return "Нет активного пользователя.";
            newLogin = newLogin.Trim();

            if (string.IsNullOrWhiteSpace(newLogin)) return "Введите логин.";
            if (newLogin.Length < 3) return "Логин минимум 3 символа.";

            var users = LoadUsers();
            if (users.Any(u => u.Id != CurrentUser.Id &&
                               string.Equals(u.Login, newLogin, StringComparison.OrdinalIgnoreCase)))
                return "Логин уже занят.";

            CurrentUser.Login = newLogin;
            SaveCurrentUser();
            return null;
        }

        public static string ChangePassword(string oldPassword, string newPassword, string confirm)
        {
            if (CurrentUser == null) return "Нет активного пользователя.";

            if (HashPassword(oldPassword) != CurrentUser.PasswordHash)
                return "Старый пароль неверный.";
            if (string.IsNullOrWhiteSpace(newPassword)) return "Введите новый пароль.";
            if (newPassword.Length < 4) return "Пароль минимум 4 символа.";
            if (newPassword != confirm) return "Пароли не совпадают.";

            CurrentUser.PasswordHash = HashPassword(newPassword);
            SaveCurrentUser();
            return null;
        }
    }
}