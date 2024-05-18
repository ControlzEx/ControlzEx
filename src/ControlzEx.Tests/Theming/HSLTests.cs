namespace ControlzEx.Tests.Theming
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using ControlzEx.Theming;
    using NUnit.Framework;

    [TestFixture]
    public class HSLTests
    {
        // This Test is very slow, so only run if really needed. Comment out the comment below to run this test
        // [Test]
        public void TestHslFromColor()
        {
            Parallel.For(0, byte.MaxValue, (a) => 
            {
                Color color;
                for (byte r = 0; r < byte.MaxValue; r++)
                {
                    for (byte g = 0; g < byte.MaxValue; g++)
                    {
                        for (byte b = 0; b < byte.MaxValue; b++)
                        {
                            color = Color.FromArgb((byte)a, r, g, b);
                            Assert.That(new HSLColor(color).ToColor(), Is.EqualTo(color));
                        }
                    }
                }
            });
        }

        [Test]
        public void TestColorPerComponent()
        {
            Color color;

            // Gray
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(255, i, i, i);
                Assert.That(new HSLColor(color).ToColor(), Is.EqualTo(color));
            }

            // A
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(i, 255, 255, 255);
                Assert.That(new HSLColor(color).ToColor(), Is.EqualTo(color));
            }
            
            // R
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(255, i, 255, 255);
                Assert.That(new HSLColor(color).ToColor(), Is.EqualTo(color));
            }

            // G
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(255, 255, i, 255);
                Assert.That(new HSLColor(color).ToColor(), Is.EqualTo(color));
            }

            // B
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(255, 255, 255, i);
                Assert.That(new HSLColor(color).ToColor(), Is.EqualTo(color));
            }
        }

        [Test]
        public void TestHslFromColor_BuildInColors()
        {
            foreach (var color in typeof(Colors).GetProperties().Where(x => x.PropertyType == typeof(Color)).Select(x => (Color)x.GetValue(null)))
            {
                Assert.That(new HSLColor(color).ToColor(), Is.EqualTo(color));
            }
        }

        [Test]
        public void TestHslFromInput()
        {
            // Transparent
            Assert.That(new HSLColor(0, 0, 0, 1).ToColor(), Is.EqualTo(Colors.Transparent));
            
            // Black
            Assert.That(new HSLColor(1, 0, 0, 0).ToColor(), Is.EqualTo(Colors.Black));

            // White
            Assert.That(new HSLColor(1, 0, 0, 1).ToColor(), Is.EqualTo(Colors.White));
            
            // Gray
            Assert.That(new HSLColor(1, 0, 0, 0.5).ToColor(), Is.EqualTo(Colors.Gray));

            // Red
            Assert.That(new HSLColor(1, 0, 1, 0.5).ToColor(), Is.EqualTo(Colors.Red));

            // Yellow
            Assert.That(new HSLColor(1, 60, 1, 0.5).ToColor(), Is.EqualTo(Colors.Yellow));

            // Lime (Green)
            Assert.That(new HSLColor(1, 120, 1, 0.5).ToColor(), Is.EqualTo(Colors.Lime));

            // Aqua
            Assert.That(new HSLColor(1, 180, 1, 0.5).ToColor(), Is.EqualTo(Colors.Aqua));

            // Blue
            Assert.That(new HSLColor(1, 240, 1, 0.5).ToColor(), Is.EqualTo(Colors.Blue));

            // Magenta
            Assert.That(new HSLColor(1, 300, 1, 0.5).ToColor(), Is.EqualTo(Colors.Magenta));
        }
    }
}
