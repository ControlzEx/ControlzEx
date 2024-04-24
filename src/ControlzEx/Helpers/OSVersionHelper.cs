// ReSharper disable InconsistentNaming
#pragma warning disable 618, SA1131

namespace ControlzEx.Helpers
{
    using System;
    using System.Runtime.InteropServices;
    using global::Windows.Win32;

    public static class OSVersionHelper
    {
        public static readonly Version OSVersion = GetOSVersion();

        /// <summary>
        /// Windows NT
        /// </summary>
        public static bool IsWindowsNT { get; } = Environment.OSVersion.Platform == PlatformID.Win32NT;

        /// <summary>
        /// Windows 10 or greater.
        /// </summary>
        public static bool IsWindows10_OrGreater { get; } = IsWindowsNT && OSVersion >= new Version(10, 0);

        /// <summary>
        /// Windows 10 19H1 Version 1903 Build 18362 or greater (May 2019 Update)-
        /// </summary>
        public static bool IsWindows10_1903_OrGreater { get; } = IsWindowsNT && OSVersion >= new Version(10, 0, 18362);

        /// <summary>
        /// Windows 11 or greater.
        /// </summary>
        public static bool IsWindows11_OrGreater { get; } = IsWindowsNT && OSVersion >= new Version(10, 0, 22000);

        /// <summary>
        /// Windows 11 22H2 or greater.
        /// </summary>
        public static bool IsWindows11_22H2_OrGreater { get; } = IsWindowsNT && OSVersion >= new Version(10, 0, 22621);

        public static Version GetOSVersion()
        {
            var osv = default(PInvoke.RTL_OSVERSIONINFOEX);
            osv.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osv);
            PInvoke.RtlGetVersion(out osv);
            return new Version((int)osv.dwMajorVersion, (int)osv.dwMinorVersion, (int)osv.dwBuildNumber, (int)osv.dwRevision);
        }
    }
}