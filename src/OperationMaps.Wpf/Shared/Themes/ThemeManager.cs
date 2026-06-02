using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace OperationMaps.Wpf.Shared.Themes
{
    public enum AppTheme { Light, Dark }
    class ThemeManager
    {
        private static readonly ThemeManager _instance = new();
        public static ThemeManager Instance => _instance;

        private AppTheme _current = AppTheme.Light;
        public AppTheme Current => _current;

        public event Action<AppTheme>? ThemeChanged;

        private const string LightThemeUri = "pack://application:,,,/Shared/Themes/LightTheme.xaml";
        private const string DarkThemeUri = "pack://application:,,,/Shared/Themes/DarkTheme.xaml";

        private ThemeManager() { }

        private ResourceDictionary? _themeDict;
        public void Initialize(ResourceDictionary appResources)
        {
            _themeDict = FindBySource(appResources, "LightTheme")
          ?? FindBySource(appResources, "DarkTheme");

        }


        public void Apply(AppTheme theme)
        {
            _themeDict ??= FindBySource(System.Windows.Application.Current.Resources, "LightTheme")
             ?? FindBySource(System.Windows.Application.Current.Resources, "DarkTheme");

            if (_themeDict is null) return;

            _themeDict.Source = theme == AppTheme.Dark
                ? new Uri(DarkThemeUri)
                : new Uri(LightThemeUri);

            _current = theme;
            ThemeChanged?.Invoke(theme);

        }

        public void Toggle() => Apply(_current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);

        private static ResourceDictionary? FindBySource(ResourceDictionary dict, string nameContains)
        {
            if (dict.Source?.ToString().Contains(nameContains) == true)
                return dict;

            foreach (var merged in dict.MergedDictionaries)
            {
                var found = FindBySource(merged, nameContains);
                if (found is not null) return found;
            }

            return null;
        }

    }
}
