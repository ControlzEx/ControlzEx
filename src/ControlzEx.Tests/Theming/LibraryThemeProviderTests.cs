namespace ControlzEx.Tests.Theming
{
    using ControlzEx.Tests.TestClasses;
    using NUnit.Framework;

    [TestFixture]
    public class LibraryThemeProviderTests
    {
        [Test]
        public void TestProvideMissingLibraryTheme()
        {
            var provider = new TestLibraryThemeProvider(false);

            Assert.That(provider.GetLibraryThemes(), Is.Not.Empty);
        }
    }
}