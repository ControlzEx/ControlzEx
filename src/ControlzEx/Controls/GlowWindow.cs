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
    using ControlzEx.Standard;
    using JetBrains.Annotations;

#pragma warning disable 618, SA1602, SA1401

    [PublicAPI]
    public abstract class HwndWrapper : DisposableObject
    {
        private IntPtr handle;

        private bool isHandleCreationAllowed = true;

        private ushort wndClassAtom;

        private Delegate? wndProc;

        public static int LastDestroyWindowError { get; private set; }

        [CLSCompliant(false)]
        protected ushort WindowClassAtom
        {
            get
            {
                if (this.wndClassAtom == 0)
                {
                    this.wndClassAtom = this.CreateWindowClassCore();
                }

                return this.wndClassAtom;
            }
        }

        public IntPtr Handle
        {
            get
            {
                this.EnsureHandle();
                return this.handle;
            }
        }

        protected virtual bool IsWindowSubClassed => false;

        [CLSCompliant(false)]
        protected virtual ushort CreateWindowClassCore()
        {
            return this.RegisterClass("ControlzEx_HwndWrapper_" + Guid.NewGuid().ToString());
        }

        protected virtual void DestroyWindowClassCore()
        {
            if (this.wndClassAtom != 0)
            {
                var moduleHandle = NativeMethods.GetModuleHandle(null);
                NativeMethods.UnregisterClass(this.wndClassAtom, moduleHandle);
                this.wndClassAtom = 0;
            }
        }

        [CLSCompliant(false)]
        protected ushort RegisterClass(string className)
        {
            var lpWndClass = default(WNDCLASS);
            lpWndClass.cbClsExtra = 0;
            lpWndClass.cbWndExtra = 0;
            lpWndClass.hbrBackground = IntPtr.Zero;
            lpWndClass.hCursor = IntPtr.Zero;
            lpWndClass.hIcon = IntPtr.Zero;
            lpWndClass.lpfnWndProc = this.wndProc = new WndProc(this.WndProc);
            lpWndClass.lpszClassName = className;
            lpWndClass.lpszMenuName = null;
            lpWndClass.style = 0u;
            return NativeMethods.RegisterClass(ref lpWndClass);
        }

        private void SubclassWndProc()
        {
            this.wndProc = new WndProc(this.WndProc);
            NativeMethods.SetWindowLongPtr(this.handle, GWL.WNDPROC, Marshal.GetFunctionPointerForDelegate(this.wndProc));
        }

        protected abstract IntPtr CreateWindowCore();

        protected virtual void DestroyWindowCore()
        {
            if (this.handle != IntPtr.Zero)
            {
                if (!NativeMethods.DestroyWindow(this.handle))
                {
                    LastDestroyWindowError = Marshal.GetLastWin32Error();
                }

                this.handle = IntPtr.Zero;
            }
        }

        protected virtual IntPtr WndProc(IntPtr hwnd, WM message, IntPtr wParam, IntPtr lParam)
        {
            return NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);
        }

        public void EnsureHandle()
        {
            if (this.handle != IntPtr.Zero)
            {
                return;
            }

            if (this.isHandleCreationAllowed == false)
            {
                return;
            }

            if (this.IsDisposed)
            {
                return;
            }

            this.isHandleCreationAllowed = false;
            this.handle = this.CreateWindowCore();

            if (this.IsWindowSubClassed)
            {
                this.SubclassWndProc();
            }
        }

        protected override void DisposeNativeResources()
        {
            this.isHandleCreationAllowed = false;
            this.DestroyWindowCore();
            this.DestroyWindowClassCore();
        }
    }

    public sealed class ChangeScope : DisposableObject
    {
        private readonly GlowWindowBehavior behavior;

        public ChangeScope(GlowWindowBehavior behavior)
        {
            this.behavior = behavior;
            this.behavior.DeferGlowChangesCount++;
        }

        protected override void DisposeManagedResources()
        {
            this.behavior.DeferGlowChangesCount--;
            if (this.behavior.DeferGlowChangesCount == 0)
            {
                this.behavior.EndDeferGlowChanges();
            }
        }
    }

    [PublicAPI]
    public class DisposableObject : IDisposable
    {
        private EventHandler? disposingEventHandlers;

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
            if (this.IsDisposed)
            {
                return;
            }

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

        private readonly IntPtr pbits;

        private readonly BITMAPINFO bitmapInfo;

        public SafeHBITMAP Handle { get; }

        public IntPtr DiBits => this.pbits;

        public int Width => this.bitmapInfo.bmiHeader.biWidth;

        public int Height => -this.bitmapInfo.bmiHeader.biHeight;

        public GlowBitmap(SafeDC hdcScreen, int width, int height)
        {
            this.bitmapInfo.bmiHeader.biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            this.bitmapInfo.bmiHeader.biPlanes = 1;
            this.bitmapInfo.bmiHeader.biBitCount = 32;
            this.bitmapInfo.bmiHeader.biCompression = 0;
            this.bitmapInfo.bmiHeader.biXPelsPerMeter = 0;
            this.bitmapInfo.bmiHeader.biYPelsPerMeter = 0;
            this.bitmapInfo.bmiHeader.biWidth = width;
            this.bitmapInfo.bmiHeader.biHeight = -height;
            this.Handle = NativeMethods.CreateDIBSection(hdcScreen, ref this.bitmapInfo, out this.pbits, IntPtr.Zero, 0);
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
        public BLENDFUNCTION Blend;

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

        public SafeDC? ScreenDc { get; }

        public SafeDC? WindowDc { get; }

        public SafeDC? BackgroundDc { get; }

        public int Width => this.windowBitmap?.Width ?? 0;

        public int Height => this.windowBitmap?.Height ?? 0;

        public GlowDrawingContext(int width, int height)
        {
            this.ScreenDc = SafeDC.GetDC(IntPtr.Zero);
            if (this.ScreenDc.DangerousGetHandle() == IntPtr.Zero)
            {
                return;
            }

            this.WindowDc = SafeDC.CreateCompatibleDC(this.ScreenDc);

            if (this.WindowDc.DangerousGetHandle() == IntPtr.Zero)
            {
                return;
            }

            this.BackgroundDc = SafeDC.CreateCompatibleDC(this.ScreenDc);

            if (this.BackgroundDc.DangerousGetHandle() == IntPtr.Zero)
            {
                return;
            }

            this.Blend.BlendOp = 0;
            this.Blend.BlendFlags = 0;
            this.Blend.SourceConstantAlpha = byte.MaxValue;
            this.Blend.AlphaFormat = AC.SRC_ALPHA;
            this.windowBitmap = new GlowBitmap(this.ScreenDc, width, height);
            NativeMethods.SelectObject(this.WindowDc, this.windowBitmap.Handle);
        }

        protected override void DisposeManagedResources()
        {
            this.windowBitmap?.Dispose();
        }

        protected override void DisposeNativeResources()
        {
            this.ScreenDc?.Dispose();

            this.WindowDc?.Dispose();

            this.BackgroundDc?.Dispose();
        }
    }

    [PublicAPI]
    public sealed class GlowWindow : HwndWrapper
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

        private const string GlowWindowClassName = "ControlzEx_GlowWindow";

        private readonly Window targetWindow;
        private readonly GlowWindowBehavior behavior;

        private readonly Dock orientation;

        private readonly GlowBitmap[] activeGlowBitmaps = new GlowBitmap[GlowBitmap.GlowBitmapPartCount];

        private readonly GlowBitmap[] inactiveGlowBitmaps = new GlowBitmap[GlowBitmap.GlowBitmapPartCount];

        private static ushort sharedWindowClassAtom;

        // Member to keep reference alive
        // ReSharper disable NotAccessedField.Local
#pragma warning disable IDE0052 // Remove unread private members
        private static WndProc? sharedWndProc;
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

        private bool IsDeferringChanges => this.behavior.DeferGlowChangesCount > 0;

        private static ushort SharedWindowClassAtom
        {
            get
            {
                if (sharedWindowClassAtom == 0)
                {
                    var lpWndClass = default(WNDCLASS);
                    lpWndClass.cbClsExtra = 0;
                    lpWndClass.cbWndExtra = 0;
                    lpWndClass.hbrBackground = IntPtr.Zero;
                    lpWndClass.hCursor = IntPtr.Zero;
                    lpWndClass.hIcon = IntPtr.Zero;
                    lpWndClass.lpfnWndProc = sharedWndProc = NativeMethods.DefWindowProc;
                    lpWndClass.lpszClassName = GlowWindowClassName;
                    lpWndClass.lpszMenuName = null;
                    lpWndClass.style = 0u;
                    sharedWindowClassAtom = NativeMethods.RegisterClass(ref lpWndClass);
                }

                return sharedWindowClassAtom;
            }
        }

        public bool IsVisible
        {
            get => this.isVisible;
            set => this.UpdateProperty(ref this.isVisible, value, FieldInvalidationTypes.Render | FieldInvalidationTypes.Visibility);
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

        private IntPtr TargetWindowHandle => new WindowInteropHelper(this.targetWindow).Handle;

        protected override bool IsWindowSubClassed => true;

        private bool IsPositionValid => !this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Location | FieldInvalidationTypes.Size | FieldInvalidationTypes.Visibility);

        public GlowWindow(Window owner, GlowWindowBehavior behavior, Dock orientation)
        {
            this.targetWindow = owner ?? throw new ArgumentNullException(nameof(owner));
            this.behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            this.orientation = orientation;
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
                this.CommitChanges();
            }
        }

        [CLSCompliant(false)]
        protected override ushort CreateWindowClassCore()
        {
            return SharedWindowClassAtom;
        }

        protected override void DestroyWindowClassCore()
        {
            // Do nothing here as we registered a shared class/atom
        }

        protected override IntPtr CreateWindowCore()
        {
            const WS_EX EX_STYLE = WS_EX.TOOLWINDOW | WS_EX.LAYERED | WS_EX.NOACTIVATE;
            const WS STYLE = WS.POPUP | WS.CLIPSIBLINGS | WS.CLIPCHILDREN;

            var windowHandle = NativeMethods.CreateWindowEx(EX_STYLE, new IntPtr(this.WindowClassAtom), string.Empty, STYLE, 0, 0, 0, 0, this.TargetWindowHandle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            return windowHandle;
        }

        public void ChangeOwner(IntPtr newOwner)
        {
            if (this.IsDisposed)
            {
                return;
            }

            NativeMethods.SetWindowLongPtr(this.Handle, GWL.HWNDPARENT, newOwner);
        }

        protected override IntPtr WndProc(IntPtr hwnd, WM message, IntPtr wParam, IntPtr lParam)
        {
            //System.Diagnostics.Trace.WriteLine($"{DateTime.Now} {message}");

            switch (message)
            {
                case WM.DESTROY:
                    this.Dispose();
                    break;

                case WM.NCHITTEST:
                    return new IntPtr((int)this.WmNcHitTest(lParam));

                case WM.NCLBUTTONDOWN:
                case WM.NCLBUTTONDBLCLK:
                case WM.NCRBUTTONDOWN:
                case WM.NCRBUTTONDBLCLK:
                case WM.NCMBUTTONDOWN:
                case WM.NCMBUTTONDBLCLK:
                case WM.NCXBUTTONDOWN:
                case WM.NCXBUTTONDBLCLK:
                {
                    var targetWindowHandle = this.TargetWindowHandle;
                    // WA_CLICKACTIVE = 2
                    NativeMethods.SendMessage(targetWindowHandle, WM.ACTIVATE, new IntPtr(2), IntPtr.Zero);
                    NativeMethods.SendMessage(targetWindowHandle, message, wParam, IntPtr.Zero);
                    return IntPtr.Zero;
                }

                case WM.WINDOWPOSCHANGED:
                case WM.WINDOWPOSCHANGING:
                {
                    var windowpos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS))!;
                    windowpos.flags |= SWP.NOACTIVATE;
                    Marshal.StructureToPtr(windowpos, lParam, true);
                    break;
                }

                case WM.SETFOCUS:
                    // Move focus back as we don't want to get focused
                    NativeMethods.SetFocus(wParam);
                    return IntPtr.Zero;

                case WM.ACTIVATE:
                    return IntPtr.Zero;

                case WM.NCACTIVATE:
                    NativeMethods.SendMessage(this.TargetWindowHandle, message, wParam, lParam);
                    // We have to return true according to https://docs.microsoft.com/en-us/windows/win32/winmsg/wm-ncactivate
                    // If we don't do that here the owner window can't be activated.
                    return new IntPtr(1);

                case WM.MOUSEACTIVATE:
                    NativeMethods.SendMessage(this.TargetWindowHandle, WM.ACTIVATE, wParam, lParam);
                    NativeMethods.SendMessage(this.TargetWindowHandle, WM.NCACTIVATE, new IntPtr(1), IntPtr.Zero);

                    return new IntPtr(3) /* MA_NOACTIVATE */;

                case WM.DISPLAYCHANGE:
                    if (this.IsVisible)
                    {
                        this.RenderLayeredWindow();
                    }

                    break;
            }

            return base.WndProc(hwnd, message, wParam, lParam);
        }

        private HT WmNcHitTest(IntPtr lParam)
        {
            if (this.IsDisposed)
            {
                return HT.NOWHERE;
            }

            var xLParam = Utility.GET_X_LPARAM(lParam);
            var yLParam = Utility.GET_Y_LPARAM(lParam);
            var lpRect = NativeMethods.GetWindowRect(this.Handle);

            switch (this.orientation)
            {
                case Dock.Left:
                    if (yLParam - this.cornerGripThickness < lpRect.Top)
                    {
                        return HT.TOPLEFT;
                    }

                    if (yLParam + this.cornerGripThickness > lpRect.Bottom)
                    {
                        return HT.BOTTOMLEFT;
                    }

                    return HT.LEFT;

                case Dock.Right:
                    if (yLParam - this.cornerGripThickness < lpRect.Top)
                    {
                        return HT.TOPRIGHT;
                    }

                    if (yLParam + this.cornerGripThickness > lpRect.Bottom)
                    {
                        return HT.BOTTOMRIGHT;
                    }

                    return HT.RIGHT;

                case Dock.Top:
                    if (xLParam - this.cornerGripThickness < lpRect.Left)
                    {
                        return HT.TOPLEFT;
                    }

                    if (xLParam + this.cornerGripThickness > lpRect.Right)
                    {
                        return HT.TOPRIGHT;
                    }

                    return HT.TOP;

                default:
                    if (xLParam - this.cornerGripThickness < lpRect.Left)
                    {
                        return HT.BOTTOMLEFT;
                    }

                    if (xLParam + this.cornerGripThickness > lpRect.Right)
                    {
                        return HT.BOTTOMRIGHT;
                    }

                    return HT.BOTTOM;
            }
        }

        public void CommitChanges()
        {
            this.InvalidateCachedBitmaps();
            this.UpdateWindowPosCore();
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

        private void UpdateWindowPosCore()
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Location | FieldInvalidationTypes.Size | FieldInvalidationTypes.Visibility))
            {
                var flags = SWP.NOZORDER | SWP.NOACTIVATE | SWP.NOOWNERZORDER;
                if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Visibility))
                {
                    flags = this.IsVisible
                        ? flags | SWP.SHOWWINDOW
                        : flags | SWP.HIDEWINDOW | SWP.NOMOVE | SWP.NOSIZE;
                }

                if (!this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Location))
                {
                    flags |= SWP.NOMOVE;
                }

                if (!this.InvalidatedValuesHasFlag(FieldInvalidationTypes.Size))
                {
                    flags |= SWP.NOSIZE;
                }

                NativeMethods.SetWindowPos(this.Handle, IntPtr.Zero, this.Left, this.Top, this.Width, this.Height, flags);
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

        private void RenderLayeredWindow()
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

            var pptDest = new POINT(this.Left, this.Top);
            var psize = new SIZE(this.Width, this.Height);
            var pptSrc = new POINT(0, 0);

            NativeMethods.UpdateLayeredWindow(this.Handle, glowDrawingContext.ScreenDc, ref pptDest, ref psize, glowDrawingContext.WindowDc, ref pptSrc, 0, ref glowDrawingContext.Blend, ULW.ALPHA);
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

            NativeMethods.SelectObject(drawingContext.BackgroundDc, cornerTopLeftBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, 0, cornerTopLeftBitmap.Width, cornerTopLeftBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, cornerTopLeftBitmap.Width, cornerTopLeftBitmap.Height, drawingContext.Blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, leftTopBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, bitmapHeight, leftTopBitmap.Width, leftTopBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, leftTopBitmap.Width, leftTopBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, leftBitmap.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num, leftBitmap.Width, num4, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, leftBitmap.Width, leftBitmap.Height, drawingContext.Blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, leftBottomBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num3, leftBottomBitmap.Width, leftBottomBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, leftBottomBitmap.Width, leftBottomBitmap.Height, drawingContext.Blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, cornerBottomLeftBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num2, cornerBottomLeftBitmap.Width, cornerBottomLeftBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, cornerBottomLeftBitmap.Width, cornerBottomLeftBitmap.Height, drawingContext.Blend);
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

            NativeMethods.SelectObject(drawingContext.BackgroundDc, cornerTopRightBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, 0, cornerTopRightBitmap.Width, cornerTopRightBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, cornerTopRightBitmap.Width, cornerTopRightBitmap.Height, drawingContext.Blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, rightTopBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, bitmapHeight, rightTopBitmap.Width, rightTopBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, rightTopBitmap.Width, rightTopBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, rightBitmap.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num, rightBitmap.Width, num4, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, rightBitmap.Width, rightBitmap.Height, drawingContext.Blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, rightBottomBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num3, rightBottomBitmap.Width, rightBottomBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, rightBottomBitmap.Width, rightBottomBitmap.Height, drawingContext.Blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, cornerBottomRightBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num2, cornerBottomRightBitmap.Width, cornerBottomRightBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, cornerBottomRightBitmap.Width, cornerBottomRightBitmap.Height, drawingContext.Blend);
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

            NativeMethods.SelectObject(drawingContext.BackgroundDc, topLeftBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num, 0, topLeftBitmap.Width, topLeftBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, topLeftBitmap.Width, topLeftBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, topBitmap.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num2, 0, num4, topBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, topBitmap.Width, topBitmap.Height, drawingContext.Blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, topRightBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num3, 0, topRightBitmap.Width, topRightBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, topRightBitmap.Width, topRightBitmap.Height, drawingContext.Blend);
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

            NativeMethods.SelectObject(drawingContext.BackgroundDc, bottomLeftBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num, 0, bottomLeftBitmap.Width, bottomLeftBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, bottomLeftBitmap.Width, bottomLeftBitmap.Height, drawingContext.Blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, bottomBitmap.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num2, 0, num4, bottomBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, bottomBitmap.Width, bottomBitmap.Height, drawingContext.Blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, bottomRightBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num3, 0, bottomRightBitmap.Width, bottomRightBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, bottomRightBitmap.Width, bottomRightBitmap.Height, drawingContext.Blend);
        }

        public void UpdateWindowPos()
        {
            var targetWindowHandle = this.TargetWindowHandle;

            if (this.IsVisible == false
                || NativeMethods.GetMappedClientRect(targetWindowHandle, out var lpRect) == false)
            {
                return;
            }

            switch (this.orientation)
            {
                case Dock.Left:
                    this.Left = lpRect.Left - this.GlowDepth;
                    this.Top = lpRect.Top - this.GlowDepth;
                    this.Width = this.GlowDepth;
                    this.Height = lpRect.Height + this.GlowDepth + this.GlowDepth;
                    break;

                case Dock.Top:
                    this.Left = lpRect.Left - this.GlowDepth;
                    this.Top = lpRect.Top - this.GlowDepth;
                    this.Width = lpRect.Width + this.GlowDepth + this.GlowDepth;
                    this.Height = this.GlowDepth;
                    break;

                case Dock.Right:
                    this.Left = lpRect.Right;
                    this.Top = lpRect.Top - this.GlowDepth;
                    this.Width = this.GlowDepth;
                    this.Height = lpRect.Height + this.GlowDepth + this.GlowDepth;
                    break;

                case Dock.Bottom:
                    this.Left = lpRect.Left - this.GlowDepth;
                    this.Top = lpRect.Bottom;
                    this.Width = lpRect.Width + this.GlowDepth + this.GlowDepth;
                    this.Height = this.GlowDepth;
                    break;
            }
        }
    }
}