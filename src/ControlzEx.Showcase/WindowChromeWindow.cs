namespace ControlzEx.Showcase
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Threading;
    using ControlzEx.Behaviors;
    using Microsoft.Xaml.Behaviors;

    public class WindowChromeWindow : Window
    {
        static WindowChromeWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(typeof(WindowChromeWindow)));

            BorderThicknessProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(new Thickness(1)));
            WindowStyleProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(WindowStyle.SingleBorderWindow));

            AllowsTransparencyProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(false));
        }

        public WindowChromeWindow()
        {
            this.InitializeBehaviors();

            // Using Loaded causes the glow to show and then window window startup animation renders into that "frame"
            //this.Loaded += this.WindowChromeWindow_Loaded;

            // Using ContentRendered causes the window startup animation to show and then shows the glow
            this.ContentRendered += this.WindowChromeWindow_ContentRendered;
        }

        private void WindowChromeWindow_ContentRendered(object sender, EventArgs e)
        {
            this.ContentRendered -= this.WindowChromeWindow_ContentRendered;
            this.InitializeGlowWindowBehavior();
        }

        private void WindowChromeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.WindowChromeWindow_Loaded;
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(this.InitializeGlowWindowBehavior));
        }

        private void InitializeBehaviors()
        {
            this.InitializeWindowChromeBehavior();

            // Uncommenting this causes the window startup animation to not work
            //this.InitializeGlowWindowBehavior();
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        private void InitializeWindowChromeBehavior()
        {
            var behavior = new WindowChromeBehavior();
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty, new Binding { Path = new PropertyPath(IgnoreTaskbarOnMaximizeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.KeepBorderOnMaximizeProperty, new Binding { Path = new PropertyPath(KeepBorderOnMaximizeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.TryToBeFlickerFreeProperty, new Binding { Path = new PropertyPath(TryToBeFlickerFreeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.EnableMinimizeProperty, new Binding { Path = new PropertyPath(ShowMinButtonProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.EnableMaxRestoreProperty, new Binding { Path = new PropertyPath(ShowMaxRestoreButtonProperty), Source = this });

            this.SetBinding(IsNCActiveProperty, new Binding { Path = new PropertyPath(WindowChromeBehavior.IsNCActiveProperty), Source = behavior });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        private void InitializeGlowWindowBehavior()
        {
            var behavior = new GlowWindowBehavior();
            //behavior.IsGlowTransitionEnabled = true;
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.GlowBrushProperty, new Binding { Path = new PropertyPath(GlowBrushProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.NonActiveGlowBrushProperty, new Binding { Path = new PropertyPath(NonActiveGlowBrushProperty), Source = this });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        public Thickness ResizeBorderThickness
        {
            get { return (Thickness)this.GetValue(ResizeBorderThicknessProperty); }
            set { this.SetValue(ResizeBorderThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ResizeBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.ResizeBorderThicknessProperty.DefaultMetadata.DefaultValue));

        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty = DependencyProperty.Register(nameof(IgnoreTaskbarOnMaximize), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty.DefaultMetadata.DefaultValue));

        public bool IgnoreTaskbarOnMaximize
        {
            get { return (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty); }
            set { this.SetValue(IgnoreTaskbarOnMaximizeProperty, value); }
        }

        /// <summary>
        /// Gets/sets if the border thickness value should be kept on maximize
        /// if the MaxHeight/MaxWidth of the window is less than the monitor resolution.
        /// </summary>
        public bool KeepBorderOnMaximize
        {
            get { return (bool)this.GetValue(KeepBorderOnMaximizeProperty); }
            set { this.SetValue(KeepBorderOnMaximizeProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="KeepBorderOnMaximize"/>.
        /// </summary>
        public static readonly DependencyProperty KeepBorderOnMaximizeProperty = DependencyProperty.Register(nameof(KeepBorderOnMaximize), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the resizing of the window should be tried in a way that does not cause flicker/jitter, especially when resizing from the left side.
        /// </summary>
        /// <remarks>
        /// Please note that setting this to <c>true</c> may cause resize lag and black areas appearing on some systems.
        /// </remarks>
        public bool TryToBeFlickerFree
        {
            get { return (bool)this.GetValue(TryToBeFlickerFreeProperty); }
            set { this.SetValue(TryToBeFlickerFreeProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="TryToBeFlickerFree"/>.
        /// </summary>
        public static readonly DependencyProperty TryToBeFlickerFreeProperty = DependencyProperty.Register(nameof(TryToBeFlickerFree), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(default(bool)));

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        public static readonly DependencyProperty ShowMinButtonProperty = DependencyProperty.Register(nameof(ShowMinButton), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether if the minimize button is visible.
        /// </summary>
        public bool ShowMinButton
        {
            get { return (bool)this.GetValue(ShowMinButtonProperty); }
            set { this.SetValue(ShowMinButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowMaxRestoreButtonProperty = DependencyProperty.Register(nameof(ShowMaxRestoreButton), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether if the Maximize/Restore button is visible.
        /// </summary>
        public bool ShowMaxRestoreButton
        {
            get { return (bool)this.GetValue(ShowMaxRestoreButtonProperty); }
            set { this.SetValue(ShowMaxRestoreButtonProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is active.
        /// </summary>
        public Brush GlowBrush
        {
            get { return (Brush)this.GetValue(GlowBrushProperty); }
            set { this.SetValue(GlowBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="NonActiveGlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty NonActiveGlowBrushProperty = DependencyProperty.Register(nameof(NonActiveGlowBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is not active.
        /// </summary>
        public Brush NonActiveGlowBrush
        {
            get { return (Brush)this.GetValue(NonActiveGlowBrushProperty); }
            set { this.SetValue(NonActiveGlowBrushProperty, value); }
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="IsNCActive"/>.
        /// </summary>
        public static readonly DependencyProperty IsNCActiveProperty = DependencyProperty.Register(nameof(IsNCActive), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Gets whether the non-client area is active or not.
        /// </summary>
        public bool IsNCActive
        {
            get { return (bool)this.GetValue(IsNCActiveProperty); }
            private set { this.SetValue(IsNCActiveProperty, value); }
        }

        public static readonly DependencyProperty NCActiveBrushProperty = DependencyProperty.Register(nameof(NCActiveBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        public Brush NCActiveBrush
        {
            get { return (Brush)this.GetValue(NCActiveBrushProperty); }
            set { this.SetValue(NCActiveBrushProperty, value); }
        }

        public static readonly DependencyProperty NCNonActiveBrushProperty = DependencyProperty.Register(nameof(NCNonActiveBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        public Brush NCNonActiveBrush
        {
            get { return (Brush)this.GetValue(NCNonActiveBrushProperty); }
            set { this.SetValue(NCNonActiveBrushProperty, value); }
        }

        public static readonly DependencyProperty NCCurrentBrushProperty = DependencyProperty.Register(nameof(NCCurrentBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        public Brush NCCurrentBrush
        {
            get { return (Brush)this.GetValue(NCCurrentBrushProperty); }
            set { this.SetValue(NCCurrentBrushProperty, value); }
        }
    }
}