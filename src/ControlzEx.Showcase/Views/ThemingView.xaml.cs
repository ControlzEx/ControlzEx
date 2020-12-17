namespace ControlzEx.Showcase.Views
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Data;
    using ControlzEx.Theming;
    using JetBrains.Annotations;

    public partial class ThemingView : INotifyPropertyChanged
    {
        public ThemingView()
        {
            this.CurrentThemeCollection = new ObservableCollection<Theme>();
            this.CurrentColorSchemeCollection = new ObservableCollection<string>();

            this.Themes = new CompositeCollection { new CollectionContainer { Collection = ThemeManager.Current.Themes }, new CollectionContainer { Collection = this.CurrentThemeCollection } };
            this.ColorSchemes = new CompositeCollection { new CollectionContainer { Collection = ThemeManager.Current.ColorSchemes }, new CollectionContainer { Collection = this.CurrentColorSchemeCollection } };

            ThemeManager.Current.ThemeChanged += this.ThemeManager_ThemeChanged;

            this.InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            var currentTheme = this.CurrentTheme;

            if (currentTheme is not null
                && this.CurrentThemeCollection.Contains(currentTheme) == false)
            {
                this.CurrentThemeCollection.Clear();
                this.CurrentThemeCollection.Add(currentTheme);

                this.CurrentColorSchemeCollection.Clear();
                this.CurrentColorSchemeCollection.Add(currentTheme.ColorScheme);
            }
        }

        private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            // Add the new theme first, if it's generated
            if (e.NewTheme is not null
                && e.NewTheme.IsRuntimeGenerated)
            {
                this.CurrentThemeCollection.Add(e.NewTheme);

                if (e.OldTheme?.ColorScheme != e.NewTheme?.ColorScheme)
                {
                    this.CurrentColorSchemeCollection.Add(e.NewTheme.ColorScheme);
                }
            }

            // Notify about selection change
            this.OnPropertyChanged(nameof(this.CurrentTheme));
            this.OnPropertyChanged(nameof(this.CurrentColorScheme));

            // Remove the old theme, if it's generated
            // We have to do this after the notification to ensure the selection does not get reset by the removal
            if (e.OldTheme is not null
                && e.OldTheme.IsRuntimeGenerated)
            {
                this.CurrentThemeCollection.Remove(e.OldTheme);

                if (e.OldTheme?.ColorScheme != e.NewTheme?.ColorScheme)
                {
                    this.CurrentColorSchemeCollection.Remove(e.OldTheme.ColorScheme);
                }
            }
        }

        public ObservableCollection<Theme> CurrentThemeCollection { get; }

        public CompositeCollection Themes { get; }

        public ObservableCollection<string> CurrentColorSchemeCollection { get; }

        public CompositeCollection ColorSchemes { get; }

        public Theme CurrentTheme
        {
            get => ThemeManager.Current.DetectTheme();

            set
            {
                if (value is null)
                {
                    return;
                }

                ThemeManager.Current.ChangeTheme(Application.Current, value);

                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.CurrentBaseColor));
            }
        }

        public string CurrentBaseColor
        {
            get => this.CurrentTheme.BaseColorScheme;

            set
            {
                if (string.IsNullOrEmpty(value) == false)
                {
                    ThemeManager.Current.ChangeThemeBaseColor(Application.Current, value);
                }

                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.CurrentTheme));
            }
        }

        public string CurrentColorScheme
        {
            get => this.CurrentTheme.ColorScheme;

            set
            {
                if (string.IsNullOrEmpty(value) == false)
                {
                    ThemeManager.Current.ChangeThemeColorScheme(Application.Current, value);
                }

                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.CurrentTheme));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SyncNow_OnClick(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.SyncTheme();
        }
    }
}