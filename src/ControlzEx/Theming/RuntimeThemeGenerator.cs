namespace ControlzEx.Theming
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Media;
    using ControlzEx.Internal;

    public class RuntimeThemeGenerator
    {
        public static Theme GenerateRuntimeThemeFromWindowsSettings(string baseColor, IEnumerable<ThemeResourceReader> themeResourceReaders)
        {
            var windowsAccentColor = WindowsThemeHelper.GetWindowsAccentColor();

            if (windowsAccentColor.IsNull())
            {
                return null;
            }

            var accentColor = windowsAccentColor.Value;

            Theme theme = null;

            foreach (var themeResourceReader in themeResourceReaders)
            {
                var themeGeneratorParametersContent = themeResourceReader.GetThemeGeneratorParametersContent();

                if (string.IsNullOrEmpty(themeGeneratorParametersContent))
                {
                    continue;
                }

                var generatorParameters = ThemeGenerator.GetParametersFromString(themeGeneratorParametersContent);

                var baseColorScheme = generatorParameters.BaseColorSchemes.First(x => x.Name == baseColor);

                var colorScheme = new ThemeGenerator.ThemeGeneratorColorScheme
                {
                    Name = accentColor.ToString()
                };
                var values = colorScheme.Values;

                var accentColor80Percent = Color.FromArgb(204, accentColor.R, accentColor.G, accentColor.B);
                var accentColor60Percent = Color.FromArgb(153, accentColor.R, accentColor.G, accentColor.B);
                var accentColor40Percent = Color.FromArgb(102, accentColor.R, accentColor.G, accentColor.B);
                var accentColor20Percent = Color.FromArgb(51, accentColor.R, accentColor.G, accentColor.B);

                var highlightColor = GetHighlightColor(accentColor);

                var idealForegroundColor = GetIdealTextColor(accentColor);

                values.Add("ThemeGenerator.Colors.AccentBaseColor", accentColor.ToString());
                values.Add("ThemeGenerator.Colors.AccentColor80", accentColor.ToString());
                values.Add("ThemeGenerator.Colors.AccentColor60", accentColor60Percent.ToString());
                values.Add("ThemeGenerator.Colors.AccentColor40", accentColor40Percent.ToString());
                values.Add("ThemeGenerator.Colors.AccentColor20", accentColor20Percent.ToString());
                            
                values.Add("ThemeGenerator.Colors.HighlightColor", highlightColor.ToString());
                values.Add("ThemeGenerator.Colors.IdealForegroundColor", idealForegroundColor.ToString());

                themeResourceReader.FillColorSchemeValues(values, accentColor, accentColor80Percent, accentColor60Percent, accentColor40Percent, accentColor20Percent, highlightColor, idealForegroundColor);

                var themeFileContent = ThemeGenerator.GenerateColorSchemeFileContent(generatorParameters, baseColorScheme, colorScheme, themeResourceReader.GetThemeTemplateContent(), $"{baseColor}.Runtime_{accentColor}", $"Runtime {accentColor} ({baseColor})");
                var resourceDictionary = (ResourceDictionary)XamlReader.Parse(themeFileContent);

                var libraryTheme = new LibraryTheme(resourceDictionary, true);

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