namespace ControlzEx.Showcase.Theming
{
    using System.Collections.Generic;
    using ControlzEx.Theming;

    public class ShowcaseLibraryThemeProvider : LibraryThemeProvider
    {
        public static readonly ShowcaseLibraryThemeProvider DefaultInstance = new ShowcaseLibraryThemeProvider();

        public ShowcaseLibraryThemeProvider()
            : base(true)
        {
        }

        public override void FillColorSchemeValues(Dictionary<string, string> values, RuntimeThemeColorValues colorValues)
        {
            values.Add("ControlzEx.Colors.AccentBaseColor", colorValues.AccentBaseColor.ToString());
            values.Add("ControlzEx.Colors.AccentColor80", colorValues.AccentColor80.ToString());
            values.Add("ControlzEx.Colors.AccentColor60", colorValues.AccentColor60.ToString());
            values.Add("ControlzEx.Colors.AccentColor40", colorValues.AccentColor40.ToString());
            values.Add("ControlzEx.Colors.AccentColor20", colorValues.AccentColor20.ToString());

            values.Add("ControlzEx.Colors.HighlightColor", colorValues.HighlightColor.ToString());
            values.Add("ControlzEx.Colors.IdealForegroundColor", colorValues.IdealForegroundColor.ToString());
        }
    }
}