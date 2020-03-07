#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Windows;

    /// <summary>
    /// Class which is used as argument for an event to signal theme changes.
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        public ThemeChangedEventArgs(ResourceDictionary targetResourceDictionary, Theme? oldTheme, Theme newTheme)
        {
            this.TargetResourceDictionary = targetResourceDictionary;
            this.OldTheme = oldTheme;
            this.NewTheme = newTheme;
        }

        /// <summary>
        /// The <see cref="ResourceDictionary"/> for which was targeted by the theme change.
        /// </summary>
        public ResourceDictionary TargetResourceDictionary { get; }

        /// <summary>
        /// The old theme.
        /// </summary>
        public Theme? OldTheme { get; set; }

        /// <summary>
        /// The new theme.
        /// </summary>
        public Theme NewTheme { get; set; }
    }
}