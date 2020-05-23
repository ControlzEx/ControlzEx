#nullable enable
namespace ControlzEx.Theming
{
    /// <summary>
    /// Options being used to generate one single <see cref="LibraryTheme"/>.
    /// </summary>
    public class RuntimeThemeOptions
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
    }
}