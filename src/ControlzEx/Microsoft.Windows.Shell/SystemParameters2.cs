#pragma warning disable 1591, 618
/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

namespace ControlzEx.Windows.Shell
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using System.Windows.Media;
    using ControlzEx.Standard;

    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    internal class SystemParameters2 : INotifyPropertyChanged
    {
        private delegate void SystemMetricUpdate(IntPtr wParam, IntPtr lParam);

        [ThreadStatic]
        private static SystemParameters2 _threadLocalSingleton;

        private MessageWindow _messageHwnd;

        private bool _isGlassEnabled;
        private Color _glassColor;
        private SolidColorBrush _glassColorBrush;
        private Thickness _windowResizeBorderThickness;
        private Thickness _windowNonClientFrameThickness;
        private double _captionHeight;
        private Size _smallIconSize;
        private string _uxThemeName;
        private string _uxThemeColor;
        private bool _isHighContrast;
        private CornerRadius _windowCornerRadius;
        private Rect _captionButtonLocation;

        private readonly Dictionary<WM, List<SystemMetricUpdate>> _updateTable;

        #region Initialization and Update Methods

        // Most properties exposed here have a way of being queried directly
        // and a way of being notified of updates via a window message.
        // This region is a grouping of both, for each of the exposed properties.

        private void _InitializeIsGlassEnabled()
        {
            this.IsGlassEnabled = NativeMethods.DwmIsCompositionEnabled();
        }

        private void _UpdateIsGlassEnabled(IntPtr wParam, IntPtr lParam)
        {
            // Neither the wParam or lParam are used in this case.
            this._InitializeIsGlassEnabled();
        }

        private void _InitializeGlassColor()
        {
            NativeMethods.DwmGetColorizationColor(out uint color, out bool isOpaque);
            color |= isOpaque ? 0xFF000000 : 0;

            this.WindowGlassColor = Utility.ColorFromArgbDword(color);

            var glassBrush = new SolidColorBrush(this.WindowGlassColor);
            glassBrush.Freeze();

            this.WindowGlassBrush = glassBrush;
        }

        private void _UpdateGlassColor(IntPtr wParam, IntPtr lParam)
        {
            bool isOpaque = lParam != IntPtr.Zero;
            uint color = unchecked((uint)(int)wParam.ToInt64());
            color |= isOpaque ? 0xFF000000 : 0;
            this.WindowGlassColor = Utility.ColorFromArgbDword(color);
            var glassBrush = new SolidColorBrush(this.WindowGlassColor);
            glassBrush.Freeze();
            this.WindowGlassBrush = glassBrush;
        }

        private void _InitializeCaptionHeight()
        {
            Point ptCaption = new Point(0, NativeMethods.GetSystemMetrics(SM.CYCAPTION));
            this.WindowCaptionHeight = DpiHelper.DevicePixelsToLogical(ptCaption, DpiX / 96.0, Dpi / 96.0).Y;
        }

        private void _UpdateCaptionHeight(IntPtr wParam, IntPtr lParam)
        {
            this._InitializeCaptionHeight();
        }

        private void _InitializeWindowResizeBorderThickness()
        {
            Size frameSize = new Size(
                NativeMethods.GetSystemMetrics(SM.CXSIZEFRAME),
                NativeMethods.GetSystemMetrics(SM.CYSIZEFRAME));
            Size frameSizeInDips = DpiHelper.DeviceSizeToLogical(frameSize, DpiX / 96.0, Dpi / 96.0);
            this.WindowResizeBorderThickness = new Thickness(frameSizeInDips.Width, frameSizeInDips.Height, frameSizeInDips.Width, frameSizeInDips.Height);
        }

        private void _UpdateWindowResizeBorderThickness(IntPtr wParam, IntPtr lParam)
        {
            this._InitializeWindowResizeBorderThickness();
        }

        private void _InitializeWindowNonClientFrameThickness()
        {
            Size frameSize = new Size(
                NativeMethods.GetSystemMetrics(SM.CXSIZEFRAME),
                NativeMethods.GetSystemMetrics(SM.CYSIZEFRAME));
            Size frameSizeInDips = DpiHelper.DeviceSizeToLogical(frameSize, DpiX / 96.0, Dpi / 96.0);
            int captionHeight = NativeMethods.GetSystemMetrics(SM.CYCAPTION);
            double captionHeightInDips = DpiHelper.DevicePixelsToLogical(new Point(0, captionHeight), DpiX / 96.0, Dpi / 96.0).Y;
            this.WindowNonClientFrameThickness = new Thickness(frameSizeInDips.Width, frameSizeInDips.Height + captionHeightInDips, frameSizeInDips.Width, frameSizeInDips.Height);
        }

        private void _UpdateWindowNonClientFrameThickness(IntPtr wParam, IntPtr lParam)
        {
            this._InitializeWindowNonClientFrameThickness();
        }

        private void _InitializeSmallIconSize()
        {
            this.SmallIconSize = new Size(
                NativeMethods.GetSystemMetrics(SM.CXSMICON),
                NativeMethods.GetSystemMetrics(SM.CYSMICON));
        }

        private void _UpdateSmallIconSize(IntPtr wParam, IntPtr lParam)
        {
            this._InitializeSmallIconSize();
        }

        private void _LegacyInitializeCaptionButtonLocation()
        {
            // This calculation isn't quite right, but it's pretty close.
            // I expect this is good enough for the scenarios where this is expected to be used.
            int captionX = NativeMethods.GetSystemMetrics(SM.CXSIZE);
            int captionY = NativeMethods.GetSystemMetrics(SM.CYSIZE);

            int frameX = NativeMethods.GetSystemMetrics(SM.CXSIZEFRAME) + NativeMethods.GetSystemMetrics(SM.CXEDGE);
            int frameY = NativeMethods.GetSystemMetrics(SM.CYSIZEFRAME) + NativeMethods.GetSystemMetrics(SM.CYEDGE);

            Rect captionRect = new Rect(0, 0, captionX * 3, captionY);
            captionRect.Offset(-frameX - captionRect.Width, frameY);

            this.WindowCaptionButtonsLocation = captionRect;
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void _InitializeCaptionButtonLocation()
        {
            // There is a completely different way to do this on XP.
            if (!Utility.IsOsVistaOrNewer || !NativeMethods.IsThemeActive())
            {
                this._LegacyInitializeCaptionButtonLocation();
                return;
            }

            var tbix = new TITLEBARINFOEX { cbSize = Marshal.SizeOf(typeof(TITLEBARINFOEX)) };
            IntPtr lParam = Marshal.AllocHGlobal(tbix.cbSize);
            try
            {
                Marshal.StructureToPtr(tbix, lParam, false);
                // This might flash a window in the taskbar while being calculated.
                // WM_GETTITLEBARINFOEX doesn't work correctly unless the window is visible while processing.
                // use SW.SHOWNA instead SW.SHOW to avoid some brief flashing when launched the window
                NativeMethods.ShowWindow(this._messageHwnd.Handle, SW.SHOWNA);
                NativeMethods.SendMessage(this._messageHwnd.Handle, WM.GETTITLEBARINFOEX, IntPtr.Zero, lParam);
                tbix = (TITLEBARINFOEX)Marshal.PtrToStructure(lParam, typeof(TITLEBARINFOEX));
            }
            finally
            {
                NativeMethods.ShowWindow(this._messageHwnd.Handle, SW.HIDE);
                Utility.SafeFreeHGlobal(ref lParam);
            }

            // TITLEBARINFOEX has information relative to the screen.  We need to convert the containing rect
            // to instead be relative to the top-right corner of the window.
            RECT rcAllCaptionButtons = RECT.Union(tbix.rgrect_CloseButton, tbix.rgrect_MinimizeButton);
            // For all known themes, the RECT for the maximize box shouldn't add anything to the union of the minimize and close boxes.
            Assert.AreEqual(rcAllCaptionButtons, RECT.Union(rcAllCaptionButtons, tbix.rgrect_MaximizeButton));

            RECT rcWindow = NativeMethods.GetWindowRect(this._messageHwnd.Handle);

            // Reorient the Top/Right to be relative to the top right edge of the Window.
            var deviceCaptionLocation = new Rect(
                rcAllCaptionButtons.Left - rcWindow.Width - rcWindow.Left,
                rcAllCaptionButtons.Top - rcWindow.Top,
                rcAllCaptionButtons.Width,
                rcAllCaptionButtons.Height);

            Rect logicalCaptionLocation = DpiHelper.DeviceRectToLogical(deviceCaptionLocation, DpiX / 96.0, Dpi / 96.0);

            this.WindowCaptionButtonsLocation = logicalCaptionLocation;
        }

        private void _UpdateCaptionButtonLocation(IntPtr wParam, IntPtr lParam)
        {
            this._InitializeCaptionButtonLocation();
        }

        private void _InitializeHighContrast()
        {
            HIGHCONTRAST hc = NativeMethods.SystemParameterInfo_GetHIGHCONTRAST();
            this.HighContrast = (hc.dwFlags & HCF.HIGHCONTRASTON) != 0;
        }

        private void _UpdateHighContrast(IntPtr wParam, IntPtr lParam)
        {
            this._InitializeHighContrast();
        }

        private void _InitializeThemeInfo()
        {
            if (!NativeMethods.IsThemeActive())
            {
                this.UxThemeName = "Classic";
                this.UxThemeColor = "";
                return;
            }

            try
            {
                NativeMethods.GetCurrentThemeName(out string name, out string color, out _);

                // Consider whether this is the most useful way to expose this...
                this.UxThemeName = System.IO.Path.GetFileNameWithoutExtension(name);
                this.UxThemeColor = color;
            }
            catch (Exception)
            {
                this.UxThemeName = "Classic";
                this.UxThemeColor = "";
            }
        }

        private void _UpdateThemeInfo(IntPtr wParam, IntPtr lParam)
        {
            this._InitializeThemeInfo();
        }

        private void _InitializeWindowCornerRadius()
        {
            // The radius of window corners isn't exposed as a true system parameter.
            // It instead is a logical size that we're approximating based on the current theme.
            // There aren't any known variations based on theme color.
            Assert.IsNeitherNullNorEmpty(this.UxThemeName);

            // These radii are approximate.  The way WPF does rounding is different than how
            //     rounded-rectangle HRGNs are created, which is also different than the actual
            //     round corners on themed Windows.  For now we're not exposing anything to
            //     mitigate the differences.
            CornerRadius cornerRadius;

            // This list is known to be incomplete and very much not future-proof.
            // On XP there are at least a couple of shipped themes that this won't catch,
            // "Zune" and "Royale", but WPF doesn't know about these either.
            // If a new theme was to replace Aero, then this will fall back on "classic" behaviors.
            // This isn't ideal, but it's not the end of the world.  WPF will generally have problems anyways.
            switch (this.UxThemeName.ToUpperInvariant())
            {
                case "LUNA":
                    cornerRadius = new CornerRadius(6, 6, 0, 0);
                    break;
                case "AERO":
                    // Aero has two cases.  One with glass and one without...
                    cornerRadius = NativeMethods.DwmIsCompositionEnabled() ? new CornerRadius(8) : new CornerRadius(6, 6, 0, 0);
                    break;
                default:
                    cornerRadius = new CornerRadius(0);
                    break;
            }

            this.WindowCornerRadius = cornerRadius;
        }

        private void _UpdateWindowCornerRadius(IntPtr wParam, IntPtr lParam)
        {
            // Neither the wParam or lParam are used in this case.
            this._InitializeWindowCornerRadius();
        }


        #endregion

        /// <summary>
        /// Private constructor.  The public way to access this class is through the static Current property.
        /// </summary>
        private SystemParameters2()
        {
            // This window gets used for calculations about standard caption button locations
            // so it has WS_OVERLAPPEDWINDOW as a style to give it normal caption buttons.
            // This window may be shown during calculations of caption bar information, so create it at a location that's likely offscreen.
            this._messageHwnd = new MessageWindow(0, WS.OVERLAPPEDWINDOW | WS.DISABLED, 0, new Rect(-16000, -16000, 100, 100), "", this._WndProc);
            this._messageHwnd.Dispatcher.ShutdownStarted += (sender, e) => Utility.SafeDispose(ref this._messageHwnd);

            // Fixup the default values of the DPs.
            this._InitializeIsGlassEnabled();
            this._InitializeGlassColor();
            this._InitializeCaptionHeight();
            this._InitializeWindowNonClientFrameThickness();
            this._InitializeWindowResizeBorderThickness();
            this._InitializeCaptionButtonLocation();
            this._InitializeSmallIconSize();
            this._InitializeHighContrast();
            this._InitializeThemeInfo();
            // WindowCornerRadius isn't exposed by true system parameters, so it requires the theme to be initialized first.
            this._InitializeWindowCornerRadius();

            this._updateTable = new Dictionary<WM, List<SystemMetricUpdate>>
            {
                { WM.THEMECHANGED,
                    new List<SystemMetricUpdate>
                    {
                        this._UpdateThemeInfo,
                        this._UpdateHighContrast,
                        this._UpdateWindowCornerRadius,
                        this._UpdateCaptionButtonLocation, } },
                { WM.SETTINGCHANGE,
                    new List<SystemMetricUpdate>
                    {
                        this._UpdateCaptionHeight,
                        this._UpdateWindowResizeBorderThickness,
                        this._UpdateSmallIconSize,
                        this._UpdateHighContrast,
                        this._UpdateWindowNonClientFrameThickness,
                        this._UpdateCaptionButtonLocation, } },
                { WM.DWMNCRENDERINGCHANGED, new List<SystemMetricUpdate> {this._UpdateIsGlassEnabled } },
                { WM.DWMCOMPOSITIONCHANGED, new List<SystemMetricUpdate> {this._UpdateIsGlassEnabled } },
                { WM.DWMCOLORIZATIONCOLORCHANGED, new List<SystemMetricUpdate> {this._UpdateGlassColor } },
            };
        }

        public static SystemParameters2 Current => _threadLocalSingleton ?? (_threadLocalSingleton = new SystemParameters2());

        private IntPtr _WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            // Don't do this if called within the SystemParameters2 constructor
            if (this._updateTable != null)
            {
                if (this._updateTable.TryGetValue(msg, out List<SystemMetricUpdate> handlers))
                {
                    Assert.IsNotNull(handlers);
                    foreach (var handler in handlers)
                    {
                        handler(wParam, lParam);
                    }
                }
            }

            return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        public bool IsGlassEnabled
        {
            get => NativeMethods.DwmIsCompositionEnabled();
            private set
            {
                if (value != this._isGlassEnabled)
                {
                    this._isGlassEnabled = value;
                    this._NotifyPropertyChanged("IsGlassEnabled");
                }
            }
        }

        public Color WindowGlassColor
        {
            get => this._glassColor;
            private set
            {
                if (value != this._glassColor)
                {
                    this._glassColor = value;
                    this._NotifyPropertyChanged("WindowGlassColor");
                }
            }
        }

        public SolidColorBrush WindowGlassBrush
        {
            get => this._glassColorBrush;
            private set
            {
                Assert.IsNotNull(value);
                Assert.IsTrue(value.IsFrozen);
                if (this._glassColorBrush == null || value.Color != this._glassColorBrush.Color)
                {
                    this._glassColorBrush = value;
                    this._NotifyPropertyChanged("WindowGlassBrush");
                }
            }
        }

        public Thickness WindowResizeBorderThickness
        {
            get => this._windowResizeBorderThickness;
            private set
            {
                if (value != this._windowResizeBorderThickness)
                {
                    this._windowResizeBorderThickness = value;
                    this._NotifyPropertyChanged("WindowResizeBorderThickness");
                }
            }
        }

        public Thickness WindowNonClientFrameThickness
        {
            get => this._windowNonClientFrameThickness;
            private set
            {
                if (value != this._windowNonClientFrameThickness)
                {
                    this._windowNonClientFrameThickness = value;
                    this._NotifyPropertyChanged("WindowNonClientFrameThickness");
                }
            }
        }

        public double WindowCaptionHeight
        {
            get => this._captionHeight;
            private set
            {
                if (value != this._captionHeight)
                {
                    this._captionHeight = value;
                    this._NotifyPropertyChanged("WindowCaptionHeight");
                }
            }
        }

        public Size SmallIconSize
        {
            get => new Size(this._smallIconSize.Width, this._smallIconSize.Height);
            private set
            {
                if (value != this._smallIconSize)
                {
                    this._smallIconSize = value;
                    this._NotifyPropertyChanged("SmallIconSize");
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ux")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ux")]
        public string UxThemeName
        {
            get => this._uxThemeName;
            private set
            {
                if (value != this._uxThemeName)
                {
                    this._uxThemeName = value;
                    this._NotifyPropertyChanged("UxThemeName");
                }
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ux")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ux")]
        public string UxThemeColor
        {
            get => this._uxThemeColor;
            private set
            {
                if (value != this._uxThemeColor)
                {
                    this._uxThemeColor = value;
                    this._NotifyPropertyChanged("UxThemeColor");
                }
            }
        }

        public bool HighContrast
        {
            get => this._isHighContrast;
            private set
            {
                if (value != this._isHighContrast)
                {
                    this._isHighContrast = value;
                    this._NotifyPropertyChanged("HighContrast");
                }
            }
        }

        public CornerRadius WindowCornerRadius
        {
            get => this._windowCornerRadius;
            private set
            {
                if (value != this._windowCornerRadius)
                {
                    this._windowCornerRadius = value;
                    this._NotifyPropertyChanged("WindowCornerRadius");
                }
            }
        }

        public Rect WindowCaptionButtonsLocation
        {
            get => this._captionButtonLocation;
            private set
            {
                if (value != this._captionButtonLocation)
                {
                    this._captionButtonLocation = value;
                    this._NotifyPropertyChanged("WindowCaptionButtonsLocation");
                }
            }
        }

        #region Per monitor dpi support

        private enum CacheSlot
        {
            DpiX,

            NumSlots
        }

        private static int _dpi;
        private static bool _dpiInitialized;
        private static readonly object DpiLock = new object();
        private static bool _setDpiX = true;
        private static BitArray _cacheValid = new BitArray((int)CacheSlot.NumSlots);
        private static int _dpiX;

        internal static int Dpi
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                if (!_dpiInitialized)
                {
                    lock (DpiLock)
                    {
                        if (!_dpiInitialized)
                        {
                            using (var dc = SafeDC.GetDesktop())
                            {
                                if (dc.DangerousGetHandle() == IntPtr.Zero)
                                {
                                    throw new Win32Exception();
                                }

                                _dpi = NativeMethods.GetDeviceCaps(dc, DeviceCap.LOGPIXELSY);
                                _dpiInitialized = true;
                            }
                        }
                    }
                }
                return _dpi;
            }
        }

        ///<SecurityNote>
        ///  Critical as this accesses Native methods.
        ///  TreatAsSafe - it would be ok to expose this information - DPI in partial trust
        ///</SecurityNote>
        internal static int DpiX
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                if (_setDpiX)
                {
                    lock (_cacheValid)
                    {
                        if (_setDpiX)
                        {
                            _setDpiX = false;

                            // Win32Exception will get the Win32 error code so we don't have to
#pragma warning disable 6523
                            using (var dc = SafeDC.GetDesktop())
                            {
                                // Detecting error case from unmanaged call, required by PREsharp to throw a Win32Exception
#pragma warning disable 6503
                                if (dc.DangerousGetHandle() == IntPtr.Zero)
                                {
                                    throw new Win32Exception();
                                }
#pragma warning restore 6503
#pragma warning restore 6523

                                _dpiX = NativeMethods.GetDeviceCaps(dc, DeviceCap.LOGPIXELSX);
                                _cacheValid[(int) CacheSlot.DpiX] = true;
                            }
                        }
                    }
                }

                return _dpiX;
            }
        }

        #endregion Per monitor dpi support

        #region INotifyPropertyChanged Members

        private void _NotifyPropertyChanged(string propertyName)
        {
            Assert.IsNeitherNullNorEmpty(propertyName);
            var handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}