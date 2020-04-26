namespace ControlzEx.Tests.Theming
{
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class ThemeChangedEventArgsTests
    {
        [Test]
        public void Test()
        {
            var resourceDictionary = new ResourceDictionary();
            var oldTheme = new Theme(string.Empty, string.Empty, string.Empty, string.Empty, Colors.Blue, Brushes.Blue, true, false);
            var newTheme = new Theme(string.Empty, string.Empty, string.Empty, string.Empty, Colors.Blue, Brushes.Blue, true, false);

            {
                var args = new ThemeChangedEventArgs(null, resourceDictionary, null, newTheme);

                Assert.That(args.Target, Is.Null);
                Assert.That(args.TargetResourceDictionary, Is.EqualTo(resourceDictionary));
                Assert.That(args.OldTheme, Is.Null);
                Assert.That(args.NewTheme, Is.EqualTo(newTheme));
            }

            {
                var args = new ThemeChangedEventArgs(null, resourceDictionary, oldTheme, newTheme);

                Assert.That(args.Target, Is.Null);
                Assert.That(args.TargetResourceDictionary, Is.EqualTo(resourceDictionary));
                Assert.That(args.OldTheme, Is.EqualTo(oldTheme));
                Assert.That(args.NewTheme, Is.EqualTo(newTheme));
            }

            {
                var target = new object();
                var args = new ThemeChangedEventArgs(target, resourceDictionary, oldTheme, newTheme);

                Assert.That(args.Target, Is.EqualTo(target));
                Assert.That(args.TargetResourceDictionary, Is.EqualTo(resourceDictionary));
                Assert.That(args.OldTheme, Is.EqualTo(oldTheme));
                Assert.That(args.NewTheme, Is.EqualTo(newTheme));
            }
        }
    }
}