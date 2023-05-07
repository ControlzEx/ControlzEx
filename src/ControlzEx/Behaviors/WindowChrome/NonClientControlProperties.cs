#pragma warning disable 618
// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System.Windows;
    using ControlzEx.Native;

    public static class NonClientControlProperties
    {
        public static readonly DependencyProperty HitTestResultProperty = DependencyProperty.RegisterAttached(
            "HitTestResult", typeof(HT), typeof(NonClientControlProperties), new FrameworkPropertyMetadata(HT.NOWHERE, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetHitTestResult(DependencyObject element, HT value)
        {
            element.SetValue(HitTestResultProperty, value);
        }

        public static HT GetHitTestResult(DependencyObject element)
        {
            return (HT)element.GetValue(HitTestResultProperty);
        }

        public static readonly DependencyProperty IsNCPressedProperty = DependencyProperty.RegisterAttached(
            "IsNCPressed", typeof(bool), typeof(NonClientControlProperties), new PropertyMetadata(false));

        public static void SetIsNCPressed(DependencyObject element, bool value)
        {
            element.SetValue(IsNCPressedProperty, value);
        }

        public static bool GetIsNCPressed(DependencyObject element)
        {
            return (bool)element.GetValue(IsNCPressedProperty);
        }

        public static readonly DependencyProperty IsNCMouseOverProperty = DependencyProperty.RegisterAttached(
            "IsNCMouseOver", typeof(bool), typeof(NonClientControlProperties), new PropertyMetadata(false));

        public static void SetIsNCMouseOver(DependencyObject element, bool value)
        {
            element.SetValue(IsNCMouseOverProperty, value);
        }

        public static bool GetIsNCMouseOver(DependencyObject element)
        {
            return (bool)element.GetValue(IsNCMouseOverProperty);
        }

        public static readonly DependencyProperty ClickStrategyProperty = DependencyProperty.RegisterAttached(
            "ClickStrategy", typeof(NonClientControlClickStrategy), typeof(NonClientControlProperties), new PropertyMetadata(NonClientControlClickStrategy.None));

        public static void SetClickStrategy(DependencyObject element, NonClientControlClickStrategy value)
        {
            element.SetValue(ClickStrategyProperty, value);
        }

        public static NonClientControlClickStrategy GetClickStrategy(DependencyObject element)
        {
            return (NonClientControlClickStrategy)element.GetValue(ClickStrategyProperty);
        }
    }
}