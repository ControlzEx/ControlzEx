namespace ControlzEx.Showcase
{
    using System.Windows;
    using ControlzEx.Theming;

    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncAll;

            ThemeManager.Current.SyncTheme();
        }
    }
}