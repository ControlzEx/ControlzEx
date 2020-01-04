namespace ControlzEx.Theming
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Internal;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a theme.
    /// </summary>
    [DebuggerDisplay("DisplayName={" + nameof(DisplayName) + "}, Name={" + nameof(Name) + "}")]
    public class Theme
    {
        /// <summary>
        /// Gets the key for the theme name.
        /// </summary>
        public const string ThemeNameKey = "Theme.Name";

        /// <summary>
        /// Gets the key for the theme origin.
        /// </summary>
        public const string ThemeOriginKey = "Theme.Origin";

        /// <summary>
        /// Gets the key for the theme display name.
        /// </summary>
        public const string ThemeDisplayNameKey = "Theme.DisplayName";

        /// <summary>
        /// Gets the key for the theme base color scheme.
        /// </summary>
        public const string ThemeBaseColorSchemeKey = "Theme.BaseColorScheme";

        /// <summary>
        /// Gets the key for the theme color scheme.
        /// </summary>
        public const string ThemeColorSchemeKey = "Theme.ColorScheme";

        /// <summary>
        /// Gets the key for the theme showcase brush.
        /// </summary>
        public const string ThemeShowcaseBrushKey = "Theme.ShowcaseBrush";

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="libraryTheme">The first <see cref="LibraryTheme"/> of the theme.</param>
        public Theme([NotNull] LibraryTheme libraryTheme)
            : this(libraryTheme.Name, libraryTheme.DisplayName, libraryTheme.BaseColorScheme, libraryTheme.ColorScheme, libraryTheme.ShowcaseBrush, libraryTheme.IsRuntimeGenerated)
        {
            if (libraryTheme is null)
            {
                throw new ArgumentNullException(nameof(libraryTheme));
            }

            this.AddLibraryTheme(libraryTheme);
        }

        public Theme(string name, string displayName, string baseColorScheme, string colorScheme, SolidColorBrush showcaseBrush, bool isRuntimeGenerated)
        {
            this.IsRuntimeGenerated = isRuntimeGenerated;

            this.Name = name;
            this.DisplayName = displayName;
            this.BaseColorScheme = baseColorScheme;
            this.ColorScheme = colorScheme;
            this.ShowcaseBrush = showcaseBrush;

            this.LibraryThemes = new ReadOnlyObservableCollection<LibraryTheme>(this.LibraryThemesInternal);
        }

        public bool IsRuntimeGenerated { get; }

        /// <summary>
        /// The ResourceDictionaries that represent this theme.
        /// </summary>
        public ReadOnlyObservableCollection<LibraryTheme> LibraryThemes { get; }

        /// <summary>
        /// The ResourceDictionaries that represent this theme.
        /// </summary>
        private ObservableCollection<LibraryTheme> LibraryThemesInternal { get; } = new ObservableCollection<LibraryTheme>();

        /// <summary>
        /// Gets the name of the theme.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the display name of the theme.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Get the base color scheme for this theme.
        /// </summary>
        public string BaseColorScheme { get; }

        /// <summary>
        /// Gets the color scheme for this theme.
        /// </summary>
        public string ColorScheme { get; }

        /// <summary>
        /// Gets a brush which can be used to showcase this theme.
        /// </summary>
        public SolidColorBrush ShowcaseBrush { get; }

        public IEnumerable<ResourceDictionary> GetAllResources() => this.LibraryThemes.SelectMany(x => x.Resources);

        public Theme AddLibraryTheme([NotNull] LibraryTheme libraryTheme)
        {
            if (libraryTheme is null)
            {
                throw new ArgumentNullException(nameof(libraryTheme));
            }

            if (libraryTheme.Name != this.Name)
            {
                throw new ArgumentException("The theme key does not match the current theme key.");
            }

            if (libraryTheme.ParentTheme.IsNotNull())
            {
                throw new ArgumentException("The theme already has a parent.");
            }

            this.LibraryThemesInternal.Add(libraryTheme);
            libraryTheme.ParentTheme = this;

            return this;
        }

        public static string GetThemeName([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            var key = (string)resourceDictionary[ThemeNameKey];

            return key;
        }

        public static bool IsThemeDictionary([NotNull] ResourceDictionary resources)
        {
            return string.IsNullOrEmpty(GetThemeName(resources)) == false;
        }
    }
}