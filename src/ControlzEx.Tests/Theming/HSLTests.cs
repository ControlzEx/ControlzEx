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
                            Assert.AreEqual(color, new HSLColor(color).ToColor());
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
                Assert.AreEqual(color, new HSLColor(color).ToColor());
            }

            // A
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(i, 255, 255, 255);
                Assert.AreEqual(color, new HSLColor(color).ToColor());
            }
            
            // R
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(255, i, 255, 255);
                Assert.AreEqual(color, new HSLColor(color).ToColor());
            }

            // G
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(255, 255, i, 255);
                Assert.AreEqual(color, new HSLColor(color).ToColor());
            }

            // B
            for (byte i = 0; i < byte.MaxValue; i++)
            {
                color = Color.FromArgb(255, 255, 255, i);
                Assert.AreEqual(color, new HSLColor(color).ToColor());
            }
        }

        [Test]
        public void TestHslFromColor_BuildInColors()
        {
            foreach (var color in typeof(Colors).GetProperties().Where(x => x.PropertyType == typeof(Color)).Select(x => (Color)x.GetValue(null)))
            {
                Assert.AreEqual(color, new HSLColor(color).ToColor());
            }
        }

        [Test]
        public void TestHslFromInput()
        {
            // Transparent
            Assert.AreEqual(Colors.Transparent, new HSLColor(0, 0, 0, 1).ToColor());
            
            // Black
            Assert.AreEqual(Colors.Black, new HSLColor(1, 0, 0, 0).ToColor());

            // White
            Assert.AreEqual(Colors.White, new HSLColor(1, 0, 0, 1).ToColor());
            
            // Gray
            Assert.AreEqual(Colors.Gray, new HSLColor(1, 0, 0, 0.5).ToColor());

            // Red
            Assert.AreEqual(Colors.Red, new HSLColor(1, 0, 1, 0.5).ToColor());

            // Yellow
            Assert.AreEqual(Colors.Yellow, new HSLColor(1, 60, 1, 0.5).ToColor());

            // Lime (Green)
            Assert.AreEqual(Colors.Lime, new HSLColor(1, 120, 1, 0.5).ToColor());

            // Aqua
            Assert.AreEqual(Colors.Aqua, new HSLColor(1, 180, 1, 0.5).ToColor());

            // Blue
            Assert.AreEqual(Colors.Blue, new HSLColor(1, 240, 1, 0.5).ToColor());

            // Magenta
            Assert.AreEqual(Colors.Magenta, new HSLColor(1, 300, 1, 0.5).ToColor());
        }
    }
}
