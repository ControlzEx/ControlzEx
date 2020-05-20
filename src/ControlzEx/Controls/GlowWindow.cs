namespace ControlzEx.Controls
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using ControlzEx.Behaviors;
    using ControlzEx.Standard;

#pragma warning disable 618

    public abstract class HwndWrapper : DisposableObject
    {
        private IntPtr handle;

        private bool isHandleCreationAllowed = true;

        private ushort wndClassAtom;

        private Delegate wndProc;

        private static long failedDestroyWindows;

        private static int lastDestroyWindowError;

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
            return this.RegisterClass(Guid.NewGuid().ToString());
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
                    lastDestroyWindowError = Marshal.GetLastWin32Error();
                    failedDestroyWindows++;
                }

                this.handle = IntPtr.Zero;
            }
        }

        protected virtual IntPtr WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        public void EnsureHandle()
        {
            if (!(this.handle == IntPtr.Zero))
            {
                return;
            }

            if (!this.isHandleCreationAllowed)
            {
                //todo: error handling
                //VSDebug.Fail_Tagged("HWWR0001", "HwndWrapper.EnsureHandle should not be called when not allowed. Because of the current state, a new HWND will not be created", "Q:\\cmd\\13\\src\\env\\shell\\PackageFramework\\Current\\Shell\\UI\\Common\\HwndWrapper.cs", 152u);
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

    public class ChangeScope : DisposableObject
    {
        private readonly GlowWindowBehavior behavior;

        public ChangeScope(GlowWindowBehavior behavior)
        {
            this.behavior = behavior;
            this.behavior.deferGlowChangesCount++;
        }

        protected override void DisposeManagedResources()
        {
            this.behavior.deferGlowChangesCount--;
            if (this.behavior.deferGlowChangesCount == 0)
            {
                this.behavior.EndDeferGlowChanges();
            }
        }
    }

    public class DisposableObject : IDisposable
    {
        private EventHandler disposing;

        public bool IsDisposed { get; private set; }

        public event EventHandler Disposing
        {
            add
            {
                this.ThrowIfDisposed();
                this.disposing = (EventHandler)Delegate.Combine(this.disposing, value);
            }
            remove => this.disposing = (EventHandler)Delegate.Remove(this.disposing, value);
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

        protected void Dispose(bool disposing)
        {
            if (this.IsDisposed)
            {
                return;
            }

            try
            {
                if (disposing)
                {
                    this.disposing?.Invoke(this, EventArgs.Empty);
                    this.disposing = null;
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

    internal sealed class GlowBitmap : DisposableObject
    {
        private sealed class CachedBitmapInfo
        {
            public readonly int width;

            public readonly int height;

            public readonly byte[] diBits;

            public CachedBitmapInfo(byte[] diBits, int width, int height)
            {
                this.width = width;
                this.height = height;
                this.diBits = diBits;
            }
        }

        public const int GlowBitmapPartCount = 16;

        private const int BytesPerPixelBgra32 = 4;

        private static readonly CachedBitmapInfo[] transparencyMasks = new CachedBitmapInfo[GlowBitmapPartCount];

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

        public static GlowBitmap Create(GlowDrawingContext drawingContext, GlowBitmapPart bitmapPart, Color color)
        {
            var orCreateAlphaMask = GetOrCreateAlphaMask(bitmapPart);
            var glowBitmap = new GlowBitmap(drawingContext.ScreenDc, orCreateAlphaMask.width, orCreateAlphaMask.height);
            for (var i = 0; i < orCreateAlphaMask.diBits.Length; i += BytesPerPixelBgra32)
            {
                var b = orCreateAlphaMask.diBits[i + 3];
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

        private static CachedBitmapInfo GetOrCreateAlphaMask(GlowBitmapPart bitmapPart)
        {
            var num = (int)bitmapPart;
            if (transparencyMasks[num] is null)
            {
                var bitmapImage = new BitmapImage(MakePackUri(typeof(GlowBitmap).Assembly, "Resources/" + bitmapPart + ".png"));
                var array = new byte[BytesPerPixelBgra32 * bitmapImage.PixelWidth * bitmapImage.PixelHeight];
                var stride = BytesPerPixelBgra32 * bitmapImage.PixelWidth;
                bitmapImage.CopyPixels(array, stride, 0);
                transparencyMasks[num] = new CachedBitmapInfo(array, bitmapImage.PixelWidth, bitmapImage.PixelHeight);
            }

            return transparencyMasks[num];
        }

        private static Uri MakePackUri(Assembly assembly, string path)
        {
            System.Diagnostics.Trace.WriteLine(nameof(MakePackUri));
            var name = assembly.GetName().Name;
            return new Uri($"pack://application:,,,/{name};component/{path}", UriKind.Absolute);
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
        public BLENDFUNCTION blend;

        private readonly GlowBitmap windowBitmap;

        public bool IsInitialized
        {
            get
            {
                if (this.ScreenDc.DangerousGetHandle() != IntPtr.Zero
                    && this.WindowDc.DangerousGetHandle() != IntPtr.Zero
                    && this.BackgroundDc.DangerousGetHandle() != IntPtr.Zero)
                {
                    return this.windowBitmap != null;
                }

                return false;
            }
        }

        public SafeDC ScreenDc { get; }

        public SafeDC WindowDc { get; }

        public SafeDC BackgroundDc { get; }

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

            this.blend.BlendOp = 0;
            this.blend.BlendFlags = 0;
            this.blend.SourceConstantAlpha = byte.MaxValue;
            this.blend.AlphaFormat = AC.SRC_ALPHA;
            this.windowBitmap = new GlowBitmap(this.ScreenDc, width, height);
            NativeMethods.SelectObject(this.WindowDc, this.windowBitmap.Handle);
        }

        protected override void DisposeManagedResources()
        {
            this.windowBitmap?.Dispose();
        }

        protected override void DisposeNativeResources()
        {
            this.ScreenDc.Dispose();

            this.WindowDc.Dispose();

            this.BackgroundDc.Dispose();
        }
    }

    public sealed class GlowWindow : HwndWrapper
    {
        [Flags]
        private enum FieldInvalidationTypes
        {
            None = 0x0,
            Location = 0x1,
            Size = 0x2,
            ActiveColor = 0x4,
            InactiveColor = 0x8,
            Render = 0x10,
            Visibility = 0x20
        }

        private const string GlowWindowClassName = "ControlzExGlowWindow";

        private const int GlowDepth = 9;

        private const int CornerGripThickness = 18;

        private readonly Window targetWindow;
        private readonly GlowWindowBehavior behavior;

        private readonly Dock orientation;

        private readonly GlowBitmap[] activeGlowBitmaps = new GlowBitmap[GlowBitmap.GlowBitmapPartCount];

        private readonly GlowBitmap[] inactiveGlowBitmaps = new GlowBitmap[GlowBitmap.GlowBitmapPartCount];

        private static ushort sharedWindowClassAtom;

        // Member to keep reference alive
        // ReSharper disable NotAccessedField.Local
#pragma warning disable IDE0052 // Remove unread private members
        private static WndProc sharedWndProc;

        // For diagnostics
        private static long createdGlowWindows;

        // For diagnostics
        private static long disposedGlowWindows;
#pragma warning restore IDE0052 // Remove unread private members
        // ReSharper restore NotAccessedField.Local

        private int left;

        private int top;

        private int width;

        private int height;

        private bool isVisible;

        private bool isActive;

        private Color activeGlowColor = Colors.Transparent;

        private Color inactiveGlowColor = Colors.Transparent;

        private FieldInvalidationTypes invalidatedValues;

        private bool pendingDelayRender;

        private bool IsDeferringChanges => this.behavior.deferGlowChangesCount > 0;

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
            createdGlowWindows++;
        }

        private void UpdateProperty<T>(ref T field, T value, FieldInvalidationTypes invalidation) 
            where T : struct
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

        protected override ushort CreateWindowClassCore()
        {
            return SharedWindowClassAtom;
        }

        protected override void DestroyWindowClassCore()
        {
        }

        protected override IntPtr CreateWindowCore()
        {
            return NativeMethods.CreateWindowEx(524416, new IntPtr(this.WindowClassAtom), string.Empty, -2046820352, 0, 0, 0, 0, new WindowInteropHelper(this.targetWindow).Owner, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        public void ChangeOwner(IntPtr newOwner)
        {
            NativeMethods.SetWindowLongPtr(this.Handle, GWL.HWNDPARENT, newOwner);
        }

        protected override IntPtr WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM.NCHITTEST:
                    return new IntPtr(this.WmNcHitTest(lParam));

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
                    NativeMethods.SendMessage(targetWindowHandle, WM.ACTIVATE, new IntPtr(2), IntPtr.Zero);
                    NativeMethods.SendMessage(targetWindowHandle, msg, wParam, IntPtr.Zero);
                    return IntPtr.Zero;
                }

                case WM.WINDOWPOSCHANGING:
                {
                    var windowpos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                    windowpos.flags |= SWP.NOACTIVATE;
                    Marshal.StructureToPtr(windowpos, lParam, true);
                    break;
                }

                case WM.ACTIVATE:
                    return IntPtr.Zero;

                case WM.DISPLAYCHANGE:
                    if (this.IsVisible)
                    {
                        this.RenderLayeredWindow();
                    }

                    break;
            }

            return base.WndProc(hwnd, msg, wParam, lParam);
        }

        private int WmNcHitTest(IntPtr lParam)
        {
            var xLParam = Utility.GET_X_LPARAM(lParam);
            var yLParam = Utility.GET_Y_LPARAM(lParam);
            var lpRect = NativeMethods.GetWindowRect(this.Handle);

            switch (this.orientation)
            {
                case Dock.Left:
                    if (yLParam - CornerGripThickness < lpRect.Top)
                    {
                        return 13;
                    }

                    if (yLParam + CornerGripThickness > lpRect.Bottom)
                    {
                        return 16;
                    }

                    return 10;

                case Dock.Right:
                    if (yLParam - CornerGripThickness < lpRect.Top)
                    {
                        return 14;
                    }

                    if (yLParam + CornerGripThickness > lpRect.Bottom)
                    {
                        return 17;
                    }

                    return 11;

                case Dock.Top:
                    if (xLParam - CornerGripThickness < lpRect.Left)
                    {
                        return 13;
                    }

                    if (xLParam + CornerGripThickness > lpRect.Right)
                    {
                        return 14;
                    }

                    return 12;

                default:
                    if (xLParam - CornerGripThickness < lpRect.Left)
                    {
                        return 16;
                    }

                    if (xLParam + CornerGripThickness > lpRect.Right)
                    {
                        return 17;
                    }

                    return 15;
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
            if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.ActiveColor))
            {
                ClearCache(this.activeGlowBitmaps);
            }

            if (this.InvalidatedValuesHasFlag(FieldInvalidationTypes.InactiveColor))
            {
                ClearCache(this.inactiveGlowBitmaps);
            }
        }

        private void UpdateWindowPosCore()
        {
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

        private void CommitDelayedRender(object sender, EventArgs e)
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
            using (var glowDrawingContext = new GlowDrawingContext(this.Width, this.Height))
            {
                if (glowDrawingContext.IsInitialized)
                {
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
                        default:
                            this.DrawBottom(glowDrawingContext);
                            break;
                    }

                    var point = default(POINT);
                    point.X = this.Left;
                    point.Y = this.Top;
                    var pptDest = point;
                    var win32Size = default(SIZE);
                    win32Size.cx = this.Width;
                    win32Size.cy = this.Height;
                    var psize = win32Size;
                    point = default;
                    point.X = 0;
                    point.Y = 0;
                    var pptSrc = point;
                    NativeMethods.UpdateLayeredWindow(this.Handle, glowDrawingContext.ScreenDc, ref pptDest, ref psize, glowDrawingContext.WindowDc, ref pptSrc, 0, ref glowDrawingContext.blend, ULW.ALPHA);
                }
            }
        }

        private GlowBitmap GetOrCreateBitmap(GlowDrawingContext drawingContext, GlowBitmapPart bitmapPart)
        {
            GlowBitmap[] array;
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

            return array[(int)bitmapPart] ?? (array[(int)bitmapPart] = GlowBitmap.Create(drawingContext, bitmapPart, color));
        }

        private static void ClearCache(GlowBitmap[] cache)
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

        protected override void DisposeNativeResources()
        {
            base.DisposeNativeResources();
            disposedGlowWindows++;
        }

        private void DrawLeft(GlowDrawingContext drawingContext)
        {
            var orCreateBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerTopLeft);
            var orCreateBitmap2 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.LeftTop);
            var orCreateBitmap3 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Left);
            var orCreateBitmap4 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.LeftBottom);
            var orCreateBitmap5 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerBottomLeft);

            var bitmapHeight = orCreateBitmap.Height;
            var num = bitmapHeight + orCreateBitmap2.Height;
            var num2 = drawingContext.Height - orCreateBitmap5.Height;
            var num3 = num2 - orCreateBitmap4.Height;
            var num4 = num3 - num;

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap2.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, bitmapHeight, orCreateBitmap2.Width, orCreateBitmap2.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap2.Width, orCreateBitmap2.Height, drawingContext.blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap3.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num, orCreateBitmap3.Width, num4, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap3.Width, orCreateBitmap3.Height, drawingContext.blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap4.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num3, orCreateBitmap4.Width, orCreateBitmap4.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap4.Width, orCreateBitmap4.Height, drawingContext.blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap5.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num2, orCreateBitmap5.Width, orCreateBitmap5.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap5.Width, orCreateBitmap5.Height, drawingContext.blend);
        }

        private void DrawRight(GlowDrawingContext drawingContext)
        {
            var orCreateBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerTopRight);
            var orCreateBitmap2 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.RightTop);
            var orCreateBitmap3 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Right);
            var orCreateBitmap4 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.RightBottom);
            var orCreateBitmap5 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.CornerBottomRight);

            var bitmapHeight = orCreateBitmap.Height;
            var num = bitmapHeight + orCreateBitmap2.Height;
            var num2 = drawingContext.Height - orCreateBitmap5.Height;
            var num3 = num2 - orCreateBitmap4.Height;
            var num4 = num3 - num;

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap2.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, bitmapHeight, orCreateBitmap2.Width, orCreateBitmap2.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap2.Width, orCreateBitmap2.Height, drawingContext.blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap3.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num, orCreateBitmap3.Width, num4, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap3.Width, orCreateBitmap3.Height, drawingContext.blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap4.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num3, orCreateBitmap4.Width, orCreateBitmap4.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap4.Width, orCreateBitmap4.Height, drawingContext.blend);
            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap5.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), 0, num2, orCreateBitmap5.Width, orCreateBitmap5.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap5.Width, orCreateBitmap5.Height, drawingContext.blend);
        }

        private void DrawTop(GlowDrawingContext drawingContext)
        {
            var orCreateBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.TopLeft);
            var orCreateBitmap2 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Top);
            var orCreateBitmap3 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.TopRight);

            var num = GlowDepth;
            var num2 = num + orCreateBitmap.Width;
            var num3 = drawingContext.Width - GlowDepth - orCreateBitmap3.Width;
            var num4 = num3 - num2;

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap2.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num2, 0, num4, orCreateBitmap2.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap2.Width, orCreateBitmap2.Height, drawingContext.blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap3.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num3, 0, orCreateBitmap3.Width, orCreateBitmap3.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap3.Width, orCreateBitmap3.Height, drawingContext.blend);
        }

        private void DrawBottom(GlowDrawingContext drawingContext)
        {
            var orCreateBitmap = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.BottomLeft);
            var orCreateBitmap2 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.Bottom);
            var orCreateBitmap3 = this.GetOrCreateBitmap(drawingContext, GlowBitmapPart.BottomRight);

            var num = GlowDepth;
            var num2 = num + orCreateBitmap.Width;
            var num3 = drawingContext.Width - GlowDepth - orCreateBitmap3.Width;
            var num4 = num3 - num2;

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap.Width, orCreateBitmap.Height, drawingContext.blend);

            if (num4 > 0)
            {
                NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap2.Handle);
                NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num2, 0, num4, orCreateBitmap2.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap2.Width, orCreateBitmap2.Height, drawingContext.blend);
            }

            NativeMethods.SelectObject(drawingContext.BackgroundDc, orCreateBitmap3.Handle);
            NativeMethods.AlphaBlend(drawingContext.WindowDc.DangerousGetHandle(), num3, 0, orCreateBitmap3.Width, orCreateBitmap3.Height, drawingContext.BackgroundDc.DangerousGetHandle(), 0, 0, orCreateBitmap3.Width, orCreateBitmap3.Height, drawingContext.blend);
        }

        public void UpdateWindowPos()
        {
            var targetWindowHandle = this.TargetWindowHandle;
            var lpRect = NativeMethods.GetWindowRect(targetWindowHandle);

            if (this.IsVisible == false)
            {
                return;
            }

            switch (this.orientation)
            {
                case Dock.Left:
                    this.Left = lpRect.Left - GlowDepth;
                    this.Top = lpRect.Top - GlowDepth;
                    this.Width = GlowDepth;
                    this.Height = lpRect.Height + CornerGripThickness;
                    break;

                case Dock.Top:
                    this.Left = lpRect.Left - GlowDepth;
                    this.Top = lpRect.Top - GlowDepth;
                    this.Width = lpRect.Width + CornerGripThickness;
                    this.Height = GlowDepth;
                    break;

                case Dock.Right:
                    this.Left = lpRect.Right;
                    this.Top = lpRect.Top - GlowDepth;
                    this.Width = GlowDepth;
                    this.Height = lpRect.Height + CornerGripThickness;
                    break;

                case Dock.Bottom:
                    this.Left = lpRect.Left - GlowDepth;
                    this.Top = lpRect.Bottom;
                    this.Width = lpRect.Width + CornerGripThickness;
                    this.Height = GlowDepth;
                    break;
            }
        }
    }
}