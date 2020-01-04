namespace ControlzEx.Showcase.Themes
{
    using System.Collections.Generic;
    using System.Windows.Media;
    using ControlzEx.Theming;

    public class ShowcaseThemeResourceReader : ThemeResourceReader
    {
        public static readonly ShowcaseThemeResourceReader DefaultInstance = new ShowcaseThemeResourceReader();

        private ShowcaseThemeResourceReader()
            : base(true)
        {
        }

        public override void FillColorSchemeValues(Dictionary<string, string> values, Color accentColor, Color accentColor80Percent, Color accentColor60Percent, Color accentColor40Percent, Color accentColor20Percent, Color highlightColor, Color idealForegroundColor)
        {
            values.Add("ControlzEx.Colors.AccentBaseColor", accentColor.ToString());
            values.Add("ControlzEx.Colors.AccentColor80", accentColor.ToString());
            values.Add("ControlzEx.Colors.AccentColor60", accentColor60Percent.ToString());
            values.Add("ControlzEx.Colors.AccentColor40", accentColor40Percent.ToString());
            values.Add("ControlzEx.Colors.AccentColor20", accentColor20Percent.ToString());
                            
            values.Add("ControlzEx.Colors.HighlightColor", highlightColor.ToString());
            values.Add("ControlzEx.Colors.IdealForegroundColor", idealForegroundColor.ToString());
        }
    }
}