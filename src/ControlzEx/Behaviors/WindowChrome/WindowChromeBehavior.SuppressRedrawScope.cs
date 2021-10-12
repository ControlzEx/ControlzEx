#pragma warning disable 618
#pragma warning disable SA1300 // Element should begin with upper-case letter
// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System;
    using ControlzEx.Native;
    using ControlzEx.Standard;

    public partial class WindowChromeBehavior
    {
        private class SuppressRedrawScope : IDisposable
        {
            private readonly IntPtr hwnd;

            private readonly bool suppressedRedraw;

            public SuppressRedrawScope(IntPtr hwnd)
            {
                this.hwnd = hwnd;

                if (((WS)NativeMethods.GetWindowLongPtr(hwnd, GWL.STYLE) & WS.VISIBLE) != 0)
                {
                    this.SetRedraw(state: false);
                    this.suppressedRedraw = true;
                }
            }

            public void Dispose()
            {
                if (this.suppressedRedraw)
                {
                    this.SetRedraw(state: true);
                    const Constants.RedrawWindowFlags FLAGS = Constants.RedrawWindowFlags.Invalidate | Constants.RedrawWindowFlags.AllChildren | Constants.RedrawWindowFlags.Frame;
                    NativeMethods.RedrawWindow(this.hwnd, IntPtr.Zero, IntPtr.Zero, FLAGS);
                }
            }

            private void SetRedraw(bool state)
            {
                NativeMethods.SendMessage(this.hwnd, WM.SETREDRAW, new IntPtr(Convert.ToInt32(state)), IntPtr.Zero);
            }
        }
    }
}