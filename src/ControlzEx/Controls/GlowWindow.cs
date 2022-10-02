// ReSharper disable IdentifierTypo
#nullable enable

// ReSharper disable once CheckNamespace
namespace ControlzEx.Controls.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Behaviors;
    using ControlzEx.Helpers;
    using ControlzEx.Native;
    using global::Windows.Win32;
    using global::Windows.Win32.Foundation;
    using global::Windows.Win32.Graphics.Gdi;
    using global::Windows.Win32.UI.WindowsAndMessaging;
    using JetBrains.Annotations;
    using COLORREF = Windows.Win32.Foundation.COLORREF;

#pragma warning disable 618, SA1602, SA1401

    [PublicAPI]
    public abstract class HwndWrapper : DisposableObject
    {
        private HWND hwnd;

        private bool isHandleCreationAllowed = true;

        private WNDPROC? wndProc;

        public abstract string ClassName { get; }

        public static int LastDestroyWindowError { get; private set; }

        [CLSCompliant(false)]
        protected ushort WindowClassAtom { get; private set; }

        public IntPtr Handle => this.Hwnd;

        internal HWND Hwnd
        {
            get
            {
                this.EnsureHandle();
                return this.hwnd;
            }
        }

        protected virtual bool IsWindowSubClassed => false;

        [CLSCompliant(false)]
        protected virtual ushort CreateWindowClassCore()
        {
            return this.RegisterClass(this.ClassName);
        }

        protected virtual void DestroyWindowClassCore()
        {
            if (this.WindowClassAtom != 0)
            {
                var moduleHandle = PInvoke.GetModuleHandle((string?)null);
                PInvoke.UnregisterClass(this.ClassName, moduleHandle);
                this.WindowClassAtom = 0;
            }
        }

        [CLSCompliant(false)]
        protected unsafe ushort RegisterClass(string className)
        {
            this.wndProc = this.WndProcWrapper;

            fixed (char* cls = className)
            {
                var lpWndClass = new WNDCLASSEXW
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
                    hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                    lpfnWndProc = this.wndProc,
                    lpszClassName = cls,
                };

                var atom = PInvoke.RegisterClassEx(lpWndClass);

                return atom;
            }
        }

        private void SubclassWndProc()
        {
            this.wndProc = this.WndProcWrapper;
            PInvoke.SetWindowLongPtr(this.Hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(this.wndProc));
        }

        protected abstract IntPtr CreateWindowCore();

        protected virtual void DestroyWindowCore()
        {
            if (this.hwnd != IntPtr.Zero)
            {
                if (PInvoke.DestroyWindow(this.hwnd) == false)
                {
                    LastDestroyWindowError = Marshal.GetLastWin32Error();
                }

                this.hwnd = default;
            }
        }

        private LRESULT WndProcWrapper(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            return new LRESULT(this.WndProc(hwnd, msg, wParam, lParam));
        }

        [CLSCompliant(false)]
        protected virtual nint WndProc(nint hwnd, uint msg, nuint wParam, nint lParam)
        {
            return PInvoke.DefWindowProc(new HWND(hwnd), msg, wParam, lParam);
        }

        public IntPtr EnsureHandle()
        {
            if (this.hwnd != IntPtr.Zero)
            {
                return this.hwnd;
            }

            if (this.isHandleCreationAllowed == false)
            {
                return IntPtr.Zero;
            }

            if (this.IsDisposed)
            {
                return IntPtr.Zero;
            }

            this.isHandleCreationAllowed = false;
            this.WindowClassAtom = this.CreateWindowClassCore();
            this.hwnd = new HWND(this.CreateWindowCore());

            if (this.IsWindowSubClassed)
            {
                this.SubclassWndProc();
            }

            return this.hwnd;
        }

        protected override void DisposeNativeResources()
        {
            this.isHandleCreationAllowed = false;
            this.DestroyWindowCore();
            this.DestroyWindowClassCore();
        }
    }

    [PublicAPI]
    public class DisposableObject : IDisposable
    {
        private EventHandler? disposingEventHandlers;

        public bool IsDisposing { get; private set; }

        public bool IsDisposed { get; private set; }

        public event EventHandler Disposing
        {
            add
            {
                this.ThrowIfDisposed();
                this.disposingEventHandlers = (EventHandler)Delegate.Combine(this.disposingEventHandlers, value);
            }
            remove => this.disposingEventHandlers = (EventHandler?)Delegate.Remove(this.disposingEventHandlers, value);
        }

        ~DisposableObject()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed
                || this.IsDisposing)
            {
                return;
            }

            this.IsDisposing = true;

            try
            {
                if (disposing)
                {
                    this.disposingEventHandlers?.Invoke(this, EventArgs.Empty);
                    this.disposingEventHandlers = null;
                    this.DisposeManagedResources();
                }

                this.DisposeNativeResources();
            }
            finally
            {
                this.IsDisposed = true;
                this.IsDisposing = false;
            }
        }

        protected virtual void DisposeManagedResources()
        {
        }

        protected virtual void DisposeNativeResources()
        {
        }
    }

    [PublicAPI]
    public sealed class GlowBitmap : DisposableObject
    {
        private sealed class CachedBitmapInfo
        {
            public readonly int Width;

            public readonly int Height;

            public readonly byte[] DiBits;

            public CachedBitmapInfo(byte[] diBits, int width, int height)
            {
                this.Width = width;
                this.Height = height;
                this.DiBits = diBits;
            }
        }

        public const int GlowBitmapPartCount = 16;

        private const int BytesPerPixelBgra32 = 4;

        private static readonly Dictionary<CachedBitmapInfoKey, CachedBitmapInfo?[]> transparencyMasks = new();

        private IntPtr pbits;

        private readonly BITMAPINFO bitmapInfo;

        public SafeHandle Handle { get; }

        public IntPtr DiBits => this.pbits;

        public int Width => this.bitmapInfo.bmiHeader.biWidth;

        public int Height => -this.bitmapInfo.bmiHeader.biHeight;

        public unsafe GlowBitmap(SafeHandle hdcScreen, int width, int height)
        {
            this.bitmapInfo.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            this.bitmapInfo.bmiHeader.biPlanes = 1;
            this.bitmapInfo.bmiHeader.biBitCount = 32;
            this.bitmapInfo.bmiHeader.biCompression = 0;
            this.bitmapInfo.bmiHeader.biXPelsPerMeter = 0;
            this.bitmapInfo.bmiHeader.biYPelsPerMeter = 0;
            this.bitmapInfo.bmiHeader.biWidth = width;
            this.bitmapInfo.bmiHeader.biHeight = -height;

            fixed (BITMAPINFO* pbitmapinfo = &this.bitmapInfo)
            {
                this.Handle = new DeleteObjectSafeHandle(PInvoke.CreateDIBSection(new HDC(hdcScreen.DangerousGetHandle()), pbitmapinfo, DIB_USAGE.DIB_RGB_COLORS, out var bits, default, 0));
                this.pbits = bits;
            }
        }

        protected override void DisposeNativeResources()
        {
            this.Handle.Dispose();
        }

        private static byte PremultiplyAlpha(byte channel, byte alpha)
        {
            return (byte)(channel * alpha / 255.0);
        }

        public static GlowBitmap? Create(GlowDrawingContext drawingContext, GlowBitmapPart bitmapPart, Color color, int glowDepth, bool useRadialGradientForCorners)
        {
            if (drawingContext.ScreenDc is null)
            {
                return null;
            }

            var alphaMask = GetOrCreateAlphaMask(bitmapPart, glowDepth, useRadialGradientForCorners);
            var glowBitmap = new GlowBitmap(drawingContext.ScreenDc, alphaMask.Width, alphaMask.Height);
            for (var i = 0; i < alphaMask.DiBits.Length; i += BytesPerPixelBgra32)
            {
                var b = alphaMask.DiBits[i + 3];
                var val = PremultiplyAlpha(color.R, b);
                var val2 = PremultiplyAlpha(color.G, b);
                var val3 = PremultiplyAlpha(color.B, b);
                Marshal.WriteByte(glowBitmap.DiBits, i, val3);
                Marshal.WriteByte(glowBitmap.DiBits, i + 1, val2);
                Marshal.WriteByte(glowBitmap.DiBits, i + 2, val);
                Marshal.WriteByte(glowBitmap.DiBits, i + 3, b);
            }

            return glowBitmap;
        }

        private static CachedBitmapInfo GetOrCreateAlphaMask(GlowBitmapPart bitmapPart, int glowDepth, bool useRadialGradientForCorners)
        {
            var cacheKey = new CachedBitmapInfoKey(glowDepth, useRadialGradientForCorners);
            if (transparencyMasks.TryGetValue(cacheKey, out var transparencyMasksForGlowDepth) == false)
            {
                transparencyMasksForGlowDepth = new CachedBitmapInfo?[GlowBitmapPartCount];
                transparencyMasks[cacheKey] = transparencyMasksForGlowDepth;
            }

            var num = (int)bitmapPart;
            if (transparencyMasksForGlowDepth[num] is { } transparencyMask)
            {
                return transparencyMask;
            }

            var bitmapImage = GlowWindowBitmapGenerator.GenerateBitmapSource(bitmapPart, glowDepth, useRadialGradientForCorners);
            var array = new byte[BytesPerPixelBgra32 * bitmapImage.PixelWidth * bitmapImage.PixelHeight];
            var stride = BytesPerPixelBgra32 * bitmapImage.PixelWidth;
            bitmapImage.CopyPixels(array, stride, 0);
            var cachedBitmapInfo = new CachedBitmapInfo(array, bitmapImage.PixelWidth, bitmapImage.PixelHeight);
            transparencyMasksForGlowDepth[num] = cachedBitmapInfo;

            return cachedBitmapInfo;
        }
    }

    internal readonly struct CachedBitmapInfoKey : IEquatable<CachedBitmapInfoKey>
    {
        public CachedBitmapInfoKey(int glowDepth, bool useRadialGradientForCorners)
        {
            this.GlowDepth = glowDepth;
            this.UseRadialGradientForCorners = useRadialGradientForCorners;
        }

        public int GlowDepth { get; }

        public bool UseRadialGradientForCorners { get; }

        public bool Equals(CachedBitmapInfoKey other)
        {
            return this.GlowDepth == other.GlowDepth && this.UseRadialGradientForCorners == other.UseRadialGradientForCorners;
        }

        public override bool Equals(object? obj)
        {
            return obj is CachedBitmapInfoKey other
                   && this.Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.GlowDepth * 397) ^ this.UseRadialGradientForCorners.GetHashCode();
            }
        }

        public static bool operator ==(CachedBitmapInfoKey left, CachedBitmapInfoKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CachedBitmapInfoKey left, CachedBitmapInfoKey right)
        {
            return !left.Equals(right);
        }
    }

    public enum GlowBitmapPart
    {
        CornerTopLeft,
        CornerTopRight,
        CornerBottomLeft,
        CornerBottomRight,
        TopLeft,
        Top,
        TopRight,
        LeftTop,
        Left,
        LeftBottom,
        BottomLeft,
        Bottom,
        BottomRight,
        RightTop,
        Right,
        RightBottom
    }

    public sealed class GlowDrawingContext : DisposableObject
    {
        internal BLENDFUNCTION Blend;

        private readonly GlowBitmap? windowBitmap;

        [MemberNotNullWhen(true, nameof(ScreenDc))]
        [MemberNotNullWhen(true, nameof(WindowDc))]
        [MemberNotNullWhen(true, nameof(BackgroundDc))]
        [MemberNotNullWhen(true, nameof(windowBitmap))]
        public bool IsInitialized
        {
            get
            {
                if (this.ScreenDc is null
                    || this.WindowDc is null
                    || this.BackgroundDc is null
                    || this.windowBitmap is null)
                {
                    return false;
                }

                if (this.ScreenDc.DangerousGetHandle() != IntPtr.Zero
                    && this.WindowDc.DangerousGetHandle() != IntPtr.Zero
                    && this.BackgroundDc.DangerousGetHandle() != IntPtr.Zero)
                {
                    return this.windowBitmap is not null;
                }

                return false;
            }
        }

        public SafeHandle? ScreenDc { get; private set; }

        public SafeHandle? WindowDc { get; }

        public SafeHandle? BackgroundDc { get; }

        public int Width => this.windowBitmap?.Width ?? 0;

        public int Height => this.windowBitmap?.Height ?? 0;

        private static SafeHandle? desktopDC;

        public GlowDrawingContext(int width, int height)
        {
            this.SetupDesktopDC();

            if (this.ScreenDc is null)
            {
                return;
            }

            try
            {
                this.WindowDc = PInvoke.CreateCompatibleDC(this.ScreenDc);
            }
            catch
            {
                desktopDC?.Dispose();
                desktopDC = null;
                this.SetupDesktopDC();

                this.WindowDc = PInvoke.CreateCompatibleDC(this.ScreenDc);
            }

            if (this.WindowDc.DangerousGetHandle() == IntPtr.Zero)
            {
                return;
            }

            this.BackgroundDc = PInvoke.CreateCompatibleDC(this.ScreenDc);

            if (this.BackgroundDc.DangerousGetHandle() == IntPtr.Zero)
            {
                return;
            }

            this.Blend.BlendOp = 0;
            this.Blend.BlendFlags = 0;
            this.Blend.SourceConstantAlpha = byte.MaxValue;
            this.Blend.AlphaFormat = 0x01; // AC_SRC_ALPHA;
            this.windowBitmap = new GlowBitmap(this.ScreenDc, width, height);
            PInvoke.SelectObject(this.WindowDc, this.windowBitmap.Handle);
        }

        private void SetupDesktopDC()
        {
            desktopDC ??= new DeleteDCSafeHandle(PInvoke.GetDC(default));

            this.ScreenDc = desktopDC;
            if (this.ScreenDc.DangerousGetHandle() == IntPtr.Zero)
            {
                this.ScreenDc?.Dispose();
                this.ScreenDc = null;
            }
        }

        protected override void DisposeManagedResources()
        {
            this.windowBitmap?.Dispose();
        }

        protected override void DisposeNativeResources()
        {
            this.WindowDc?.Dispose();

            this.BackgroundDc?.Dispose();
        }
    }

    [CLSCompliant(false)]
    [PublicAPI]
    public sealed class GlowWindow : HwndWrapper, IGlowWindow
    {
        [Flags]
        private enum FieldInvalidationTypes
        {
            None = 0,
            Location = 1 << 1,
            Size = 1 << 2,
            ActiveColor = 1 << 3,
            InactiveColor = 1 << 4,
            Render = 1 << 5,
            Visibility = 1 << 6,
            GlowDepth = 1 << 7
        }

        private readonly Window targetWindow;
        private readonly GlowWindowBehavior behavior;

        private readonly Dock orientation;

        private readonly GlowBitmap[] activeGlowBitmaps = new GlowBitmap[GlowBitmap.GlowBitmapPartCount];

        private readonly GlowBitmap[] inactiveGlowBitmaps = new GlowBitmap[GlowBitmap.GlowBitmapPartCount];

        private static ushort sharedWindowClassAtom;

        // Member to keep reference alive
        // ReSharper disable NotAccessedField.Local
#pragma warning disable IDE0052 // Remove unread private members
        private static WNDPROC? sharedWndProc;
#pragma warning restore IDE0052 // Remove unread private members
        // ReSharper restore NotAccessedField.Local

        private int left;

        private int top;

        private int width;

        private int height;

        private int glowDepth = 9;
        private readonly int cornerGripThickness = Constants.ResizeCornerGripThickness;

        private bool useRadialGradientForCorners = true;

        private bool isVisible;

        private bool isActive;

        private Color activeGlowColor = Colors.Transparent;

        private Color inactiveGlowColor = Colors.Transparent;

        private FieldInvalidationTypes invalidatedValues;

        private bool pendingDelayRender;
        private string title;

#pragma warning disable SA1310
        private static readonly LPARAM SW_PARENTCLOSING = new(1);
        private static readonly LPARAM SW_PARENTOPENING = new(3);
#pragma warning restore SA1310

        private bool IsDeferringChanges => this.behavior.DeferGlowChangesCount > 0;

        private unsafe ushort SharedWindowClassAtom
        {
            get
            {
                if (sharedWindowClassAtom == 0)
                {
                    sharedWndProc ??= PInvoke.DefWindowProc;

                    fixed (char* cls = this.ClassName)
                    {
                        var lpWndClass = new WNDCLASSEXW
                        {
                            cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
                            hInstance = PInvoke.GetModuleHandle((PCWSTR)null),
                            lpfnWndProc = sharedWndProc,
                            lpszClassName = cls,
                        };

                        sharedWindowClassAtom = PInvoke.RegisterClassEx(lpWndClass);
                    }
                }

                return sharedWindowClassAtom;
            }
        }

        public override string ClassName { get; } = "ControlzEx_GlowWindow";

        public bool IsVisible
        {
            get => this.isVisible;
            set
            {
                this.UpdateProperty(ref this.isVisible, value, FieldInvalidationTypes.Render | FieldInvalidationTypes.Visibility);

                if (value
                    && this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Visibility))
                {
                    this.UpdateWindowPos();
                }
            }
        }

        public int Left
        {
            get => this.left;
            set => this.UpdateProperty(ref this.left, value, FieldInvalidationTypes.Location);
        }

        public int Top
        {
            get => this.top;
            set => this.UpdateProperty(ref this.top, value, FieldInvalidationTypes.Location);
        }

        public int Width
        {
            get => this.width;
            set => this.UpdateProperty(ref this.width, value, FieldInvalidationTypes.Size | FieldInvalidationTypes.Render);
        }

        public int Height
        {
            get => this.height;
            set => this.UpdateProperty(ref this.height, value, FieldInvalidationTypes.Size | FieldInvalidationTypes.Render);
        }

        public int GlowDepth
        {
            get => this.glowDepth;
            set => this.UpdateProperty(ref this.glowDepth, value, FieldInvalidationTypes.GlowDepth | FieldInvalidationTypes.Render | FieldInvalidationTypes.Location);
        }

        public bool UseRadialGradientForCorners
        {
            get => this.useRadialGradientForCorners;
            set => this.UpdateProperty(ref this.useRadialGradientForCorners, value, FieldInvalidationTypes.GlowDepth | FieldInvalidationTypes.Render | FieldInvalidationTypes.Location);
        }

        public bool IsActive
        {
            get => this.isActive;
            set => this.UpdateProperty(ref this.isActive, value, FieldInvalidationTypes.Render);
        }

        public Color ActiveGlowColor
        {
            get => this.activeGlowColor;
            set => this.UpdateProperty(ref this.activeGlowColor, value, FieldInvalidationTypes.ActiveColor | FieldInvalidationTypes.Render);
        }

        public Color InactiveGlowColor
        {
            get => this.inactiveGlowColor;
            set => this.UpdateProperty(ref this.inactiveGlowColor, value, FieldInvalidationTypes.InactiveColor | FieldInvalidationTypes.Render);
        }

        private HWND TargetWindowHandle { get; }

        protected override bool IsWindowSubClassed => true;

        private bool IsPositionValid => !this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Location | FieldInvalidationTypes.Size | FieldInvalidationTypes.Visibility);

        public GlowWindow(Window owner, GlowWindowBehavior behavior, Dock orientation)
        {
            this.targetWindow = owner ?? throw new ArgumentNullException(nameof(owner));
            this.behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            this.orientation = orientation;

            this.TargetWindowHandle = new(new WindowInteropHelper(this.targetWindow).EnsureHandle());

            if (this.TargetWindowHandle == IntPtr.Zero
                || PInvoke.IsWindow(this.TargetWindowHandle) == false)
            {
                throw new Exception($"TargetWindowHandle {this.TargetWindowHandle} must be a window.");
            }

            this.title = $"Glow_{this.orientation}";
        }

        private void UpdateProperty<T>(ref T field, T value, FieldInvalidationTypes invalidation)
            where T : struct, IEquatable<T>
        {
            if (field.Equals(value))
            {
                return;
            }

            field = value;
            this.invalidatedValues |= invalidation;

            if (this.IsDeferringChanges == false)
            {
                this.CommitChanges(IntPtr.Zero);
            }
        }

        protected override ushort CreateWindowClassCore()
        {
            return this.SharedWindowClassAtom;
        }

        protected override void DestroyWindowClassCore()
        {
            // Do nothing here as we registered a shared class/atom
        }

        protected override unsafe IntPtr CreateWindowCore()
        {
            const WINDOW_EX_STYLE EX_STYLE = WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_LAYERED;
            const WINDOW_STYLE STYLE = WINDOW_STYLE.WS_POPUP | WINDOW_STYLE.WS_CLIPSIBLINGS | WINDOW_STYLE.WS_CLIPCHILDREN;

            var windowHandle = PInvoke.CreateWindowEx(EX_STYLE, this.ClassName, this.title, STYLE, 0, 0, 0, 0, this.TargetWindowHandle, null, null, null);

            return windowHandle;
        }

        protected override nint WndProc(nint hwnd, uint msg, nuint wParam, nint lParam)
        {
            var message = (WM)msg;
            //System.Diagnostics.Trace.WriteLine($"{DateTime.Now} {hwnd} {message} {wParam} {lParam}");

            switch (message)
            {
                case WM.DESTROY:
                    this.Dispose();
                    break;

                case WM.NCHITTEST:
                    return (nint)this.WmNcHitTest(lParam);

                case WM.NCLBUTTONDOWN:
                case WM.NCLBUTTONDBLCLK:
                case WM.NCRBUTTONDOWN:
                case WM.NCRBUTTONDBLCLK:
                case WM.NCMBUTTONDOWN:
                case WM.NCMBUTTONDBLCLK:
                case WM.NCXBUTTONDOWN:
                case WM.NCXBUTTONDBLCLK:
                {
                    PInvoke.SendMessage(this.TargetWindowHandle, (uint)message, wParam, IntPtr.Zero);
                    return default;
                }

                case WM.WINDOWPOSCHANGED:
                case WM.WINDOWPOSCHANGING:
                {
                    var windowpos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    windowpos.flags |= SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
                    Marshal.StructureToPtr(windowpos, lParam, true);
                    break;
                }

                case WM.SETFOCUS:
                    // Move focus back as we don't want to get focused
                    PInvoke.SetFocus(new HWND((nint)wParam));
                    return default;

                case WM.ACTIVATE:
                    return default;

                case WM.NCACTIVATE:
                    PInvoke.SendMessage(this.TargetWindowHandle, (uint)message, wParam, lParam);
                    // We have to return true according to https://docs.microsoft.com/en-us/windows/win32/winmsg/wm-ncactivate
                    // If we don't do that here the owner window can't be activated.
                    return 1;

                case WM.MOUSEACTIVATE:
                    // WA_CLICKACTIVE = 2
                    PInvoke.SendMessage(this.TargetWindowHandle, (uint)WM.ACTIVATE, new(2), IntPtr.Zero);

                    return 3 /* MA_NOACTIVATE */;

                case WM.DISPLAYCHANGE:
                {
                    if (this.IsVisible)
                    {
                        this.RenderLayeredWindow();
                    }

                    break;
                }

                case WM.SHOWWINDOW:
                {
                    // Prevent glow from getting visible before the owner/parent is visible
                    if (lParam == SW_PARENTOPENING)
                    {
                        return default;
                    }

                    break;
                }
            }

            return base.WndProc(hwnd, msg, wParam, lParam);
        }

        private unsafe HT WmNcHitTest(IntPtr lParam)
        {
            if (this.IsDisposed)
            {
                return HT.NOWHERE;
            }

            var xLParam = PInvoke.GetXLParam(lParam.ToInt32());
            var yLParam = PInvoke.GetYLParam(lParam.ToInt32());
            RECT lpRect = default;
            PInvoke.GetWindowRect(this.Hwnd, &lpRect);

            switch (this.orientation)
            {
                case Dock.Left:
                    if (yLParam - this.cornerGripThickness < lpRect.top)
                    {
                        return HT.TOPLEFT;
                    }

                    if (yLParam + this.cornerGripThickness > lpRect.bottom)
                    {
                        return HT.BOTTOMLEFT;
                    }

                    return HT.LEFT;

                case Dock.Right:
                    if (yLParam - this.cornerGripThickness < lpRect.top)
                    {
                        return HT.TOPRIGHT;
                    }

                    if (yLParam + this.cornerGripThickness > lpRect.bottom)
                    {
                        return HT.BOTTOMRIGHT;
                    }

                    return HT.RIGHT;

                case Dock.Top:
                    if (xLParam - this.cornerGripThickness < lpRect.left)
                    {
                        return HT.TOPLEFT;
                    }

                    if (xLParam + this.cornerGripThickness > lpRect.right)
                    {
                        return HT.TOPRIGHT;
                    }

                    return HT.TOP;

                default:
                    if (xLParam - this.cornerGripThickness < lpRect.left)
                    {
                        return HT.BOTTOMLEFT;
                    }

                    if (xLParam + this.cornerGripThickness > lpRect.right)
                    {
                        return HT.BOTTOMRIGHT;
                    }

                    return HT.BOTTOM;
            }
        }

        public void CommitChanges(IntPtr windowPosInfo)
        {
            this.InvalidateCachedBitmaps();
            this.UpdateWindowPosCore(windowPosInfo);
            this.UpdateLayeredWindowCore();
            this.invalidatedValues = FieldInvalidationTypes.None;
        }

        private bool InvalidatedValuesHasFlag(FieldInvalidationTypes flag)
        {
            return (this.invalidatedValues & flag) != 0;
        }

        private void InvalidateCachedBitmaps()
        {
            if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.ActiveColor)
                || this.InvalidatedValuesHasFlag(FieldInvalidationTypes.GlowDepth))
            {
                ClearCache(this.activeGlowBitmaps);
            }

            if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.InactiveColor)
                || this.InvalidatedValuesHasFlag(FieldInvalidationTypes.GlowDepth))
            {
                ClearCache(this.inactiveGlowBitmaps);
            }
        }

        private void UpdateWindowPosCore(IntPtr windowPosInfo)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Location | FieldInvalidationTypes.Size | FieldInvalidationTypes.Visibility))
            {
                var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER;
                if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Visibility))
                {
                    flags = this.IsVisible
                        ? flags | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
                        : flags | SET_WINDOW_POS_FLAGS.SWP_HIDEWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
                }

                if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Location) == false)
                {
                    flags |= SET_WINDOW_POS_FLAGS.SWP_NOMOVE;
                }

                if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Size) == false)
                {
                    flags |= SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
                }

                if (windowPosInfo == IntPtr.Zero)
                {
                    PInvoke.SetWindowPos(this.Hwnd, default, this.Left, this.Top, this.Width, this.Height, flags);
                }
                else
                {
                    PInvoke.DeferWindowPos(windowPosInfo, this.Hwnd, default, this.Left, this.Top, this.Width, this.Height, flags);
                }
            }
        }

        private void UpdateLayeredWindowCore()
        {
            if (this.IsVisible
                && this.IsDisposed == false
                && this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Render))
            {
                if (this.IsPositionValid)
                {
                    this.BeginDelayedRender();
                    return;
                }

                this.CancelDelayedRender();
                this.RenderLayeredWindow();
            }
        }

        private void BeginDelayedRender()
        {
            if (this.pendingDelayRender == false)
            {
                this.pendingDelayRender = true;
                CompositionTarget.Rendering += this.CommitDelayedRender;
            }
        }

        private void CancelDelayedRender()
        {
            if (this.pendingDelayRender)
            {
                this.pendingDelayRender = false;
                CompositionTarget.Rendering -= this.CommitDelayedRender;
            }
        }

        private void CommitDelayedRender(object? sender, EventArgs e)
        {
            this.CancelDelayedRender();

            if (this.IsVisible
                && this.IsDisposed == false)
            {
                this.RenderLayeredWindow();
            }
        }

        private unsafe void RenderLayeredWindow()
        {
            if (this.IsDisposed
                || this.Width == 0
                || this.Height == 0)
            {
                return;
            }

            using var glowDrawingContext = new GlowDrawingContext(this.Width, this.Height);
            if (glowDrawingContext.IsInitialized == false)
            {
                return;
            }

            switch (this.orientation)
            {
                case Dock.Left:
                    this.DrawLeft(glowDrawingContext);
                    break;
                case Dock.Right:
                    this.DrawRight(glowDrawingContext);
                    break;
                case Dock.Top:
                    this.DrawTop(glowDrawingContext);
                    break;
                case Dock.Bottom:
                    this.DrawBottom(glowDrawingContext);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(this.orientation), this.orientation, null);
            }

            var pptDest = new System.Drawing.Point { X = this.Left, Y = this.Top };
            var psize = new SIZE { cx = this.Width, cy = this.Height };
            var pptSrc = new System.Drawing.Point { X = 0, Y = 0 };
            var color = default(COLORREF);

            fixed (BLENDFUNCTION* blend = &glowDrawingContext.Blend)
            {
                PInvoke.UpdateLayeredWindow(this.Hwnd,
                                            new HDC(glowDrawingContext.ScreenDc.DangerousGetHandle()),
                                            &pptDest,
                                            &psize,
                                            new HDC(glowDrawingContext.WindowDc.DangerousGetHandle()),
                                            &pptSrc,
                                            color,
                                            blend,
                                            UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);
            }
        }

        private GlowBitmap? GetOrCreateBitmap(GlowDrawingContext drawingContext, GlowBitmapPart bitmapPart)
        {
            if (drawingContext.ScreenDc is null)
            {
                return null;
            }

            GlowBitmap?[] array;
            Color color;

            if (this.IsActive)
            {
                array = this.activeGlowBitmaps;
                color = this.ActiveGlowColor;
            }
            else
            {
                array = this.inactiveGlowBitmaps;
                color = this.InactiveGlowColor;
            }

            return array[(int)bitmapPart] ?? (array[(int)bitmapPart] = GlowBitmap.Create(drawingContext, bitmapPart, color, this.GlowDepth, this.UseRadialGradientForCorners));
        }

        private static void ClearCache(GlowBitmap?[] cache)
        {
            for (var i = 0; i < cache.Length; i++)
            {
                using (cache[i])
                {
                    cache[i] = null;
                }
            }
        }

        protected override void DisposeManagedResources()
        {
            ClearCache(this.activeGlowBitmaps);
            ClearCache(this.inactiveGlowBitmaps);
        }

        private void DrawLeft(GlowDrawingContext drawingContext)
        {
            if (drawingContext.ScreenDc is null
                || drawingContext.WindowDc is null
                || drawingContext.BackgroundDc is null)
            {
                return;
            }

            var cornerTopLeftBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerTopLeft)!;
            var leftTopBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.LeftTop)!;
            var leftBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Left)!;
            var leftBottomBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.LeftBottom)!;
            var cornerBottomLeftBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerBottomLeft)!;

            var bitmapHeight = cornerTopLeftBitmap.Height;
            var num = bitmapHeight + leftTopBitmap.Height;
            var num2 = drawingContext.Height - cornerBottomLeftBitmap.Height;
            var num3 = num2 - leftBottomBitmap.Height;
            var num4 = num3 - num;

            PInvoke.SelectObject(drawingContext.BackgroundDc, cornerTopLeftBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, 0, cornerTopLeftBitmap.Width, cornerTopLeftBitmap.Height, drawingContext.BackgroundDc, 0, 0, cornerTopLeftBitmap.Width, cornerTopLeftBitmap.Height, drawingContext.Blend);
            PInvoke.SelectObject(drawingContext.BackgroundDc, leftTopBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, bitmapHeight, leftTopBitmap.Width, leftTopBitmap.Height, drawingContext.BackgroundDc, 0, 0, leftTopBitmap.Width, leftTopBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                PInvoke.SelectObject(drawingContext.BackgroundDc, leftBitmap.Handle);
                PInvoke.AlphaBlend(drawingContext.WindowDc, 0, num, leftBitmap.Width, num4, drawingContext.BackgroundDc, 0, 0, leftBitmap.Width, leftBitmap.Height, drawingContext.Blend);
            }

            PInvoke.SelectObject(drawingContext.BackgroundDc, leftBottomBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, num3, leftBottomBitmap.Width, leftBottomBitmap.Height, drawingContext.BackgroundDc, 0, 0, leftBottomBitmap.Width, leftBottomBitmap.Height, drawingContext.Blend);
            PInvoke.SelectObject(drawingContext.BackgroundDc, cornerBottomLeftBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, num2, cornerBottomLeftBitmap.Width, cornerBottomLeftBitmap.Height, drawingContext.BackgroundDc, 0, 0, cornerBottomLeftBitmap.Width, cornerBottomLeftBitmap.Height, drawingContext.Blend);
        }

        private void DrawRight(GlowDrawingContext drawingContext)
        {
            if (drawingContext.ScreenDc is null
                || drawingContext.WindowDc is null
                || drawingContext.BackgroundDc is null)
            {
                return;
            }

            var cornerTopRightBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerTopRight)!;
            var rightTopBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.RightTop)!;
            var rightBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Right)!;
            var rightBottomBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.RightBottom)!;
            var cornerBottomRightBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerBottomRight)!;

            var bitmapHeight = cornerTopRightBitmap.Height;
            var num = bitmapHeight + rightTopBitmap.Height;
            var num2 = drawingContext.Height - cornerBottomRightBitmap.Height;
            var num3 = num2 - rightBottomBitmap.Height;
            var num4 = num3 - num;

            PInvoke.SelectObject(drawingContext.BackgroundDc, cornerTopRightBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, 0, cornerTopRightBitmap.Width, cornerTopRightBitmap.Height, drawingContext.BackgroundDc, 0, 0, cornerTopRightBitmap.Width, cornerTopRightBitmap.Height, drawingContext.Blend);
            PInvoke.SelectObject(drawingContext.BackgroundDc, rightTopBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, bitmapHeight, rightTopBitmap.Width, rightTopBitmap.Height, drawingContext.BackgroundDc, 0, 0, rightTopBitmap.Width, rightTopBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                PInvoke.SelectObject(drawingContext.BackgroundDc, rightBitmap.Handle);
                PInvoke.AlphaBlend(drawingContext.WindowDc, 0, num, rightBitmap.Width, num4, drawingContext.BackgroundDc, 0, 0, rightBitmap.Width, rightBitmap.Height, drawingContext.Blend);
            }

            PInvoke.SelectObject(drawingContext.BackgroundDc, rightBottomBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, num3, rightBottomBitmap.Width, rightBottomBitmap.Height, drawingContext.BackgroundDc, 0, 0, rightBottomBitmap.Width, rightBottomBitmap.Height, drawingContext.Blend);
            PInvoke.SelectObject(drawingContext.BackgroundDc, cornerBottomRightBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, 0, num2, cornerBottomRightBitmap.Width, cornerBottomRightBitmap.Height, drawingContext.BackgroundDc, 0, 0, cornerBottomRightBitmap.Width, cornerBottomRightBitmap.Height, drawingContext.Blend);
        }

        private void DrawTop(GlowDrawingContext drawingContext)
        {
            if (drawingContext.ScreenDc is null
                || drawingContext.WindowDc is null
                || drawingContext.BackgroundDc is null)
            {
                return;
            }

            var topLeftBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.TopLeft)!;
            var topBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Top)!;
            var topRightBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.TopRight)!;

            var num = this.GlowDepth;
            var num2 = num + topLeftBitmap.Width;
            var num3 = drawingContext.Width - this.GlowDepth - topRightBitmap.Width;
            var num4 = num3 - num2;

            PInvoke.SelectObject(drawingContext.BackgroundDc, topLeftBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, num, 0, topLeftBitmap.Width, topLeftBitmap.Height, drawingContext.BackgroundDc, 0, 0, topLeftBitmap.Width, topLeftBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                PInvoke.SelectObject(drawingContext.BackgroundDc, topBitmap.Handle);
                PInvoke.AlphaBlend(drawingContext.WindowDc, num2, 0, num4, topBitmap.Height, drawingContext.BackgroundDc, 0, 0, topBitmap.Width, topBitmap.Height, drawingContext.Blend);
            }

            PInvoke.SelectObject(drawingContext.BackgroundDc, topRightBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, num3, 0, topRightBitmap.Width, topRightBitmap.Height, drawingContext.BackgroundDc, 0, 0, topRightBitmap.Width, topRightBitmap.Height, drawingContext.Blend);
        }

        private void DrawBottom(GlowDrawingContext drawingContext)
        {
            if (drawingContext.ScreenDc is null
                || drawingContext.WindowDc is null
                || drawingContext.BackgroundDc is null)
            {
                return;
            }

            var bottomLeftBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.BottomLeft)!;
            var bottomBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Bottom)!;
            var bottomRightBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.BottomRight)!;

            var num = this.GlowDepth;
            var num2 = num + bottomLeftBitmap.Width;
            var num3 = drawingContext.Width - this.GlowDepth - bottomRightBitmap.Width;
            var num4 = num3 - num2;

            PInvoke.SelectObject(drawingContext.BackgroundDc, bottomLeftBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, num, 0, bottomLeftBitmap.Width, bottomLeftBitmap.Height, drawingContext.BackgroundDc, 0, 0, bottomLeftBitmap.Width, bottomLeftBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                PInvoke.SelectObject(drawingContext.BackgroundDc, bottomBitmap.Handle);
                PInvoke.AlphaBlend(drawingContext.WindowDc, num2, 0, num4, bottomBitmap.Height, drawingContext.BackgroundDc, 0, 0, bottomBitmap.Width, bottomBitmap.Height, drawingContext.Blend);
            }

            PInvoke.SelectObject(drawingContext.BackgroundDc, bottomRightBitmap.Handle);
            PInvoke.AlphaBlend(drawingContext.WindowDc, num3, 0, bottomRightBitmap.Width, bottomRightBitmap.Height, drawingContext.BackgroundDc, 0, 0, bottomRightBitmap.Width, bottomRightBitmap.Height, drawingContext.Blend);
        }

        public void UpdateWindowPos()
        {
            var targetWindowHandle = this.TargetWindowHandle;

            if (this.IsVisible == false
                || PInvoke.GetMappedClientRect(targetWindowHandle, out var lpRect) == false)
            {
                return;
            }

            switch (this.orientation)
            {
                case Dock.Left:
                    this.Left = lpRect.left - this.GlowDepth;
                    this.Top = lpRect.top - this.GlowDepth;
                    this.Width = this.GlowDepth;
                    this.Height = lpRect.GetHeight() + this.GlowDepth + this.GlowDepth;
                    break;

                case Dock.Top:
                    this.Left = lpRect.left - this.GlowDepth;
                    this.Top = lpRect.top - this.GlowDepth;
                    this.Width = lpRect.GetWidth() + this.GlowDepth + this.GlowDepth;
                    this.Height = this.GlowDepth;
                    break;

                case Dock.Right:
                    this.Left = lpRect.right;
                    this.Top = lpRect.top - this.GlowDepth;
                    this.Width = this.GlowDepth;
                    this.Height = lpRect.GetHeight() + this.GlowDepth + this.GlowDepth;
                    break;

                case Dock.Bottom:
                    this.Left = lpRect.left - this.GlowDepth;
                    this.Top = lpRect.bottom;
                    this.Width = lpRect.GetWidth() + this.GlowDepth + this.GlowDepth;
                    this.Height = this.GlowDepth;
                    break;
            }
        }
    }
}