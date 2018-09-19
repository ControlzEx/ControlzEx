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
            return (bool) element.GetValue(AutoMoveProperty);
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
            return (double) element.GetValue(AutoMoveHorizontalOffsetProperty);
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
            return (double) element.GetValue(AutoMoveVerticalOffsetProperty);
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
            var toolTip = (ToolTip) dependencyObject;
            if (eventArgs.OldValue != eventArgs.NewValue && eventArgs.NewValue != null)
            {
                var autoMove = (bool) eventArgs.NewValue;
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
            var toolTip = (ToolTip) sender;
            var target = toolTip.PlacementTarget as FrameworkElement;
            if (target == null)
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
            var toolTip = (ToolTip) sender;
            var target = toolTip.PlacementTarget as FrameworkElement;
            if (target == null)
            {
                return;
            }

            target.MouseMove -= ToolTipTargetPreviewMouseMove;
            Debug.WriteLine(">>tool tip closed");
        }

        private static void ToolTipTargetPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var target = sender as FrameworkElement;
            var toolTip = (target != null ? target.ToolTip : null) as ToolTip;
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

            Debug.WriteLine(">>dpi       >> x: {0} \t y: {1}", dpi.DpiScaleX, dpi.DpiScaleY);

            var hDPIOffset = DpiHelper.TransformToDeviceX(toolTip.PlacementTarget, hOffsetFromToolTip, dpi.DpiScaleX);
            var vDPIOffset = DpiHelper.TransformToDeviceY(toolTip.PlacementTarget, vOffsetFromToolTip, dpi.DpiScaleY);

            var position = Mouse.GetPosition(toolTip.PlacementTarget);
            var newHorizontalOffset = position.X + hDPIOffset;
            var newVerticalOffset = position.Y + vDPIOffset;

            var topLeftFromScreen = toolTip.PlacementTarget.PointToScreen(new Point(0, 0));

#pragma warning disable 618
            MONITORINFO monitorINFO = null;
#pragma warning restore 618

            try
            {
                monitorINFO = MonitorHelper.GetMonitorInfoFromPoint();
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine("UnauthorizedAccessException occurred getting MONITORINFO: {0}", ex.Message);
            }

            if (monitorINFO != null)
            {
                Debug.WriteLine(">>rcWork    >> w: {0} \t h: {1}", monitorINFO.rcWork.Width, monitorINFO.rcWork.Height);
                Debug.WriteLine(">>rcMonitor >> w: {0} \t h: {1}", monitorINFO.rcMonitor.Width, monitorINFO.rcMonitor.Height);

                var monitorWorkWidth = Math.Abs(monitorINFO.rcWork.Width);
                var monitorWorkHeight = Math.Abs(monitorINFO.rcWork.Height);

                //if (topLeftFromScreen.X < 0)
                {
                    topLeftFromScreen.X = -monitorINFO.rcWork.Left + topLeftFromScreen.X;
                }
                //if (topLeftFromScreen.Y < 0)
                {
                    topLeftFromScreen.Y = -monitorINFO.rcWork.Top + topLeftFromScreen.Y;
                }

                var locationX = (int) topLeftFromScreen.X % monitorWorkWidth;
                var locationY = (int) topLeftFromScreen.Y % monitorWorkHeight;

                var renderDPIWidth = DpiHelper.TransformToDeviceX(toolTip.PlacementTarget, toolTip.RenderSize.Width, dpi.DpiScaleX);
                var rightX = locationX + newHorizontalOffset + renderDPIWidth;
                if (rightX > monitorWorkWidth)
                {
                    newHorizontalOffset = position.X - toolTip.RenderSize.Width - 0.5 * hDPIOffset;
                }

                var renderDPIHeight = DpiHelper.TransformToDeviceY(toolTip.PlacementTarget, toolTip.RenderSize.Height, dpi.DpiScaleY);
                var bottomY = locationY + newVerticalOffset + renderDPIHeight;
                if (bottomY > monitorWorkHeight)
                {
                    newVerticalOffset = position.Y - toolTip.RenderSize.Height - 0.5 * vDPIOffset;
                }

                Debug.WriteLine(">>tooltip   >> bY: {0:F} \t rX: {1:F}", bottomY, rightX);

                toolTip.HorizontalOffset = newHorizontalOffset;
                toolTip.VerticalOffset = newVerticalOffset;

                Debug.WriteLine(">>offset    >> ho: {0:F} \t vo: {1:F}", toolTip.HorizontalOffset, toolTip.VerticalOffset);
            }
        }
    }
}