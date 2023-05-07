#nullable enable
namespace ControlzEx.Theming
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;

    /// <summary>
    /// Global options for <see cref="RuntimeThemeGenerator"/>.
    /// </summary>
    [PublicAPI]
    public class RuntimeThemeGeneratorOptions : INotifyPropertyChanged
    {
        private bool useHSL;

        public bool UseHSL
        {
            get => this.useHSL;
            set
            {
                if (value == this.useHSL)
                {
                    return;
                }

                this.useHSL = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Used to create the options being used to generate a single <see cref="LibraryTheme"/>.
        /// </summary>
        public virtual RuntimeThemeOptions CreateRuntimeThemeOptions(bool isHighContrast, ThemeGenerator.ThemeGeneratorParameters? generatorParameters, ThemeGenerator.ThemeGeneratorBaseColorScheme? baseColorScheme)
        {
            return new RuntimeThemeOptions(this.UseHSL, isHighContrast, generatorParameters, baseColorScheme);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}