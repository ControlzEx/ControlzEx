namespace ControlzEx.Tests.Theming
{
    using System;
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class LibraryThemeTests
    {
        [Test]
        public void TestConstraint()
        {
            Assert.That(() => new LibraryTheme((ResourceDictionary)null, null), Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("resourceDictionary"));

            Assert.That(() => new LibraryTheme((Uri)null, null), Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("resourceAddress"));
        }

        [Test]
        public void TestRequiredResourcesFromResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary();

            Assert.That(() => new LibraryTheme(resourceDictionary, null), Throws.ArgumentException.With.Message.Contains("Resource key "));

            resourceDictionary.Add(Theme.ThemeNameKey, "gen");

            Assert.That(() => new LibraryTheme(resourceDictionary, null), Throws.ArgumentException.With.Message.Contains("Resource key "));

            resourceDictionary.Add(Theme.ThemeDisplayNameKey, "gen display");

            Assert.That(() => new LibraryTheme(resourceDictionary, null), Throws.ArgumentException.With.Message.Contains("Resource key "));

            resourceDictionary.Add(Theme.ThemePrimaryAccentColorKey, "gen display");

            Assert.That(() => new LibraryTheme(resourceDictionary, null), Throws.ArgumentException.With.Message.Contains("Resource key "));

            resourceDictionary[Theme.ThemePrimaryAccentColorKey] = Colors.Red;

            Assert.That(() => new LibraryTheme(resourceDictionary, null), Throws.Nothing);
        }

        [Test]
        public void TestResources()
        {
            var theme = GenerateValidLibraryTheme();

            var firstResourceDictionary = new ResourceDictionary();
            theme.AddResource(firstResourceDictionary);

            Assert.That(theme.Resources.MergedDictionaries, Has.Count.EqualTo(2));
            Assert.That(theme.Resources.MergedDictionaries, Does.Contain(firstResourceDictionary));

            var secondResourceDictionary = new ResourceDictionary();
            theme.AddResource(secondResourceDictionary);
            Assert.That(theme.Resources.MergedDictionaries, Has.Count.EqualTo(3));
            Assert.That(theme.Resources.MergedDictionaries, Does.Contain(firstResourceDictionary));
            Assert.That(theme.Resources.MergedDictionaries, Does.Contain(secondResourceDictionary));

            Assert.That(() => theme.AddResource(null), Throws.Exception);

            Assert.That(theme.Resources.MergedDictionaries, Has.Count.EqualTo(3));
            Assert.That(theme.Resources.MergedDictionaries, Does.Contain(firstResourceDictionary));
            Assert.That(theme.Resources.MergedDictionaries, Does.Contain(secondResourceDictionary));
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

        [Test]
        public void TestIsRuntimeGeneratedThemeDictionary()
        {
            Assert.That(LibraryTheme.IsRuntimeGeneratedThemeDictionary(new ResourceDictionary()), Is.False);

            Assert.That(LibraryTheme.IsRuntimeGeneratedThemeDictionary(GenerateValidResourceDictionary()), Is.False);

            Assert.That(LibraryTheme.IsRuntimeGeneratedThemeDictionary(GenerateValidRuntimeGeneratedResourceDictionary()), Is.True);
        }

        [Test]
        public void TestFromUri()
        {
            var source = new Uri("pack://application:,,,/ControlzEx.Tests;component/Themes/Themes/Dark.Red.xaml");
            var theme = new LibraryTheme(source, null);

            Assert.That(theme.IsHighContrast, Is.False);
            Assert.That(theme.PrimaryAccentColor, Is.EqualTo(ColorConverter.ConvertFromString("#FFE51400")));
            Assert.That(theme.AlternativeColorScheme, Is.Null);
            Assert.That(theme.ColorScheme, Is.EqualTo("Red"));
            Assert.That(theme.IsRuntimeGenerated, Is.False);
            Assert.That(theme.BaseColorScheme, Is.EqualTo("Dark"));
            Assert.That(theme.DisplayName, Is.EqualTo("Red (Dark)"));
            Assert.That(theme.Name, Is.EqualTo("Dark.Red"));
            Assert.That(theme.LibraryThemeProvider, Is.Null);
            Assert.That(theme.Origin, Is.EqualTo("ControlzEx.Showcase"));
            Assert.That(theme.ParentTheme, Is.Null);
            Assert.That(theme.ShowcaseBrush.ToString(), Is.EqualTo("#FFE51400"));
            Assert.That(theme.Resources, Is.Not.Null);
            Assert.That(theme.Resources[LibraryTheme.LibraryThemeInstanceKey], Is.EqualTo(theme));

            Assert.That(theme.ToString(), Is.EqualTo("DisplayName=Red (Dark), Name=Dark.Red, Origin=ControlzEx.Showcase, IsHighContrast=False"));
        }

        [Test]
        public void TestPropertiesFromResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary
            {
                {
                    Theme.ThemeNameKey, "Generated"
                },
                {
                    Theme.ThemeColorSchemeKey, "Red"
                },
                {
                    Theme.ThemeDisplayNameKey, "Generated DisplayName"
                },
                {
                    Theme.ThemePrimaryAccentColorKey, Colors.Red
                },
                {
                    Theme.ThemeBaseColorSchemeKey, "Custom"
                },
                {
                    Theme.ThemeOriginKey, "UnitTest"
                },
                {
                    Theme.ThemeIsHighContrastKey, true
                },
                {
                    Theme.ThemeShowcaseBrushKey, Brushes.Pink
                },
                {
                    Theme.ThemeIsRuntimeGeneratedKey, true
                },
                {
                    LibraryTheme.LibraryThemeAlternativeColorSchemeKey, "Alternative"
                }
            };
            
            var theme = new LibraryTheme(resourceDictionary, null);

            Assert.That(theme.IsHighContrast, Is.True);
            Assert.That(theme.PrimaryAccentColor, Is.EqualTo(Colors.Red));
            Assert.That(theme.AlternativeColorScheme, Is.EqualTo("Alternative"));
            Assert.That(theme.ColorScheme, Is.EqualTo("Red"));
            Assert.That(theme.IsRuntimeGenerated, Is.True);
            Assert.That(theme.BaseColorScheme, Is.EqualTo("Custom"));
            Assert.That(theme.DisplayName, Is.EqualTo("Generated DisplayName"));
            Assert.That(theme.Name, Is.EqualTo("Generated"));
            Assert.That(theme.LibraryThemeProvider, Is.Null);
            Assert.That(theme.Origin, Is.EqualTo("UnitTest"));
            Assert.That(theme.ParentTheme, Is.Null);
            Assert.That(theme.ShowcaseBrush, Is.EqualTo(Brushes.Pink));
            Assert.That(theme.Resources, Is.Not.Null);
            Assert.That(theme.Resources[LibraryTheme.LibraryThemeInstanceKey], Is.EqualTo(theme));

            Assert.That(theme.ToString(), Is.EqualTo("DisplayName=Generated DisplayName, Name=Generated, Origin=UnitTest, IsHighContrast=True"));
        }

        [Test]
        public void TestMatches()
        {
            {
                var first = GenerateValidLibraryTheme();
                var second = GenerateValidLibraryTheme();

                Assert.That(first.Matches(second), Is.True);
                Assert.That(first.MatchesSecondTry(second), Is.False);
                Assert.That(first.MatchesThirdTry(second), Is.True);

                Assert.That(second.Matches(first), Is.True);
                Assert.That(second.MatchesSecondTry(first), Is.False);
                Assert.That(second.MatchesThirdTry(first), Is.True);
            }

            {
                var firstResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                firstResourceDictionary[Theme.ThemeIsHighContrastKey] = false;
                var first = new LibraryTheme(firstResourceDictionary, null);

                var secondResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                secondResourceDictionary[Theme.ThemeIsHighContrastKey] = true;
                var second = new LibraryTheme(secondResourceDictionary, null);

                Assert.That(first.Matches(second), Is.False);
                Assert.That(first.MatchesSecondTry(second), Is.False);
                Assert.That(first.MatchesThirdTry(second), Is.False);

                Assert.That(second.Matches(first), Is.False);
                Assert.That(second.MatchesSecondTry(first), Is.False);
                Assert.That(second.MatchesThirdTry(first), Is.False);
            }

            {
                var firstResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                firstResourceDictionary[Theme.ThemeIsHighContrastKey] = true;
                var first = new LibraryTheme(firstResourceDictionary, null);

                var secondResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                secondResourceDictionary[Theme.ThemeIsHighContrastKey] = false;
                var second = new LibraryTheme(secondResourceDictionary, null);

                Assert.That(first.Matches(second), Is.False);
                Assert.That(first.MatchesSecondTry(second), Is.False);
                Assert.That(first.MatchesThirdTry(second), Is.False);

                Assert.That(second.Matches(first), Is.False);
                Assert.That(second.MatchesSecondTry(first), Is.False);
                Assert.That(second.MatchesThirdTry(first), Is.False);
            }

            {
                var firstResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                firstResourceDictionary[Theme.ThemeIsHighContrastKey] = true;
                var first = new LibraryTheme(firstResourceDictionary, null);

                var secondResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                secondResourceDictionary[Theme.ThemeIsHighContrastKey] = true;
                var second = new LibraryTheme(secondResourceDictionary, null);

                Assert.That(first.Matches(second), Is.True);
                Assert.That(first.MatchesSecondTry(second), Is.False);
                Assert.That(first.MatchesThirdTry(second), Is.True);

                Assert.That(second.Matches(first), Is.True);
                Assert.That(second.MatchesSecondTry(first), Is.False);
                Assert.That(second.MatchesThirdTry(first), Is.True);
            }

            {
                var firstResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                firstResourceDictionary[Theme.ThemeColorSchemeKey] = "Generated with alternative";
                var first = new LibraryTheme(firstResourceDictionary, null);

                var secondResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                var second = new LibraryTheme(secondResourceDictionary, null);

                Assert.That(first.Matches(second), Is.False);
                Assert.That(first.MatchesSecondTry(second), Is.False);
                Assert.That(first.MatchesThirdTry(second), Is.True);

                Assert.That(second.Matches(first), Is.False);
                Assert.That(second.MatchesSecondTry(first), Is.False);
                Assert.That(second.MatchesThirdTry(first), Is.True);
            }

            {
                var firstResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                firstResourceDictionary[Theme.ThemeColorSchemeKey] = "Generated with alternative";
                var first = new LibraryTheme(firstResourceDictionary, null);

                var secondResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                secondResourceDictionary[Theme.ThemePrimaryAccentColorKey] = Colors.Blue;
                var second = new LibraryTheme(secondResourceDictionary, null);

                Assert.That(first.Matches(second), Is.False);
                Assert.That(first.MatchesSecondTry(second), Is.False);
                Assert.That(first.MatchesThirdTry(second), Is.False);

                Assert.That(second.Matches(first), Is.False);
                Assert.That(second.MatchesSecondTry(first), Is.False);
                Assert.That(second.MatchesThirdTry(first), Is.False);
            }

            {
                var firstResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                firstResourceDictionary[Theme.ThemeColorSchemeKey] = "Generated with alternative";
                var first = new LibraryTheme(firstResourceDictionary, null);

                var secondResourceDictionary = GenerateValidRuntimeGeneratedResourceDictionary();
                secondResourceDictionary[Theme.ThemePrimaryAccentColorKey] = Colors.Blue;
                secondResourceDictionary[LibraryTheme.LibraryThemeAlternativeColorSchemeKey] = "Generated with alternative";
                var second = new LibraryTheme(secondResourceDictionary, null);

                Assert.That(first.Matches(second), Is.False);
                Assert.That(first.MatchesSecondTry(second), Is.False);
                Assert.That(first.MatchesThirdTry(second), Is.False);

                Assert.That(second.Matches(first), Is.False);
                Assert.That(second.MatchesSecondTry(first), Is.True);
                Assert.That(second.MatchesThirdTry(first), Is.False);
            }
        }

        private static ResourceDictionary GenerateValidResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary
            {
                {
                    Theme.ThemeNameKey, "Generated"
                },
                {
                    Theme.ThemeColorSchemeKey, "Red"
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

        private static ResourceDictionary GenerateValidRuntimeGeneratedResourceDictionary()
        {
            var resourceDictionary = GenerateValidResourceDictionary();
            resourceDictionary.Add(Theme.ThemeIsRuntimeGeneratedKey, true);
            return resourceDictionary;
        }

        private static LibraryTheme GenerateValidLibraryTheme()
        {
            var resourceDictionary = GenerateValidResourceDictionary();

            return new LibraryTheme(resourceDictionary, null);
        }
    }
}