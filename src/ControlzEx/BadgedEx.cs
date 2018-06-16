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
        protected FrameworkElement BadgeContainer;

        public static readonly DependencyProperty BadgeProperty = DependencyProperty.Register(
            "Badge", typeof(object), typeof(BadgedEx), new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.AffectsArrange, OnBadgeChanged));

        public object Badge
        {
            get => this.GetValue(BadgeProperty);
            set => this.SetValue(BadgeProperty, value);
        }

        public static readonly DependencyProperty BadgeBackgroundProperty = DependencyProperty.Register(
            "BadgeBackground", typeof(Brush), typeof(BadgedEx), new PropertyMetadata(default(Brush)));

/*
        public Brush BadgeBackground
        {
            get => (Brush) this.GetValue(BadgeBackgroundProperty);
            set => this.SetValue(BadgeBackgroundProperty, value);
        }
*/

        public static readonly DependencyProperty BadgeForegroundProperty = DependencyProperty.Register(
            "BadgeForeground", typeof(Brush), typeof(BadgedEx), new PropertyMetadata(default(Brush)));

        public Brush BadgeForeground
        {
            get => (Brush) this.GetValue(BadgeForegroundProperty);
            set => this.SetValue(BadgeForegroundProperty, value);
        }

        public static readonly DependencyProperty BadgePlacementModeProperty = DependencyProperty.Register(
            "BadgePlacementMode", typeof(BadgePlacementMode), typeof(BadgedEx), new PropertyMetadata(default(BadgePlacementMode)));        

        public BadgePlacementMode BadgePlacementMode
        {
            get => (BadgePlacementMode) this.GetValue(BadgePlacementModeProperty);
            set => this.SetValue(BadgePlacementModeProperty, value);
        }

        public static readonly RoutedEvent BadgeChangedEvent =
            EventManager.RegisterRoutedEvent(
                "BadgeChanged",
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<object>),
                typeof(BadgedEx));

        public event RoutedPropertyChangedEventHandler<object> BadgeChanged
        {
            add => this.AddHandler(BadgeChangedEvent, value);
            remove => this.RemoveHandler(BadgeChangedEvent, value);
        }

        private static readonly DependencyPropertyKey IsBadgeSetPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsBadgeSet", typeof(bool), typeof(BadgedEx),
                new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsBadgeSetProperty =
            IsBadgeSetPropertyKey.DependencyProperty;

        public bool IsBadgeSet
        {
            get => (bool) this.GetValue(IsBadgeSetProperty);
            private set => this.SetValue(IsBadgeSetPropertyKey, value);
        }

        private static void OnBadgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {            
            var instance = (BadgedEx)d;

            instance.IsBadgeSet = !string.IsNullOrWhiteSpace(e.NewValue as string) || (e.NewValue != null && !(e.NewValue is string));

            var args = new RoutedPropertyChangedEventArgs<object>(
                e.OldValue,
                e.NewValue) {RoutedEvent = BadgeChangedEvent};
            instance.RaiseEvent(args);
        } 

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.BadgeContainer = this.GetTemplateChild(BadgeContainerPartName) as FrameworkElement;
        }        

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var result = base.ArrangeOverride(arrangeBounds);

            if (this.BadgeContainer == null) return result;
            
            var containerDesiredSize = this.BadgeContainer.DesiredSize;
            if ((containerDesiredSize.Width <= 0.0 || containerDesiredSize.Height <= 0.0)
                && !double.IsNaN(this.BadgeContainer.ActualWidth) && !double.IsInfinity(this.BadgeContainer.ActualWidth)
                && !double.IsNaN(this.BadgeContainer.ActualHeight) && !double.IsInfinity(this.BadgeContainer.ActualHeight))
            {
                containerDesiredSize = new Size(this.BadgeContainer.ActualWidth, this.BadgeContainer.ActualHeight);
            }

            var h = 0 - containerDesiredSize.Width / 2;
            var v = 0 - containerDesiredSize.Height / 2;
            this.BadgeContainer.Margin = new Thickness(0);
            this.BadgeContainer.Margin = new Thickness(h, v, h, v);

            return result;
        }
    }
}
