namespace ControlzEx.Tests.Theming
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using ControlzEx.Tests.TestClasses;
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class ThemeManagerTest
    {
        private ThemeManager testThemeManager;

        [SetUp]
        public void SetUp()
        {
            this.testThemeManager = new ThemeManager();

            this.testThemeManager.RegisterLibraryThemeProvider(TestLibraryThemeProvider.DefaultInstance);
        }

        [Test]
        public void ChangeThemeForAppShouldThrowArgumentNullException()
        {
            Assert.That(() => this.testThemeManager.ChangeTheme((Application)null, this.testThemeManager.GetTheme("Light.Red")), Throws.ArgumentNullException.With.Message.Contains("app"));
            Assert.That(() => this.testThemeManager.ChangeTheme(Application.Current, this.testThemeManager.GetTheme("UnknownTheme")), Throws.ArgumentNullException.With.Message.Contains("newTheme"));
        }

        [Test]
        public void ChangeThemeForWindowShouldThrowArgumentNullException()
        {
            using (var window = new TestWindow())
            {
                Assert.Throws<ArgumentNullException>(() => this.testThemeManager.ChangeTheme((Window)null, this.testThemeManager.GetTheme("Light.Red")));
                Assert.Throws<ArgumentNullException>(() => this.testThemeManager.ChangeTheme(Application.Current.MainWindow, this.testThemeManager.GetTheme("UnknownTheme")));
            }
        }

        [Test]
        public void CanAddThemeBeforeGetterIsCalled()
        {
            {
                var source = new Uri("pack://application:,,,/ControlzEx.Tests;component/Themes/Themes/Dark.Cobalt.xaml");
                var newTheme = new Theme(new LibraryTheme(source, null));
                Assert.That(this.testThemeManager.AddTheme(newTheme), Is.Not.EqualTo(newTheme));
            }

            {
                var resource = new ResourceDictionary
                {
                    {
                        Theme.ThemeNameKey, "Runtime"
                    },
                    {
                        Theme.ThemeDisplayNameKey, "Runtime"
                    },
                    {
                        Theme.ThemePrimaryAccentColorKey, Colors.Blue
                    },
                    {
                        Theme.ThemeIsRuntimeGeneratedKey, true
                    }
                };
                var newTheme = new Theme(new LibraryTheme(resource, null));
                Assert.That(this.testThemeManager.AddTheme(newTheme), Is.EqualTo(newTheme));
            }
        }

        [Test]
        public void NewThemeAddsNewBaseColorAndColorScheme()
        {
            var resource = new ResourceDictionary
                           {
                               {
                                   Theme.ThemeNameKey, "Runtime"
                               },
                               {
                                   Theme.ThemeDisplayNameKey, "Runtime"
                               },
                               {
                                    Theme.ThemeBaseColorSchemeKey, "Foo"
                               },
                               {
                                   Theme.ThemeColorSchemeKey, "Bar"
                               },
                               {
                                   Theme.ThemePrimaryAccentColorKey, Colors.Blue
                               },
                               {
                                   Theme.ThemeIsRuntimeGeneratedKey, true
                               }
                           };

            var newTheme = new Theme(new LibraryTheme(resource, null));
            Assert.That(this.testThemeManager.AddTheme(newTheme), Is.EqualTo(newTheme));
            Assert.That(this.testThemeManager.BaseColors, Is.EqualTo(new[] { ThemeManager.BaseColorLight, ThemeManager.BaseColorDark, "Foo" }));
            Assert.That(this.testThemeManager.ColorSchemes, Does.Contain("Bar"));
        }

        [Test]
        public void ChangingAppThemeChangesWindowTheme()
        {
            using (var window = new TestWindow())
            {
                var expectedTheme = this.testThemeManager.GetTheme("Dark.Teal");
                this.testThemeManager.ChangeTheme(Application.Current, expectedTheme);

                Assert.That(this.testThemeManager.DetectTheme(Application.Current), Is.EqualTo(expectedTheme));
                Assert.That(this.testThemeManager.DetectTheme(window), Is.EqualTo(expectedTheme));
            }
        }

        [Test]
        public void ChangeBaseColor()
        {
            this.testThemeManager.ChangeTheme(Application.Current, this.testThemeManager.Themes.First());

            {
                var currentTheme = this.testThemeManager.DetectTheme(Application.Current);

                Assert.That(currentTheme, Is.Not.Null);
                this.testThemeManager.ChangeThemeBaseColor(Application.Current, this.testThemeManager.GetInverseTheme(currentTheme).BaseColorScheme);

                Assert.That(this.testThemeManager.DetectTheme(Application.Current).BaseColorScheme, Is.Not.EqualTo(currentTheme.BaseColorScheme));
                Assert.That(this.testThemeManager.DetectTheme(Application.Current).ColorScheme, Is.EqualTo(currentTheme.ColorScheme));
            }

            {
                using (var window = new TestWindow())
                {
                    var currentTheme = this.testThemeManager.DetectTheme(window);

                    Assert.That(currentTheme, Is.Not.Null);
                    this.testThemeManager.ChangeThemeBaseColor(window, this.testThemeManager.GetInverseTheme(currentTheme).BaseColorScheme);

                    Assert.That(this.testThemeManager.DetectTheme(window).BaseColorScheme, Is.Not.EqualTo(currentTheme.BaseColorScheme));
                    Assert.That(this.testThemeManager.DetectTheme(window).ColorScheme, Is.EqualTo(currentTheme.ColorScheme));
                }
            }

            {
                var currentTheme = this.testThemeManager.DetectTheme(Application.Current);

                Assert.That(currentTheme, Is.Not.Null);

                var control = new Control();
                this.testThemeManager.ChangeThemeBaseColor(control, control.Resources, currentTheme, this.testThemeManager.GetInverseTheme(currentTheme).BaseColorScheme);

                Assert.That(this.testThemeManager.DetectTheme(control.Resources).BaseColorScheme, Is.Not.EqualTo(currentTheme.BaseColorScheme));
                Assert.That(this.testThemeManager.DetectTheme(control.Resources).ColorScheme, Is.EqualTo(currentTheme.ColorScheme));
            }
        }

        [Test]
        public void ChangeColorScheme()
        {
            this.testThemeManager.ChangeTheme(Application.Current, this.testThemeManager.Themes.First());

            {
                var currentTheme = this.testThemeManager.DetectTheme(Application.Current);

                Assert.That(currentTheme, Is.Not.Null);
                this.testThemeManager.ChangeThemeColorScheme(Application.Current, "Yellow");

                Assert.That(this.testThemeManager.DetectTheme(Application.Current).BaseColorScheme, Is.EqualTo(currentTheme.BaseColorScheme));
                Assert.That(this.testThemeManager.DetectTheme(Application.Current).ColorScheme, Is.EqualTo("Yellow"));
            }

            {
                using (var window = new TestWindow())
                {
                    var currentTheme = this.testThemeManager.DetectTheme(window);

                    Assert.That(currentTheme, Is.Not.Null);
                    this.testThemeManager.ChangeThemeColorScheme(window, "Green");

                    Assert.That(this.testThemeManager.DetectTheme(window).BaseColorScheme, Is.EqualTo(currentTheme.BaseColorScheme));
                    Assert.That(this.testThemeManager.DetectTheme(window).ColorScheme, Is.EqualTo("Green"));
                }
            }

            {
                var currentTheme = this.testThemeManager.DetectTheme(Application.Current);

                Assert.That(currentTheme, Is.Not.Null);

                var control = new Control();
                this.testThemeManager.ChangeThemeColorScheme(control, control.Resources, currentTheme, "Red");

                Assert.That(this.testThemeManager.DetectTheme(control.Resources).BaseColorScheme, Is.EqualTo(currentTheme.BaseColorScheme));
                Assert.That(this.testThemeManager.DetectTheme(control.Resources).ColorScheme, Is.EqualTo("Red"));
            }

            Assert.That(this.testThemeManager.DetectTheme(Application.Current).ColorScheme, Is.EqualTo("Yellow"));
        }

        [Test]
        public void ChangeBaseColorAndColorScheme()
        {
            this.testThemeManager.ChangeTheme(Application.Current, this.testThemeManager.Themes.First());

            {
                var currentTheme = this.testThemeManager.DetectTheme(Application.Current);

                Assert.That(currentTheme, Is.Not.Null);
                this.testThemeManager.ChangeTheme(Application.Current, ThemeManager.BaseColorDark, "Yellow");

                Assert.That(this.testThemeManager.DetectTheme(Application.Current).BaseColorScheme, Is.EqualTo(ThemeManager.BaseColorDark));
                Assert.That(this.testThemeManager.DetectTheme(Application.Current).ColorScheme, Is.EqualTo("Yellow"));
            }

            {
                using (var window = new TestWindow())
                {
                    var currentTheme = this.testThemeManager.DetectTheme(window);

                    Assert.That(currentTheme, Is.Not.Null);
                    this.testThemeManager.ChangeTheme(window, ThemeManager.BaseColorLight, "Green");

                    Assert.That(this.testThemeManager.DetectTheme(window).BaseColorScheme, Is.EqualTo(ThemeManager.BaseColorLight));
                    Assert.That(this.testThemeManager.DetectTheme(window).ColorScheme, Is.EqualTo("Green"));
                }
            }

            {
                var currentTheme = this.testThemeManager.DetectTheme(Application.Current);

                Assert.That(currentTheme, Is.Not.Null);

                var control = new Control();
                this.testThemeManager.ChangeTheme(control, control.Resources, currentTheme, ThemeManager.BaseColorDark, "Red");

                Assert.That(this.testThemeManager.DetectTheme(control.Resources).BaseColorScheme, Is.EqualTo(ThemeManager.BaseColorDark));
                Assert.That(this.testThemeManager.DetectTheme(control.Resources).ColorScheme, Is.EqualTo("Red"));
            }

            Assert.That(this.testThemeManager.DetectTheme(Application.Current).ColorScheme, Is.EqualTo("Yellow"));
        }

        [Test]
        public void GetInverseThemeReturnsDarkTheme()
        {
            var theme = this.testThemeManager.GetInverseTheme(this.testThemeManager.GetTheme("Light.Blue"));

            Assert.That(theme.Name, Is.EqualTo("Dark.Blue"));
        }

        [Test]
        public void GetInverseThemeReturnsLightTheme()
        {
            var theme = this.testThemeManager.GetInverseTheme(this.testThemeManager.GetTheme("Dark.Blue"));

            Assert.That(theme.Name, Is.EqualTo("Light.Blue"));
        }

        [Test]
        public void GetInverseThemeReturnsNullForMissingTheme()
        {
            var resource = new ResourceDictionary
                           {
                               {
                                   "Theme.Name", "Runtime"
                               },
                               {
                                   "Theme.DisplayName", "Runtime"
                               },
                               {
                                   Theme.ThemePrimaryAccentColorKey, Colors.Blue
                               },
                               {
                                   Theme.ThemeIsRuntimeGeneratedKey, true
                               }
                           };
            var theme = new Theme(new LibraryTheme(resource, null));

            var inverseTheme = this.testThemeManager.GetInverseTheme(theme);

            Assert.Null(inverseTheme);
        }

        [Test]
        [TestCase(ThemeManager.BaseColorLightConst, ThemeManager.BaseColorDarkConst)]
        [TestCase(ThemeManager.BaseColorDarkConst, ThemeManager.BaseColorLightConst)]
        public void GetInverseThemeReturnsInverseThemeForRuntimeGeneratedTheme(string baseColor, string inverseBaseColor)
        {
            var resource = new ResourceDictionary
            {
                {
                    Theme.ThemeNameKey, "Runtime"
                },
                {
                    Theme.ThemeDisplayNameKey, "Runtime"
                },
                {
                    Theme.ThemeBaseColorSchemeKey, baseColor
                },
                {
                    Theme.ThemePrimaryAccentColorKey, Colors.Blue
                },
                {
                    Theme.ThemeIsRuntimeGeneratedKey, true
                }
            };
            var theme = new Theme(new LibraryTheme(resource, null));

            var inverseTheme = this.testThemeManager.GetInverseTheme(theme);

            Assert.AreEqual(inverseTheme.BaseColorScheme, inverseBaseColor);
        }

        [Test]
        public void GetThemeIsCaseInsensitive()
        {
            var theme = this.testThemeManager.GetTheme("Dark.Blue");

            Assert.NotNull(theme);
            Assert.That(theme.GetAllResources().First().Source.ToString(), Is.EqualTo("pack://application:,,,/ControlzEx.Tests;component/Themes/Themes/Dark.Blue.xaml").IgnoreCase);
        }

        [Test]
        public void GetThemeWithUriIsCaseInsensitive()
        {
            var dic = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/ControlzEx.Tests;component/Themes/Themes/daRK.Blue.xaml")
            };

            var theme = this.testThemeManager.GetTheme(dic);

            Assert.NotNull(theme);
            Assert.That(theme.Name, Is.EqualTo("Dark.Blue"));
        }

        [Test]
        public void GetThemeWithEmptyThemeManager()
        {
            var themeManager = new ThemeManager();
            var theme = themeManager.GetTheme("Test");

            Assert.That(theme, Is.Null);
        }

        [Test]
        public void HighContrastScenarios()
        {
            var themeManager = new ThemeManager();

            {
                var resource = new ResourceDictionary
                {
                    {
                        Theme.ThemeNameKey, "Theme 1"
                    },
                    {
                        Theme.ThemeDisplayNameKey, "Theme 1"
                    },
                    {
                        Theme.ThemeBaseColorSchemeKey, ThemeManager.BaseColorDark
                    },
                    {
                        Theme.ThemeColorSchemeKey, "Bar"
                    },
                    {
                        Theme.ThemePrimaryAccentColorKey, Colors.Blue
                    },
                    {
                        Theme.ThemeIsHighContrastKey, false
                    }
                };

                var newTheme = new Theme(new LibraryTheme(resource, null));

                themeManager.AddTheme(newTheme);
            }

            {
                var resource = new ResourceDictionary
                {
                    {
                        Theme.ThemeNameKey, "Theme 2"
                    },
                    {
                        Theme.ThemeDisplayNameKey, "Theme 2"
                    },
                    {
                        Theme.ThemeBaseColorSchemeKey, ThemeManager.BaseColorLight
                    },
                    {
                        Theme.ThemeColorSchemeKey, "Bar"
                    },
                    {
                        Theme.ThemePrimaryAccentColorKey, Colors.Blue
                    },
                    {
                        Theme.ThemeIsHighContrastKey, false
                    }
                };

                var newTheme = new Theme(new LibraryTheme(resource, null));

                themeManager.AddTheme(newTheme);
            }

            {
                var resource = new ResourceDictionary
                {
                    {
                        Theme.ThemeNameKey, "Theme 1"
                    },
                    {
                        Theme.ThemeDisplayNameKey, "Theme 1"
                    },
                    {
                        Theme.ThemeBaseColorSchemeKey, ThemeManager.BaseColorDark
                    },
                    {
                        Theme.ThemeColorSchemeKey, "Bar"
                    },
                    {
                        Theme.ThemePrimaryAccentColorKey, Colors.Blue
                    },
                    {
                        Theme.ThemeIsHighContrastKey, true
                    }
                };

                var newTheme = new Theme(new LibraryTheme(resource, null));

                themeManager.AddTheme(newTheme);
            }

            {
                var resource = new ResourceDictionary
                {
                    {
                        Theme.ThemeNameKey, "Theme 2"
                    },
                    {
                        Theme.ThemeDisplayNameKey, "Theme 2"
                    },
                    {
                        Theme.ThemeBaseColorSchemeKey, ThemeManager.BaseColorLight
                    },
                    {
                        Theme.ThemeColorSchemeKey, "Bar"
                    },
                    {
                        Theme.ThemePrimaryAccentColorKey, Colors.Blue
                    },
                    {
                        Theme.ThemeIsHighContrastKey, true
                    }
                };

                var newTheme = new Theme(new LibraryTheme(resource, null));

                themeManager.AddTheme(newTheme);
            }

            {
                var theme = themeManager.GetTheme(ThemeManager.BaseColorDark, "Bar");

                Assert.That(theme, Is.Not.Null);
                Assert.That(theme.IsHighContrast, Is.False);
            }

            {
                var theme = themeManager.GetTheme(ThemeManager.BaseColorDark, "Bar", true);

                Assert.That(theme, Is.Not.Null);
                Assert.That(theme.IsHighContrast, Is.True);

                var inverseTheme = themeManager.GetInverseTheme(theme);

                Assert.That(inverseTheme, Is.Not.Null);
                Assert.That(inverseTheme, Is.Not.EqualTo(theme));
                Assert.That(inverseTheme.IsHighContrast, Is.True);
            }

            {
                var frameworkElement = new FrameworkElement();
                var theme = themeManager.GetTheme(ThemeManager.BaseColorDark, "Bar", true);
                var changeTheme = themeManager.ChangeTheme(frameworkElement, theme!);

                Assert.That(changeTheme, Is.EqualTo(theme));

                var changeThemeBaseColor = themeManager.ChangeThemeBaseColor(frameworkElement, ThemeManager.BaseColorLight);

                Assert.That(changeThemeBaseColor, Is.Not.Null);
                Assert.That(changeThemeBaseColor, Is.Not.EqualTo(changeTheme));
                Assert.That(changeThemeBaseColor.IsHighContrast, Is.True);
            }
        }

        [Test]
        public void GetThemes()
        {
            var expectedThemes = new[]
                                 {
                                     "Amber (Dark)",
                                     "Amber (Light)",
                                     "Blue (Dark)",
                                     "Blue (Light)",
                                     "Brown (Dark)",
                                     "Brown (Light)",
                                     "Cobalt (Dark)",
                                     "Cobalt (Light)",
                                     "Crimson (Dark)",
                                     "Crimson (Light)",
                                     "Cyan (Dark)",
                                     "Cyan (Light)",
                                     "Emerald (Dark)",
                                     "Emerald (Light)",
                                     "Green (Dark)",
                                     "Green (Light)",
                                     "Indigo (Dark)",
                                     "Indigo (Light)",
                                     "Lime (Dark)",
                                     "Lime (Light)",
                                     "Magenta (Dark)",
                                     "Magenta (Light)",
                                     "Mauve (Dark)",
                                     "Mauve (Light)",
                                     "Olive (Dark)",
                                     "Olive (Light)",
                                     "Orange (Dark)",
                                     "Orange (Light)",
                                     "Pink (Dark)",
                                     "Pink (Light)",
                                     "Purple (Dark)",
                                     "Purple (Light)",
                                     "Red (Dark)",
                                     "Red (Light)",
                                     "Sienna (Dark)",
                                     "Sienna (Light)",
                                     "Steel (Dark)",
                                     "Steel (Light)",
                                     "Taupe (Dark)",
                                     "Taupe (Light)",
                                     "Teal (Dark)",
                                     "Teal (Light)",
                                     "Violet (Dark)",
                                     "Violet (Light)",
                                     "Yellow (Dark)",
                                     "Yellow (Light)"
                                 };
            Assert.That(CollectionViewSource.GetDefaultView(this.testThemeManager.Themes).Cast<Theme>().Select(x => x.DisplayName).ToList(), Is.EqualTo(expectedThemes));
        }

        [Test]
        public void GetBaseColors()
        {
            this.testThemeManager.ClearThemes();

            Assert.That(this.testThemeManager.BaseColors, Is.Not.Empty);
        }

        [Test]
        public void GetColorSchemes()
        {
            this.testThemeManager.ClearThemes();

            Assert.That(this.testThemeManager.ColorSchemes, Is.Not.Empty);
        }

        [Test]
        public void CreateDynamicThemeWithColor()
        {
            var applicationTheme = this.testThemeManager.DetectTheme(Application.Current);

            this.testThemeManager.ChangeTheme(Application.Current, RuntimeThemeGenerator.Current.GenerateRuntimeTheme(ThemeManager.BaseColorLight, Colors.Red));

            var detected = this.testThemeManager.DetectTheme(Application.Current);
            Assert.NotNull(detected);
            Assert.That(detected.Name, Is.EqualTo("Light.Runtime_#FFFF0000"));

            this.testThemeManager.ChangeTheme(Application.Current, RuntimeThemeGenerator.Current.GenerateRuntimeTheme(ThemeManager.BaseColorDark, Colors.Green));

            detected = this.testThemeManager.DetectTheme(Application.Current);
            Assert.NotNull(detected);
            Assert.That(detected.Name, Is.EqualTo("Dark.Runtime_#FF008000"));

            this.testThemeManager.ChangeTheme(Application.Current, applicationTheme);
        }

        [Test]
        public void CreateDynamicAccentWithColorAndChangeBaseColorScheme()
        {
            var darkRedTheme = this.testThemeManager.AddTheme(RuntimeThemeGenerator.Current.GenerateRuntimeTheme(ThemeManager.BaseColorDark, Colors.Red));
            var lightRedTheme = this.testThemeManager.AddTheme(RuntimeThemeGenerator.Current.GenerateRuntimeTheme(ThemeManager.BaseColorLight, Colors.Red));
            
            this.testThemeManager.ChangeTheme(Application.Current, lightRedTheme);

            var detected = this.testThemeManager.DetectTheme(Application.Current);
            Assert.NotNull(detected);
            Assert.That(detected.ColorScheme, Is.EqualTo(Colors.Red.ToString()));

            {
                var newTheme = this.testThemeManager.ChangeThemeBaseColor(Application.Current, ThemeManager.BaseColorDark);
                Assert.That(newTheme, Is.EqualTo(darkRedTheme));
            }

            {
                var newTheme = this.testThemeManager.ChangeThemeBaseColor(Application.Current, ThemeManager.BaseColorLight);
                Assert.That(newTheme, Is.EqualTo(lightRedTheme));
            }
        }

        [Test]
        [TestCase("pack://application:,,,/ControlzEx.Tests;component/Themes/themes/dark.blue.xaml", "Dark", "#FF2B579A", "#FF086F9E")]
        [TestCase("pack://application:,,,/ControlzEx.Tests;component/Themes/themes/dark.green.xaml", "Dark", "#FF60A917", "#FF477D11")]

        [TestCase("pack://application:,,,/ControlzEx.Tests;component/Themes/themes/Light.blue.xaml", "Light", "#FF2B579A", "#FF086F9E")]
        [TestCase("pack://application:,,,/ControlzEx.Tests;component/Themes/themes/Light.green.xaml", "Light", "#FF60A917", "#FF477D11")]
        public void CompareGeneratedAppStyleWithShipped(string source, string baseColor, string color, string highlightColor)
        {
            var dic = new ResourceDictionary
            {
                Source = new Uri(source)
            };

            var newTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme(baseColor, (Color)ColorConverter.ConvertFromString(color));

            var ignoredKeyValues = new[]
                                   {
                                       Theme.ThemeNameKey,
                                       Theme.ThemeDisplayNameKey,
                                       Theme.ThemeColorSchemeKey,
                                       Theme.ThemeInstanceKey,
                                       Theme.ThemeIsRuntimeGeneratedKey,
                                       LibraryTheme.LibraryThemeInstanceKey,
                                       "ControlzEx.Colors.HighlightColor", // Ignored because it's hand crafted
                                       "ControlzEx.Brushes.HighlightBrush", // Ignored because it's hand crafted,
                                       "Theme.RuntimeThemeColorValues"
                                   };
            CompareResourceDictionaries(dic, newTheme.GetAllResources().First(), ignoredKeyValues);
            CompareResourceDictionaries(newTheme.GetAllResources().First(), dic, ignoredKeyValues);
        }

        private static void CompareResourceDictionaries(ResourceDictionary first, ResourceDictionary second, params string[] ignoredKeyValues)
        {
            foreach (var key in first.Keys)
            {
                if (ignoredKeyValues.Contains(key) == false)
                {
                    if (second.Contains(key) == false)
                    {
                        throw new Exception($"Key \"{key}\" is missing from {second.Source}.");
                    }

                    Assert.That(second[key].ToString(), Is.EqualTo(first[key].ToString()), $"Values for {key} should be equal.");
                }
            }
        }
    }
}