using System;
using System.Linq;
using System.Windows;

namespace gamelibrary4.Services
{
    public enum AppTheme
    {
        Dark,
        Light
    }

    public static class ThemeService
    {
        private const string DarkUri = "Styles/Colors.Dark.xaml";
        private const string LightUri = "Styles/Colors.Light.xaml";

        public static AppTheme Current { get; private set; } = AppTheme.Dark;

        public static event Action ThemeChanged;

        public static void Apply(AppTheme theme)
        {
            var uri = new Uri(theme == AppTheme.Dark ? DarkUri : LightUri, UriKind.Relative);

            var dict = new ResourceDictionary { Source = uri };

            var appDict = Application.Current.Resources.MergedDictionaries;

            // Ищем старый словарь цветов и заменяем его
            var old = appDict.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.OriginalString.Contains("Colors.Dark") ||
                 d.Source.OriginalString.Contains("Colors.Light")));

            if (old != null)
            {
                int index = appDict.IndexOf(old);
                appDict[index] = dict;
            }
            else
            {
                // На случай, если ещё не добавлен — ставим в начало
                appDict.Insert(0, dict);
            }

            Current = theme;

            // Сохраняем в настройки
            SettingsService.Current.Theme = theme.ToString();
            SettingsService.Save();

            ThemeChanged?.Invoke();
        }

        public static void Toggle()
        {
            Apply(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
        }

        public static void InitializeFromSettings()
        {
            if (Enum.TryParse<AppTheme>(SettingsService.Current.Theme, out var saved))
                Apply(saved);
            else
                Apply(AppTheme.Dark);
        }
    }
}