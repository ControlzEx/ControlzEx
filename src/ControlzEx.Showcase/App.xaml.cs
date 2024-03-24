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

            ThemeManager.Current.ThemeChanged += CurrentOnThemeChanged;

#pragma warning disable CS0618 // Type or member is obsolete
            AppModeHelper.SyncAppMode();

            void CurrentOnThemeChanged(object sender, ThemeChangedEventArgs themeChangedEventArgs)
            {
                AppModeHelper.SyncAppMode();
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}