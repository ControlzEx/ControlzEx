#nullable enable
namespace ControlzEx.Theming
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;

    /// <summary>
    /// Options being used to generate one single <see cref="LibraryTheme"/>.
    /// </summary>
    [PublicAPI]
    public class RuntimeThemeOptions : INotifyPropertyChanged
    {
        public RuntimeThemeOptions(bool useHSL, bool isHighContrast, ThemeGenerator.ThemeGeneratorParameters? generatorParameters, ThemeGenerator.ThemeGeneratorBaseColorScheme? baseColorScheme)
        {
            this.UseHSL = useHSL;
            this.IsHighContrast = isHighContrast;
            this.GeneratorParameters = generatorParameters;
            this.BaseColorScheme = baseColorScheme;
        }

        public bool UseHSL { get; }

        public bool IsHighContrast { get; }

        public ThemeGenerator.ThemeGeneratorParameters? GeneratorParameters { get; }

        public ThemeGenerator.ThemeGeneratorBaseColorScheme? BaseColorScheme { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}