namespace ControlzEx.Theming
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents the background theme of the application.
    /// </summary>
    [DebuggerDisplay("DisplayName={" + nameof(DisplayName) + "}, Name={" + nameof(Name) + "}, Sources={" + nameof(Sources) + "}")]
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
        /// <param name="resourceAddress">The URI of the theme ResourceDictionary.</param>
        public Theme([NotNull] Uri resourceAddress, bool isRuntimeGenerated)
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
        public Theme([NotNull] ResourceDictionary resourceDictionary, bool isRuntimeGenerated)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            this.IsRuntimeGenerated = isRuntimeGenerated;

            this.Name = (string)resourceDictionary[ThemeNameKey];
            this.Origin = (string)resourceDictionary[ThemeOriginKey];
            this.DisplayName = (string)resourceDictionary[ThemeDisplayNameKey];
            this.BaseColorScheme = (string)resourceDictionary[ThemeBaseColorSchemeKey];
            this.ColorScheme = (string)resourceDictionary[ThemeColorSchemeKey];
            this.ShowcaseBrush = (SolidColorBrush)resourceDictionary[ThemeShowcaseBrushKey];

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

        public string Sources { get; private set; }

        /// <summary>
        /// Gets a brush which can be used to showcase this theme.
        /// </summary>
        public SolidColorBrush ShowcaseBrush { get; }

        public static string GetThemeName([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            var key = (string)resourceDictionary[ThemeNameKey];

            return key;
        }

        public Theme AddResource([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            var themeName = GetThemeName(resourceDictionary);

            if (themeName != this.Name)
            {
                throw new ArgumentException("The theme key does not match the current theme key.");
            }

            this.ResourcesInternal.Add(resourceDictionary);

            this.Sources = string.Join("; ", this.ResourcesInternal.Select(x => x.Source));

            return this;
        }

        public void AddResource(ReadOnlyObservableCollection<ResourceDictionary> resourceDictionaries)
        {
            foreach (var resourceDictionary in resourceDictionaries)
            {
                this.AddResource(resourceDictionary);
            }
        }

        public static bool IsThemeDictionary(ResourceDictionary resources)
        {
            return string.IsNullOrEmpty(GetThemeName(resources)) == false;
        }
    }
}