#nullable enable
namespace ControlzEx.Theming
{
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
    using System.Windows.Media;
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
        public static readonly string BaseColorLight = BaseColorLightConst;

        /// <summary>
        /// Gets the name for the light base color.
        /// </summary>
        public const string BaseColorLightConst = "Light";

        /// <summary>
        /// Gets the name for the dark base color.
        /// </summary>
        public static readonly string BaseColorDark = BaseColorDarkConst;

        /// <summary>
        /// Gets the name for the dark base color.
        /// </summary>
        public const string BaseColorDarkConst = "Dark";

        private bool isEnsuringThemesOrRegisteringProvider;

        private readonly ObservableCollection<LibraryThemeProvider> libraryThemeProvidersInternal;

        private readonly ObservableCollection<Theme> themesInternal;
        private readonly ReadOnlyObservableCollection<Theme> themes;

        private readonly ObservableCollection<string> baseColorsInternal;
        private readonly ReadOnlyObservableCollection<string> baseColors;

        private readonly ObservableCollection<string> colorSchemesInternal;
        private readonly ReadOnlyObservableCollection<string> colorSchemes;

        public static ThemeManager Current { get; set; }

        static ThemeManager()
        {
            Current = new ThemeManager();
        }

        public ThemeManager()
        {
            {
                this.libraryThemeProvidersInternal = new ObservableCollection<LibraryThemeProvider>();
                this.LibraryThemeProviders = new ReadOnlyObservableCollection<LibraryThemeProvider>(this.libraryThemeProvidersInternal);
            }

            {
                this.themesInternal = new ObservableCollection<Theme>();
                this.themes = new ReadOnlyObservableCollection<Theme>(this.themesInternal);

                var collectionView = CollectionViewSource.GetDefaultView(this.themes);
                collectionView.SortDescriptions.Add(new SortDescription(nameof(Theme.DisplayName), ListSortDirection.Ascending));

                this.themesInternal.CollectionChanged += this.ThemesInternalCollectionChanged;
            }

            {
                this.baseColorsInternal = new ObservableCollection<string>();
                this.baseColors = new ReadOnlyObservableCollection<string>(this.baseColorsInternal);

                var collectionView = CollectionViewSource.GetDefaultView(this.baseColors);
                collectionView.SortDescriptions.Add(new SortDescription(string.Empty, ListSortDirection.Ascending));
            }

            {
                this.colorSchemesInternal = new ObservableCollection<string>();
                this.colorSchemes = new ReadOnlyObservableCollection<string>(this.colorSchemesInternal);

                var collectionView = CollectionViewSource.GetDefaultView(this.colorSchemes);
                collectionView.SortDescriptions.Add(new SortDescription(string.Empty, ListSortDirection.Ascending));
            }
        }

        /// <summary>
        /// Gets a list of all library theme providers.
        /// </summary>
        public ReadOnlyObservableCollection<LibraryThemeProvider> LibraryThemeProviders { get; }

        /// <summary>
        /// Gets a list of all themes.
        /// </summary>
        public ReadOnlyObservableCollection<Theme> Themes
        {
            get
            {
                this.EnsureThemes();

                return this.themes;
            }
        }

        /// <summary>
        /// Gets a list of all available base colors.
        /// </summary>
        public ReadOnlyObservableCollection<string> BaseColors
        {
            get
            {
                this.EnsureThemes();

                return this.baseColors;
            }
        }

        /// <summary>
        /// Gets a list of all available color schemes.
        /// </summary>
        public ReadOnlyObservableCollection<string> ColorSchemes
        {
            get
            {
                this.EnsureThemes();

                return this.colorSchemes;
            }
        }

