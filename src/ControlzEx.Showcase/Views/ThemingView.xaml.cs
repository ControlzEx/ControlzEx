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

            this.Themes = new CompositeCollection { new CollectionContainer { Collection = ThemeManager.Themes }, new CollectionContainer { Collection = this.CurrentThemeCollection } };

            ThemeManager.ThemeChanged += this.ThemeManager_ThemeChanged;

            this.InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            var currentTheme = this.CurrentTheme;

            if (currentTheme is null == false
                && this.CurrentThemeCollection.Contains(currentTheme) == false)
            {
                this.CurrentThemeCollection.Clear();
                this.CurrentThemeCollection.Add(currentTheme);
            }
        }

        private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            if (e.NewTheme is null == false)
            {
                this.CurrentThemeCollection.Add(e.NewTheme);
            }

            if (e.OldTheme is null == false)
            {
                this.CurrentThemeCollection.Remove(e.OldTheme);
            }

            this.OnPropertyChanged(nameof(this.CurrentTheme));
        }

        public ObservableCollection<Theme> CurrentThemeCollection { get; }

        public CompositeCollection Themes { get; }

        public Theme CurrentTheme
        {
            get => ThemeManager.DetectTheme();

            set
            {
                if (value is null)
                {
                    return;
                }

                ThemeManager.ChangeTheme(Application.Current, value);

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
                    ThemeManager.ChangeThemeBaseColor(Application.Current, value);
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
                    ThemeManager.ChangeThemeColorScheme(Application.Current, value);
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
    }
}