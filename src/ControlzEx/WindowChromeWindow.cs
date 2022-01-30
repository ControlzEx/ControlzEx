#pragma warning disable 618
namespace ControlzEx
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using ControlzEx.Behaviors;
    using ControlzEx.Internal.KnownBoxes;
    using ControlzEx.Standard;
    using ControlzEx.Theming;
    using Microsoft.Xaml.Behaviors;

    public class WindowChromeWindow : Window
    {
        static WindowChromeWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(typeof(WindowChromeWindow)));

            WindowStyleProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(WindowStyle.SingleBorderWindow));

            AllowsTransparencyProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));
        }

        public WindowChromeWindow()
        {
            this.InitializeBehaviors();
        }

        private void InitializeBehaviors()
        {
            this.InitializeWindowChromeBehavior();

            this.InitializeGlowWindowBehavior();
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
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.EnableMinimizeProperty, new Binding { Path = new PropertyPath(ShowMinButtonProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.EnableMaxRestoreProperty, new Binding { Path = new PropertyPath(ShowMaxRestoreButtonProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.CornerPreferenceProperty, new Binding { Path = new PropertyPath(CornerPreferenceProperty), Source = this });

            this.SetBinding(IsNCActiveProperty, new Binding { Path = new PropertyPath(WindowChromeBehavior.IsNCActiveProperty), Source = behavior });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        private void InitializeGlowWindowBehavior()
        {
            var behavior = new GlowWindowBehavior();
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.GlowDepthProperty, new Binding { Path = new PropertyPath(GlowDepthProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.GlowColorProperty, new Binding { Path = new PropertyPath(GlowColorProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.NonActiveGlowColorProperty, new Binding { Path = new PropertyPath(NonActiveGlowColorProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.UseRadialGradientForCornersProperty, new Binding { Path = new PropertyPath(UseRadialGradientForCornersProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.IsGlowTransitionEnabledProperty, new Binding { Path = new PropertyPath(IsGlowTransitionEnabledProperty), Source = this });
            BindingOperations.SetBinding(behavior, GlowWindowBehavior.PreferDWMBorderColorProperty, new Binding { Path = new PropertyPath(PreferDWMBorderColorProperty), Source = this });

            this.SetBinding(DWMSupportsBorderColorProperty, new Binding { Path = new PropertyPath(GlowWindowBehavior.DWMSupportsBorderColorProperty), Source = behavior });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        public Thickness ResizeBorderThickness
        {
            get => (Thickness)this.GetValue(ResizeBorderThicknessProperty);
            set => this.SetValue(ResizeBorderThicknessProperty, value);
        }

        // Using a DependencyProperty as the backing store for ResizeBorderThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.ResizeBorderThicknessProperty.DefaultMetadata.DefaultValue));

        public int GlowDepth
        {
            get => (int)this.GetValue(GlowDepthProperty);
            set => this.SetValue(GlowDepthProperty, value);
        }

        // Using a DependencyProperty as the backing store for GlowDepth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GlowDepthProperty =
            DependencyProperty.Register(nameof(GlowDepth), typeof(int), typeof(WindowChromeWindow), new PropertyMetadata(GlowWindowBehavior.GlowDepthProperty.DefaultMetadata.DefaultValue));

        public static readonly DependencyProperty UseRadialGradientForCornersProperty = DependencyProperty.Register(
            nameof(UseRadialGradientForCorners), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(GlowWindowBehavior.UseRadialGradientForCornersProperty.DefaultMetadata.DefaultValue));

        public bool UseRadialGradientForCorners
        {
            get => (bool)this.GetValue(UseRadialGradientForCornersProperty);
            set => this.SetValue(UseRadialGradientForCornersProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty IsGlowTransitionEnabledProperty = DependencyProperty.Register(
            nameof(IsGlowTransitionEnabled), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(GlowWindowBehavior.IsGlowTransitionEnabledProperty.DefaultMetadata.DefaultValue));

        public bool IsGlowTransitionEnabled
        {
            get => (bool)this.GetValue(IsGlowTransitionEnabledProperty);
            set => this.SetValue(IsGlowTransitionEnabledProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty = DependencyProperty.Register(nameof(IgnoreTaskbarOnMaximize), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty.DefaultMetadata.DefaultValue));

        public bool IgnoreTaskbarOnMaximize
        {
            get => (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty);
            set => this.SetValue(IgnoreTaskbarOnMaximizeProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        /// Gets/sets if the border thickness value should be kept on maximize
        /// if the MaxHeight/MaxWidth of the window is less than the monitor resolution.
        /// </summary>
        public bool KeepBorderOnMaximize
        {
            get => (bool)this.GetValue(KeepBorderOnMaximizeProperty);
            set => this.SetValue(KeepBorderOnMaximizeProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="KeepBorderOnMaximize"/>.
        /// </summary>
        public static readonly DependencyProperty KeepBorderOnMaximizeProperty = DependencyProperty.Register(nameof(KeepBorderOnMaximize), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.TrueBox));

        public static readonly DependencyProperty ShowMinButtonProperty = DependencyProperty.Register(nameof(ShowMinButton), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Gets or sets whether if the minimize button is visible.
        /// </summary>
        public bool ShowMinButton
        {
            get => (bool)this.GetValue(ShowMinButtonProperty);
            set => this.SetValue(ShowMinButtonProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty ShowMaxRestoreButtonProperty = DependencyProperty.Register(nameof(ShowMaxRestoreButton), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Gets or sets whether if the Maximize/Restore button is visible.
        /// </summary>
        public bool ShowMaxRestoreButton
        {
            get => (bool)this.GetValue(ShowMaxRestoreButtonProperty);
            set => this.SetValue(ShowMaxRestoreButtonProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="GlowColor"/>.
        /// </summary>
        public static readonly DependencyProperty GlowColorProperty = DependencyProperty.Register(nameof(GlowColor), typeof(Color?), typeof(WindowChromeWindow), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is active.
        /// </summary>
        public Color? GlowColor
        {
            get => (Color?)this.GetValue(GlowColorProperty);
            set => this.SetValue(GlowColorProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="NonActiveGlowColor"/>.
        /// </summary>
        public static readonly DependencyProperty NonActiveGlowColorProperty = DependencyProperty.Register(nameof(NonActiveGlowColor), typeof(Color?), typeof(WindowChromeWindow), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a brush which is used as the glow when the window is not active.
        /// </summary>
        public Color? NonActiveGlowColor
        {
            get => (Color?)this.GetValue(NonActiveGlowColorProperty);
            set => this.SetValue(NonActiveGlowColorProperty, value);
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="IsNCActive"/>.
        /// </summary>
        public static readonly DependencyProperty IsNCActiveProperty = DependencyProperty.Register(nameof(IsNCActive), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Gets whether the non-client area is active or not.
        /// </summary>
        public bool IsNCActive
        {
            get => (bool)this.GetValue(IsNCActiveProperty);
            private set => this.SetValue(IsNCActiveProperty, BooleanBoxes.Box(value));
        }

        public static readonly DependencyProperty NCActiveBrushProperty = DependencyProperty.Register(nameof(NCActiveBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        public Brush? NCActiveBrush
        {
            get => (Brush?)this.GetValue(NCActiveBrushProperty);
            set => this.SetValue(NCActiveBrushProperty, value);
        }

        public static readonly DependencyProperty NCNonActiveBrushProperty = DependencyProperty.Register(nameof(NCNonActiveBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        public Brush? NCNonActiveBrush
        {
            get => (Brush?)this.GetValue(NCNonActiveBrushProperty);
            set => this.SetValue(NCNonActiveBrushProperty, value);
        }

        public static readonly DependencyProperty NCCurrentBrushProperty = DependencyProperty.Register(nameof(NCCurrentBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        public Brush? NCCurrentBrush
        {
            get => (Brush?)this.GetValue(NCCurrentBrushProperty);
            set => this.SetValue(NCCurrentBrushProperty, value);
        }

        /// <summary>Identifies the <see cref="PreferDWMBorderColor"/> dependency property.</summary>
        public static readonly DependencyProperty PreferDWMBorderColorProperty =
            DependencyProperty.Register(nameof(PreferDWMBorderColor), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <inheritdoc cref="GlowWindowBehavior.PreferDWMBorderColor"/>
        public bool PreferDWMBorderColor
        {
            get => (bool)this.GetValue(PreferDWMBorderColorProperty);
            set => this.SetValue(PreferDWMBorderColorProperty, BooleanBoxes.Box(value));
        }

        /// <summary>
        /// <see cref="DependencyProperty"/> for <see cref="DWMSupportsBorderColor"/>.
        /// </summary>
        public static readonly DependencyProperty DWMSupportsBorderColorProperty = DependencyProperty.Register(nameof(DWMSupportsBorderColor), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <inheritdoc cref="GlowWindowBehavior.DWMSupportsBorderColor"/>
        public bool DWMSupportsBorderColor
        {
            get => (bool)this.GetValue(DWMSupportsBorderColorProperty);
            private set => this.SetValue(DWMSupportsBorderColorProperty, BooleanBoxes.Box(value));
        }

#pragma warning disable WPF0010
        public static readonly DependencyProperty CornerPreferenceProperty = DependencyProperty.Register(
            nameof(CornerPreference), typeof(DWM_WINDOW_CORNER_PREFERENCE), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.CornerPreferenceProperty.DefaultMetadata.DefaultValue));
#pragma warning restore WPF0010

        public DWM_WINDOW_CORNER_PREFERENCE CornerPreference
        {
            get => (DWM_WINDOW_CORNER_PREFERENCE)this.GetValue(CornerPreferenceProperty);
            set => this.SetValue(CornerPreferenceProperty, value);
        }
    }
}