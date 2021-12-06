#pragma warning disable 618
namespace ControlzEx.Showcase
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Shell;
    using ControlzEx.Standard;
    using ControlzEx.Theming;

    public partial class NativeWindow
    {
        private IntPtr windowHandle;

        public NativeWindow()
        {
            this.Background = Brushes.Transparent;
            var chrome = new WindowChrome
            {
                CornerRadius = default(CornerRadius),
                GlassFrameThickness = new Thickness(0, 0, 0, 1),
                UseAeroCaptionButtons = false
            };
            WindowChrome.SetWindowChrome(this, chrome);
            this.InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            this.windowHandle = new WindowInteropHelper(this).EnsureHandle();

            WindowEffectManager.UpdateWindowEffect(this, this.IsActive);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            WindowEffectManager.UpdateWindowEffect(this, this.IsActive);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);

            WindowEffectManager.UpdateWindowEffect(this, this.IsActive);
        }
    }

    public static class WindowEffectManager
    {
        public static void UpdateWindowEffect(Window window, bool isWindowActive = true)
        {
            window.Background = Brushes.Transparent;
            // This is important to prevent black squares from appearing randomly
            ((HwndSource)HwndSource.FromVisual(window)).CompositionTarget.BackgroundColor = Colors.Transparent;

            UpdateWindowEffect(new WindowInteropHelper(window).EnsureHandle(), isWindowActive);
        }

        public static void UpdateWindowEffect(IntPtr windowHandle, bool isWindowActive = false)
        {
            var isDarkTheme = WindowsThemeHelper.AppsUseLightTheme() == false;

            // {
            //     var wtaOptions = new WTA_OPTIONS
            //     {
            //         dwFlags = WTNCA.NODRAWCAPTION | WTNCA.NODRAWICON | WTNCA.NOSYSMENU | WTNCA.NOMIRRORHELP
            //     };
            //     wtaOptions.dwMask = wtaOptions.dwFlags;
            //     NativeMethods.SetWindowThemeAttribute(windowHandle, WINDOWTHEMEATTRIBUTETYPE.WTA_NONCLIENT, ref wtaOptions, (uint)Marshal.SizeOf(typeof(WTA_OPTIONS)));
            // }

            if (OSVersionHelper.IsWindows11_OrGreater)
            {
                EnableMicaEffect(windowHandle, isDarkTheme);
            }
            else
            {
                SetAccentPolicy(windowHandle, isWindowActive, isDarkTheme);
            }
        }

        private static void EnableMicaEffect(IntPtr windowHandle, bool isDarkTheme)
        {
            DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS(-1, -1, -1, -1));
            //DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS(1, 1, 1, 1));
            var value = NativeMethods.DwmGetWindowAttribute(windowHandle, DWMWINDOWATTRIBUTE.CAPTION_BUTTON_BOUNDS, out RECT rect, Marshal.SizeOf<RECT>());

            var trueValue = 0x01;
            var falseValue = 0x00;

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (isDarkTheme)
            {
                DwmHelper.SetWindowAttributeValue(windowHandle, DWMWINDOWATTRIBUTE.USE_IMMERSIVE_DARK_MODE, trueValue);
            }
            else
            {
                DwmHelper.SetWindowAttributeValue(windowHandle, DWMWINDOWATTRIBUTE.USE_IMMERSIVE_DARK_MODE, falseValue);
            }

            DwmHelper.SetWindowAttributeValue(windowHandle, DWMWINDOWATTRIBUTE.MICA_EFFECT, trueValue);
        }

        private static void SetAccentPolicy(IntPtr windowHandle, bool isWindowActive, bool isDarkTheme)
        {
            //DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS(-1, -1, -1, -1));
            DwmHelper.WindowExtendIntoClientArea(windowHandle, new MARGINS(0, 0, 0, 1));
            //DwmHelper.SetWindowAttributeValue(windowHandle, DWMWINDOWATTRIBUTE.VISIBLE_FRAME_BORDER_THICKNESS, 0);

            var accentPolicy = default(AccentPolicy);
            var accentPolicySize = Marshal.SizeOf(accentPolicy);

            accentPolicy.AccentFlags = 2;

            accentPolicy.AccentState = isWindowActive switch
            {
                true when OSVersionHelper.IsWindows10_1803_OrGreater => AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                true when OSVersionHelper.IsWindows10_OrGreater => AccentState.ACCENT_ENABLE_BLURBEHIND,
                true => AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT,
                false => AccentState.ACCENT_ENABLE_HOSTBACKDROP
            };

            accentPolicy.GradientColor = isDarkTheme ? 0x99000000 : 0x99FFFFFF; //ResourceHelper.GetResource<uint>(ResourceToken.BlurGradientValue);

            var accentPtr = Marshal.AllocHGlobal(accentPolicySize);
            Marshal.StructureToPtr(accentPolicy, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentPolicySize,
                Data = accentPtr
            };
            NativeMethods.SetWindowCompositionAttribute(windowHandle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
    }
}