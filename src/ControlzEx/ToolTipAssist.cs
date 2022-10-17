namespace ControlzEx
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using ControlzEx.Standard;

    public static class ToolTipAssist
    {
        public static readonly DependencyProperty AutoMoveProperty
            = DependencyProperty.RegisterAttached("AutoMove",
                                                  typeof(bool),
                                                  typeof(ToolTipAssist),
                                                  new FrameworkPropertyMetadata(false, OnAutoMoveChanged));

        /// <summary>
        /// Indicates whether a tooltip should follow the mouse cursor.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static bool GetAutoMove(ToolTip element)
        {
            return (bool)element.GetValue(AutoMoveProperty);
        }

        /// <summary>
        /// Sets whether a tooltip should follow the mouse cursor.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static void SetAutoMove(ToolTip element, bool value)
        {
            element.SetValue(AutoMoveProperty, value);
        }

        public static readonly DependencyProperty AutoMoveHorizontalOffsetProperty
            = DependencyProperty.RegisterAttached("AutoMoveHorizontalOffset",
                                                  typeof(double),
                                                  typeof(ToolTipAssist),
                                                  new FrameworkPropertyMetadata(16d));

        /// <summary>
        /// Gets the horizontal offset for the relative placement of the Tooltip.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static double GetAutoMoveHorizontalOffset(ToolTip element)
        {
            return (double)element.GetValue(AutoMoveHorizontalOffsetProperty);
        }

        /// <summary>
        /// Sets the horizontal offset for the relative placement of the Tooltip.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static void SetAutoMoveHorizontalOffset(ToolTip element, double value)
        {
            element.SetValue(AutoMoveHorizontalOffsetProperty, value);
        }

        public static readonly DependencyProperty AutoMoveVerticalOffsetProperty
            = DependencyProperty.RegisterAttached("AutoMoveVerticalOffset",
                                                  typeof(double),
                                                  typeof(ToolTipAssist),
                                                  new FrameworkPropertyMetadata(16d));

        /// <summary>
        /// Gets the vertical offset for the relative placement of the Tooltip.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static double GetAutoMoveVerticalOffset(ToolTip element)
        {
            return (double)element.GetValue(AutoMoveVerticalOffsetProperty);
        }

        /// <summary>
        /// Sets the vertical offset for the relative placement of the Tooltip.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(ToolTip))]
        public static void SetAutoMoveVerticalOffset(ToolTip element, double value)
        {
            element.SetValue(AutoMoveVerticalOffsetProperty, value);
        }

        private static void OnAutoMoveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var toolTip = (ToolTip)dependencyObject;
            if (eventArgs.OldValue != eventArgs.NewValue
                && eventArgs.NewValue is not null)
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
            if (toolTip.PlacementTarget is FrameworkElement target)
            {
                // move the tooltip on opening to the correct position
                MoveToolTip(target, toolTip);
                target.MouseMove += ToolTipTargetPreviewMouseMove;
                Debug.WriteLine(">>tool tip opened");
            }
        }

        private static void ToolTip_Closed(object sender, RoutedEventArgs e)
        {
            var toolTip = (ToolTip)sender;
            if (toolTip.PlacementTarget is FrameworkElement target)
            {
                target.MouseMove -= ToolTipTargetPreviewMouseMove;
                Debug.WriteLine(">>tool tip closed");
            }
        }

        private static void ToolTipTargetPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var toolTip = (sender is FrameworkElement target
                ? target.ToolTip
                : null) as ToolTip;
            MoveToolTip(sender as IInputElement, toolTip);
        }

        private static void MoveToolTip(IInputElement? target, ToolTip? toolTip)
        {
            if (toolTip is null
                || target is null
                || toolTip.PlacementTarget is null
                || PresentationSource.FromVisual(toolTip.PlacementTarget) is null)
            {
                return;
            }

            toolTip.SetCurrentValue(ToolTip.PlacementProperty, PlacementMode.Relative);

            var hOffsetFromToolTip = GetAutoMoveHorizontalOffset(toolTip);
            var vOffsetFromToolTip = GetAutoMoveVerticalOffset(toolTip);

            var dpi = DpiHelper.GetDpi(toolTip);

            Debug.WriteLine(">>dpi       >> x: {0} \t y: {1}", dpi.DpiScaleX, dpi.DpiScaleY);

            var hDPIOffset = DpiHelper.TransformToDeviceX(toolTip.PlacementTarget, hOffsetFromToolTip, dpi.DpiScaleX);
            var vDPIOffset = DpiHelper.TransformToDeviceY(toolTip.PlacementTarget, vOffsetFromToolTip, dpi.DpiScaleY);

            var position = Mouse.GetPosition(toolTip.PlacementTarget);
            var newHorizontalOffset = position.X + hDPIOffset;
            var newVerticalOffset = position.Y + vDPIOffset;

            var topLeftFromScreen = toolTip.PlacementTarget.PointToScreen(new Point(0, 0));

            if (MonitorHelper.TryGetMonitorInfoFromPoint(out var mInfo))
            {
                Debug.WriteLine(">>rcWork    >> w: {0} \t h: {1}", mInfo.rcWork.Width, mInfo.rcWork.Height);
                Debug.WriteLine(">>rcMonitor >> w: {0} \t h: {1}", mInfo.rcMonitor.Width, mInfo.rcMonitor.Height);

                var monitorWorkWidth = Math.Abs(mInfo.rcWork.Width);
                var monitorWorkHeight = Math.Abs(mInfo.rcWork.Height);

                if (monitorWorkWidth == 0
                    || monitorWorkHeight == 0)
                {
                    Trace.TraceError("Got wrong monitor info values ({0})", mInfo.rcWork);
                    return;
                }

                topLeftFromScreen.X = -mInfo.rcWork.Left + topLeftFromScreen.X;
                topLeftFromScreen.Y = -mInfo.rcWork.Top + topLeftFromScreen.Y;

                var locationX = (int)topLeftFromScreen.X % monitorWorkWidth;
                var locationY = (int)topLeftFromScreen.Y % monitorWorkHeight;

                var renderDpiWidth = DpiHelper.TransformToDeviceX(toolTip.PlacementTarget, toolTip.RenderSize.Width, dpi.DpiScaleX);
                var rightX = locationX + newHorizontalOffset + renderDpiWidth;
                if (rightX > monitorWorkWidth)
                {
                    newHorizontalOffset = position.X - toolTip.RenderSize.Width - (0.5 * hDPIOffset);
                }

                var renderDPIHeight = DpiHelper.TransformToDeviceY(toolTip.PlacementTarget, toolTip.RenderSize.Height, dpi.DpiScaleY);
                var bottomY = locationY + newVerticalOffset + renderDPIHeight;
                if (bottomY > monitorWorkHeight)
                {
                    newVerticalOffset = position.Y - toolTip.RenderSize.Height - (0.5 * vDPIOffset);
                }

                Debug.WriteLine(">>tooltip   >> bY: {0:F} \t rX: {1:F}", bottomY, rightX);

                toolTip.HorizontalOffset = newHorizontalOffset;
                toolTip.VerticalOffset = newVerticalOffset;

                Debug.WriteLine(">>offset    >> ho: {0:F} \t vo: {1:F}", toolTip.HorizontalOffset, toolTip.VerticalOffset);
            }
        }
    }
}