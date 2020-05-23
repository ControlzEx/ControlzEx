using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ControlzEx
{
    public enum BadgePlacementMode
    {
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }

    [TemplatePart(Name = BadgeContainerPartName, Type = typeof(UIElement))]
    public class BadgedEx : ContentControl
    {
        public const string BadgeContainerPartName = "PART_BadgeContainer";
        protected FrameworkElement _badgeContainer;

        /// <summary>Identifies the <see cref="Badge"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeProperty
            = DependencyProperty.Register(nameof(Badge),
                                          typeof(object),
                                          typeof(BadgedEx),
                                          new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.AffectsArrange, OnBadgeChanged));

        /// <summary>
        /// Gets or sets the Badge content to display.
        /// </summary>
        public object Badge
        {
            get => (object)GetValue(BadgeProperty);
            set => SetValue(BadgeProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeFontFamily"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeFontFamilyProperty =
            DependencyProperty.RegisterAttached(nameof(BadgeFontFamily),
                                                typeof(FontFamily),
                                                typeof(BadgedEx),
                                                new FrameworkPropertyMetadata(
                                                    SystemFonts.MessageFontFamily,
                                                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The BadgeFontFamily property specifies the name of font family.
        /// </summary>
        [Bindable(true)]
        [Localizability(LocalizationCategory.Font)]
        public FontFamily BadgeFontFamily
        {
            get => (FontFamily)GetValue(BadgeFontFamilyProperty);
            set => SetValue(BadgeFontFamilyProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeFontStyle"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeFontStyleProperty =
            DependencyProperty.RegisterAttached(nameof(BadgeFontStyle),
                                                typeof(FontStyle),
                                                typeof(BadgedEx),
                                                new FrameworkPropertyMetadata(
                                                    SystemFonts.MessageFontStyle,
                                                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The BadgeFontStyle property requests normal, italic, and oblique faces within a font family.
        /// </summary>
        [Bindable(true)]
        public FontStyle BadgeFontStyle
        {
            get => (FontStyle)GetValue(BadgeFontStyleProperty);
            set => SetValue(BadgeFontStyleProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeFontWeight"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeFontWeightProperty =
            DependencyProperty.RegisterAttached(nameof(BadgeFontWeight),
                                                typeof(FontWeight),
                                                typeof(BadgedEx),
                                                new FrameworkPropertyMetadata(
                                                    SystemFonts.MessageFontWeight,
                                                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The BadgeFontWeight property specifies the weight of the font.
        /// </summary>
        [Bindable(true)]
        public FontWeight BadgeFontWeight
        {
            get => (FontWeight)GetValue(BadgeFontWeightProperty);
            set => SetValue(BadgeFontWeightProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeFontStretch"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeFontStretchProperty =
            DependencyProperty.RegisterAttached(nameof(BadgeFontStretch),
                                                typeof(FontStretch),
                                                typeof(BadgedEx),
                                                new FrameworkPropertyMetadata(
                                                    FontStretches.Normal,
                                                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The BadgeFontStretch property selects a normal, condensed, or extended face from a font family.
        /// </summary>
        [Bindable(true)]
        public FontStretch BadgeFontStretch
        {
            get => (FontStretch)GetValue(BadgeFontStretchProperty);
            set => SetValue(BadgeFontStretchProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeFontSize"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeFontSizeProperty =
            DependencyProperty.RegisterAttached(nameof(BadgeFontSize),
                                                typeof(double),
                                                typeof(BadgedEx),
                                                new FrameworkPropertyMetadata(
                                                    SystemFonts.MessageFontSize,
                                                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The BadgeFontSize property specifies the size of the font.
        /// </summary>
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double BadgeFontSize
        {
            get => (double)GetValue(BadgeFontSizeProperty);
            set => SetValue(BadgeFontSizeProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeBackground"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeBackgroundProperty
            = DependencyProperty.Register(nameof(BadgeBackground),
                                          typeof(Brush),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the background brush for the Badge.
        /// </summary>
        public Brush BadgeBackground
        {
            get => (Brush)GetValue(BadgeBackgroundProperty);
            set => SetValue(BadgeBackgroundProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeForeground"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeForegroundProperty
            = DependencyProperty.Register(nameof(BadgeForeground),
                                          typeof(Brush),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the foreground brush for the Badge.
        /// </summary>
        public Brush BadgeForeground
        {
            get => (Brush)GetValue(BadgeForegroundProperty);
            set => SetValue(BadgeForegroundProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeBorderBrush"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeBorderBrushProperty
            = DependencyProperty.Register(nameof(BadgeBorderBrush),
                                          typeof(Brush),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the border brush for the Badge.
        /// </summary>
        public Brush BadgeBorderBrush
        {
            get => (Brush)GetValue(BadgeBorderBrushProperty);
            set => SetValue(BadgeBorderBrushProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeBorderThickness"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeBorderThicknessProperty
            = DependencyProperty.Register(nameof(BadgeBorderThickness),
                                          typeof(Thickness),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(new Thickness()));

        /// <summary>
        /// Gets or sets the border thickness for the Badge.
        /// </summary>
        public Thickness BadgeBorderThickness
        {
            get => (Thickness)GetValue(BadgeBorderThicknessProperty);
            set => SetValue(BadgeBorderThicknessProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgePlacementMode"/> dependency property.</summary>
        public static readonly DependencyProperty BadgePlacementModeProperty
            = DependencyProperty.Register(nameof(BadgePlacementMode),
                                          typeof(BadgePlacementMode),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(default(BadgePlacementMode)));

        /// <summary>
        /// Gets or sets the placement of the Badge relative to its content.
        /// </summary>
        public BadgePlacementMode BadgePlacementMode
        {
            get => (BadgePlacementMode)GetValue(BadgePlacementModeProperty);
            set => SetValue(BadgePlacementModeProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeMargin"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeMarginProperty
            = DependencyProperty.Register(nameof(BadgeMargin),
                                          typeof(Thickness),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Gets or sets a margin which can be used to make minor adjustments to the placement of the Badge.
        /// </summary>
        public Thickness BadgeMargin
        {
            get => (Thickness)GetValue(BadgeMarginProperty);
            set => SetValue(BadgeMarginProperty, value);
        }


        /// <summary>Identifies the <see cref="BadgeTemplate"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeTemplateProperty
            = DependencyProperty.Register(nameof(BadgeTemplate),
                                          typeof(DataTemplate),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> for the Badge
        /// </summary>
        public DataTemplate BadgeTemplate
        {
            get => (DataTemplate)GetValue(BadgeTemplateProperty);
            set => SetValue(BadgeTemplateProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeTemplateSelector"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeTemplateSelectorProperty
            = DependencyProperty.Register(nameof(BadgeTemplateSelector),
                                          typeof(DataTemplateSelector),
                                          typeof(BadgedEx),
                                          new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplateSelector"/> for the Badge
        /// </summary>
        public DataTemplateSelector BadgeTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(BadgeTemplateSelectorProperty);
            set => SetValue(BadgeTemplateSelectorProperty, value);
        }

        /// <summary>Identifies the <see cref="BadgeStringFormat"/> dependency property.</summary>
        public static readonly DependencyProperty BadgeStringFormatProperty
            = DependencyProperty.Register(nameof(BadgeStringFormat),
                                          typeof(string),
                                          typeof(BadgedEx),
                                          new FrameworkPropertyMetadata((string)null));

        /// <summary>
        /// Gets or sets a composite string that specifies how to format the Badge property if it is displayed as a string.
        /// </summary>
        /// <remarks> 
        /// This property is ignored if <seealso cref="BadgeTemplate"/> is set.
        /// </remarks>
        public string BadgeStringFormat
        {
            get => (string)this.GetValue(BadgeStringFormatProperty);
            set => this.SetValue(BadgeStringFormatProperty, value);
        }

        public static readonly RoutedEvent BadgeChangedEvent
            = EventManager.RegisterRoutedEvent(nameof(BadgeChanged),
                                               RoutingStrategy.Bubble,
                                               typeof(RoutedPropertyChangedEventHandler<object>),
                                               typeof(BadgedEx));

        public event RoutedPropertyChangedEventHandler<object> BadgeChanged
        {
            add => AddHandler(BadgeChangedEvent, value);
            remove => RemoveHandler(BadgeChangedEvent, value);
        }

        private static readonly DependencyPropertyKey IsBadgeSetPropertyKey
            = DependencyProperty.RegisterReadOnly(nameof(IsBadgeSet),
                                                  typeof(bool),
                                                  typeof(BadgedEx),
                                                  new PropertyMetadata(default(bool)));

        /// <summary>Identifies the <see cref="IsBadgeSet"/> dependency property.</summary>
        public static readonly DependencyProperty IsBadgeSetProperty = IsBadgeSetPropertyKey.DependencyProperty;

        /// <summary>
        /// Indicates whether the Badge has content to display.
        /// </summary>
        public bool IsBadgeSet
        {
            get => (bool)GetValue(IsBadgeSetProperty);
            private set => SetValue(IsBadgeSetPropertyKey, value);
        }

        private static void OnBadgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (BadgedEx)d;

            instance.IsBadgeSet = !string.IsNullOrWhiteSpace(e.NewValue as string) || (e.NewValue != null && !(e.NewValue is string));

            var args = new RoutedPropertyChangedEventArgs<object>(e.OldValue, e.NewValue) { RoutedEvent = BadgeChangedEvent };
            instance.RaiseEvent(args);
        }

        static BadgedEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BadgedEx), new FrameworkPropertyMetadata(typeof(BadgedEx)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _badgeContainer = GetTemplateChild(BadgeContainerPartName) as FrameworkElement;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var result = base.ArrangeOverride(arrangeBounds);

            if (_badgeContainer == null) return result;

            var containerDesiredSize = _badgeContainer.DesiredSize;
            if ((containerDesiredSize.Width <= 0.0 || containerDesiredSize.Height <= 0.0)
                && !double.IsNaN(_badgeContainer.ActualWidth)
                && !double.IsInfinity(_badgeContainer.ActualWidth)
                && !double.IsNaN(_badgeContainer.ActualHeight)
                && !double.IsInfinity(_badgeContainer.ActualHeight))
            {
                containerDesiredSize = new Size(_badgeContainer.ActualWidth, _badgeContainer.ActualHeight);
            }

            var h = 0 - containerDesiredSize.Width / 2;
            var v = 0 - containerDesiredSize.Height / 2;
            _badgeContainer.Margin = new Thickness(0);
            _badgeContainer.Margin = new Thickness(h, v, h, v);

            return result;
        }
    }
}