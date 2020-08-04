namespace ControlzEx.Standard
{
    using System;
    using System.Runtime.InteropServices;

    [Obsolete(DesignerConstants.Win32ElementWarning)]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [Obsolete(DesignerConstants.Win32ElementWarning)]
    public enum MonitorOptions : uint
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }
}