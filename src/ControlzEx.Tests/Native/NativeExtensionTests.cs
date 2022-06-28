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
            var windowPos1 = new WINDOWPOS
            {
                x = 0,
                y = 0,
                cx = 800,
                cy = 600
            };
            var windowPos2 = new WINDOWPOS
            {
                x = -500,
                y = -500,
                cx = 800,
                cy = 600
            };
            var windowPos3 = new WINDOWPOS
            {
                x = 5000,
                y = 5000,
                cx = 800,
                cy = 600
            };

            var rect1 = windowPos1.ToRECT();
            var rect2 = windowPos2.ToRECT();
            var rect3 = windowPos3.ToRECT();

            Assert.That(rect1.GetWidth(), Is.EqualTo(800));
            Assert.That(rect1.GetHeight(), Is.EqualTo(600));
            Assert.That(rect2.GetWidth(), Is.EqualTo(800));
            Assert.That(rect2.GetHeight(), Is.EqualTo(600));
            Assert.That(rect3.GetWidth(), Is.EqualTo(800));
            Assert.That(rect3.GetHeight(), Is.EqualTo(600));
        }
    }
}
