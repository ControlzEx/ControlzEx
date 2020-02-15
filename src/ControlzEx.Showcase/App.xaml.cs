namespace ControlzEx.Showcase
{
    using System.Windows;
    using ControlzEx.Theming;

    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.ThemeSyncMode = ThemeSyncMode.SyncAll;

            ThemeManager.SyncThemeColorSchemeWithWindowsAccentColor();
        }
    }
}