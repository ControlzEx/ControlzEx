using System.Windows;

namespace ControlzEx.Showcase
{
    using System.Diagnostics;
    using System.Windows.Media;
    using ControlzEx.Theming;

    public partial class App : Application
    {
        public App()
        {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.ThemeSyncMode = ThemeSyncMode.SyncAll;

            ThemeManager.SyncThemeColorSchemeWithWindowsAccentColor();
        }

        }
    }
}