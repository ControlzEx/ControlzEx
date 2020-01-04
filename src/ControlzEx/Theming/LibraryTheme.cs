namespace ControlzEx.Theming
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a theme.
    /// </summary>
    [DebuggerDisplay("DisplayName={" + nameof(DisplayName) + "}, Name={" + nameof(Name) + "}, Origin={" + nameof(Origin) + "}")]
    public class LibraryTheme
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="resourceAddress">The URI of the theme ResourceDictionary.</param>
        public LibraryTheme([NotNull] Uri resourceAddress, bool isRuntimeGenerated)
            : this(new ResourceDictionary { Source = resourceAddress }, isRuntimeGenerated)
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
        public LibraryTheme([NotNull] ResourceDictionary resourceDictionary, bool isRuntimeGenerated)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            this.IsRuntimeGenerated = isRuntimeGenerated;

            this.Name = (string)resourceDictionary[Theme.ThemeNameKey];
            this.Origin = (string)resourceDictionary[Theme.ThemeOriginKey];
            this.DisplayName = (string)resourceDictionary[Theme.ThemeDisplayNameKey];
            this.BaseColorScheme = (string)resourceDictionary[Theme.ThemeBaseColorSchemeKey];
            this.ColorScheme = (string)resourceDictionary[Theme.ThemeColorSchemeKey];
            this.ShowcaseBrush = (SolidColorBrush)resourceDictionary[Theme.ThemeShowcaseBrushKey];

            this.AddResource(resourceDictionary);

            this.Resources = new ReadOnlyObservableCollection<ResourceDictionary>(this.ResourcesInternal);
        }

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
        public SolidColorBrush ShowcaseBrush { get; }

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