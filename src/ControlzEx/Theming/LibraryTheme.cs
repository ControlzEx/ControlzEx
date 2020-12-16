#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Internal;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a theme.
    /// </summary>
    public class LibraryTheme
    {
        /// <summary>
        /// Gets the key for the library theme instance.
        /// </summary>
        public const string LibraryThemeInstanceKey = "Theme.LibraryThemeInstance";

        /// <summary>
        /// Gets the key for the theme color scheme.
        /// </summary>
        public const string LibraryThemeAlternativeColorSchemeKey = "Theme.AlternativeColorScheme";

        /// <summary>
        /// Gets the key for the color values being used to generate a runtime theme.
        /// </summary>
        public const string RuntimeThemeColorValuesKey = "Theme.RuntimeThemeColorValues";

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="resourceAddress">The URI of the theme ResourceDictionary.</param>
        /// <param name="libraryThemeProvider">The <see cref="ControlzEx.Theming.LibraryThemeProvider"/> which created this instance.</param>
        public LibraryTheme([NotNull] Uri resourceAddress, LibraryThemeProvider? libraryThemeProvider)
            : this(CreateResourceDictionary(resourceAddress), libraryThemeProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary of the theme.</param>
        /// <param name="libraryThemeProvider">The <see cref="ControlzEx.Theming.LibraryThemeProvider"/> which created this instance.</param>
        public LibraryTheme([NotNull] ResourceDictionary resourceDictionary, LibraryThemeProvider? libraryThemeProvider)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            this.LibraryThemeProvider = libraryThemeProvider;

            this.IsRuntimeGenerated = (bool)(resourceDictionary[Theme.ThemeIsRuntimeGeneratedKey] ?? false);
            this.IsHighContrast = (bool)(resourceDictionary[Theme.ThemeIsHighContrastKey] ?? false);

            this.Name = (string)resourceDictionary[Theme.ThemeNameKey];
            this.Origin = (string)resourceDictionary[Theme.ThemeOriginKey];
            this.DisplayName = (string)resourceDictionary[Theme.ThemeDisplayNameKey];
            this.BaseColorScheme = (string)resourceDictionary[Theme.ThemeBaseColorSchemeKey];
            this.ColorScheme = (string)resourceDictionary[Theme.ThemeColorSchemeKey];
            this.AlternativeColorScheme = (string)resourceDictionary[LibraryTheme.LibraryThemeAlternativeColorSchemeKey];
            this.PrimaryAccentColor = resourceDictionary[Theme.ThemePrimaryAccentColorKey] as Color? ?? throw new ArgumentException($"Resource key \"{Theme.ThemePrimaryAccentColorKey}\" is missing, is null or is not a color.");
            this.ShowcaseBrush = (Brush)resourceDictionary[Theme.ThemeShowcaseBrushKey] ?? new SolidColorBrush(this.PrimaryAccentColor);

            this.AddResource(resourceDictionary);

            this.Resources[LibraryThemeInstanceKey] = this;
        }

        /// <inheritdoc cref="Theme.IsRuntimeGenerated"/>
        public bool IsRuntimeGenerated { get; }

        /// <inheritdoc cref="Theme.IsHighContrast"/>
        public bool IsHighContrast { get; }

        /// <inheritdoc cref="Theme.Name"/>
        public string Name { get; }

        /// <summary>
        /// Get the origin of the theme.
        /// </summary>
        public string? Origin { get; }

        /// <inheritdoc cref="Theme.DisplayName"/>
        public string DisplayName { get; }

        /// <inheritdoc cref="Theme.BaseColorScheme"/>
        public string BaseColorScheme { get; }

        /// <inheritdoc cref="Theme.ColorScheme"/>
        public string ColorScheme { get; }

        /// <inheritdoc cref="Theme.PrimaryAccentColor"/>
        public Color PrimaryAccentColor { get; set; }

        /// <inheritdoc cref="Theme.ShowcaseBrush"/>
        public Brush ShowcaseBrush { get; }

        /// <summary>
        /// The root <see cref="System.Windows.ResourceDictionary"/> containing all resource dictionaries belonging to this instance as <see cref="System.Windows.ResourceDictionary.MergedDictionaries"/>
        /// </summary>
        public ResourceDictionary Resources { get; } = new ResourceDictionary();

        /// <summary>
        /// Gets the alternative color scheme for this theme.
        /// </summary>
        public string AlternativeColorScheme { get; set; }

        public Theme? ParentTheme { get; internal set; }

        public LibraryThemeProvider? LibraryThemeProvider { get; }

        public virtual bool Matches(LibraryTheme libraryTheme)
        {
            return this.BaseColorScheme == libraryTheme.BaseColorScheme
                   && this.ColorScheme == libraryTheme.ColorScheme
                   && this.IsHighContrast == libraryTheme.IsHighContrast;
        }

        public virtual bool MatchesSecondTry(LibraryTheme libraryTheme)
        {
            return this.BaseColorScheme == libraryTheme.BaseColorScheme
                   && this.AlternativeColorScheme == libraryTheme.ColorScheme
                   && this.IsHighContrast == libraryTheme.IsHighContrast;
        }

        public virtual bool MatchesThirdTry(LibraryTheme libraryTheme)
        {
            return this.BaseColorScheme == libraryTheme.BaseColorScheme
                   && this.ShowcaseBrush.ToString() == libraryTheme.ShowcaseBrush.ToString()
                   && this.IsHighContrast == libraryTheme.IsHighContrast;
        }

        public LibraryTheme AddResource([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            this.Resources.MergedDictionaries.Add(resourceDictionary);

            return this;
        }

        public override string ToString()
        {
            return $"DisplayName={this.DisplayName}, Name={this.Name}, Origin={this.Origin}, IsHighContrast={this.IsHighContrast}";
        }

        public static string? GetThemeName([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.GetThemeName(resourceDictionary);
        }

        public static bool IsThemeDictionary([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.IsThemeDictionary(resourceDictionary)
                || ResourceDictionaryHelper.ContainsKey(resourceDictionary, LibraryThemeInstanceKey);
        }

        public static bool IsRuntimeGeneratedThemeDictionary([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.IsRuntimeGeneratedThemeDictionary(resourceDictionary)
                || (ResourceDictionaryHelper.ContainsKey(resourceDictionary, LibraryThemeInstanceKey) && ((LibraryTheme)resourceDictionary[LibraryThemeInstanceKey]).IsRuntimeGenerated);
        }

        private static ResourceDictionary CreateResourceDictionary(Uri resourceAddress)
        {
            if (resourceAddress is null)
            {
                throw new ArgumentNullException(nameof(resourceAddress));
            }

            return new ResourceDictionary { Source = resourceAddress };
        }
    }
}