namespace ControlzEx.Theming
{
#nullable enable
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Security;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Threading;
    using JetBrains.Annotations;
    using Microsoft.Win32;

    /// <summary>
    /// A class that allows for the detection and alteration of a theme.
    /// </summary>
    [PublicAPI]
    public class ThemeManager
    {
        /// <summary>
        /// Gets the name for the light base color.
        /// </summary>
        public static readonly string BaseColorLight = "Light";

        /// <summary>
        /// Gets the name for the dark base color.
        /// </summary>
        public static readonly string BaseColorDark = "Dark";

        private static bool isEnsuringThemes;

        private static readonly ObservableCollection<LibraryThemeProvider> libraryThemeProvidersInternal;
        private static readonly ReadOnlyObservableCollection<LibraryThemeProvider> libraryThemeProviders;

        private static readonly ObservableCollection<Theme> themesInternal;
        private static readonly ReadOnlyObservableCollection<Theme> themes;

        private static readonly ObservableCollection<string> baseColorsInternal;
        private static readonly ReadOnlyObservableCollection<string> baseColors;

        private static readonly ObservableCollection<string> colorSchemesInternal;
        private static readonly ReadOnlyObservableCollection<string> colorSchemes;

        static ThemeManager()
        {
            {
                libraryThemeProvidersInternal = new ObservableCollection<LibraryThemeProvider>();
                libraryThemeProviders = new ReadOnlyObservableCollection<LibraryThemeProvider>(libraryThemeProvidersInternal);
            }

            {
                themesInternal = new ObservableCollection<Theme>();
                themes = new ReadOnlyObservableCollection<Theme>(themesInternal);

                var collectionView = CollectionViewSource.GetDefaultView(themes);
                collectionView.SortDescriptions.Add(new SortDescription(nameof(Theme.DisplayName), ListSortDirection.Ascending));

                themesInternal.CollectionChanged += ThemesInternalCollectionChanged;
            }

            {
                baseColorsInternal = new ObservableCollection<string>();
                baseColors = new ReadOnlyObservableCollection<string>(baseColorsInternal);

                var collectionView = CollectionViewSource.GetDefaultView(baseColors);
                collectionView.SortDescriptions.Add(new SortDescription(string.Empty, ListSortDirection.Ascending));
            }

            {
                colorSchemesInternal = new ObservableCollection<string>();
                colorSchemes = new ReadOnlyObservableCollection<string>(colorSchemesInternal);

                var collectionView = CollectionViewSource.GetDefaultView(colorSchemes);
                collectionView.SortDescriptions.Add(new SortDescription(string.Empty, ListSortDirection.Ascending));
            }
        }

        /// <summary>
        /// Gets a list of all library theme providers.
        /// </summary>
        public static ReadOnlyObservableCollection<LibraryThemeProvider> LibraryThemeProviders => libraryThemeProviders;

        /// <summary>
        /// Gets a list of all themes.
        /// </summary>
        public static ReadOnlyObservableCollection<Theme> Themes
        {
            get
            {
                EnsureThemes();

                return themes;
            }
        }

        /// <summary>
        /// Gets a list of all available base colors.
        /// </summary>
        public static ReadOnlyObservableCollection<string> BaseColors
        {
            get
            {
                EnsureThemes();

                return baseColors;
            }
        }

        /// <summary>
        /// Gets a list of all available color schemes.
        /// </summary>
        public static ReadOnlyObservableCollection<string> ColorSchemes
        {
            get
            {
                EnsureThemes();

                return colorSchemes;
            }
        }