        private void EnsureThemes()
        {
            if (this.themes.Count > 0
                || this.isEnsuringThemesOrRegisteringProvider)
            {
                return;
            }

            try
            {
                this.isEnsuringThemesOrRegisteringProvider = true;

                foreach (var libraryThemeProvider in this.libraryThemeProvidersInternal)
                {
                    foreach (var libraryTheme in libraryThemeProvider.GetLibraryThemes())
                    {
                        this.AddLibraryTheme(libraryTheme);
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("This exception happens because you are maybe running that code out of the scope of a WPF application. Most likely because you are testing your configuration inside a unit test.", e);
            }
            finally
            {
                this.isEnsuringThemesOrRegisteringProvider = false;
            }
        }

        public void RegisterLibraryThemeProvider([NotNull] LibraryThemeProvider libraryThemeProvider)
        {
            if (libraryThemeProvider is null)
            {
                throw new ArgumentNullException(nameof(libraryThemeProvider));
            }

            if (this.libraryThemeProvidersInternal.Any(x => x.GetType() == libraryThemeProvider.GetType()))
            {
                return;
            }

            this.libraryThemeProvidersInternal.Add(libraryThemeProvider);

            try
            {
                this.isEnsuringThemesOrRegisteringProvider = true;

                foreach (var libraryTheme in libraryThemeProvider.GetLibraryThemes())
                {
                    this.AddLibraryTheme(libraryTheme);
                }
            }
            finally
            {
                this.isEnsuringThemesOrRegisteringProvider = false;
            }
        }

#if NET5_0_OR_GREATER
        private void ThemesInternalCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
#else
        private void ThemesInternalCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
#endif
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems is not null)
                    {
                        foreach (var newItem in e.NewItems.OfType<Theme>())
                        {
                            if (this.baseColorsInternal.Contains(newItem.BaseColorScheme) == false)
                            {
                                this.baseColorsInternal.Add(newItem.BaseColorScheme);
                            }

                            if (this.colorSchemesInternal.Contains(newItem.ColorScheme) == false)
                            {
                                this.colorSchemesInternal.Add(newItem.ColorScheme);
                            }
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is not null)
                    {
                        foreach (var oldItem in e.OldItems.OfType<Theme>())
                        {
                            if (this.themesInternal.Any(x => x.BaseColorScheme == oldItem.BaseColorScheme) == false)
                            {
                                this.baseColorsInternal.Remove(oldItem.BaseColorScheme);
                            }

                            if (this.themesInternal.Any(x => x.ColorScheme == oldItem.ColorScheme) == false)
                            {
                                this.baseColorsInternal.Remove(oldItem.ColorScheme);
                            }
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.baseColorsInternal.Clear();
                    this.colorSchemesInternal.Clear();

                    foreach (var theme in this.themesInternal.GroupBy(x => x.BaseColorScheme).Select(x => x.First()))
                    {
                        this.baseColorsInternal.Add(theme.BaseColorScheme);
                    }

                    foreach (var theme in this.themesInternal.GroupBy(x => x.ColorScheme).Select(x => x.First()))
                    {
                        this.colorSchemesInternal.Add(theme.ColorScheme);
                    }

                    break;
            }
        }

        /// <summary>
        /// Clears the internal themes list.
        /// </summary>
        public void ClearThemes()
        {
            this.themesInternal.Clear();
        }

        public Theme AddLibraryTheme([NotNull] LibraryTheme libraryTheme)
        {
            var theme = this.GetTheme(libraryTheme.Name, libraryTheme.IsHighContrast);
            if (theme is not null)
            {
                theme.AddLibraryTheme(libraryTheme);
                return theme;
            }

            theme = new Theme(libraryTheme);

            this.themesInternal.Add(theme);
            return theme;
        }

        public Theme AddTheme([NotNull] Theme theme)
        {
            var existingTheme = this.GetTheme(theme.Name, theme.IsHighContrast);
            if (existingTheme is not null)
            {
                return existingTheme;
            }

            this.themesInternal.Add(theme);
            return theme;
        }

        /// <summary>
        /// Gets the <see cref="Theme"/> with the given name.
        /// </summary>
        /// <returns>The <see cref="Theme"/> or <c>null</c>, if the theme wasn't found</returns>
        public Theme? GetTheme([NotNull] string name, bool highContrast = false)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return this.Themes.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.IsHighContrast == highContrast);
        }

