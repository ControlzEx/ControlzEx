namespace ControlzEx.Theming
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public static class ThemeGenerator
    {
        public static GeneratorParameters GetParametersFromString(string input)
        {
#if NETCOREAPP
            return System.Text.Json.JsonSerializer.Deserialize<GeneratorParameters>(input);
#else
            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<GeneratorParameters>(input);
#endif
        }

        public static string GenerateColorSchemeFileContent(GeneratorParameters parameters, BaseColorScheme baseColorScheme, ColorScheme colorScheme, string templateContent, string themeName, string themeDisplayName)
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

        public class GeneratorParameters
        {
            public string TemplateFile { get; set; }

            public Dictionary<string, string> DefaultValues { get; set; } = new Dictionary<string, string>();

            public BaseColorScheme[] BaseColorSchemes { get; set; }

            public ColorScheme[] ColorSchemes { get; set; }
        }

        [DebuggerDisplay("{" + nameof(Name) + "}")]
        public class BaseColorScheme
        {
            public string Name { get; set; }

            public Dictionary<string, string> Values { get; set; }

            public BaseColorScheme Clone()
            {
                return (BaseColorScheme)this.MemberwiseClone();
            }
        }

        [DebuggerDisplay("{" + nameof(Name) + "}")]
        public class ColorScheme
        {
            public string CustomBaseColorSchemeName { get; set; }

            public string BaseColorSchemeReference { get; set; }

            public string Name { get; set; }

            public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
        }
    }
}