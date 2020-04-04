namespace ControlzEx.Tests.Theming
{
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class RuntimeThemeGeneratorTests
    {
        [Test]
        public void Current_Should_Not_Be_Null()
        {
            Assert.That(RuntimeThemeGenerator.Current, Is.Not.Null);
        }
    }
}