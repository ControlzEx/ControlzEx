namespace ControlzEx.Theming
{
#nullable enable
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Media;

    public class RuntimeThemeGenerator
    {
        public static RuntimeThemeGenerator Current { get; set; }

        static RuntimeThemeGenerator()
        {
            Current = new RuntimeThemeGenerator();
        }

        public RuntimeThemeColorOptions ColorOptions { get; } = new RuntimeThemeColorOptions();

        public Theme? GenerateRuntimeThemeFromWindowsSettings(string baseColor, params LibraryThemeProvider[] libraryThemeProviders)
        {
            return this.GenerateRuntimeThemeFromWindowsSettings(baseColor, libraryThemeProviders.ToList());
        }

        public virtual Theme? GenerateRuntimeThemeFromWindowsSettings(string baseColor, IEnumerable<LibraryThemeProvider> libraryThemeProviders)
        {
            var windowsAccentColor = WindowsThemeHelper.GetWindowsAccentColor();

            if (windowsAccentColor is null)
            {
                return null;
            }

            var accentColor = windowsAccentColor.Value;

            return this.GenerateRuntimeTheme(baseColor, accentColor, libraryThemeProviders);
        }

        public virtual Theme? GenerateRuntimeTheme(string baseColor, Color accentColor)
        {
            return this.GenerateRuntimeTheme(baseColor, accentColor, ThemeManager.LibraryThemeProviders.ToList());
        }

        public Theme? GenerateRuntimeTheme(string baseColor, Color accentColor, params LibraryThemeProvider[] libraryThemeProviders)
        {
            return this.GenerateRuntimeTheme(baseColor, accentColor, libraryThemeProviders.ToList());
        }

        public virtual Theme? GenerateRuntimeTheme(string baseColor, Color accentColor, IEnumerable<LibraryThemeProvider> libraryThemeProviders)
        {
            Theme? theme = null;

            foreach (var libraryThemeProvider in libraryThemeProviders)
            {
                var libraryTheme = this.GenerateRuntimeLibraryTheme(baseColor, accentColor, libraryThemeProvider);

                if (libraryTheme == null)
                {
                    continue;
                }

                if (theme == null)
                {
                    theme = new Theme(libraryTheme);
                }
                else
                {
                    theme.AddLibraryTheme(libraryTheme);
                }
            }

            return theme;
        }

        public virtual LibraryTheme? GenerateRuntimeLibraryTheme(string baseColor, Color accentColor, LibraryThemeProvider libraryThemeProvider)
        {
            var themeGeneratorParametersContent = libraryThemeProvider.GetThemeGeneratorParametersContent();

            if (string.IsNullOrEmpty(themeGeneratorParametersContent))
            {
                return null;
            }

            var generatorParameters = ThemeGenerator.GetParametersFromString(themeGeneratorParametersContent);

            var baseColorScheme = generatorParameters.BaseColorSchemes.First(x => x.Name == baseColor);

            var colorScheme = new ThemeGenerator.ThemeGeneratorColorScheme
            {
                Name = accentColor.ToString()
            };
            var values = colorScheme.Values;

            var runtimeThemeColorValues = this.GetColors(accentColor, this.ColorOptions);

            values.Add("ThemeGenerator.Colors.PrimaryAccentColor", runtimeThemeColorValues.PrimaryAccentColor.ToString());
            values.Add("ThemeGenerator.Colors.AccentBaseColor", runtimeThemeColorValues.AccentBaseColor.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor80", runtimeThemeColorValues.AccentColor80.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor60", runtimeThemeColorValues.AccentColor60.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor40", runtimeThemeColorValues.AccentColor40.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor20", runtimeThemeColorValues.AccentColor20.ToString());

            values.Add("ThemeGenerator.Colors.HighlightColor", runtimeThemeColorValues.HighlightColor.ToString());
            values.Add("ThemeGenerator.Colors.IdealForegroundColor", runtimeThemeColorValues.IdealForegroundColor.ToString());

            libraryThemeProvider.FillColorSchemeValues(values, runtimeThemeColorValues);

            var xamlContent = ThemeGenerator.GenerateColorSchemeFileContent(generatorParameters, baseColorScheme, colorScheme, libraryThemeProvider.GetThemeTemplateContent(), $"{baseColor}.Runtime_{accentColor}", $"Runtime {accentColor} ({baseColor})");

            var fixedXamlContent = this.FixXamlContent(xamlContent);

            var resourceDictionary = (ResourceDictionary)XamlReader.Parse(fixedXamlContent);

            var runtimeLibraryTheme = new LibraryTheme(resourceDictionary, libraryThemeProvider, true);
            return runtimeLibraryTheme;
        }

        public virtual string FixXamlContent(string xamlContent)
        {
            xamlContent = this.FixXamlReaderBug(xamlContent);

            return xamlContent;
        }

        protected virtual string FixXamlReaderBug(string xamlContent)
        {
            // Check if we have to fix something
            if (xamlContent.Contains("WithAssembly=\"") == false)
            {
                return xamlContent;
            }

            // search for namespace names with suffix "WithAssembly"
            var withAssemblyMatches = Regex.Matches(xamlContent, @"\s*xmlns:(?<namespace_name>.+?)WithAssembly=(?<namespace>"".+?"")");

            foreach (var withAssemblyMatch in withAssemblyMatches!.OfType<Match>())
            {
                // search for namespaces that are the same without suffix "WithAssembly"
                var originalMatches = Regex.Matches(xamlContent, $@"\s*xmlns:({withAssemblyMatch.Groups["namespace_name"].Value})=(?<namespace>"".+?"")");

                foreach (var originalMatch in originalMatches!.OfType<Match>())
                {
                    // replace the used namespace value of the namespaces without the suffix with the content from the namespace with suffix
                    xamlContent = xamlContent.Replace(originalMatch.Groups["namespace"].Value, withAssemblyMatch.Groups["namespace"].Value);
                }
            }

            return xamlContent;
        }

        public virtual RuntimeThemeColorValues GetColors(Color accentColor, RuntimeThemeColorOptions options)
        {
            if (options.UseHSL)
            {
                var accentColorWithoutTransparency = Color.FromRgb(accentColor.R, accentColor.G, accentColor.B);

                return new RuntimeThemeColorValues
                {
                    Options = options,

                    AccentColor = accentColor,
                    AccentBaseColor = accentColor,
                    PrimaryAccentColor = accentColor,

                    AccentColor80 = HSLColor.GetTintedColor(accentColorWithoutTransparency, 0.2),
                    AccentColor60 = HSLColor.GetTintedColor(accentColorWithoutTransparency, 0.4),
                    AccentColor40 = HSLColor.GetTintedColor(accentColorWithoutTransparency, 0.6),
                    AccentColor20 = HSLColor.GetTintedColor(accentColorWithoutTransparency, 0.8),

                    HighlightColor = GetHighlightColor(accentColorWithoutTransparency),

                    IdealForegroundColor = GetIdealTextColor(accentColorWithoutTransparency),
                };
            }
            else
            {
                return new RuntimeThemeColorValues
                {
                    Options = options,

                    AccentColor = accentColor,
                    AccentBaseColor = accentColor,
                    PrimaryAccentColor = accentColor,

                    AccentColor80 = Color.FromArgb(204, accentColor.R, accentColor.G, accentColor.B),
                    AccentColor60 = Color.FromArgb(153, accentColor.R, accentColor.G, accentColor.B),
                    AccentColor40 = Color.FromArgb(102, accentColor.R, accentColor.G, accentColor.B),
                    AccentColor20 = Color.FromArgb(51, accentColor.R, accentColor.G, accentColor.B),

                    HighlightColor = GetHighlightColor(accentColor),

                    IdealForegroundColor = GetIdealTextColor(accentColor),
                };
            }
        }

        /// <summary>
        ///     Determining Ideal Text Color Based on Specified Background Color
        ///     http://www.codeproject.com/KB/GDI-plus/IdealTextColor.aspx
        /// </summary>
        /// <param name="color">The background color.</param>
        /// <returns></returns>
        public static Color GetIdealTextColor(Color color)
        {
            const int THRESHOLD = 105;
            var bgDelta = Convert.ToInt32((color.R * 0.299) + (color.G * 0.587) + (color.B * 0.114));
            var foreColor = 255 - bgDelta < THRESHOLD
                ? Colors.Black
                : Colors.White;
            return foreColor;
        }

        public static Color GetHighlightColor(Color color, int highlightFactor = 7)
        {
            //calculate a highlight color from c
            return Color.FromRgb((byte)(color.R + highlightFactor > 255 ? 255 : color.R + highlightFactor),
                                  (byte)(color.G + highlightFactor > 255 ? 255 : color.G + highlightFactor),
                                  (byte)(color.B + highlightFactor > 255 ? 255 : color.B + highlightFactor));
        }
    }
}