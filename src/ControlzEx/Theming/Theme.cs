#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Internal;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a theme.
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// Gets the key for the themes name.
        /// </summary>
        public const string ThemeNameKey = "Theme.Name";

        /// <summary>
        /// Gets the key for the themes origin.
        /// </summary>
        public const string ThemeOriginKey = "Theme.Origin";

        /// <summary>
        /// Gets the key for the themes display name.
        /// </summary>
        public const string ThemeDisplayNameKey = "Theme.DisplayName";

        /// <summary>
        /// Gets the key for the themes base color scheme.
        /// </summary>
        public const string ThemeBaseColorSchemeKey = "Theme.BaseColorScheme";

        /// <summary>
        /// Gets the key for the themes color scheme.
        /// </summary>
        public const string ThemeColorSchemeKey = "Theme.ColorScheme";

        /// <summary>
        /// Gets the key for the themes primary accent color.
        /// </summary>
        public const string ThemePrimaryAccentColorKey = "Theme.PrimaryAccentColor";

        /// <summary>
        /// Gets the key for the themes showcase brush.
        /// </summary>
        public const string ThemeShowcaseBrushKey = "Theme.ShowcaseBrush";

        /// <summary>
        /// Gets the key for the themes runtime generation flag.
        /// </summary>
        public const string ThemeIsRuntimeGeneratedKey = "Theme.IsRuntimeGenerated";

        /// <summary>
        /// Gets the key for the themes high contrast flag.
        /// </summary>
        public const string ThemeIsHighContrastKey = "Theme.IsHighContrast";

        /// <summary>
        /// Gets the key for the theme instance.
        /// </summary>
        public const string ThemeInstanceKey = "Theme.ThemeInstance";

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="libraryTheme">The first <see cref="LibraryTheme"/> of the theme.</param>
        public Theme([NotNull] LibraryTheme libraryTheme)
            : this(libraryTheme.Name, libraryTheme.DisplayName, libraryTheme.BaseColorScheme, libraryTheme.ColorScheme, libraryTheme.PrimaryAccentColor, libraryTheme.ShowcaseBrush, libraryTheme.IsRuntimeGenerated, libraryTheme.IsHighContrast)
        {
            if (libraryTheme is null)
            {
                throw new ArgumentNullException(nameof(libraryTheme));
            }

            this.AddLibraryTheme(libraryTheme);
        }

        public Theme(string name, string displayName, string baseColorScheme, string colorScheme, Color primaryAccentColor, Brush showcaseBrush, bool isRuntimeGenerated, bool isHighContrast)
        {
            this.IsRuntimeGenerated = isRuntimeGenerated;
            this.IsHighContrast = isHighContrast;

            this.Name = name;
            this.DisplayName = displayName;
            this.BaseColorScheme = baseColorScheme;
            this.ColorScheme = colorScheme;
            this.PrimaryAccentColor = primaryAccentColor;
            this.ShowcaseBrush = showcaseBrush;

            this.LibraryThemes = new ReadOnlyObservableCollection<LibraryTheme>(this.LibraryThemesInternal);

            this.Resources[ThemeInstanceKey] = this;
        }

        public static readonly Dictionary<Uri, bool> ThemeDictionaryCache = new Dictionary<Uri, bool>();

        /// <summary>
        /// Gets whether this theme was generated at runtime.
        /// </summary>
        public bool IsRuntimeGenerated { get; }

        /// <summary>
        /// Gets whether this theme is for high contrast mode.
        /// </summary>
        public bool IsHighContrast { get; }

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
        /// The root <see cref="System.Windows.ResourceDictionary"/> containing all resource dictionaries of all <see cref="LibraryTheme"/> belonging to this instance as <see cref="System.Windows.ResourceDictionary.MergedDictionaries"/>
        /// </summary>
        public ResourceDictionary Resources { get; } = new ResourceDictionary();

        /// <summary>
        /// The ResourceDictionaries that represent this theme.
        /// </summary>
        public ReadOnlyObservableCollection<LibraryTheme> LibraryThemes { get; }

        /// <summary>
        /// The ResourceDictionaries that represent this theme.
        /// </summary>
        private ObservableCollection<LibraryTheme> LibraryThemesInternal { get; } = new ObservableCollection<LibraryTheme>();

        /// <summary>
        /// Ensures that all <see cref="LibraryThemeProvider"/> from <see cref="ThemeManager.LibraryThemeProviders"/> provided a <see cref="LibraryTheme"/> for this <see cref="Theme"/>.
        /// </summary>
        /// <returns>This instance for fluent call usage.</returns>
        public Theme EnsureAllLibraryThemeProvidersProvided()
        {
            var libraryThemeProvidersWhichDidNotProvideLibraryTheme = ThemeManager.Current.LibraryThemeProviders.Except(this.LibraryThemes.Select(x => x.LibraryThemeProvider));

            foreach (var libraryThemeProvider in libraryThemeProvidersWhichDidNotProvideLibraryTheme)
            {
                var libraryTheme = libraryThemeProvider?.ProvideMissingLibraryTheme(this);

                if (libraryTheme is null)
                {
                    continue;
                }

                this.AddLibraryTheme(libraryTheme);
            }

            return this;
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
                foreach (var libraryThemeResource in libraryTheme.Resources.MergedDictionaries)
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

            if (libraryTheme.ParentTheme is not null)
            {
                throw new ArgumentException("The theme already has a parent.");
            }

            if (this.LibraryThemesInternal.Contains(libraryTheme) == false)
            {
                this.LibraryThemesInternal.Add(libraryTheme);
                libraryTheme.ParentTheme = this;

                this.Resources.MergedDictionaries.Add(libraryTheme.Resources);
            }

            return this;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"DisplayName={this.DisplayName}, Name={this.Name}, IsHighContrast={this.IsHighContrast}, IsRuntimeGenerated={this.IsRuntimeGenerated}";
        }

        public static string? GetThemeName([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (IsThemeDictionary(resourceDictionary) == false)
            {
                return null;
            }

            return ResourceDictionaryHelper.GetValueFromKey(resourceDictionary, ThemeNameKey) as string;
        }

        public static Theme? GetThemeInstance([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (IsThemeDictionary(resourceDictionary) == false)
            {
                return null;
            }

            return ResourceDictionaryHelper.GetValueFromKey(resourceDictionary, ThemeInstanceKey) as Theme;
        }

        public static bool IsThemeDictionary(ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            var source = resourceDictionary.Source;
            if (source is not null)
            {
                if (ThemeDictionaryCache.TryGetValue(source, out var existingValue))
                {
                    return existingValue;
                }
            }

            // We are not allowed to use other methods like GetThemeInstance or GetThemeName here as that would cause an endless-loop
            var result = ResourceDictionaryHelper.ContainsKey(resourceDictionary, ThemeInstanceKey)
                         || string.IsNullOrEmpty(ResourceDictionaryHelper.GetValueFromKey(resourceDictionary, ThemeNameKey) as string) == false;

            if (source is not null)
            {
                ThemeDictionaryCache[source] = result;
            }

            return result;
        }

        public static bool IsRuntimeGeneratedThemeDictionary(ResourceDictionary resourceDictionary)
        {
            if (IsThemeDictionary(resourceDictionary))
            {
                return (ResourceDictionaryHelper.ContainsKey(resourceDictionary, ThemeInstanceKey) && ((Theme)resourceDictionary[ThemeInstanceKey]).IsRuntimeGenerated)
                        || (ResourceDictionaryHelper.ContainsKey(resourceDictionary, ThemeIsRuntimeGeneratedKey) && (bool)resourceDictionary[ThemeIsRuntimeGeneratedKey]);
            }

            return false;
        }
    }
}