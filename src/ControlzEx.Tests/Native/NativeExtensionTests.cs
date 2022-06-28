namespace ControlzEx.Tests.Native
{
    using global::Windows.Win32;
    using global::Windows.Win32.UI.WindowsAndMessaging;
    using NUnit.Framework;

#pragma warning disable CS0618 // Type or member is obsolete
    [TestFixture]
    public class NativeExtensionTests
    {
        [Test]
        public void RectConvertedFromMonitorPositionIsCorrectSize()
        {
            const int WIDTH = 800;
            const int HEIGHT = 600;
            var windowPos1 = new WINDOWPOS
            {
                x = 0,
                y = 0,
                cx = WIDTH,
                cy = HEIGHT
            };
            var windowPos2 = new WINDOWPOS
            {
                x = -500,
                y = -500,
                cx = WIDTH,
                cy = HEIGHT
            };
            var windowPos3 = new WINDOWPOS
            {
                x = 5000,
                y = 5000,
                cx = WIDTH,
                cy = HEIGHT
            };

            var rect1 = windowPos1.ToRECT();
            var rect2 = windowPos2.ToRECT();
            var rect3 = windowPos3.ToRECT();

            Assert.That(rect1.GetWidth(), Is.EqualTo(WIDTH));
            Assert.That(rect1.GetHeight(), Is.EqualTo(HEIGHT));
            Assert.That(rect2.GetWidth(), Is.EqualTo(WIDTH));
            Assert.That(rect2.GetHeight(), Is.EqualTo(HEIGHT));
            Assert.That(rect3.GetWidth(), Is.EqualTo(WIDTH));
            Assert.That(rect3.GetHeight(), Is.EqualTo(HEIGHT));
        }
    }
}
