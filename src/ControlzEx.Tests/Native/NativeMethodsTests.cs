namespace ControlzEx.Tests.Native
{
    using ControlzEx.Standard;
    using NUnit.Framework;

#pragma warning disable CS0618 // Type or member is obsolete
    [TestFixture]
    public class NativeMethodsTests
    {
        [Test]
        public void GetMonitorInfoShouldReturnValidData()
        {
            var cursorPos = NativeMethods.GetCursorPos();

            var monitor = NativeMethods.MonitorFromPoint(cursorPos, MonitorOptions.MONITOR_DEFAULTTONEAREST);

            var monitorInfo = NativeMethods.GetMonitorInfo(monitor);

            Assert.That(monitorInfo.rcMonitor.Width, Is.GreaterThan(0));
            Assert.That(monitorInfo.rcMonitor.Height, Is.GreaterThan(0));

            Assert.That(monitorInfo.rcWork.Width, Is.GreaterThan(0));
            Assert.That(monitorInfo.rcWork.Width, Is.GreaterThan(0));
        }
    }
}