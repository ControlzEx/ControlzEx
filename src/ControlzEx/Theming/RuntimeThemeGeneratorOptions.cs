#nullable enable
namespace ControlzEx.Theming
{
    /// <summary>
    /// Global options for <see cref="RuntimeThemeGenerator"/>.
    /// </summary>
    public class RuntimeThemeGeneratorOptions
    {
        public bool UseHSL { get; set; }

        /// <summary>
        /// Used to create the options being used to generate a single <see cref="LibraryTheme"/>.
        /// </summary>
        public virtual RuntimeThemeOptions CreateRuntimeThemeOptions(bool isHighContrast, ThemeGenerator.ThemeGeneratorParameters? generatorParameters, ThemeGenerator.ThemeGeneratorBaseColorScheme? baseColorScheme)
        {
            return new RuntimeThemeOptions(this.UseHSL, isHighContrast, generatorParameters, baseColorScheme);
        }
    }
}