        /// <summary>
        /// Gets the <see cref="Theme"/> with the given name.
        /// </summary>
        /// <returns>The <see cref="Theme"/> or <c>null</c>, if the theme wasn't found</returns>
        public Theme? GetTheme([NotNull] string baseColorScheme, [NotNull] string colorScheme, bool highContrast = false)
        {
            if (baseColorScheme is null)
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            if (colorScheme is null)
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var theme = this.Themes.FirstOrDefault(x => x.BaseColorScheme == baseColorScheme && x.ColorScheme == colorScheme && x.IsHighContrast == highContrast);

            if (theme is null
                && highContrast)
            {
                theme = this.Themes.FirstOrDefault(x => x.BaseColorScheme == baseColorScheme && x.ColorScheme == "Generic" && x.IsHighContrast == highContrast)
                        ?? this.Themes.FirstOrDefault(x => x.BaseColorScheme == baseColorScheme && x.IsHighContrast == highContrast);
            }

            return theme;
        }

        /// <summary>
        /// Gets the <see cref="Theme"/> with the given resource dictionary.
        /// </summary>
        /// <param name="resourceDictionary"><see cref="ResourceDictionary"/> from which the theme should be retrieved.</param>
        /// <returns>The <see cref="Theme"/> or <c>null</c>, if the theme wasn't found.</returns>
        public Theme? GetTheme([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            var themeInstance = Theme.GetThemeInstance(resourceDictionary);

            if (themeInstance is not null)
            {
                return themeInstance;
            }

            var builtInTheme = this.Themes.FirstOrDefault(x => x.Name == Theme.GetThemeName(resourceDictionary));
            if (builtInTheme is not null)
            {
                return builtInTheme;
            }

            // support dynamically created runtime resource dictionaries
            if (this.IsRuntimeGeneratedThemeDictionary(resourceDictionary))
            {
                foreach (var resourceDictionaryKey in resourceDictionary.Keys)
                {
                    if (Theme.ThemeInstanceKey.Equals(resourceDictionaryKey))
                    {
                        return (Theme)resourceDictionary[resourceDictionaryKey];
                    }
                }

                foreach (var resourceDictionaryKey in resourceDictionary.Keys)
                {
                    if (LibraryTheme.LibraryThemeInstanceKey.Equals(resourceDictionaryKey))
                    {
                        var parentTheme = ((LibraryTheme)resourceDictionary[resourceDictionaryKey]).ParentTheme;

                        if (parentTheme is not null)
                        {
                            return parentTheme;
                        }
                    }
                }

                return new Theme(new LibraryTheme(resourceDictionary, null));
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
        public Theme? GetInverseTheme([NotNull] Theme theme)
        {
            if (theme is null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            if (theme.IsRuntimeGenerated)
            {
                switch (theme.BaseColorScheme)
                {
                    case BaseColorDarkConst:
                        return RuntimeThemeGenerator.Current.GenerateRuntimeTheme(BaseColorLight, theme.PrimaryAccentColor, theme.IsHighContrast);

                    case BaseColorLightConst:
                        return RuntimeThemeGenerator.Current.GenerateRuntimeTheme(BaseColorDark, theme.PrimaryAccentColor, theme.IsHighContrast);
                }
            }
            else
            {
                switch (theme.BaseColorScheme)
                {
                    case BaseColorDarkConst:
                        return this.GetTheme(BaseColorLight, theme.ColorScheme, theme.IsHighContrast);

                    case BaseColorLightConst:
                        return this.GetTheme(BaseColorDark, theme.ColorScheme, theme.IsHighContrast);
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified resource dictionary represents a <see cref="Theme"/>.
        /// <para />
        /// This might include runtime themes which do not have a resource uri.
        /// </summary>
        /// <param name="resourceDictionary">The resources.</param>
        /// <returns><c>true</c> if the resource dictionary is an <see cref="Theme"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">resources</exception>
        public bool IsThemeDictionary([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.IsThemeDictionary(resourceDictionary);
        }

        /// <summary>
        /// Determines whether the specified resource dictionary represents a <see cref="Theme"/> and was generated at runtime.
        /// <para />
        /// This might include runtime themes which do not have a resource uri.
        /// </summary>
        /// <param name="resourceDictionary">The resources.</param>
        /// <returns><c>true</c> if the resource dictionary is an <see cref="Theme"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">resources</exception>
        public bool IsRuntimeGeneratedThemeDictionary([NotNull] ResourceDictionary resourceDictionary)
        {
            return Theme.IsRuntimeGeneratedThemeDictionary(resourceDictionary);
        }

        /// <summary>
        /// Change the theme for the whole application.
        /// </summary>
        [SecurityCritical]
        public Theme ChangeTheme([NotNull] Application app, [NotNull] string themeName, bool highContrast = false)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentNullException(nameof(themeName));
            }

            var theme = this.GetTheme(themeName, highContrast);

            if (theme is null)
            {
                throw new ArgumentException($"Could not find a theme matching \"{themeName}\" and high contrast = \"{highContrast}\".");
            }

            return this.ChangeTheme(app, app.Resources, theme);
        }

        /// <summary>
        /// Change theme for the given window.
        /// </summary>
        [SecurityCritical]
        public Theme ChangeTheme([NotNull] FrameworkElement frameworkElement, [NotNull] string themeName, bool highContrast = false)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(themeName))
            {
                throw new ArgumentNullException(nameof(themeName));
            }

            var theme = this.GetTheme(themeName, highContrast);

            if (theme is null)
            {
                throw new ArgumentException($"Could not find a theme matching \"{themeName}\" and high contrast = \"{highContrast}\".");
            }

            return this.ChangeTheme(frameworkElement, theme);
        }

        /// <summary>
        /// Change theme for the whole application.
        /// </summary>
        /// <param name="app">The instance of Application to change.</param>
        /// <param name="newTheme">The theme to apply.</param>
        [SecurityCritical]
        public Theme ChangeTheme([NotNull] Application app, [NotNull] Theme newTheme)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            return this.ChangeTheme(app, app.Resources, newTheme);
        }

        /// <summary>
        /// Change theme for the given ResourceDictionary.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to change.</param>
        /// <param name="newTheme">The theme to apply.</param>
        [SecurityCritical]
        public Theme ChangeTheme([NotNull] FrameworkElement frameworkElement, [NotNull] Theme newTheme)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            var oldTheme = this.DetectTheme(frameworkElement);
            return this.ChangeTheme(frameworkElement, frameworkElement.Resources, oldTheme, newTheme);
        }

        /// <summary>
        /// Change theme for the given ResourceDictionary.
        /// </summary>
        /// <param name="target">The target object for which the theme change should be made. This is optional an can be <c>null</c>.</param>
        /// <param name="resourceDictionary">The ResourceDictionary to change.</param>
        /// <param name="newTheme">The theme to apply.</param>
        [SecurityCritical]
        public Theme ChangeTheme(object? target, [NotNull] ResourceDictionary resourceDictionary, [NotNull] Theme newTheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            var oldTheme = this.DetectTheme(resourceDictionary);
            return this.ChangeTheme(target, resourceDictionary, oldTheme, newTheme);
        }

        [SecurityCritical]
        private Theme ChangeTheme(object? target, [NotNull] ResourceDictionary resourceDictionary, Theme? oldTheme, [NotNull] Theme newTheme)
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

                try
                {
                    ResourceDictionary? oldThemeDictionary = null;
                    List<ResourceDictionary>? oldThemeResources = null;

                    if (oldTheme is not null)
                    {
                        oldThemeDictionary = resourceDictionary.MergedDictionaries.FirstOrDefault(d => Theme.GetThemeInstance(d) == oldTheme);

                        if (oldThemeDictionary is null)
                        {
                            oldThemeResources = resourceDictionary.MergedDictionaries.Where(d => Theme.GetThemeName(d) == oldTheme.Name)
                                                                  .ToList();
                        }
                    }

                    {
                        newTheme.EnsureAllLibraryThemeProvidersProvided();
                        resourceDictionary.MergedDictionaries.Add(newTheme.Resources);

                        //foreach (var themeResource in newTheme.GetAllResources())
                        //{
                        //    // todo: Should we really append the theme resources or try to insert them at a specific index?
                        //    //       The problem here would be to get the correct index.
                        //    //       Inserting them at index 0 is not a good idea as user included resources, like generic.xaml, would be behind our resources.
                        //    //resourceDictionary.MergedDictionaries.Insert(0, themeResource);
                        //    resourceDictionary.MergedDictionaries.Add(themeResource);
                        //}
                    }

                    if (oldThemeDictionary is not null)
                    {
                        resourceDictionary.MergedDictionaries.Remove(oldThemeDictionary);
                    }
                    else if (oldThemeResources is not null)
                    {
                        foreach (var themeResource in oldThemeResources)
                        {
                            resourceDictionary.MergedDictionaries.Remove(themeResource);
                        }
                    }

                    themeChanged = true;
                }
                finally
                {
                    resourceDictionary.EndInit();
                }
            }

            if (themeChanged)
            {
                this.OnThemeChanged(target, resourceDictionary, oldTheme, newTheme);
            }

            return newTheme;
        }

