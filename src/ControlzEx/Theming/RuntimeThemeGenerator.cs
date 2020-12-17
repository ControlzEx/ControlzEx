#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public RuntimeThemeGeneratorOptions Options { get; } = new RuntimeThemeGeneratorOptions();

        public Theme? GenerateRuntimeThemeFromWindowsSettings(string baseColor, bool isHighContrast, params LibraryThemeProvider[] libraryThemeProviders)
        {
            return this.GenerateRuntimeThemeFromWindowsSettings(baseColor, isHighContrast, libraryThemeProviders.ToList());
        }

        public virtual Theme? GenerateRuntimeThemeFromWindowsSettings(string baseColor, bool isHighContrast, IEnumerable<LibraryThemeProvider> libraryThemeProviders)
        {
            var windowsAccentColor = WindowsThemeHelper.GetWindowsAccentColor();

            if (windowsAccentColor is null)
            {
                return null;
            }

            var accentColor = windowsAccentColor.Value;

            return this.GenerateRuntimeTheme(baseColor, accentColor, isHighContrast, libraryThemeProviders);
        }

        public Theme? GenerateRuntimeTheme(string baseColor, Color accentColor)
        {
            return this.GenerateRuntimeTheme(baseColor, accentColor, false, ThemeManager.Current.LibraryThemeProviders.ToList());
        }

        public virtual Theme? GenerateRuntimeTheme(string baseColor, Color accentColor, bool isHighContrast)
        {
            return this.GenerateRuntimeTheme(baseColor, accentColor, isHighContrast, ThemeManager.Current.LibraryThemeProviders.ToList());
        }

        public Theme? GenerateRuntimeTheme(string baseColor, Color accentColor, bool isHighContrast, params LibraryThemeProvider[] libraryThemeProviders)
        {
            return this.GenerateRuntimeTheme(baseColor, accentColor, isHighContrast, libraryThemeProviders.ToList());
        }

        public virtual Theme? GenerateRuntimeTheme(string baseColor, Color accentColor, bool isHighContrast, IEnumerable<LibraryThemeProvider> libraryThemeProviders)
        {
            Theme? theme = null;

            foreach (var libraryThemeProvider in libraryThemeProviders)
            {
                var libraryTheme = this.GenerateRuntimeLibraryTheme(baseColor, accentColor, isHighContrast, libraryThemeProvider);

                if (libraryTheme is null)
                {
                    continue;
                }

                if (theme is null)
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

        public virtual LibraryTheme? GenerateRuntimeLibraryTheme(string baseColor, Color accentColor, bool isHighContrast, LibraryThemeProvider libraryThemeProvider)
        {
            var themeGeneratorParametersContent = libraryThemeProvider.GetThemeGeneratorParametersContent();

            if (string.IsNullOrEmpty(themeGeneratorParametersContent))
            {
                return null;
            }

            var themeTemplateContent = libraryThemeProvider.GetThemeTemplateContent();

            if (string.IsNullOrEmpty(themeTemplateContent))
            {
                return null;
            }

            var generatorParameters = ThemeGenerator.Current.GetParametersFromString(themeGeneratorParametersContent!);

            var baseColorScheme = generatorParameters.BaseColorSchemes.First(x => x.Name == baseColor);

            var colorScheme = new ThemeGenerator.ThemeGeneratorColorScheme
            {
                Name = accentColor.ToString()
            };

            var themeName = $"{baseColor}.Runtime_{colorScheme.Name}";
            var themeDisplayName = $"Runtime {colorScheme.Name} ({baseColor})";

            if (isHighContrast)
            {
                themeDisplayName += " HighContrast";
            }

            var values = colorScheme.Values;

            var runtimeThemeColorValues = this.GetColors(accentColor, this.Options.CreateRuntimeThemeOptions(isHighContrast, generatorParameters, baseColorScheme));

            return this.GenerateRuntimeLibraryTheme(libraryThemeProvider, values, runtimeThemeColorValues, themeTemplateContent!, themeName, themeDisplayName, baseColorScheme, colorScheme, generatorParameters);
        }

        public virtual LibraryTheme GenerateRuntimeLibraryTheme(LibraryThemeProvider libraryThemeProvider, Dictionary<string, string> values, RuntimeThemeColorValues runtimeThemeColorValues, string themeTemplateContent, string themeName, string themeDisplayName, ThemeGenerator.ThemeGeneratorBaseColorScheme baseColorScheme, ThemeGenerator.ThemeGeneratorColorScheme colorScheme, ThemeGenerator.ThemeGeneratorParameters generatorParameters)
        {
            values.Add("ThemeGenerator.Colors.PrimaryAccentColor", runtimeThemeColorValues.PrimaryAccentColor.ToString());
            values.Add("ThemeGenerator.Colors.AccentBaseColor", runtimeThemeColorValues.AccentBaseColor.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor80", runtimeThemeColorValues.AccentColor80.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor60", runtimeThemeColorValues.AccentColor60.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor40", runtimeThemeColorValues.AccentColor40.ToString());
            values.Add("ThemeGenerator.Colors.AccentColor20", runtimeThemeColorValues.AccentColor20.ToString());

            values.Add("ThemeGenerator.Colors.HighlightColor", runtimeThemeColorValues.HighlightColor.ToString());
            values.Add("ThemeGenerator.Colors.IdealForegroundColor", runtimeThemeColorValues.IdealForegroundColor.ToString());

            libraryThemeProvider.FillColorSchemeValues(values, runtimeThemeColorValues);

            var xamlContent = ThemeGenerator.Current.GenerateColorSchemeFileContent(themeTemplateContent!, themeName, themeDisplayName, baseColorScheme.Name, colorScheme.Name, colorScheme.Name, runtimeThemeColorValues.Options.IsHighContrast, colorScheme.Values, baseColorScheme.Values, generatorParameters.DefaultValues);

            var preparedXamlContent = libraryThemeProvider.PrepareXamlContent(this, xamlContent, runtimeThemeColorValues);

            var resourceDictionary = (ResourceDictionary)XamlReader.Parse(preparedXamlContent);
            resourceDictionary.Add(Theme.ThemeIsRuntimeGeneratedKey, true);
            resourceDictionary[Theme.ThemeIsHighContrastKey] = runtimeThemeColorValues.Options.IsHighContrast;
            resourceDictionary[LibraryTheme.RuntimeThemeColorValuesKey] = runtimeThemeColorValues;

            libraryThemeProvider.PrepareRuntimeThemeResourceDictionary(this, resourceDictionary, runtimeThemeColorValues);

            var runtimeLibraryTheme = libraryThemeProvider.CreateRuntimeLibraryTheme(resourceDictionary, runtimeThemeColorValues);
            return runtimeLibraryTheme;
        }

        public virtual RuntimeThemeColorValues GetColors(Color accentColor, RuntimeThemeOptions options)
        {
            if (options.UseHSL)
            {
                var accentColorWithoutTransparency = Color.FromRgb(accentColor.R, accentColor.G, accentColor.B);

                return new RuntimeThemeColorValues(options)
                {
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
                return new RuntimeThemeColorValues(options)
                {
                    AccentColor = accentColor,
                    AccentBaseColor = accentColor,
                    PrimaryAccentColor = accentColor,

                    AccentColor80 = Color.FromArgb(204 /* 255 * 0.8 */, accentColor.R, accentColor.G, accentColor.B),
                    AccentColor60 = Color.FromArgb(153 /* 255 * 0.6 */, accentColor.R, accentColor.G, accentColor.B),
                    AccentColor40 = Color.FromArgb(102 /* 255 * 0.4 */, accentColor.R, accentColor.G, accentColor.B),
                    AccentColor20 = Color.FromArgb(51 /* 255 * 0.2 */, accentColor.R, accentColor.G, accentColor.B),

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