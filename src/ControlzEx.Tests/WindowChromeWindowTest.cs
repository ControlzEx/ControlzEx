namespace ControlzEx.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class WindowChromeWindowTest
    {
        [Test]
        public void TestShow()
        {
            var windowChromeWindow = new WindowChromeWindow();
            windowChromeWindow.Show();
            windowChromeWindow.Close();
        }
    }
}