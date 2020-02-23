namespace ControlzEx.Tests.Theming
{
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class ThemeTests
    {
        [Test]
        public void TestGetThemeName()
        {
            Assert.That(Theme.GetThemeName(new ResourceDictionary()), Is.Null.Or.Empty);

            Assert.That(Theme.GetThemeName(GenerateValidResourceDictionary()), Is.EqualTo("Generated"));
        }

        [Test]
        public void TestIsThemeDictionary()
        {
            Assert.That(Theme.IsThemeDictionary(new ResourceDictionary()), Is.False);

            Assert.That(Theme.IsThemeDictionary(GenerateValidResourceDictionary()), Is.True);
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
    }
}