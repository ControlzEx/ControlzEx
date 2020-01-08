namespace ControlzEx.Theming
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a theme.
    /// </summary>
    public class LibraryTheme
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="resourceAddress">The URI of the theme ResourceDictionary.</param>
        /// <param name="libraryThemeProvider">The <see cref="ControlzEx.Theming.LibraryThemeProvider"/> which created this instance.</param>
        /// <param name="isRuntimeGenerated">Defines if the library theme was generated at runtime.</param>
        public LibraryTheme([NotNull] Uri resourceAddress, [CanBeNull] LibraryThemeProvider libraryThemeProvider, bool isRuntimeGenerated)
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
        public LibraryTheme([NotNull] ResourceDictionary resourceDictionary, [CanBeNull] LibraryThemeProvider libraryThemeProvider, bool isRuntimeGenerated)
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
            this.ShowcaseBrush = (Brush)resourceDictionary[Theme.ThemeShowcaseBrushKey];

            this.AddResource(resourceDictionary);

            this.Resources = new ReadOnlyObservableCollection<ResourceDictionary>(this.ResourcesInternal);
        }

        [CanBeNull]
        public LibraryThemeProvider LibraryThemeProvider { get; }

        public bool IsRuntimeGenerated { get; }

        /// <summary>
        /// The ResourceDictionaries that represent this theme.
        /// </summary>
        public ReadOnlyObservableCollection<ResourceDictionary> Resources { get; }

        /// <summary>
        /// The ResourceDictionaries that represent this theme.
        /// </summary>
        private ObservableCollection<ResourceDictionary> ResourcesInternal { get; } = new ObservableCollection<ResourceDictionary>();

        public Theme ParentTheme { get; internal set; }

        /// <summary>
        /// Gets the name of the theme.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get the origin of the theme.
        /// </summary>
        public string Origin { get; }

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

            this.ResourcesInternal.Add(resourceDictionary);

            return this;
        }

        public void AddResource(ReadOnlyObservableCollection<ResourceDictionary> resourceDictionaries)
        {
            foreach (var resourceDictionary in resourceDictionaries)
            {
                this.AddResource(resourceDictionary);
            }
        }

        public override string ToString()
        {
            return $"DisplayName={this.DisplayName}, Name={this.Name}, Origin={this.Origin}";
        }

        public static string GetThemeName([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.GetThemeName(resourceDictionary);
        }

        public static bool IsThemeDictionary([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.IsThemeDictionary(resourceDictionary);
        }
    }
}