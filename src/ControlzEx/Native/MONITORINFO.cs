using System.Runtime.InteropServices;

namespace ControlzEx.Standard
{
    using System;

    [Obsolete(DesignerConstants.Win32ElementWarning)]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class Monitorinfo
    {
        public int cbSize = Marshal.SizeOf(typeof(Monitorinfo));
        public RECT rcMonitor = new RECT();
        public RECT rcWork = new RECT();
        public int dwFlags = 0;
    }

    [Obsolete(DesignerConstants.Win32ElementWarning)]
    public enum MonitorOptions : uint
    {
        MonitorDefaulttonull = 0x00000000,
        MonitorDefaulttoprimary = 0x00000001,
        MonitorDefaulttonearest = 0x00000002
    }
}