        /// <summary>
        /// Change base color and color scheme of for the given application.
        /// </summary>
        /// <param name="app">The application to modify.</param>
        /// <param name="baseColorScheme">The base color to apply to the ResourceDictionary.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeTheme([NotNull] Application app, [NotNull] string baseColorScheme, [NotNull] string colorScheme)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(baseColorScheme))
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = this.DetectTheme(app);

            if (currentTheme is null)
            {
                return null;
            }

            return this.ChangeTheme(app, app.Resources, currentTheme, baseColorScheme, colorScheme);
        }

        /// <summary>
        /// Change base color and color scheme of for the given window.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to modify.</param>
        /// <param name="baseColorScheme">The base color to apply to the ResourceDictionary.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeTheme([NotNull] FrameworkElement frameworkElement, [NotNull] string baseColorScheme, [NotNull] string colorScheme)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(baseColorScheme))
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = this.DetectTheme(frameworkElement);

            if (currentTheme is null)
            {
                return null;
            }

            return this.ChangeTheme(frameworkElement, frameworkElement.Resources, currentTheme, baseColorScheme, colorScheme);
        }

        /// <summary>
        /// Change base color and color scheme of for the given ResourceDictionary.
        /// </summary>
        /// <param name="target">The target object for which the theme change should be made. This is optional an can be <c>null</c>.</param>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="oldTheme">The old/current theme.</param>
        /// <param name="baseColorScheme">The base color to apply to the ResourceDictionary.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeTheme(object? target, [NotNull] ResourceDictionary resourceDictionary, Theme oldTheme, [NotNull] string baseColorScheme, [NotNull] string colorScheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (string.IsNullOrEmpty(baseColorScheme))
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var newTheme = this.GetTheme(baseColorScheme, colorScheme, oldTheme.IsHighContrast);

            if (newTheme is null)
            {
                Trace.TraceError($"Could not find a theme with base color scheme '{baseColorScheme}', color scheme '{colorScheme}' and high contrast equals {oldTheme.IsHighContrast}.");
                return null;
            }

            return this.ChangeTheme(target, resourceDictionary, oldTheme, newTheme);
        }

        /// <summary>
        /// Change base color for the given application.
        /// </summary>
        /// <param name="app">The application to change.</param>
        /// <param name="baseColorScheme">The base color to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeThemeBaseColor([NotNull] Application app, [NotNull] string baseColorScheme)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(baseColorScheme))
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            var currentTheme = this.DetectTheme(app);

            if (currentTheme is null)
            {
                return null;
            }

            return this.ChangeThemeBaseColor(app, app.Resources, currentTheme, baseColorScheme);
        }

        /// <summary>
        /// Change base color for the given window.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to change.</param>
        /// <param name="baseColorScheme">The base color to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeThemeBaseColor([NotNull] FrameworkElement frameworkElement, [NotNull] string baseColorScheme)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(baseColorScheme))
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            var currentTheme = this.DetectTheme(frameworkElement);

            if (currentTheme is null)
            {
                return null;
            }

            return this.ChangeThemeBaseColor(frameworkElement, frameworkElement.Resources, currentTheme, baseColorScheme);
        }

        /// <summary>
        /// Change base color of for the given ResourceDictionary.
        /// </summary>
        /// <param name="target">The target object for which the theme change should be made. This is optional an can be <c>null</c>.</param>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="oldTheme">The old/current theme.</param>
        /// <param name="baseColorScheme">The base color to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeThemeBaseColor(object? target, [NotNull] ResourceDictionary resourceDictionary, Theme? oldTheme, [NotNull] string baseColorScheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (string.IsNullOrEmpty(baseColorScheme))
            {
                throw new ArgumentNullException(nameof(baseColorScheme));
            }

            var currentTheme = oldTheme ?? this.DetectTheme(resourceDictionary);

            if (currentTheme is null)
            {
                return null;
            }

            var newTheme = this.ChangeTheme(target, resourceDictionary, currentTheme, baseColorScheme, currentTheme.ColorScheme);

            if (newTheme is null
                && currentTheme.IsRuntimeGenerated)
            {
                var runtimeTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme(baseColorScheme, currentTheme.PrimaryAccentColor, currentTheme.IsHighContrast);

                if (runtimeTheme is not null)
                {
                    newTheme = this.ChangeTheme(target, resourceDictionary, currentTheme, runtimeTheme);
                }
            }

            return newTheme;
        }

        /// <summary>
        /// Change color scheme for the given application.
        /// </summary>
        /// <param name="app">The application to change.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeThemeColorScheme([NotNull] Application app, [NotNull] string colorScheme)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = this.DetectTheme(app);

            if (currentTheme is null)
            {
                return null;
            }

            return this.ChangeThemeColorScheme(app, app.Resources, currentTheme, colorScheme);
        }

        /// <summary>
        /// Change color scheme for the given window.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to change.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeThemeColorScheme([NotNull] FrameworkElement frameworkElement, [NotNull] string colorScheme)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = this.DetectTheme(frameworkElement);

            if (currentTheme is null)
            {
                return null;
            }

            return this.ChangeThemeColorScheme(frameworkElement, frameworkElement.Resources, currentTheme, colorScheme);
        }

        /// <summary>
        /// Change color scheme for the given ResourceDictionary.
        /// </summary>
        /// <param name="target">The target object for which the theme change should be made. This is optional an can be <c>null</c>.</param>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="oldTheme">The old/current theme.</param>
        /// <param name="colorScheme">The color scheme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public Theme? ChangeThemeColorScheme(object? target, [NotNull] ResourceDictionary resourceDictionary, Theme? oldTheme, [NotNull] string colorScheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (string.IsNullOrEmpty(colorScheme))
            {
                throw new ArgumentNullException(nameof(colorScheme));
            }

            var currentTheme = oldTheme ?? this.DetectTheme(resourceDictionary);

            if (currentTheme is null)
            {
                return null;
            }

            var newTheme = this.ChangeTheme(target, resourceDictionary, currentTheme, currentTheme.BaseColorScheme, colorScheme);

            if (newTheme is null
                && currentTheme.IsRuntimeGenerated
                && TryConvertColorFromString(colorScheme, out var color))
            {
                var runtimeTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme(currentTheme.BaseColorScheme, color, currentTheme.IsHighContrast);

                if (runtimeTheme is not null)
                {
                    newTheme = this.ChangeTheme(target, resourceDictionary, currentTheme, runtimeTheme);
                }
            }

            return newTheme;
        }

        /// <summary>
        /// Changes the theme of a ResourceDictionary directly.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to modify.</param>
        /// <param name="newTheme">The theme to apply to the ResourceDictionary.</param>
        [SecurityCritical]
        public void ApplyThemeResourcesFromTheme([NotNull] ResourceDictionary resourceDictionary, [NotNull] Theme newTheme)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (newTheme is null)
            {
                throw new ArgumentNullException(nameof(newTheme));
            }

            newTheme.EnsureAllLibraryThemeProvidersProvided();

            this.ApplyResourceDictionary(resourceDictionary, newTheme.Resources);
        }

        [SecurityCritical]
        // ReSharper disable once SuggestBaseTypeForParameter
        private void ApplyResourceDictionary([NotNull] ResourceDictionary oldRd, [NotNull] ResourceDictionary newRd)
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

            this.ApplyResourceDictionaryEntries(oldRd, newRd);

            oldRd.EndInit();
        }

        private void ApplyResourceDictionaryEntries([NotNull] ResourceDictionary oldRd, [NotNull] ResourceDictionary newRd)
        {
            foreach (var newRdMergedDictionary in newRd.MergedDictionaries)
            {
                this.ApplyResourceDictionaryEntries(oldRd, newRdMergedDictionary);
            }

#pragma warning disable CS8605
            foreach (DictionaryEntry dictionaryEntry in newRd)
            {
                if (oldRd.Contains(dictionaryEntry.Key))
                {
                    oldRd.Remove(dictionaryEntry.Key);
                }

                oldRd.Add(dictionaryEntry.Key, dictionaryEntry.Value);
            }
#pragma warning restore CS8605
        }

        /// <summary>
        /// Scans the resources and returns it's theme.
        /// </summary>
        /// <remarks>If the theme can't be detected from the <see cref="Application.MainWindow"/> we try to detect it from <see cref="Application.Current"/>.</remarks>
        public Theme? DetectTheme()
        {
            if (Application.Current is null)
            {
                return null;
            }

            var theme = this.DetectTheme(Application.Current);

            if (theme is null
                && Application.Current.MainWindow is not null)
            {
                try
                {
                    theme = this.DetectTheme(Application.Current.MainWindow);

                    if (theme is not null)
                    {
                        return theme;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Failed to detect app style on main window.{Environment.NewLine}{ex}");
                }
            }

            return theme;
        }

        /// <summary>
        /// Scans the application resources and returns it's theme.
        /// </summary>
        /// <param name="app">The Application instance to scan.</param>
        public Theme? DetectTheme([NotNull] Application app)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return this.DetectTheme(app.Resources);
        }

        /// <summary>
        /// Scans the resources and returns it's theme.
        /// </summary>
        /// <param name="frameworkElement">The FrameworkElement to scan.</param>
        /// <remarks>If the theme can't be detected from the <paramref name="frameworkElement"/> we try to detect it from <see cref="Application.Current"/>.</remarks>
        public Theme? DetectTheme([NotNull] FrameworkElement frameworkElement)
        {
            if (frameworkElement is null)
            {
                throw new ArgumentNullException(nameof(frameworkElement));
            }

            var detectedStyle = this.DetectTheme(frameworkElement.Resources)
                                ?? (Application.Current is not null
                                    ? this.DetectTheme(Application.Current.Resources)
                                    : null);

            return detectedStyle;
        }

        /// <summary>
        /// Scans a resources and returns it's theme.
        /// </summary>
        /// <param name="resourceDictionary">The ResourceDictionary to scan.</param>
        public Theme? DetectTheme([NotNull] ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary is null)
            {
                throw new ArgumentNullException(nameof(resourceDictionary));
            }

            if (this.DetectThemeFromResources(resourceDictionary, out var currentTheme))
            {
                return currentTheme;
            }

            return null;
        }

        private bool DetectThemeFromResources(ResourceDictionary dict, out Theme? detectedTheme)
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
                    if ((matched = this.GetTheme(currentRd)) is not null)
                    {
                        detectedTheme = matched;
                        return true;
                    }

                    if (this.DetectThemeFromResources(currentRd, out detectedTheme))
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
        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Invalidates global colors and resources.
        /// Sometimes the ContextMenu is not changing the colors, so this will fix it.
        /// </summary>
        [SecurityCritical]
        private void OnThemeChanged(object? target, ResourceDictionary targetResourceDictionary, Theme? oldTheme, Theme newTheme)
        {
            this.ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(target, targetResourceDictionary, oldTheme, newTheme));
        }

        public void SyncTheme()
        {
            this.SyncTheme(this.ThemeSyncMode);
        }

        public void SyncTheme(ThemeSyncMode? syncMode)
        {
            try
            {
                var syncModeToUse = syncMode ?? this.themeSyncMode;

                if (syncModeToUse == ThemeSyncMode.DoNotSync)
                {
                    return;
                }

                if (Application.Current is null)
                {
                    return;
                }

                var detectedTheme = this.DetectTheme();

                string? baseColor = null;
                if (syncModeToUse.HasFlag(ThemeSyncMode.SyncWithAppMode))
                {
                    baseColor = WindowsThemeHelper.GetWindowsBaseColor();
                }
                else
                {
                    baseColor ??= detectedTheme?.BaseColorScheme ?? BaseColorLight;
                }

                string? accentColor;
                if (syncModeToUse.HasFlag(ThemeSyncMode.SyncWithAccent))
                {
                    accentColor = WindowsThemeHelper.GetWindowsAccentColor()?.ToString() ?? detectedTheme?.ColorScheme;
                }
                else
                {
                    // If there was no detected Theme just use the windows accent color.
                    accentColor = detectedTheme?.ColorScheme ?? WindowsThemeHelper.GetWindowsAccentColor()?.ToString();
                }

                if (accentColor is null)
                {
                    throw new Exception("Accent color could not be detected.");
                }

                var isHighContrast = syncModeToUse.HasFlag(ThemeSyncMode.SyncWithHighContrast)
                                     && WindowsThemeHelper.IsHighContrastEnabled();

                // Check if we previously generated a theme matching the desired settings
                var theme = this.GetTheme(baseColor, accentColor, isHighContrast)
                            ?? RuntimeThemeGenerator.Current.GenerateRuntimeThemeFromWindowsSettings(baseColor, isHighContrast, this.libraryThemeProvidersInternal);

                // Only change the theme if it's not the current already
                if (theme is not null
                    && theme != detectedTheme)
                {
                    this.ChangeTheme(Application.Current, theme);
                }
            }
            finally
            {
                this.isSyncScheduled = false;
            }
        }

        #region Windows-Settings

        private ThemeSyncMode themeSyncMode;

        public ThemeSyncMode ThemeSyncMode
        {
            get => this.themeSyncMode;

            set
            {
                if (value == this.themeSyncMode)
                {
                    return;
                }

                this.themeSyncMode = value;

                // Always remove handler first.
                // That way we prevent double registrations.
                SystemEvents.UserPreferenceChanged -= this.HandleUserPreferenceChanged;
                SystemParameters.StaticPropertyChanged -= this.HandleStaticPropertyChanged;

                if (this.themeSyncMode != ThemeSyncMode.DoNotSync)
                {
                    SystemEvents.UserPreferenceChanged += this.HandleUserPreferenceChanged;
                    SystemParameters.StaticPropertyChanged += this.HandleStaticPropertyChanged;
                }
            }
        }

        private void HandleUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General
                && this.isSyncScheduled == false)
            {
                this.isSyncScheduled = true;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.SyncTheme(this.ThemeSyncMode)));
            }
        }

