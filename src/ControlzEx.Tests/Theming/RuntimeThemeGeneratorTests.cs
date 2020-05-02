namespace ControlzEx.Tests.Theming
{
    using System.Windows.Media;
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

        [Test]
        public void GetColors()
        {
            var generator = new RuntimeThemeGenerator();

            var runtimeThemeColorOptions = new RuntimeThemeGeneratorOptions { UseHSL = false };
            var runtimeThemeOptions = runtimeThemeColorOptions.CreateRuntimeThemeOptions(false, null, null);
            var colorValues = generator.GetColors(Colors.Blue, runtimeThemeOptions);

            Assert.That(colorValues.Options, Is.EqualTo(runtimeThemeOptions));
            Assert.That(colorValues.AccentBaseColor, Is.EqualTo(Colors.Blue));
            Assert.That(colorValues.AccentColor, Is.EqualTo(Colors.Blue));
            Assert.That(colorValues.AccentColor20, Is.EqualTo(ColorConverter.ConvertFromString("#330000FF")));
            Assert.That(colorValues.AccentColor40, Is.EqualTo(ColorConverter.ConvertFromString("#660000FF")));
            Assert.That(colorValues.AccentColor60, Is.EqualTo(ColorConverter.ConvertFromString("#990000FF")));
            Assert.That(colorValues.AccentColor80, Is.EqualTo(ColorConverter.ConvertFromString("#CC0000FF")));
            Assert.That(colorValues.HighlightColor, Is.EqualTo(ColorConverter.ConvertFromString("#FF0707FF")));
            Assert.That(colorValues.IdealForegroundColor, Is.EqualTo(ColorConverter.ConvertFromString("#FFFFFFFF")));
            Assert.That(colorValues.PrimaryAccentColor, Is.EqualTo(Colors.Blue));
        }

        [Test]
        public void GetHSLColors()
        {
            var generator = new RuntimeThemeGenerator();

            var runtimeThemeColorOptions = new RuntimeThemeGeneratorOptions { UseHSL = true };
            var runtimeThemeOptions = runtimeThemeColorOptions.CreateRuntimeThemeOptions(false, null, null);
            var colorValues = generator.GetColors(Colors.Blue, runtimeThemeOptions);

            Assert.That(colorValues.Options, Is.EqualTo(runtimeThemeOptions));
            Assert.That(colorValues.AccentBaseColor, Is.EqualTo(Colors.Blue));
            Assert.That(colorValues.AccentColor, Is.EqualTo(Colors.Blue));

            // Taken values to compare from here: https://www.w3schools.com/colors/colors_picker.asp?colorhex=0000ff
            Assert.That(colorValues.AccentColor20, Is.EqualTo(ColorConverter.ConvertFromString("#FFCCCCFF")));
            Assert.That(colorValues.AccentColor40, Is.EqualTo(ColorConverter.ConvertFromString("#FF9999FF")));
            Assert.That(colorValues.AccentColor60, Is.EqualTo(ColorConverter.ConvertFromString("#FF6666FF")));
            Assert.That(colorValues.AccentColor80, Is.EqualTo(ColorConverter.ConvertFromString("#FF3333FF")));
            Assert.That(colorValues.HighlightColor, Is.EqualTo(ColorConverter.ConvertFromString("#FF0707FF")));
            Assert.That(colorValues.IdealForegroundColor, Is.EqualTo(ColorConverter.ConvertFromString("#FFFFFFFF")));
            Assert.That(colorValues.PrimaryAccentColor, Is.EqualTo(Colors.Blue));
        }
    }
}