        private static void EnsureThemes()
        {
            if (themes.Count > 0
                || isEnsuringThemes)
            {
                return;
            }

            try
            {
                isEnsuringThemes = true;

                foreach (var libraryThemeProvider in libraryThemeProvidersInternal)
                {
                    foreach (var theme in libraryThemeProvider.GetLibraryThemes())
                    {
                        AddLibraryTheme(theme);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("This exception happens because you are maybe running that code out of the scope of a WPF application. Most likely because you are testing your configuration inside a unit test.", e);
            }
            finally
            {
                isEnsuringThemes = false;
            }
        }

        public static void RegisterLibraryThemeProvider([NotNull] LibraryThemeProvider libraryThemeProvider)
        {
            if (libraryThemeProvider is null)
            {
                throw new ArgumentNullException(nameof(libraryThemeProvider));
            }

            if (libraryThemeProvidersInternal.Any(x => x.GetType() == libraryThemeProvider.GetType()))
            {
                return;
            }

            libraryThemeProvidersInternal.Add(libraryThemeProvider);

            themesInternal.Clear();
        }

        private static void ThemesInternalCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var newItem in e.NewItems.OfType<Theme>())
                    {
                        if (baseColorsInternal.Contains(newItem.BaseColorScheme) == false)
                        {
                            baseColorsInternal.Add(newItem.BaseColorScheme);
                        }

                        if (colorSchemesInternal.Contains(newItem.ColorScheme) == false)
                        {
                            colorSchemesInternal.Add(newItem.ColorScheme);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in e.OldItems.OfType<Theme>())
                    {
                        if (themesInternal.Any(x => x.BaseColorScheme == oldItem.BaseColorScheme) == false)
                        {
                            baseColorsInternal.Remove(oldItem.BaseColorScheme);
                        }

                        if (themesInternal.Any(x => x.ColorScheme == oldItem.ColorScheme) == false)
                        {
                            baseColorsInternal.Remove(oldItem.ColorScheme);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    baseColorsInternal.Clear();
                    colorSchemesInternal.Clear();

                    foreach (var theme in themesInternal.GroupBy(x => x.BaseColorScheme).Select(x => x.First()))
                    {
                        baseColorsInternal.Add(theme.BaseColorScheme);
                    }

                    foreach (var theme in themesInternal.GroupBy(x => x.ColorScheme).Select(x => x.First()))
                    {
                        colorSchemesInternal.Add(theme.ColorScheme);
                    }
                    
                    break;
            }
        }

        /// <summary>
        /// Clears the internal themes list.
        /// </summary>
        public static void ClearThemes()
        {
            themesInternal?.Clear();
        }

        ///// <summary>
        ///// Adds an theme.
        ///// </summary>
        ///// <returns>true if the app theme does not exists and can be added.</returns>
        //public static Theme AddTheme([NotNull] Uri resourceAddress)
        //{
        //    var theme = new Theme(resourceAddress);

        //    return AddTheme(theme);
        //}

        ///// <summary>
        ///// Adds an theme.
        ///// </summary>
        ///// <param name="resourceDictionary">The ResourceDictionary of the theme.</param>
        ///// <returns>true if the app theme does not exists and can be added.</returns>
        //public static Theme AddTheme([NotNull] ResourceDictionary resourceDictionary)
        //{
        //    var theme = new Theme(resourceDictionary);

        //    return AddTheme(theme);
        //}

        public static Theme AddLibraryTheme([NotNull] LibraryTheme libraryTheme)
        {
            var theme = GetTheme(libraryTheme.Name);
            if (!(theme is null))
            {
                theme.AddLibraryTheme(libraryTheme);
                return theme;
            }

            theme = new Theme(libraryTheme);

            themesInternal.Add(theme);
            return theme;
        }

        public static Theme AddTheme([NotNull] Theme theme)
        {
            var existingTheme = GetTheme(theme.Name);
            if (!(existingTheme is null))
            {
                return existingTheme;
            }

            themesInternal.Add(theme);
            return theme;
        }

        /// <summary>
        /// Gets the <see cref="Theme"/> with the given name.
        /// </summary>
        /// <returns>The <see cref="Theme"/> or <c>null</c>, if the theme wasn't found</returns>
        public static Theme? GetTheme([NotNull] string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return Themes.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the <see cref="Theme"/> with the given name.
        /// </summary>
        /// <returns>The <see cref="Theme"/> or <c>null</c>, if the theme wasn't found</returns>
        public static Theme? GetTheme([NotNull] string baseColorScheme, [NotNull] string colorScheme)
        {
            if (baseColorScheme is null)
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            if (colorScheme is null)
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            return Themes.FirstOrDefault(x => x.BaseColorScheme == baseColorScheme && x.ColorScheme == colorScheme);
        }

        /// <summary>
        /// Gets the <see cref="Theme"/> with the given resource dictionary.
        /// </summary>
        /// <param name="resourceDictionary"><see cref="ResourceDictionary"/> from which the theme should be retrieved.</param>
        /// <returns>The <see cref="Theme"/> or <c>null</c>, if the theme wasn't found.</returns>
        public static Theme? GetTheme([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            var builtInTheme = Themes.FirstOrDefault(x => x.Name == Theme.GetThemeName(resourceDictionary));
            if (!(builtInTheme is null))
            {
                return builtInTheme;
            }

            // support dynamically created runtime resource dictionaries
            if (IsThemeDictionary(resourceDictionary))
            {
                return new Theme(new LibraryTheme(resourceDictionary, null, true));
            }

            return null;
        }

        /// <summary>
        /// Gets the inverse <see cref="Theme" /> of the given <see cref="Theme"/>.
        /// This method relies on the "Dark" or "Light" affix to be present.
        /// </summary>
        /// <param name="theme">The app theme.</param>
        /// <returns>The inverse <see cref="Theme"/> or <c>null</c> if it couldn't be found.</returns>
        /// <remarks>
        /// Returns BaseLight, if BaseDark is given or vice versa.
        /// Custom Themes must end with "Dark" or "Light" for this to work, for example "CustomDark" and "CustomLight".
        /// </remarks>
        public static Theme? GetInverseTheme([NotNull] Theme theme)
        {
            if (theme is null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            if (theme.BaseColorScheme == BaseColorDark)
            {
                return GetTheme(BaseColorLight, theme.ColorScheme);
            }

            if (theme.BaseColorScheme == BaseColorLight)
            {
                return GetTheme(BaseColorDark, theme.ColorScheme);
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified resource dictionary represents an <see cref="Theme"/>.
        /// <para />
        /// This might include runtime themes which do not have a resource uri.
        /// </summary>
        /// <param name="resourceDictionary">The resources.</param>
        /// <returns><c>true</c> if the resource dictionary is an <see cref="Theme"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">resources</exception>
        public static bool IsThemeDictionary([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            return Theme.IsThemeDictionary(resourceDictionary);
        }

        /// <summary>
        /// Change the theme for the whole application.
        /// </summary>
        [SecurityCritical]
        public static Theme ChangeTheme([NotNull] Application app, [NotNull] string themeName)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentNullException(nameof(themeName));
            }

            return ChangeTheme(app.Resources, GetTheme(themeName)!);
        }

        /// <summary>
        /// Change theme for the given window.
        /// </summary>
        [SecurityCritical]
        public static Theme ChangeTheme([NotNull] FrameworkElement frameworkElement, [NotNull] string themeName)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentNullException(nameof(themeName));
            }

            return ChangeTheme(frameworkElement, GetTheme(themeName)!);
        }

        /// <summary>
        /// Change theme for the whole application.
        /// </summary>
        /// <param name="app">The instance of Application to change.</param>
        /// <param name="newTheme">The theme to apply.</param>
        [SecurityCritical]
        public static Theme ChangeTheme([NotNull] Application app, [NotNull] Theme newTheme)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            return ChangeTheme(app.Resources, newTheme);
        }

        /// <summary>
        /// Change theme for the given ResourceDictionary.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to change.</param>
        /// <param name="newTheme">The theme to apply.</param>
        [SecurityCritical]
        public static Theme ChangeTheme([NotNull] FrameworkElement frameworkElement, [NotNull] Theme newTheme)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            var oldTheme = DetectTheme(frameworkElement);
            return ChangeTheme(frameworkElement.Resources, oldTheme, newTheme);
        }

        /// <summary>
        /// Change theme for the given ResourceDictionary.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to change.</param>
        /// <param name="newTheme">The theme to apply.</param>
        [SecurityCritical]
        public static Theme ChangeTheme([NotNull] ResourceDictionary resourceDictionary, [NotNull] Theme newTheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            var oldTheme = DetectTheme(resourceDictionary);
            return ChangeTheme(resourceDictionary, oldTheme, newTheme);
        }

        [SecurityCritical]
        private static Theme ChangeTheme([NotNull] ResourceDictionary resourceDictionary, Theme? oldTheme, [NotNull] Theme newTheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            var themeChanged = false;

            if (oldTheme != newTheme)
            {
                resourceDictionary.BeginInit();

                List<ResourceDictionary>? oldThemeResources = null;
                if (!(oldTheme is null))
                {
                    oldThemeResources = resourceDictionary.MergedDictionaries.Where(d => Theme.GetThemeName(d) == oldTheme.Name)
                                                 .ToList();
                }

                {
                    foreach (var themeResource in newTheme.GetAllResources())
                    {
                        // todo: Should we really append the theme resources or try to insert them at a specific index?
                        //       The problem here would be to get the correct index.
                        //       Inserting them at index 0 is not a good idea as user included resources, like generic.xaml, would be behind our resources.
                        //resourceDictionary.MergedDictionaries.Insert(0, themeResource);
                        resourceDictionary.MergedDictionaries.Add(themeResource);
                    }
                }

                if (!(oldThemeResources is null))
                {
                    foreach (var themeResource in oldThemeResources!)
                    {
                        resourceDictionary.MergedDictionaries.Remove(themeResource);   
                    }
                }

                themeChanged = true;
                resourceDictionary.EndInit();
            }

            if (themeChanged)
            {
                OnThemeChanged(oldTheme, newTheme);
            }

            return newTheme;
        }

        /// <summary>
        /// Change base color and color scheme of for the given application.
        /// </summary>
        /// <param name="app">The application to modify.</param>
        /// <param name="baseColor">The base color to apply to the ResourceDictionary.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeTheme([NotNull] Application app, [NotNull] string baseColor, [NotNull] string colorScheme)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(baseColor))
            {
                throw new ArgumentNullException(nameof(baseColor));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = DetectTheme(app);

            if (currentTheme is null)
            {
                return null;
            }

            var newTheme = Themes.FirstOrDefault(x => x.BaseColorScheme == baseColor && x.ColorScheme == colorScheme);

            if (newTheme is null)
            {
                Trace.TraceError($"Could not find a theme with base color scheme '{baseColor}' and color scheme '{currentTheme.ColorScheme}'.");
                return null;
            }

            return ChangeTheme(app.Resources, currentTheme, newTheme);
        }

        /// <summary>
        /// Change base color and color scheme of for the given window.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to modify.</param>
        /// <param name="baseColor">The base color to apply to the ResourceDictionary.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeTheme([NotNull] FrameworkElement frameworkElement, [NotNull] string baseColor, [NotNull] string colorScheme)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(baseColor))
            {
                throw new ArgumentNullException(nameof(baseColor));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = DetectTheme(frameworkElement);

            if (currentTheme is null)
            {
                return null;
            }

            var newTheme = Themes.FirstOrDefault(x => x.BaseColorScheme == baseColor && x.ColorScheme == colorScheme);

            if (newTheme is null)
            {
                Trace.TraceError($"Could not find a theme with base color scheme '{baseColor}' and color scheme '{currentTheme.ColorScheme}'.");
                return null;
            }

            return ChangeTheme(frameworkElement.Resources, currentTheme, newTheme);
        }

        /// <summary>
        /// Change base color and color scheme of for the given ResourceDictionary.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="oldTheme">The old/current theme.</param>
        /// <param name="baseColor">The base color to apply to the ResourceDictionary.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeTheme([NotNull] ResourceDictionary resourceDictionary, Theme oldTheme, [NotNull] string baseColor, [NotNull] string colorScheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (string.IsNullOrEmpty(baseColor))
            {
                throw new ArgumentNullException(nameof(baseColor));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var newTheme = Themes.FirstOrDefault(x => x.BaseColorScheme == baseColor && x.ColorScheme == colorScheme);

            if (newTheme is null)
            {
                Trace.TraceError($"Could not find a theme with base color scheme '{baseColor}' and color scheme '{oldTheme.ColorScheme}'.");
                return null;
            }

            return ChangeTheme(resourceDictionary, oldTheme, newTheme);
        }

        /// <summary>
        /// Change base color for the given application.
        /// </summary>
        /// <param name="app">The application to change.</param>
        /// <param name="baseColor">The base color to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeThemeBaseColor([NotNull] Application app, [NotNull] string baseColor)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(baseColor))
            {
                throw new ArgumentNullException(nameof(baseColor));
            }

            var currentTheme = DetectTheme(app);

            if (currentTheme is null)
            {
                return null;
            }

            return ChangeTheme(app.Resources, currentTheme, baseColor, currentTheme.ColorScheme);
        }

        /// <summary>
        /// Change base color for the given window.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to change.</param>
        /// <param name="baseColor">The base color to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeThemeBaseColor([NotNull] FrameworkElement frameworkElement, [NotNull] string baseColor)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(baseColor))
            {
                throw new ArgumentNullException(nameof(baseColor));
            }

            var currentTheme = DetectTheme(frameworkElement);

            if (currentTheme is null)
            {
                return null;
            }

            return ChangeTheme(frameworkElement.Resources, currentTheme, baseColor, currentTheme.ColorScheme);
        }

