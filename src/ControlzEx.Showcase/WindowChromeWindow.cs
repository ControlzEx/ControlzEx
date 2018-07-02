﻿namespace ControlzEx.Showcase
{
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Interactivity;
    using System.Windows.Media;
    using ControlzEx.Behaviors;

    public class WindowChromeWindow : Window
    {
        private GlowWindowBehavior glowWindowBehavior;
        private ShadowWindowBehavior shadowWindowBehavior;

        static WindowChromeWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(typeof(WindowChromeWindow)));

            BorderThicknessProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(new Thickness(1)));
            WindowStyleProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(WindowStyle.None));

            AllowsTransparencyProperty.OverrideMetadata(typeof(WindowChromeWindow), new FrameworkPropertyMetadata(false));
        }

        public WindowChromeWindow()
        {
            this.InitializeBehaviors();
        }

        protected void EnableGlowWindowBehavior()
        {
            var behaviors = Interaction.GetBehaviors(this);
            if (!behaviors.Contains(this.glowWindowBehavior))
            {
                behaviors.Add(this.glowWindowBehavior);
            }
        }

        protected void DisableGlowWindowBehavior()
        {
            Interaction.GetBehaviors(this).Remove(this.glowWindowBehavior);
        }

        protected void EnableShadowWindowBehavior()
        {
            var behaviors = Interaction.GetBehaviors(this);
            if (!behaviors.Contains(this.shadowWindowBehavior))
            {
                behaviors.Add(this.shadowWindowBehavior);
            }
        }

        protected void DisableShadowWindowBehavior()
        {
            Interaction.GetBehaviors(this).Remove(this.shadowWindowBehavior);
        }

        private void InitializeBehaviors()
        {
            this.InitializeWindowChromeBehavior();
            this.InitializeGlowWindowBehavior();
            this.InitializeShadowWindowBevaior();
        }

        /// <summary>
        /// Initializes the WindowChromeBehavior which is needed to render the custom WindowChrome.
        /// </summary>
        private void InitializeWindowChromeBehavior()
        {
            var behavior = new WindowChromeBehavior();
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.IgnoreTaskbarOnMaximizeProperty, new Binding { Path = new PropertyPath(IgnoreTaskbarOnMaximizeProperty), Source = this });
            BindingOperations.SetBinding(behavior, WindowChromeBehavior.GlowBrushProperty, new Binding { Path = new PropertyPath(GlowBrushProperty), Source = this });

            Interaction.GetBehaviors(this).Add(behavior);
        }

        /// <summary>
        /// Initializes the GlowWindowBehavior.
        /// </summary>
        private void InitializeGlowWindowBehavior()
        {
            this.glowWindowBehavior = new GlowWindowBehavior();
            BindingOperations.SetBinding(this.glowWindowBehavior, GlowWindowBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
            BindingOperations.SetBinding(this.glowWindowBehavior, GlowWindowBehavior.GlowBrushProperty, new Binding { Path = new PropertyPath(GlowBrushProperty), Source = this });
            BindingOperations.SetBinding(this.glowWindowBehavior, GlowWindowBehavior.NonActiveGlowBrushProperty, new Binding { Path = new PropertyPath(NonActiveGlowBrushProperty), Source = this });
        }

        /// <summary>
        /// Initializes the ShadowWindowBehavior.
        /// </summary>
        private void InitializeShadowWindowBevaior()
        {
            this.shadowWindowBehavior = new ShadowWindowBehavior();
            BindingOperations.SetBinding(this.shadowWindowBehavior, ShadowWindowBehavior.ResizeBorderThicknessProperty, new Binding { Path = new PropertyPath(ResizeBorderThicknessProperty), Source = this });
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
        /// <see cref="DependencyProperty"/> for <see cref="GlowBrush"/>.
        /// </summary>
        public static readonly DependencyProperty GlowBrushProperty = DependencyProperty.Register(nameof(GlowBrush), typeof(Brush), typeof(WindowChromeWindow), new PropertyMetadata(default(Brush)));

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
    }
}