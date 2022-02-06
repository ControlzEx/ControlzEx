namespace ControlzEx.Tests.Native
{
    using global::Windows.Win32;
    using global::Windows.Win32.Graphics.Gdi;
    using NUnit.Framework;

#pragma warning disable CS0618 // Type or member is obsolete
    [TestFixture]
    public class NativeMethodsTests
    {
        [Test]
        public void GetMonitorInfoShouldReturnValidData()
        {
            var cursorPos = PInvoke.GetCursorPos();

            var monitor = PInvoke.MonitorFromPoint(cursorPos, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

            var monitorInfo = PInvoke.GetMonitorInfo(monitor);

            Assert.That(monitorInfo.rcMonitor.GetWidth(), Is.GreaterThan(0));
            Assert.That(monitorInfo.rcMonitor.GetHeight(), Is.GreaterThan(0));

            Assert.That(monitorInfo.rcWork.GetWidth(), Is.GreaterThan(0));
            Assert.That(monitorInfo.rcWork.GetHeight(), Is.GreaterThan(0));
        }
    }
}