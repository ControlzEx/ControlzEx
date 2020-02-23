namespace ControlzEx.Theming
{
    using System;

    /// <summary>
    /// Class which is used as argument for an event to signal theme changes.
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public ThemeChangedEventArgs(Theme oldTheme, Theme newTheme)
        {
            this.OldTheme = oldTheme;
            this.NewTheme = newTheme;
        }

        /// <summary>
        /// The old theme.
        /// </summary>
        public Theme OldTheme { get; set; }

        /// <summary>
        /// The new theme.
        /// </summary>
        public Theme NewTheme { get; set; }
    }
}