        /// <summary>
        /// Change base color of for the given ResourceDictionary.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="oldTheme">The old/current theme.</param>
        /// <param name="baseColor">The base color to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeThemeBaseColor([NotNull] ResourceDictionary resourceDictionary, [CanBeNull] Theme oldTheme, [NotNull] string baseColor)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (string.IsNullOrEmpty(baseColor))
            {
                throw new ArgumentNullException(nameof(baseColor));
            }

            var currentTheme = oldTheme ?? DetectTheme(resourceDictionary);

            if (currentTheme is null)
            {
                return null;
            }

            return ChangeTheme(resourceDictionary, currentTheme, baseColor, currentTheme.ColorScheme);
        }

        /// <summary>
        /// Change color scheme for the given application.
        /// </summary>
        /// <param name="app">The application to change.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeThemeColorScheme([NotNull] Application app, [NotNull] string colorScheme)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = DetectTheme(app);

            if (currentTheme is null)
            {
                return null;
            }

            return ChangeTheme(app.Resources, currentTheme, currentTheme.BaseColorScheme, colorScheme);
        }

        /// <summary>
        /// Change color scheme for the given window.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to change.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeThemeColorScheme([NotNull] FrameworkElement frameworkElement, [NotNull] string colorScheme)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = DetectTheme(frameworkElement);

            if (currentTheme is null)
            {
                return null;
            }

            return ChangeTheme(frameworkElement.Resources, currentTheme, currentTheme.BaseColorScheme, colorScheme);
        }

        /// <summary>
        /// Change color scheme for the given ResourceDictionary.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="oldTheme">The old/current theme.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static Theme? ChangeThemeColorScheme([NotNull] ResourceDictionary resourceDictionary, [CanBeNull] Theme oldTheme, [NotNull] string colorScheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = oldTheme ?? DetectTheme(resourceDictionary);

            if (currentTheme is null)
            {
                return null;
            }

            return ChangeTheme(resourceDictionary, currentTheme, currentTheme.BaseColorScheme, colorScheme);
        }

        /// <summary>
        /// Changes the theme of a ResourceDictionary directly.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="newTheme">The theme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public static void ApplyThemeResourcesFromTheme([NotNull] ResourceDictionary resourceDictionary, [NotNull] Theme newTheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            foreach (var themeResources in newTheme.GetAllResources())
            {
                ApplyResourceDictionary(resourceDictionary, themeResources);   
            }
        }

        [SecurityCritical]
        // ReSharper disable once SuggestBaseTypeForParameter
        private static void ApplyResourceDictionary([NotNull] ResourceDictionary oldRd, [NotNull] ResourceDictionary newRd)
        {
            if (oldRd is null)
            {
                throw new ArgumentNullException(nameof(oldRd));
            }

            if (newRd is null)
            {
                throw new ArgumentNullException(nameof(newRd));
            }

            oldRd.BeginInit();

            foreach (var r in newRd.OfType<DictionaryEntry>())
            {
                if (oldRd.Contains(r.Key))
                {
                    oldRd.Remove(r.Key);
                }

                oldRd.Add(r.Key, r.Value);
            }

            oldRd.EndInit();
        }

        /// <summary>
        /// Scans the resources and returns it's theme.
        /// </summary>
        /// <remarks>If the theme can't be detected from the <see cref="Application.MainWindow"/> we try to detect it from <see cref="Application.Current"/>.</remarks>
        public static Theme? DetectTheme()
        {
            var mainWindow = Application.Current?.MainWindow;

            if (!(mainWindow is null))
            {
                try
                {
                    var style = DetectTheme(mainWindow!);

                    if (!(style is null))
                    {
                        return style;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failed to detect app style on main window.{Environment.NewLine}{ex}");
                }
            }

            return Application.Current != null
                ? DetectTheme(Application.Current)
                : null;
        }

        /// <summary>
        /// Scans the application resources and returns it's theme.
        /// </summary>
        /// <param name="app">The Application instance to scan.</param>
        public static Theme? DetectTheme([NotNull] Application app)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return DetectTheme(app.Resources);
        }

        /// <summary>
        /// Scans the resources and returns it's theme.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to scan.</param>
        /// <remarks>If the theme can't be detected from the <paramref name="frameworkElement"/> we try to detect it from <see cref="Application.Current"/>.</remarks>
        public static Theme? DetectTheme([NotNull] FrameworkElement frameworkElement)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            var detectedStyle = DetectTheme(frameworkElement.Resources)
                                ?? (Application.Current != null
                                    ? DetectTheme(Application.Current.Resources)
                                    : null);

            return detectedStyle;
        }

        /// <summary>
        /// Scans a resources and returns it's theme.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to scan.</param>
        public static Theme? DetectTheme([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (DetectThemeFromResources(resourceDictionary, out var currentTheme))
            {
                return currentTheme;
            }

            return null;
        }

        private static bool DetectThemeFromResources(ResourceDictionary dict, out Theme? detectedTheme)
        {
            using (var enumerator = dict.MergedDictionaries.Reverse().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var currentRd = enumerator.Current;

                    if (currentRd is null)
                    {
                        continue;
                    }

                    Theme? matched;
                    if (!((matched = GetTheme(currentRd)) is null))
                    {
                        detectedTheme = matched;
                        return true;
                    }

                    if (DetectThemeFromResources(currentRd, out detectedTheme))
                    {
                        return true;
                    }
                }
            }

            detectedTheme = null;
            return false;
        }

        /// <summary>
        /// This event fires if the theme was changed
        /// this should be using the weak event pattern, but for now it's enough
        /// </summary>
        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Invalidates global colors and resources.
        /// Sometimes the ContextMenu is not changing the colors, so this will fix it.
        /// </summary>
        [SecurityCritical]
        private static void OnThemeChanged(Theme? oldTheme, Theme newTheme)
        {
            ThemeChanged?.Invoke(Application.Current, new ThemeChangedEventArgs(oldTheme, newTheme));
        }

        private static bool AreResourceDictionarySourcesEqual(ResourceDictionary first, ResourceDictionary second)
        {
            if (first is null
                || second is null)
            {
                return false;
            }

            // If RD does not have a source, but both have keys and the first one has at least as many keys as the second,
            // then compares their values.
            if ((first.Source is null
                || second.Source is null)
                && first.Keys.Count > 0
                && second.Keys.Count > 0
                && first.Keys.Count >= second.Keys.Count)
            {
                try
                {
                    foreach (var key in first.Keys)
                    {
                        var isTheSame = second.Contains(key)
                                        && Equals(first[key], second[key]);
                        if (!isTheSame)
                        {
                            return false;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Trace.TraceError($"Could not compare resource dictionaries: {exception} {Environment.NewLine} {exception.StackTrace}");
                    return false;
                }

                return true;
            }

            return Uri.Compare(first.Source, second.Source, UriComponents.Host | UriComponents.Path, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
        }

        #region WindowsAppModeSetting

        /// <summary>
        /// Synchronizes the current <see cref="Theme"/> with the "app mode" setting from windows.
        /// </summary>
        public static void SyncThemeBaseColorWithWindowsAppModeSetting()
        {
            if (Application.Current is null)
            {
                return;
            }

            var baseColor = WindowsThemeHelper.GetWindowsBaseColor();

            if (ThemeSyncMode.HasFlag(ThemeSyncMode.SyncWithAccent))
            {
                SyncThemeColorSchemeWithWindowsAccentColor(baseColor);
            }
            else
            {
                ChangeThemeBaseColor(Application.Current, baseColor);   
            }
        }

        public static void SyncThemeColorSchemeWithWindowsAccentColor(string? baseColor = null)
        {
            if (Application.Current is null)
            {
                return;
            }

            var accentColor = WindowsThemeHelper.GetWindowsAccentColor();

            if (accentColor is null)
            {
                return;
            }

            var detectedTheme = DetectTheme();

            if (baseColor == null
                && ThemeSyncMode.HasFlag(ThemeSyncMode.SyncWithAppMode))
            {
                baseColor = WindowsThemeHelper.GetWindowsBaseColor();
            }
            else
            {
                baseColor ??= detectedTheme?.BaseColorScheme ?? BaseColorLight;
            }

            var accentColorAsString = accentColor.ToString();

            // Check if we previously generated a theme matching the desired settings
            var theme = GetTheme(baseColor, accentColorAsString!);

            if (theme is null)
            {
                theme = RuntimeThemeGenerator.GenerateRuntimeThemeFromWindowsSettings(baseColor, libraryThemeProvidersInternal);
            }

            // Only change the theme if it's not the current already
            if (!(theme is null)
                && theme != detectedTheme)
            {
                ChangeTheme(Application.Current, theme);
            }
        }

        private static ThemeSyncMode themeSyncMode;

        public static ThemeSyncMode ThemeSyncMode
        {
            get => themeSyncMode;

            set
            {
                if (value == themeSyncMode)
                {
                    return;
                }

                themeSyncMode = value;

                // Always remove handler first.
                // That way we prevent double registrations.
                SystemEvents.UserPreferenceChanged -= HandleUserPreferenceChanged;

                if (themeSyncMode != ThemeSyncMode.DoNotSync)
                {
                    SystemEvents.UserPreferenceChanged += HandleUserPreferenceChanged;
                }
            }
        }

        private static void HandleUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General
                && isSyncScheduled == false)
            {
                isSyncScheduled = true;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => ScheduledThemeSync(ThemeSyncMode)));
            }
        }

        private static bool isSyncScheduled;

        private static void ScheduledThemeSync(ThemeSyncMode themeSyncMode)
        {
            try
            {
                switch (themeSyncMode)
                {
                    case ThemeSyncMode.SyncWithAppMode:
                        SyncThemeBaseColorWithWindowsAppModeSetting();
                        break;

                    case ThemeSyncMode.SyncWithAccent:
                        SyncThemeColorSchemeWithWindowsAccentColor();
                        break;

                    case ThemeSyncMode.SyncAll:
                        SyncThemeColorSchemeWithWindowsAccentColor(WindowsThemeHelper.GetWindowsBaseColor());
                        break;
                }
            }
            finally
            {
                isSyncScheduled = false;
            }
        }

        #endregion WindowsAppModeSetting
    }

    [Flags]
    public enum ThemeSyncMode
    {
        DoNotSync = 1 << 0,

        /// <summary>
        /// Gets or sets whether changes to the "app mode" setting from windows should be detected at runtime and the current <see cref="Theme"/> be changed accordingly.
        /// </summary>
        SyncWithAppMode = 1 << 2,

        /// <summary>
        /// Gets or sets whether changes to the "app mode" setting from windows should be detected at runtime and the current <see cref="Theme"/> be changed accordingly.
        /// </summary>
        SyncWithAccent = 1 << 3,

        SyncAll = SyncWithAppMode | SyncWithAccent
    }
}