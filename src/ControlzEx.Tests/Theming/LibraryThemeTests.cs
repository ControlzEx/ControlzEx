namespace ControlzEx.Tests.Theming
{
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class LibraryThemeTests
    {
        [Test]
        public void TestRequiredResourcesFromResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary();

            Assert.That(() => new LibraryTheme(resourceDictionary, null, true), Throws.Exception);

            resourceDictionary.Add(Theme.ThemeNameKey, "gen");

            Assert.That(() => new LibraryTheme(resourceDictionary, null, true), Throws.Exception);

            resourceDictionary.Add(Theme.ThemeDisplayNameKey, "gen display");

            Assert.That(() => new LibraryTheme(resourceDictionary, null, true), Throws.Exception);

            resourceDictionary.Add(Theme.ThemePrimaryAccentColorKey, "gen display");

            Assert.That(() => new LibraryTheme(resourceDictionary, null, true), Throws.Exception);

            resourceDictionary[Theme.ThemePrimaryAccentColorKey] = Colors.Red;

            Assert.That(() => new LibraryTheme(resourceDictionary, null, true), Throws.Nothing);
        }

        [Test]
        public void TestResources()
        {
            var librabryTheme = GenerateValidLibraryTheme();

            Assert.That(librabryTheme.Resources, Has.Count.EqualTo(1));

            librabryTheme.AddResource(new ResourceDictionary());

            Assert.That(librabryTheme.Resources, Has.Count.EqualTo(2));
        }

        [Test]
        public void TestGetThemeName()
        {
            Assert.That(LibraryTheme.GetThemeName(new ResourceDictionary()), Is.Null.Or.Empty);

            Assert.That(LibraryTheme.GetThemeName(GenerateValidResourceDictionary()), Is.EqualTo("Generated"));
        }

        [Test]
        public void TestIsThemeDictionary()
        {
            Assert.That(LibraryTheme.IsThemeDictionary(new ResourceDictionary()), Is.False);

            Assert.That(LibraryTheme.IsThemeDictionary(GenerateValidResourceDictionary()), Is.True);
        }

        private static ResourceDictionary GenerateValidResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary
            {
                {
                    Theme.ThemeNameKey, "Generated"
                },
                {
                    Theme.ThemeDisplayNameKey, "Generated DisplayName"
                },
                {
                    Theme.ThemePrimaryAccentColorKey, Colors.Red
                }
            };
            return resourceDictionary;
        }

        private static LibraryTheme GenerateValidLibraryTheme()
        {
            var resourceDictionary = GenerateValidResourceDictionary();

            return new LibraryTheme(resourceDictionary, null, true);
        }
    }
}