#pragma warning disable 618
namespace ControlzEx.Showcase
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Shell;
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
}