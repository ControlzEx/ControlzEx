#pragma warning disable 1591, 618
#pragma warning disable CA1816
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1308 // Variable names should not be prefixed
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1310 // Field names should not contain underscore
namespace ControlzEx.Standard
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Threading;

    internal sealed class MessageWindow : DispatcherObject, IDisposable
    {
        // Alias this to a static so the wrapper doesn't get GC'd
        private static readonly WndProc s_WndProc = new WndProc(_WndProc);
        private static readonly Dictionary<IntPtr, MessageWindow> s_windowLookup = new Dictionary<IntPtr, MessageWindow>();

        private WndProc _wndProcCallback;
        private string _className;
        private bool _isDisposed;

        public IntPtr Handle { get; private set; }

        public MessageWindow(CS classStyle, WS style, WS_EX exStyle, Rect location, string name, WndProc callback)
        {
            // A null callback means just use DefWindowProc.
            this._wndProcCallback = callback;
            this._className = "MessageWindowClass+" + Guid.NewGuid().ToString();

            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = classStyle,
                lpfnWndProc = s_WndProc,
                hInstance = NativeMethods.GetModuleHandle(null),
                hbrBackground = NativeMethods.GetStockObject(StockObject.NULL_BRUSH),
                lpszMenuName = string.Empty,
                lpszClassName = this._className,
            };

            NativeMethods.RegisterClassEx(ref wc);

            GCHandle gcHandle = default(GCHandle);
            try
            {
                gcHandle = GCHandle.Alloc(this);
                IntPtr pinnedThisPtr = (IntPtr)gcHandle;

                this.Handle = NativeMethods.CreateWindowEx(
                    exStyle,
                    this._className,
                    name,
                    style,
                    (int)location.X,
                    (int)location.Y,
                    (int)location.Width,
                    (int)location.Height,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    pinnedThisPtr);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        ~MessageWindow()
        {
            this._Dispose(false, false);
        }

        public void Dispose()
        {
            this._Dispose(true, false);
            GC.SuppressFinalize(this);
        }

        // This isn't right if the Dispatcher has already started shutting down.

        /* Unmerged change from project 'ControlzEx (net5.0-windows)'
        Before:
                // The HWND itself will get cleaned up on thread completion, but it will wind up leaking the class ATOM...
                [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "disposing")]
        After:
                // The HWND itself will get cleaned up on thread completion, but it will wind up leaking the class ATOM...
        */
        // The HWND itself will get cleaned up on thread completion, but it will wind up leaking the class ATOM...
        private void _Dispose(bool disposing, bool isHwndBeingDestroyed)
        {
            if (this._isDisposed)
            {
                // Block against reentrancy.
                return;
            }

            this._isDisposed = true;

            IntPtr hwnd = this.Handle;
            string className = this._className;

            if (isHwndBeingDestroyed)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)(arg => _DestroyWindow(IntPtr.Zero, className)));
            }
            else if (this.Handle != IntPtr.Zero)
            {
                if (this.CheckAccess())
                {
                    _DestroyWindow(hwnd, className);
                }
                else
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)(arg => _DestroyWindow(hwnd, className)));
                }
            }

            s_windowLookup.Remove(hwnd);

            this._className = null!;
            this.Handle = IntPtr.Zero;
        }

        private static IntPtr _WndProc(IntPtr hwnd, WM msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr ret = IntPtr.Zero;
            MessageWindow? hwndWrapper = null;

            if (msg == WM.CREATE)
            {
                var createStruct = (CREATESTRUCT)Marshal.PtrToStructure(lParam, typeof(CREATESTRUCT))!;
                GCHandle gcHandle = GCHandle.FromIntPtr(createStruct.lpCreateParams);
                hwndWrapper = (MessageWindow)gcHandle.Target!;
                s_windowLookup.Add(hwnd, hwndWrapper);
            }
            else
            {
                if (!s_windowLookup.TryGetValue(hwnd, out hwndWrapper))
                {
                    return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
                }
            }

            Assert.IsNotNull(hwndWrapper);

            WndProc callback = hwndWrapper._wndProcCallback;
            if (callback is not null)
            {
                ret = callback(hwnd, msg, wParam, lParam);
            }
            else
            {
                ret = NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
            }

            if (msg == WM.NCDESTROY)
            {
                hwndWrapper._Dispose(true, true);
                GC.SuppressFinalize(hwndWrapper);
            }

            return ret;
        }

        private static object? _DestroyWindow(IntPtr hwnd, string className)
        {
            Utility.SafeDestroyWindow(ref hwnd);
            NativeMethods.UnregisterClass(className, NativeMethods.GetModuleHandle(null));
            return null;
        }
    }
}
