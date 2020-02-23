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
            public string TemplateFile { get; set; }

            public Dictionary<string, string> DefaultValues { get; set; } = new Dictionary<string, string>();

            public ThemeGeneratorBaseColorScheme[] BaseColorSchemes { get; set; }

            public ThemeGeneratorColorScheme[] ColorSchemes { get; set; }
        }

        [DebuggerDisplay("{" + nameof(Name) + "}")]
        public class ThemeGeneratorBaseColorScheme
        {
            public string Name { get; set; }

            public Dictionary<string, string> Values { get; set; }

            public ThemeGeneratorBaseColorScheme Clone()
            {
                return (ThemeGeneratorBaseColorScheme)this.MemberwiseClone();
            }
        }

        [DebuggerDisplay("{" + nameof(Name) + "}")]
        public class ThemeGeneratorColorScheme
        {
            public string CustomBaseColorSchemeName { get; set; }

            public string BaseColorSchemeReference { get; set; }

            public string Name { get; set; }

            public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
        }
    }
}