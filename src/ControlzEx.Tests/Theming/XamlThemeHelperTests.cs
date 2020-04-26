namespace ControlzEx.Tests.Theming
{
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class XamlThemeHelperTests
    {
        [Test]
        [TestCase("", "")]
        [TestCase(@"xmlns:markup=""clr-namespace:MahApps.Metro.Markup""
        xmlns:markupWithAssembly=""clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro""", 
                  @"xmlns:markup=""clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro""
        xmlns:markupWithAssembly=""clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro""")]
        [TestCase(@"xmlns:markup1=""clr-namespace:MahApps.Metro.Markup""
        xmlns:markupWithAssembly=""clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro""", 
                  @"xmlns:markup1=""clr-namespace:MahApps.Metro.Markup""
        xmlns:markupWithAssembly=""clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro""")]
        [TestCase(@"xmlns:markup=""wrong content""
        xmlns:markupWithAssembly=""correct content""", 
                  @"xmlns:markup=""correct content""
        xmlns:markupWithAssembly=""correct content""")]
        public void TestFixXamlReaderXmlNsIssue(string input, string expected)
        {
            Assert.That(XamlThemeHelper.FixXamlReaderXmlNsIssue(input), Is.EqualTo(expected));
        }
    }
}