#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a theme.
    /// </summary>
    public class LibraryTheme
    {
        /// <summary>
        /// Gets the key for the library theme instance.
        /// </summary>
        public const string LibraryThemeInstanceKey = "LibraryTheme.LibraryThemeInstance";

        /// <summary>
        /// Gets the key for the theme color scheme.
        /// </summary>
        public const string LibraryThemeAlternativeColorSchemeKey = "Theme.AlternativeColorScheme";

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="resourceAddress">The URI of the theme ResourceDictionary.</param>
        /// <param name="libraryThemeProvider">The <see cref="ControlzEx.Theming.LibraryThemeProvider"/> which created this instance.</param>
        /// <param name="isRuntimeGenerated">Defines if the library theme was generated at runtime.</param>
        public LibraryTheme([NotNull] Uri resourceAddress, LibraryThemeProvider? libraryThemeProvider, bool isRuntimeGenerated)
            : this(new ResourceDictionary { Source = resourceAddress }, libraryThemeProvider, isRuntimeGenerated)
        {
            if (resourceAddress == null)
            {
                throw new ArgumentNullException(nameof(resourceAddress));
            }
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary of the theme.</param>
        /// <param name="libraryThemeProvider">The <see cref="ControlzEx.Theming.LibraryThemeProvider"/> which created this instance.</param>
        /// <param name="isRuntimeGenerated">Defines if the library theme was generated at runtime.</param>
        public LibraryTheme([NotNull] ResourceDictionary resourceDictionary, LibraryThemeProvider? libraryThemeProvider, bool isRuntimeGenerated)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            this.LibraryThemeProvider = libraryThemeProvider;

            this.IsRuntimeGenerated = isRuntimeGenerated;

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

        public LibraryThemeProvider? LibraryThemeProvider { get; }

        public bool IsRuntimeGenerated { get; }

        /// <summary>
        /// The root <see cref="System.Windows.ResourceDictionary"/> containing all resource dictionaries belonging to this instance as <see cref="System.Windows.ResourceDictionary.MergedDictionaries"/>
        /// </summary>
        public ResourceDictionary Resources { get; } = new ResourceDictionary();

        public Theme? ParentTheme { get; internal set; }

        /// <summary>
        /// Gets the name of the theme.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get the origin of the theme.
        /// </summary>
        public string? Origin { get; }

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
        /// Gets the alternative color scheme for this theme.
        /// </summary>
        public string AlternativeColorScheme { get; set; }

        /// <summary>
        /// Gets the primary accent color for this theme.
        /// </summary>
        public Color PrimaryAccentColor { get; set; }

        /// <summary>
        /// Gets a brush which can be used to showcase this theme.
        /// </summary>
        public Brush ShowcaseBrush { get; }

        public virtual bool Matches(LibraryTheme libraryTheme)
        {
            return this.BaseColorScheme == libraryTheme.BaseColorScheme
                   && this.ColorScheme == libraryTheme.ColorScheme;
        }

        public virtual bool MatchesSecondTry(LibraryTheme libraryTheme)
        {
            return this.BaseColorScheme == libraryTheme.BaseColorScheme
                   && this.ShowcaseBrush.ToString() == libraryTheme.ShowcaseBrush.ToString();
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
            return $"DisplayName={this.DisplayName}, Name={this.Name}, Origin={this.Origin}";
        }

        public LibraryTheme Clone()
        {
            return new LibraryTheme(this.Resources, this.LibraryThemeProvider, this.IsRuntimeGenerated);
        }

        public static string? GetThemeName([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.GetThemeName(resourceDictionary);
        }

        public static bool IsThemeDictionary([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.IsThemeDictionary(resourceDictionary);
        }
    }
}