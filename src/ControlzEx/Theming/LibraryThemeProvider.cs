namespace ControlzEx.Theming
{
#nullable enable
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Windows;
    using System.Windows.Media;

    public abstract class LibraryThemeProvider : DependencyObject
    {
        private readonly Assembly assembly;
        private readonly string assemblyName;
        private readonly string[] resourceNames;

        protected LibraryThemeProvider(bool registerAtThemeManager)
        {
            this.assembly = this.GetType().Assembly;
            this.assemblyName = this.assembly.GetName().Name!;

            this.resourceNames = this.assembly.GetManifestResourceNames();

            if (registerAtThemeManager)
            {
                ThemeManager.RegisterLibraryThemeProvider(this);
            }
        }

        public string GeneratorParametersResourceName { get; protected set; } = "GeneratorParameters.json";

        public string ThemeTemplateResourceName { get; protected set; } = "Theme.Template.xaml";

        public abstract void FillColorSchemeValues(Dictionary<string, string> values, RuntimeThemeColorValues colorValues);

        public virtual string? GetThemeGeneratorParametersContent()
        {
            foreach (var resourceName in this.resourceNames)
            {
                if (this.ResourceNamesMatch(resourceName, this.GeneratorParametersResourceName) == false)
                {
                    continue;
                }

                using (var stream = this.assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream is null)
                    {
                        continue;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        public virtual string? GetThemeTemplateContent()
        {
            foreach (var resourceName in this.resourceNames)
            {
                if (this.ResourceNamesMatch(resourceName, this.ThemeTemplateResourceName) == false)
                {
                    continue;
                }

                using (var stream = this.assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream is null)
                    {
                        continue;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return null;
        }

        public virtual LibraryTheme? GetLibraryTheme(DictionaryEntry dictionaryEntry)
        {
            if (this.IsPotentialThemeResourceDictionary(dictionaryEntry) == false)
            {
                return null;
            }

            var stringKey = dictionaryEntry.Key as string;

            if (string.IsNullOrEmpty(stringKey))
            {
                return null;
            }

            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{this.assemblyName};component/{stringKey!.Replace(".baml", ".xaml")}")
            };

            if (resourceDictionary.MergedDictionaries.Count == 0
                && ThemeManager.IsThemeDictionary(resourceDictionary))
            {
                return new LibraryTheme(resourceDictionary, this, false);
            }

            return null;
        }

        public virtual IEnumerable<LibraryTheme> GetLibraryThemes()
        {
            foreach (var resourceName in this.resourceNames)
            {
                if (resourceName.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                var resourceInfo = this.assembly.GetManifestResourceInfo(resourceName);

                if (resourceInfo is null
                    || resourceInfo.ResourceLocation == ResourceLocation.ContainedInAnotherAssembly)
                {
                    continue;
                }

                var resourceStream = this.assembly.GetManifestResourceStream(resourceName);

                if (resourceStream is null)
                {
                    continue;
                }

                using (var reader = new ResourceReader(resourceStream))
                {
                    foreach (var dictionaryEntry in reader.OfType<DictionaryEntry>())
                    {
                        var theme = this.GetLibraryTheme(dictionaryEntry);

                        if (!(theme is null))
                        {
                            yield return theme;
                        }
                    }
                }
            }
        }

        public virtual LibraryTheme? ProvideMissingLibraryTheme(LibraryTheme libraryThemeToProvideNewLibraryThemeFor)
        {
            return RuntimeThemeGenerator.Current.GenerateRuntimeLibraryTheme(libraryThemeToProvideNewLibraryThemeFor.BaseColorScheme, libraryThemeToProvideNewLibraryThemeFor.PrimaryAccentColor, this);
        }

        protected virtual bool IsPotentialThemeResourceDictionary(DictionaryEntry dictionaryEntry)
        {
            var stringKey = dictionaryEntry.Key as string;
            if (stringKey is null
                || stringKey.IndexOf("/themes/", StringComparison.OrdinalIgnoreCase) == -1
                || stringKey.EndsWith(".baml", StringComparison.OrdinalIgnoreCase) == false
                || stringKey.EndsWith("generic.baml", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        protected virtual bool ResourceNamesMatch(string resourceName, string value)
        {
            if (resourceName.Equals(value, StringComparison.OrdinalIgnoreCase)
                || (resourceName.StartsWith(this.assemblyName, StringComparison.OrdinalIgnoreCase) && resourceName.EndsWith(value, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}