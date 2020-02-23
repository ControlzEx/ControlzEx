namespace ControlzEx.Theming
{
#nullable enable
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a theme.
    /// </summary>
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
        /// Gets the key for the themes primary accent color.
        /// </summary>
        public const string ThemePrimaryAccentColorKey = "Theme.PrimaryAccentColor";

        /// <summary>
        /// Gets the key for the theme showcase brush.
        /// </summary>
        public const string ThemeShowcaseBrushKey = "Theme.ShowcaseBrush";

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="libraryTheme">The first <see cref="LibraryTheme"/> of the theme.</param>
        public Theme([NotNull] LibraryTheme libraryTheme)
            : this(libraryTheme.Name, libraryTheme.DisplayName, libraryTheme.BaseColorScheme, libraryTheme.ColorScheme, libraryTheme.PrimaryAccentColor, libraryTheme.ShowcaseBrush, libraryTheme.IsRuntimeGenerated)
        {
            if (libraryTheme is null)
            {
                throw new ArgumentNullException(nameof(libraryTheme));
            }

            this.AddLibraryTheme(libraryTheme);
        }

        public Theme(string name, string displayName, string baseColorScheme, string colorScheme, Color primaryAccentColor, Brush showcaseBrush, bool isRuntimeGenerated)
        {
            this.IsRuntimeGenerated = isRuntimeGenerated;

            this.Name = name;
            this.DisplayName = displayName;
            this.BaseColorScheme = baseColorScheme;
            this.ColorScheme = colorScheme;
            this.PrimaryAccentColor = primaryAccentColor;
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
        /// Gets the primary accent color for this theme.
        /// </summary>
        public Color PrimaryAccentColor { get; set; }

        /// <summary>
        /// Gets the color scheme for this theme.
        /// </summary>
        public string ColorScheme { get; }

        /// <summary>
        /// Gets a brush which can be used to showcase this theme.
        /// </summary>
        public Brush ShowcaseBrush { get; }

        /// <summary>
        /// Ensures that all <see cref="LibraryThemeProvider"/> from <see cref="ThemeManager.LibraryThemeProviders"/> provided a <see cref="LibraryTheme"/> for this <see cref="Theme"/>.
        /// </summary>
        /// <returns>This instance for fluent call usage.</returns>
        public Theme EnsureAllLibraryThemeProvidersProvided()
        {
            var libraryThemeProvidersWhichDidNotProvideLibraryTheme = ThemeManager.LibraryThemeProviders.Except(this.LibraryThemes.Select(x => x.LibraryThemeProvider));

            foreach (var libraryThemeProvider in libraryThemeProvidersWhichDidNotProvideLibraryTheme)
            {
                var libraryTheme = libraryThemeProvider!.ProvideMissingLibraryTheme(this);

                if (libraryTheme == null)
                {
                    continue;
                }

                this.AddLibraryTheme(libraryTheme);
            }

            return this;
        }

        /// <summary>
        /// Gets the first resource that matches <paramref name="key"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The found resource or null.</returns>
        public object? GetResource(object key)
        {
            foreach (var resources in this.GetAllResources())
            {
                var resource = resources[key];

                if (resource is null)
                {
                    continue;
                }

                return resource;
            }

            return null;
        }

        /// <summary>
        /// Gets a flat list of all <see cref="ResourceDictionary"/> from all library themes.
        /// </summary>
        /// <returns>A flat list of all <see cref="ResourceDictionary"/> from all library themes.</returns>
        public IEnumerable<ResourceDictionary> GetAllResources()
        {
            this.EnsureAllLibraryThemeProvidersProvided();

            foreach (var libraryTheme in this.LibraryThemes)
            {
                foreach (var libraryThemeResource in libraryTheme.Resources)
                {
                    yield return libraryThemeResource;
                }
            }
        }

        /// <summary>
        /// Adds a new <see cref="LibraryTheme"/> to this <see cref="Theme"/>.
        /// </summary>
        /// <param name="libraryTheme">The <see cref="LibraryTheme"/> to add.</param>
        /// <returns>This instance for fluent call usage.</returns>
        public Theme AddLibraryTheme([NotNull] LibraryTheme libraryTheme)
        {
            if (libraryTheme is null)
            {
                throw new ArgumentNullException(nameof(libraryTheme));
            }

            //// todo: How do we check if the library themes match this theme?
            //if (libraryTheme.Name != this.Name)
            //{
            //    throw new ArgumentException("The theme key does not match the current theme key.");
            //}

            if (!(libraryTheme.ParentTheme is null))
            {
                throw new ArgumentException("The theme already has a parent.");
            }

            this.LibraryThemesInternal.Add(libraryTheme);
            libraryTheme.ParentTheme = this;

            return this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"DisplayName={this.DisplayName}, Name={this.Name}";
        }

        public static string? GetThemeName([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            foreach (var resourceDictionaryKey in resourceDictionary.Keys)
            {
                if (resourceDictionaryKey is string key
                    && key.Equals(ThemeNameKey, StringComparison.Ordinal))
                {
                    return (string)resourceDictionary[resourceDictionaryKey];
                }
            }

            return null;
        }

        public static bool IsThemeDictionary([NotNull] ResourceDictionary resources)
        {
            return string.IsNullOrEmpty(GetThemeName(resources)) == false;
        }
    }
}