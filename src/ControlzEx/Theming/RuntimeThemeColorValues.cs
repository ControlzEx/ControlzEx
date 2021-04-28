#nullable enable
namespace ControlzEx.Theming
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;
    using JetBrains.Annotations;

    [PublicAPI]
    public class RuntimeThemeColorValues : INotifyPropertyChanged
    {
        private Color accentColor;
        private Color accentBaseColor;
        private Color primaryAccentColor;
        private Color accentColor80;
        private Color accentColor60;
        private Color accentColor40;
        private Color accentColor20;
        private Color highlightColor;
        private Color idealForegroundColor;

        public RuntimeThemeColorValues(RuntimeThemeOptions options)
        {
            this.Options = options;
        }

        public RuntimeThemeOptions Options { get; }

        public Color AccentColor
        {
            get => this.accentColor;
            set
            {
                if (value.Equals(this.accentColor))
                    return;
                this.accentColor = value;
                this.OnPropertyChanged();
            }
        }

        public Color AccentBaseColor
        {
            get => this.accentBaseColor;
            set
            {
                if (value.Equals(this.accentBaseColor))
                    return;
                this.accentBaseColor = value;
                this.OnPropertyChanged();
            }
        }

        public Color PrimaryAccentColor
        {
            get => this.primaryAccentColor;
            set
            {
                if (value.Equals(this.primaryAccentColor))
                    return;
                this.primaryAccentColor = value;
                this.OnPropertyChanged();
            }
        }

        public Color AccentColor80
        {
            get => this.accentColor80;
            set
            {
                if (value.Equals(this.accentColor80))
                    return;
                this.accentColor80 = value;
                this.OnPropertyChanged();
            }
        }

        public Color AccentColor60
        {
            get => this.accentColor60;
            set
            {
                if (value.Equals(this.accentColor60))
                    return;
                this.accentColor60 = value;
                this.OnPropertyChanged();
            }
        }

        public Color AccentColor40
        {
            get => this.accentColor40;
            set
            {
                if (value.Equals(this.accentColor40))
                    return;
                this.accentColor40 = value;
                this.OnPropertyChanged();
            }
        }

        public Color AccentColor20
        {
            get => this.accentColor20;
            set
            {
                if (value.Equals(this.accentColor20))
                    return;
                this.accentColor20 = value;
                this.OnPropertyChanged();
            }
        }

        public Color HighlightColor
        {
            get => this.highlightColor;
            set
            {
                if (value.Equals(this.highlightColor))
                    return;
                this.highlightColor = value;
                this.OnPropertyChanged();
            }
        }

        public Color IdealForegroundColor
        {
            get => this.idealForegroundColor;
            set
            {
                if (value.Equals(this.idealForegroundColor))
                    return;
                this.idealForegroundColor = value;
                this.OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}