#nullable enable
namespace ControlzEx.Theming
{
    using System.Windows.Media;

    public class RuntimeThemeColorValues
    {
        public RuntimeThemeColorValues(RuntimeThemeOptions options)
        {
            this.Options = options;
        }

        public RuntimeThemeOptions Options { get; }

        public Color AccentColor { get; set; }

        public Color AccentBaseColor { get; set; }

        public Color PrimaryAccentColor { get; set; }

        public Color AccentColor80 { get; set; }

        public Color AccentColor60 { get; set; }

        public Color AccentColor40 { get; set; }

        public Color AccentColor20 { get; set; }

        public Color HighlightColor { get; set; }

        public Color IdealForegroundColor { get; set; }
    }
}