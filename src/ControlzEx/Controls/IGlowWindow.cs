namespace ControlzEx.Controls
{
    using System;
    using System.Windows.Media;
    using global::Windows.Win32.Foundation;

    public interface IGlowWindow : IDisposable
    {
        IntPtr Handle { get; }

        bool IsVisible { get; set; }

        bool IsActive { get; set; }

        Color ActiveGlowColor { get; set; }

        Color InactiveGlowColor { get; set; }

        int GlowDepth { get; set; }

        bool UseRadialGradientForCorners { get; set; }

        IntPtr EnsureHandle();

        void CommitChanges(IntPtr windowPosInfo);

        void UpdateWindowPos();
    }
}