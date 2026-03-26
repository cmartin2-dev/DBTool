using System;
using System.Windows;

namespace DBTool.Commons
{
    public static class ThemeManager
    {
        private static readonly Uri LightThemeUri = new("/Themes/LightTheme.xaml", UriKind.Relative);
        private static readonly Uri DarkThemeUri = new("/Themes/DarkTheme.xaml", UriKind.Relative);
        private static readonly string ThemeFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.cfg");

        public static bool IsDarkMode { get; private set; }

        public static event Action<bool>? ThemeChanged;

        public static void ApplyTheme(bool dark)
        {
            IsDarkMode = dark;
            var uri = dark ? DarkThemeUri : LightThemeUri;
            var dict = new ResourceDictionary { Source = uri };

            var merged = Application.Current.Resources.MergedDictionaries;

            merged.Clear();
            merged.Add(dict);

            ThemeChanged?.Invoke(dark);
        }

        public static void Toggle()
        {
            ApplyTheme(!IsDarkMode);
            SaveTheme();
        }

        public static bool LoadSavedTheme()
        {
            try
            {
                if (System.IO.File.Exists(ThemeFile))
                {
                    string value = System.IO.File.ReadAllText(ThemeFile).Trim();
                    return value == "dark";
                }
            }
            catch { }
            return false;
        }

        private static void SaveTheme()
        {
            try
            {
                System.IO.File.WriteAllText(ThemeFile, IsDarkMode ? "dark" : "light");
            }
            catch { }
        }
    }
}
