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
                                          new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.AffectsArrange, OnBadgeChanged));

        /// <summary>
        /// Gets or sets the Badge content to display.
        /// </summary>
        public object Badge
        {
            get { return (object)GetValue(BadgeProperty); }
            set { SetValue(BadgeProperty, value); }
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
            get { return (Brush)GetValue(BadgeBackgroundProperty); }
            set { SetValue(BadgeBackgroundProperty, value); }
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
            get { return (Brush)GetValue(BadgeForegroundProperty); }
            set { SetValue(BadgeForegroundProperty, value); }
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
            get { return (BadgePlacementMode)GetValue(BadgePlacementModeProperty); }
            set { SetValue(BadgePlacementModeProperty, value); }
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
            get { return (Thickness)GetValue(BadgeMarginProperty); }
            set { SetValue(BadgeMarginProperty, value); }
        }

        public static readonly RoutedEvent BadgeChangedEvent
            = EventManager.RegisterRoutedEvent(nameof(BadgeChanged),
                                               RoutingStrategy.Bubble,
                                               typeof(RoutedPropertyChangedEventHandler<object>),
                                               typeof(BadgedEx));

        public event RoutedPropertyChangedEventHandler<object> BadgeChanged
        {
            add { AddHandler(BadgeChangedEvent, value); }
            remove { RemoveHandler(BadgeChangedEvent, value); }
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
            get { return (bool)GetValue(IsBadgeSetProperty); }
            private set { SetValue(IsBadgeSetPropertyKey, value); }
        }

        private static void OnBadgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (BadgedEx)d;

            instance.IsBadgeSet = !string.IsNullOrWhiteSpace(e.NewValue as string) || (e.NewValue != null && !(e.NewValue is string));

            var args = new RoutedPropertyChangedEventArgs<object>(e.OldValue, e.NewValue) { RoutedEvent = BadgeChangedEvent };
            instance.RaiseEvent(args);
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
                && !double.IsNaN(_badgeContainer.ActualWidth) && !double.IsInfinity(_badgeContainer.ActualWidth)
                && !double.IsNaN(_badgeContainer.ActualHeight) && !double.IsInfinity(_badgeContainer.ActualHeight))
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