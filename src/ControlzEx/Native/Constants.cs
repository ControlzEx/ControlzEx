using System;

namespace ControlzEx.Native
{
    [Obsolete(DesignerConstants.Win32ElementWarning)]
    public static class Constants
    {
        [Flags()]
        public enum RedrawWindowFlags : uint
        {
            /// <summary>
            /// Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
            /// </summary>
            Invalidate = 0x1,

            /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
            InternalPaint = 0x2,

            /// <summary>
            /// Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
            /// Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
            /// </summary>
            Erase = 0x4,

            /// <summary>
            /// Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
            /// This value does not affect internal WM_PAINT messages.
            /// </summary>
            Validate = 0x8,

            NoInternalPaint = 0x10,

            /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
            NoErase = 0x20,

            /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
            NoChildren = 0x40,

            /// <summary>Includes child windows, if any, in the repainting operation.</summary>
            AllChildren = 0x80,

            /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
            UpdateNow = 0x100,

            /// <summary>
            /// Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
            /// The affected windows receive WM_PAINT messages at the ordinary time.
            /// </summary>
            EraseNow = 0x200,

            Frame = 0x400,

            NoFrame = 0x800
        }

        public const int GclpHbrbackground = -0x0A;

        public const uint TpmReturncmd = 0x0100;        
        public const uint TpmLeftbutton = 0x0;

        public const uint Syscommand = 0x0112;

        public const int MfGrayed = 0x00000001;
        public const int MfBycommand = 0x00000000;
        public const int MfEnabled = 0x00000000;

        public const int VkShift = 0x10;
        public const int VkControl = 0x11;
        public const int VkMenu = 0x12;

        /* used by UnsafeNativeMethods.MapVirtualKey */
        public const uint MapvkVkToVsc = 0x00;
        public const uint MapvkVscToVk = 0x01;
        public const uint MapvkVkToChar = 0x02;
        public const uint MapvkVscToVkEx = 0x03;
        public const uint MapvkVkToVscEx = 0x04;
        /* used by UnsafeNativeMethods.MapVirtualKey (end) */

        public static readonly IntPtr HwndTopmost = new IntPtr(-1);
        public static readonly IntPtr HwndNotopmost = new IntPtr(-2);
        public static readonly IntPtr HwndTop = new IntPtr(0);
        public static readonly IntPtr HwndBottom = new IntPtr(1);

        /// <summary>
        /// Causes the dialog box to display all available colors in the set of basic colors. 
        /// </summary>
        public const int CcAnycolor = 0x00000100;
    }
}