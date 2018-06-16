using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ControlzEx
{
    using ControlzEx.Standard;

    public static class ToolTipAssist
    {
        public static readonly DependencyProperty AutoMoveProperty =
            DependencyProperty.RegisterAttached("AutoMove",
                                                typeof(bool),
                                                typeof(ToolTipAssist),
                                                new FrameworkPropertyMetadata(false, AutoMovePropertyChangedCallback));

        public static readonly DependencyProperty AutoMoveHorizontalOffsetProperty =
            DependencyProperty.RegisterAttached("AutoMoveHorizontalOffset",
                                                typeof(double),
                                                typeof(ToolTipAssist),
                                                new FrameworkPropertyMetadata(16d));

        public static readonly DependencyProperty AutoMoveVerticalOffsetProperty =
            DependencyProperty.RegisterAttached("AutoMoveVerticalOffset",
                                                typeof(double),
                                                typeof(ToolTipAssist),
                                                new FrameworkPropertyMetadata(16d));

        /// <summary>
        /// Enables a ToolTip to follow the mouse cursor.
        /// When set to <c>true</c>, the tool tip follows the mouse cursor.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static bool GetAutoMove(ToolTip element)
        {
            return (bool)element.GetValue(AutoMoveProperty);
        }

        public static void SetAutoMove(ToolTip element, bool value)
        {
            element.SetValue(AutoMoveProperty, value);
        }

        /// <summary>
        /// Gets the horizontal offset for the relative placement.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static double GetAutoMoveHorizontalOffset(ToolTip element)
        {
            return (double)element.GetValue(AutoMoveHorizontalOffsetProperty);
        }

        /// <summary>
        /// Sets the horizontal offset for the relative placement.
        /// </summary>
        public static void SetAutoMoveHorizontalOffset(ToolTip element, double value)
        {
            element.SetValue(AutoMoveHorizontalOffsetProperty, value);
        }

        /// <summary>
        /// Gets the vertical offset for the relative placement.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static double GetAutoMoveVerticalOffset(ToolTip element)
        {
            return (double)element.GetValue(AutoMoveVerticalOffsetProperty);
        }

        /// <summary>
        /// Sets the vertical offset for the relative placement.
        /// </summary>
        public static void SetAutoMoveVerticalOffset(ToolTip element, double value)
        {
            element.SetValue(AutoMoveVerticalOffsetProperty, value);
        }

        private static void AutoMovePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var toolTip = (ToolTip)dependencyObject;
            if (eventArgs.OldValue != eventArgs.NewValue && eventArgs.NewValue != null)
            {
                var autoMove = (bool)eventArgs.NewValue;
                if (autoMove)
                {
                    toolTip.Opened += ToolTip_Opened;
                    toolTip.Closed += ToolTip_Closed;
                }
                else
                {
                    toolTip.Opened -= ToolTip_Opened;
                    toolTip.Closed -= ToolTip_Closed;
                }
            }
        }

        private static void ToolTip_Opened(object sender, RoutedEventArgs e)
        {
            var toolTip = (ToolTip)sender;
            if (!(toolTip.PlacementTarget is FrameworkElement target))
            {
                return;
            }
            // move the tooltip on openeing to the correct position
            MoveToolTip(target, toolTip);
            target.MouseMove += ToolTipTargetPreviewMouseMove;
            Debug.WriteLine(">>tool tip opened");
        }

        private static void ToolTip_Closed(object sender, RoutedEventArgs e)
        {
            var toolTip = (ToolTip)sender;
            if (!(toolTip.PlacementTarget is FrameworkElement target))
            {
                return;
            }
            target.MouseMove -= ToolTipTargetPreviewMouseMove;
            Debug.WriteLine(">>tool tip closed");
        }

        private static void ToolTipTargetPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var toolTip = (sender is FrameworkElement target ? target.ToolTip : null) as ToolTip;
            MoveToolTip(sender as IInputElement, toolTip);
        }

        private static void MoveToolTip(IInputElement target, ToolTip toolTip)
        {
            if (toolTip == null || target == null || toolTip.PlacementTarget == null)
            {
                return;
            }

            toolTip.Placement = PlacementMode.Relative;

            var hOffsetFromToolTip = GetAutoMoveHorizontalOffset(toolTip);
            var vOffsetFromToolTip = GetAutoMoveVerticalOffset(toolTip);

            var dpi = DpiHelper.GetDpi(toolTip);

            var hDpiOffset = DpiHelper.TransformToDeviceX(toolTip.PlacementTarget, hOffsetFromToolTip, dpi.DpiScaleX);
            var vDpiOffset = DpiHelper.TransformToDeviceY(toolTip.PlacementTarget, vOffsetFromToolTip, dpi.DpiScaleY);

            var position = Mouse.GetPosition(toolTip.PlacementTarget);
            var newHorizontalOffset = position.X + hDpiOffset;
            var newVerticalOffset = position.Y + vDpiOffset;

            var topLeftFromScreen = toolTip.PlacementTarget.PointToScreen(new Point(0, 0));

            Monitorinfo monitorInfo = null;

            try
            {
                monitorInfo = MonitorHelper.GetMonitorInfoFromPoint();
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine("UnauthorizedAccessException occurred getting MONITORINFO: {0}", ex.Message);
            }

            if (monitorInfo != null)
            {
                Debug.WriteLine(">>rcWork    >> w: {0}     h: {1}", monitorInfo.rcWork.Width, monitorInfo.rcWork.Height);
                Debug.WriteLine(">>rcMonitor >> w: {0}     h: {1}", monitorInfo.rcMonitor.Width, monitorInfo.rcMonitor.Height);

                var monitorWorkWidth = Math.Abs(monitorInfo.rcWork.Width); // (int)DpiHelper.TransformToDeviceX(toolTip.PlacementTarget, SystemParameters.PrimaryScreenWidth);
                var monitorWorkHeight = Math.Abs(monitorInfo.rcWork.Height); // (int)DpiHelper.TransformToDeviceY(toolTip.PlacementTarget, SystemParameters.PrimaryScreenHeight);

                if (topLeftFromScreen.X < 0)
                {
                    topLeftFromScreen.X = -monitorInfo.rcWork.Left + topLeftFromScreen.X;
                }
                if (topLeftFromScreen.Y < 0)
                {
                    topLeftFromScreen.Y = -monitorInfo.rcWork.Top + topLeftFromScreen.Y;
                }

                var locationX = (int)topLeftFromScreen.X % monitorWorkWidth;
                var locationY = (int)topLeftFromScreen.Y % monitorWorkHeight;

                var renderDpiWidth = DpiHelper.TransformToDeviceX(toolTip.RenderSize.Width, dpi.DpiScaleX);
                var rightX = locationX + newHorizontalOffset + renderDpiWidth;
                if (rightX > monitorWorkWidth)
                {
                    newHorizontalOffset = position.X - toolTip.RenderSize.Width - 0.5 * hDpiOffset;
                }

                var renderDpiHeight = DpiHelper.TransformToDeviceY(toolTip.RenderSize.Height, dpi.DpiScaleY);
                var bottomY = locationY + newVerticalOffset + renderDpiHeight;
                if (bottomY > monitorWorkHeight)
                {
                    newVerticalOffset = position.Y - toolTip.RenderSize.Height - 0.5 * vDpiOffset;
                }

                Debug.WriteLine(">>tooltip   >> bottomY: {0:F}    rightX: {1:F}", bottomY, rightX);

                toolTip.HorizontalOffset = newHorizontalOffset;
                toolTip.VerticalOffset = newVerticalOffset;

                Debug.WriteLine(">>offset    >> ho: {0:F}         vo: {1:F}", toolTip.HorizontalOffset, toolTip.VerticalOffset);
            }
        }
    }
}