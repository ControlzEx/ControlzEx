namespace ControlzEx.Tests.Theming
{
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class ThemeGeneratorTests
    {
        [Test]
        public void Current_Should_Not_Be_Null()
        {
            Assert.That(ThemeGenerator.Current, Is.Not.Null);
        }
    }
}