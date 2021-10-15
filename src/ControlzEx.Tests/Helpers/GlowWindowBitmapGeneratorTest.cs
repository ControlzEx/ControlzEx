namespace ControlzEx.Tests.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Windows.Media.Imaging;
    using ControlzEx.Controls.Internal;
    using ControlzEx.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class GlowWindowBitmapGeneratorTest
    {
        [Test]
        [Explicit("Used to test the generation of glow bitmaps.")]
        public void TestGenerateGlowBitmaps()
        {
            var directory = Path.Combine(Path.GetTempPath(), "Glows");
            Directory.CreateDirectory(directory);
            foreach (var value in Enum.GetValues(typeof(GlowBitmapPart)).Cast<GlowBitmapPart>())
            {
                var bitmap = GlowWindowBitmapGenerator.GenerateBitmapSource(value, 2, true);

                var frame = BitmapFrame.Create(bitmap);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(frame);

                var fileName = Path.Combine(directory, $"{value}.png");

                using (var stream = File.Create(fileName))
                {
                    encoder.Save(stream);
                }
            }
        }
    }
}