#nullable enable
namespace ControlzEx.Theming
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public static class ThemeGenerator
    {
        public static ThemeGeneratorParameters GetParametersFromString(string input)
        {
#if NETCOREAPP
            return System.Text.Json.JsonSerializer.Deserialize<ThemeGeneratorParameters>(input);
#else
            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<ThemeGeneratorParameters>(input);
#endif
        }

        public static string GenerateColorSchemeFileContent(ThemeGeneratorParameters parameters, ThemeGeneratorBaseColorScheme baseColorScheme, ThemeGeneratorColorScheme colorScheme, string templateContent, string themeName, string themeDisplayName)
        {
            templateContent = templateContent.Replace("{{ThemeName}}", themeName);
            templateContent = templateContent.Replace("{{ThemeDisplayName}}", themeDisplayName);
            templateContent = templateContent.Replace("{{BaseColorScheme}}", baseColorScheme.Name);
            templateContent = templateContent.Replace("{{ColorScheme}}", colorScheme.Name);

            foreach (var value in colorScheme.Values)
            {
                templateContent = templateContent.Replace($"{{{{{value.Key}}}}}", value.Value);
            }

            foreach (var value in baseColorScheme.Values)
            {
                templateContent = templateContent.Replace($"{{{{{value.Key}}}}}", value.Value);
            }

            foreach (var value in parameters.DefaultValues)
            {
                templateContent = templateContent.Replace($"{{{{{value.Key}}}}}", value.Value);
            }

            return templateContent;
        }

        public class ThemeGeneratorParameters
        {
            public Dictionary<string, string> DefaultValues { get; set; } = new Dictionary<string, string>();

            public ThemeGeneratorBaseColorScheme[] BaseColorSchemes { get; set; } = new ThemeGeneratorBaseColorScheme[0];

            public ThemeGeneratorColorScheme[] ColorSchemes { get; set; } = new ThemeGeneratorColorScheme[0];
        }

        [DebuggerDisplay("{" + nameof(Name) + "}")]
        public class ThemeGeneratorBaseColorScheme
        {
            public string Name { get; set; } = string.Empty;

            public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();

            public ThemeGeneratorBaseColorScheme Clone()
            {
                return (ThemeGeneratorBaseColorScheme)this.MemberwiseClone();
            }
        }

        [DebuggerDisplay("{" + nameof(Name) + "}")]
        public class ThemeGeneratorColorScheme
        {
            public string Name { get; set; } = string.Empty;

            public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
        }
    }
}