#if NET5_0_OR_GREATER
        private void HandleStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
#else
        private void HandleStaticPropertyChanged(object sender, PropertyChangedEventArgs e)
#endif
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast)
                && this.isSyncScheduled == false)
            {
                this.isSyncScheduled = true;

                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.SyncTheme(this.ThemeSyncMode)));
            }
        }

        private bool isSyncScheduled;

        #endregion WindowsAppModeSetting

        private static bool TryConvertColorFromString(string colorScheme, out Color color)
        {
            try
            {
                var convertedValue = ColorConverter.ConvertFromString(colorScheme);

                if (convertedValue is null)
                {
                    color = default;
                    return false;
                }

                color = (Color)convertedValue;

                return true;
            }
            catch (Exception e)
            {
                Trace.TraceWarning(e.ToString());

                color = default;
                return false;
            }
        }
    }

#pragma warning disable CA1008
    [Flags]
    public enum ThemeSyncMode
    {
        /// <summary>
        /// No synchronization will happen.
        /// </summary>
        DoNotSync = 0,

        /// <summary>
        /// Gets or sets whether changes to the "app mode" setting from windows should be detected at runtime and the current <see cref="Theme"/> be changed accordingly.
        /// </summary>
        SyncWithAppMode = 1 << 2,

        /// <summary>
        /// Gets or sets whether changes to the accent color settings from windows should be detected at runtime and the current <see cref="Theme"/> be changed accordingly.
        /// </summary>
        SyncWithAccent = 1 << 3,

        /// <summary>
        /// Gets or sets whether changes to the high contrast setting from windows should be detected at runtime and the current <see cref="Theme"/> be changed accordingly.
        /// </summary>
        SyncWithHighContrast = 1 << 4,

        /// <summary>
        /// All synchronizations are active.
        /// </summary>
        SyncAll = SyncWithAppMode | SyncWithAccent | SyncWithHighContrast
    }
#pragma warning restore CA1008
}
