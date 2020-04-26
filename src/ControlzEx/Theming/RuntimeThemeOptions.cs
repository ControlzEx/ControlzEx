#nullable enable
namespace ControlzEx.Theming
{
    /// <summary>
    /// Options being used to generate one single <see cref="LibraryTheme"/>.
    /// </summary>
    public class RuntimeThemeOptions
    {
        public RuntimeThemeOptions(bool useHSL, ThemeGenerator.ThemeGeneratorParameters? generatorParameters, ThemeGenerator.ThemeGeneratorBaseColorScheme? baseColorScheme)
        {
            this.UseHSL = useHSL;
            this.GeneratorParameters = generatorParameters;
            this.BaseColorScheme = baseColorScheme;
        }

        public bool UseHSL { get; }

        public ThemeGenerator.ThemeGeneratorParameters? GeneratorParameters { get; }

        public ThemeGenerator.ThemeGeneratorBaseColorScheme? BaseColorScheme { get; }
    }
}