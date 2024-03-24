#pragma warning disable 618
namespace ControlzEx
{
    using System;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Behaviors;
    using ControlzEx.Internal;
    using ControlzEx.Internal.KnownBoxes;
    using ControlzEx.Native;
    using ControlzEx.Theming;
    using JetBrains.Annotations;
    using Microsoft.Xaml.Behaviors;
    using Windows.Win32;
    using Windows.Win32.Foundation;
    using Windows.Win32.Graphics.Dwm;
    using COLORREF = Windows.Win32.COLORREF;

    [PublicAPI]
    public partial class WindowChromeWindow : Window
    {
        private static readonly object defaultContentPadding = new Thickness(0, 1, 0, 0);
        private static readonly object emptyContentPadding = default(Thickness);

        static WindowChromeWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(typeof(WindowChromeWindow)));

            // todo: Move to dedicated class and never call it automatically. That way consumers are in full control.
            PInvoke.SetPreferredAppMode(PInvoke.PreferredAppMode.AllowDark);
            PInvoke.FlushMenuThemes();
        }

        public WindowChromeWindow()
        {
            WeakEventManager<ThemeManager, ThemeChangedEventArgs>.AddHandler(ThemeManager.Current, nameof(ThemeManager.Current.ThemeChanged), this.OnThemeManagerThemeChanged);
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            this.windowHandle = new HWND(new WindowInteropHelper(this).Handle);
            this.hwndSource = HwndSource.FromHwnd(this.windowHandle);

            if (this.hwndSource?.CompositionTarget is { } compositionTarget)
            {
                compositionTarget.BackgroundColor = Colors.Transparent;
            }

            WindowBackdropManager.UpdateWindowEffect(this);

            if (this.MitigateWhiteFlashDuringShow
                && this.AllowsTransparency is false)
            {
                var isDarkTheme = WindowsThemeHelper.AppsUseLightTheme() is false;
                DwmHelper.SetWindowAttributeValue(this.windowHandle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, isDarkTheme
                                                      ? DWMAttributeValues.True
                                                      : DWMAttributeValues.False);
            }

            this.InitializeMessageHandling();

            this.InitializeBehaviors();
        }

        /// <summary>
        /// Used to initialize the required behaviors.
        /// </summary>
        protected virtual void InitializeBehaviors()
        {
            this.InitializeWindowChromeBehavior();

            this.InitializeGlowWindowBehavior();
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        protected virtual void InitializeWindowChromeBehavior()
        {
            var behavior = new WindowChromeBehavior();
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty, new Binding { Path = new PropertyPath(IgnoreTaskbarOnMaximizeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.KeepBorderOnMaximizeProperty, new Binding { Path = new PropertyPath(KeepBorderOnMaximizeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.EnableMinimizeProperty, new Binding { Path = new PropertyPath(ShowMinButtonProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.EnableMaxRestoreProperty, new Binding { Path = new PropertyPath(ShowMaxRestoreButtonProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.CornerPreferenceProperty, new Binding { Path = new PropertyPath(CornerPreferenceProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.UseNativeCaptionButtonsProperty, new Binding { Path = new PropertyPath(UseNativeCaptionButtonsProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.CaptionButtonsSizeProperty, new Binding { Path = new PropertyPath(CaptionButtonsSizeProperty), Source = this, Mode = BindingMode.TwoWay });

            this.SetBinding(IsNCActiveProperty, new Binding { Path = new PropertyPath(WindowChromeBehavior.IsNCActiveProperty), Source = behavior });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        protected virtual void InitializeGlowWindowBehavior()
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

        protected virtual void OnThemeManagerThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            if (e.OldTheme?.BaseColorScheme != e.NewTheme.BaseColorScheme)
            {
                WindowBackdropManager.UpdateWindowEffect(this);
            }
        }

        /// <inheritdoc cref="WindowChromeBehavior.ResizeBorderThickness"/>
        public Thickness ResizeBorderThickness
        {
            get => (Thickness)this.GetValue(ResizeBorderThicknessProperty);
            set => this.SetValue(ResizeBorderThicknessProperty, value);
        }

        /// <summary>Identifies the <see cref="ResizeBorderThickness"/> dependency property.</summary>
        public static readonly DependencyProperty ResizeBorderThicknessProperty =
            DependencyProperty.Register(nameof(ResizeBorderThickness), typeof(Thickness), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.ResizeBorderThicknessProperty.DefaultMetadata.DefaultValue));

        /// <inheritdoc cref="GlowWindowBehavior.GlowDepth"/>
        /// <remarks>
        /// Only relevant if the DWM border is not used.
        /// </remarks>
        public int GlowDepth
        {
            get => (int)this.GetValue(GlowDepthProperty);
            set => this.SetValue(GlowDepthProperty, value);
        }

        /// <summary>Identifies the <see cref="GlowDepth"/> dependency property.</summary>
        public static readonly DependencyProperty GlowDepthProperty =
            DependencyProperty.Register(nameof(GlowDepth), typeof(int), typeof(WindowChromeWindow), new PropertyMetadata(GlowWindowBehavior.GlowDepthProperty.DefaultMetadata.DefaultValue));

        /// <summary>Identifies the <see cref="UseRadialGradientForCorners"/> dependency property.</summary>
        public static readonly DependencyProperty UseRadialGradientForCornersProperty = DependencyProperty.Register(
            nameof(UseRadialGradientForCorners), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(GlowWindowBehavior.UseRadialGradientForCornersProperty.DefaultMetadata.DefaultValue));

        /// <inheritdoc cref="GlowWindowBehavior.UseRadialGradientForCorners"/>
        /// <remarks>
        /// Only relevant if the DWM border is not used.
        /// </remarks>
        public bool UseRadialGradientForCorners
        {
            get => (bool)this.GetValue(UseRadialGradientForCornersProperty);
            set => this.SetValue(UseRadialGradientForCornersProperty, BooleanBoxes.Box(value));
        }

        /// <summary>Identifies the <see cref="IsGlowTransitionEnabled"/> dependency property.</summary>
        public static readonly DependencyProperty IsGlowTransitionEnabledProperty = DependencyProperty.Register(
            nameof(IsGlowTransitionEnabled), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(GlowWindowBehavior.IsGlowTransitionEnabledProperty.DefaultMetadata.DefaultValue));

        /// <inheritdoc cref="GlowWindowBehavior.IsGlowTransitionEnabled"/>
        /// <remarks>
        /// Only relevant if the DWM border is not used.
        /// </remarks>
        public bool IsGlowTransitionEnabled
        {
            get => (bool)this.GetValue(IsGlowTransitionEnabledProperty);
            set => this.SetValue(IsGlowTransitionEnabledProperty, BooleanBoxes.Box(value));
        }

        /// <summary>Identifies the <see cref="IgnoreTaskbarOnMaximize"/> dependency property.</summary>
        public static readonly DependencyProperty IgnoreTaskbarOnMaximizeProperty = DependencyProperty.Register(nameof(IgnoreTaskbarOnMaximize), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty.DefaultMetadata.DefaultValue));

        /// <inheritdoc cref="WindowChromeBehavior.IgnoreTaskbarOnMaximize"/>
        public bool IgnoreTaskbarOnMaximize
        {
            get => (bool)this.GetValue(IgnoreTaskbarOnMaximizeProperty);
            set => this.SetValue(IgnoreTaskbarOnMaximizeProperty, BooleanBoxes.Box(value));
        }

        /// <inheritdoc cref="WindowChromeBehavior.KeepBorderOnMaximize"/>
        public bool KeepBorderOnMaximize
        {
            get => (bool)this.GetValue(KeepBorderOnMaximizeProperty);
            set => this.SetValue(KeepBorderOnMaximizeProperty, BooleanBoxes.Box(value));
        }

        /// <summary>Identifies the <see cref="KeepBorderOnMaximize"/> dependency property.</summary>
        public static readonly DependencyProperty KeepBorderOnMaximizeProperty = DependencyProperty.Register(nameof(KeepBorderOnMaximize), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>Identifies the <see cref="ShowMinButton"/> dependency property.</summary>
        public static readonly DependencyProperty ShowMinButtonProperty = DependencyProperty.Register(nameof(ShowMinButton), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <inheritdoc cref="WindowChromeBehavior.EnableMinimize"/>
        public bool ShowMinButton
        {
            get => (bool)this.GetValue(ShowMinButtonProperty);
            set => this.SetValue(ShowMinButtonProperty, BooleanBoxes.Box(value));
        }

        /// <summary>Identifies the <see cref="ShowMaxRestoreButton"/> dependency property.</summary>
        public static readonly DependencyProperty ShowMaxRestoreButtonProperty = DependencyProperty.Register(nameof(ShowMaxRestoreButton), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <inheritdoc cref="WindowChromeBehavior.EnableMaxRestore"/>
        public bool ShowMaxRestoreButton
        {
            get => (bool)this.GetValue(ShowMaxRestoreButtonProperty);
            set => this.SetValue(ShowMaxRestoreButtonProperty, BooleanBoxes.Box(value));
        }

        /// <summary>Identifies the <see cref="GlowColor"/> dependency property.</summary>
        public static readonly DependencyProperty GlowColorProperty = DependencyProperty.Register(nameof(GlowColor), typeof(Color?), typeof(WindowChromeWindow), new PropertyMetadata(null, OnGlowColorChanged));

        private static void OnGlowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WindowChromeWindow)d).UpdatePadding();
        }

        /// <inheritdoc cref="GlowWindowBehavior.GlowColor"/>
        public Color? GlowColor
        {
            get => (Color?)this.GetValue(GlowColorProperty);
            set => this.SetValue(GlowColorProperty, value);
        }

        /// <summary>Identifies the <see cref="NonActiveGlowColor"/> dependency property.</summary>
        public static readonly DependencyProperty NonActiveGlowColorProperty = DependencyProperty.Register(nameof(NonActiveGlowColor), typeof(Color?), typeof(WindowChromeWindow), new PropertyMetadata(null, OnNonActiveGlowColorChanged));

        private static void OnNonActiveGlowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WindowChromeWindow)d).UpdatePadding();
        }

        /// <inheritdoc cref="GlowWindowBehavior.NonActiveGlowColor"/>
        public Color? NonActiveGlowColor
        {
            get => (Color?)this.GetValue(NonActiveGlowColorProperty);
            set => this.SetValue(NonActiveGlowColorProperty, value);
        }

        public static readonly DependencyProperty CaptionColorProperty = DependencyProperty.Register(nameof(CaptionColor), typeof(Color?), typeof(WindowChromeWindow), new PropertyMetadata(null, OnCaptionColorChanged));

        /// <summary>
        /// Gets or sets the native window caption color.
        /// </summary>
        /// <remarks>
        /// Only works on Windows 11 and later.
        /// </remarks>
        public Color? CaptionColor
        {
            get => (Color?)this.GetValue(CaptionColorProperty);
            set => this.SetValue(CaptionColorProperty, value);
        }

        private static void OnCaptionColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WindowChromeWindow)d).UpdateCaptionColor();
        }

        private void UpdateCaptionColor()
        {
            var color = this.CaptionColor.HasValue
                ? this.CaptionColor.Value == Colors.Transparent
                    ? DWMAttributeValues.DWMWA_COLOR_NONE
                    : new COLORREF(this.CaptionColor.Value).dwColor
                : DWMAttributeValues.DWMWA_COLOR_DEFAULT;
            DwmHelper.SetWindowAttributeValue(this.windowHandle, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_COLOR, color);
        }

        public static readonly DependencyProperty UseNativeCaptionButtonsProperty = DependencyProperty.Register(nameof(UseNativeCaptionButtons), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.FalseBox));

        public bool UseNativeCaptionButtons
        {
            get => (bool)this.GetValue(UseNativeCaptionButtonsProperty);
            set => this.SetValue(UseNativeCaptionButtonsProperty, value);
        }

        public static readonly DependencyProperty CaptionButtonsSizeProperty = DependencyProperty.Register(
            nameof(CaptionButtonsSize), typeof(Size), typeof(WindowChromeWindow), new PropertyMetadata(default(Size)));

        public Size CaptionButtonsSize
        {
            get => (Size)this.GetValue(CaptionButtonsSizeProperty);
            set => this.SetValue(CaptionButtonsSizeProperty, value);
        }

        /// <summary>Identifies the <see cref="IsNCActive"/> dependency property.</summary>
        public static readonly DependencyProperty IsNCActiveProperty = DependencyProperty.Register(nameof(IsNCActive), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.FalseBox, OnPropertyChangedThatAffectsNCCurrentBrush));

        /// <summary>
        /// Gets whether the non-client area is active or not.
        /// </summary>
        public bool IsNCActive
        {
            get => (bool)this.GetValue(IsNCActiveProperty);
            private set => this.SetValue(IsNCActiveProperty, BooleanBoxes.Box(value));
        }

        private static void OnPropertyChangedThatAffectsNCCurrentBrush(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var windowChromeWindow = (WindowChromeWindow)d;
            windowChromeWindow.NCCurrentBrush = windowChromeWindow.IsNCActive
                ? windowChromeWindow.NCActiveBrush
                : windowChromeWindow.NCNonActiveBrush;
        }

        /// <summary>Identifies the <see cref="NCActiveBrush"/> dependency property.</summary>
        public static readonly DependencyProperty NCActiveBrushProperty = DependencyProperty.Register(nameof(NCActiveBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush), OnPropertyChangedThatAffectsNCCurrentBrush));

        /// <summary>
        /// Defines the brush to use when the non-client area is active.
        /// </summary>
        public Brush? NCActiveBrush
        {
            get => (Brush?)this.GetValue(NCActiveBrushProperty);
            set => this.SetValue(NCActiveBrushProperty, value);
        }

        /// <summary>Identifies the <see cref="NCNonActiveBrush"/> dependency property.</summary>
        public static readonly DependencyProperty NCNonActiveBrushProperty = DependencyProperty.Register(nameof(NCNonActiveBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush), OnPropertyChangedThatAffectsNCCurrentBrush));

        /// <summary>
        /// Defines the brush to use when the non-client area is not active.
        /// </summary>
        public Brush? NCNonActiveBrush
        {
            get => (Brush?)this.GetValue(NCNonActiveBrushProperty);
            set => this.SetValue(NCNonActiveBrushProperty, value);
        }

        public static readonly DependencyPropertyKey NCCurrentBrushPropertyKey = DependencyProperty.RegisterReadOnly(nameof(NCCurrentBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

        /// <summary>Identifies the <see cref="NCCurrentBrush"/> dependency property.</summary>
        public static readonly DependencyProperty NCCurrentBrushProperty = NCCurrentBrushPropertyKey.DependencyProperty;

        /// <summary>
        /// Defines the current non-client area brush (active or inactive).
        /// </summary>
        /// <remarks>
        /// This property should only be set through style triggers.
        /// </remarks>
        public Brush? NCCurrentBrush
        {
            get => (Brush?)this.GetValue(NCCurrentBrushProperty);
            private set => this.SetValue(NCCurrentBrushPropertyKey, value);
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

        /// <summary>Identifies the <see cref="DWMSupportsBorderColor"/> dependency property.</summary>
        public static readonly DependencyProperty DWMSupportsBorderColorProperty = DependencyProperty.Register(nameof(DWMSupportsBorderColor), typeof(bool), typeof(WindowChromeWindow), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <inheritdoc cref="GlowWindowBehavior.DWMSupportsBorderColor"/>
        public bool DWMSupportsBorderColor
        {
            get => (bool)this.GetValue(DWMSupportsBorderColorProperty);
            private set => this.SetValue(DWMSupportsBorderColorProperty, BooleanBoxes.Box(value));
        }

        /// <summary>Identifies the <see cref="CornerPreference"/> dependency property.</summary>
        public static readonly DependencyProperty CornerPreferenceProperty = DependencyProperty.Register(
            nameof(CornerPreference), typeof(WindowCornerPreference), typeof(WindowChromeWindow), new PropertyMetadata((WindowCornerPreference)WindowChromeBehavior.CornerPreferenceProperty.DefaultMetadata.DefaultValue));

        /// <inheritdoc cref="WindowChromeBehavior.CornerPreference"/>
        public WindowCornerPreference CornerPreference
        {
            get => (WindowCornerPreference)this.GetValue(CornerPreferenceProperty);
            set => this.SetValue(CornerPreferenceProperty, value);
        }

        /// <inheritdoc />
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.UpdatePadding();
        }

        /// <inheritdoc />
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            this.UpdatePadding();
        }

        /// <inheritdoc />
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            this.UpdatePadding();
        }

        /// <summary>
        /// Updates the padding used for the window content.
        /// </summary>
        protected virtual void UpdatePadding()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.SetCurrentValue(PaddingProperty, emptyContentPadding);
                return;
            }

            if (this.IsActive
                && this.GlowColor is not null)
            {
                this.SetCurrentValue(PaddingProperty, defaultContentPadding);
                return;
            }

            if (this.IsActive == false
                && this.NonActiveGlowColor is not null)
            {
                this.SetCurrentValue(PaddingProperty, defaultContentPadding);
                return;
            }

            this.SetCurrentValue(PaddingProperty, emptyContentPadding);
        }
    }
}