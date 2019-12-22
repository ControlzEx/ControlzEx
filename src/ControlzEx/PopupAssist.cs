using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ControlzEx
{
    using ControlzEx.Standard;

    public static class PopupAssist
    {
        public static readonly DependencyProperty AutoMoveProperty
            = DependencyProperty.RegisterAttached("AutoMove",
                                                  typeof(bool),
                                                  typeof(PopupAssist),
                                                  new FrameworkPropertyMetadata(false, OnAutoMoveChanged));

        /// <summary>
        /// Indicates whether a popup should follow the mouse cursor.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static bool GetAutoMove(Popup element)
        {
            return (bool)element.GetValue(AutoMoveProperty);
        }

        /// <summary>
        /// Sets whether a popup should follow the mouse cursor.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static void SetAutoMove(Popup element, bool value)
        {
            element.SetValue(AutoMoveProperty, value);
        }

        public static readonly DependencyProperty AutoMoveHorizontalOffsetProperty
            = DependencyProperty.RegisterAttached("AutoMoveHorizontalOffset",
                                                  typeof(double),
                                                  typeof(PopupAssist),
                                                  new FrameworkPropertyMetadata(16d));

        /// <summary>
        /// Gets the horizontal offset for the relative placement of the Popup.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static double GetAutoMoveHorizontalOffset(Popup element)
        {
            return (double)element.GetValue(AutoMoveHorizontalOffsetProperty);
        }

        /// <summary>
        /// Sets the horizontal offset for the relative placement of the Popup.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static void SetAutoMoveHorizontalOffset(Popup element, double value)
        {
            element.SetValue(AutoMoveHorizontalOffsetProperty, value);
        }

        public static readonly DependencyProperty AutoMoveVerticalOffsetProperty
            = DependencyProperty.RegisterAttached("AutoMoveVerticalOffset",
                                                  typeof(double),
                                                  typeof(PopupAssist),
                                                  new FrameworkPropertyMetadata(16d));

        /// <summary>
        /// Gets the vertical offset for the relative placement of the Popup.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static double GetAutoMoveVerticalOffset(Popup element)
        {
            return (double)element.GetValue(AutoMoveVerticalOffsetProperty);
        }

        /// <summary>
        /// Sets the vertical offset for the relative placement of the Popup.
        /// </summary>
        [AttachedPropertyBrowsableForType(typeof(Popup))]
        public static void SetAutoMoveVerticalOffset(Popup element, double value)
        {
            element.SetValue(AutoMoveVerticalOffsetProperty, value);
        }


        // This private attached Property is needed because Popup has no relation to its Target.
        static readonly DependencyProperty AutoMovingPopupProperty
            = DependencyProperty.RegisterAttached("AutoMovingPopup",
                                                  typeof(Popup),
                                                  typeof(PopupAssist),
                                                  new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets the Popup that should be Automoved
        /// </summary>
        static Popup GetAutoMovingPopup(FrameworkElement element)
        {
            return (Popup)element.GetValue(AutoMovingPopupProperty);
        }

        /// <summary>
        /// Sets the Popup that should be Automoved
        /// </summary>
        static void SetAutoMovingPopup(FrameworkElement element, Popup value)
        {
            element.SetValue(AutoMovingPopupProperty, value);
        }



        private static void OnAutoMoveChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var popup = (Popup)dependencyObject;
            if (eventArgs.OldValue != eventArgs.NewValue && eventArgs.NewValue != null)
            {
                var autoMove = (bool)eventArgs.NewValue;
                if (autoMove)
                {
                    popup.Opened += Popup_Opened;
                    popup.Closed += Popup_Closed;
                }
                else
                {
                    popup.Opened -= Popup_Opened;
                    popup.Closed -= Popup_Closed;
                }
            }
        }

        private static void Popup_Opened(object sender, EventArgs e)
        {
            var popup = (Popup)sender;
            if (popup.PlacementTarget is FrameworkElement target)
            {
                // Register the popup with the placementtarget
                target.SetCurrentValue(AutoMovingPopupProperty, popup);

                // move the Popup on opening to the correct position
                MovePopup(target, popup);
                target.MouseMove += PopupTargetPreviewMouseMove;
                Debug.WriteLine(">>popup opened");
            }
        }

        private static void Popup_Closed(object sender, EventArgs e)
        {
            var popup = (Popup)sender;
            if (popup.PlacementTarget is FrameworkElement target)
            {
                // Unegister the popup with the placementtarget
                target.SetCurrentValue(AutoMovingPopupProperty, popup);

                target.MouseMove -= PopupTargetPreviewMouseMove;
                Debug.WriteLine(">>popup closed");
            }
        }

        private static void PopupTargetPreviewMouseMove(object sender, MouseEventArgs e)
        {
            var popup = (sender is FrameworkElement target ? target.GetValue(AutoMovingPopupProperty) : null) as Popup;
            MovePopup(sender as IInputElement, popup);
        }

        private static void MovePopup(IInputElement target, Popup popup)
        {
            if (popup == null || target == null || popup.PlacementTarget == null)
            {
                return;
            }

            popup.SetCurrentValue(Popup.PlacementProperty, PlacementMode.Relative);

            var hOffsetFromPopup = GetAutoMoveHorizontalOffset(popup);
            var vOffsetFromPopup = GetAutoMoveVerticalOffset(popup);

            var dpi = DpiHelper.GetDpi(popup);

            Debug.WriteLine(">>dpi       >> x: {0} \t y: {1}", dpi.DpiScaleX, dpi.DpiScaleY);

            var hDPIOffset = DpiHelper.TransformToDeviceX(popup.PlacementTarget, hOffsetFromPopup, dpi.DpiScaleX);
            var vDPIOffset = DpiHelper.TransformToDeviceY(popup.PlacementTarget, vOffsetFromPopup, dpi.DpiScaleY);

            var position = Mouse.GetPosition(popup.PlacementTarget);
            var newHorizontalOffset = position.X + hDPIOffset;
            var newVerticalOffset = position.Y + vDPIOffset;

            var topLeftFromScreen = popup.PlacementTarget.PointToScreen(new Point(0, 0));

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

                topLeftFromScreen.X = -monitorINFO.rcWork.Left + topLeftFromScreen.X;
                topLeftFromScreen.Y = -monitorINFO.rcWork.Top + topLeftFromScreen.Y;

                var locationX = (int)topLeftFromScreen.X % monitorWorkWidth;
                var locationY = (int)topLeftFromScreen.Y % monitorWorkHeight;

                var renderDpiWidth = DpiHelper.TransformToDeviceX(popup.PlacementTarget, popup.Child.RenderSize.Width, dpi.DpiScaleX);
                var rightX = locationX + newHorizontalOffset + renderDpiWidth;
                if (rightX > monitorWorkWidth)
                {
                    newHorizontalOffset = position.X - popup.Child.RenderSize.Width - 0.5 * hDPIOffset;
                }

                var renderDPIHeight = DpiHelper.TransformToDeviceY(popup.PlacementTarget, popup.Child.RenderSize.Height, dpi.DpiScaleY);
                var bottomY = locationY + newVerticalOffset + renderDPIHeight;
                if (bottomY > monitorWorkHeight)
                {
                    newVerticalOffset = position.Y - popup.Child.RenderSize.Height - 0.5 * vDPIOffset;
                }

                Debug.WriteLine(">>Popup   >> bY: {0:F} \t rX: {1:F}", bottomY, rightX);

                popup.SetCurrentValue(Popup.HorizontalOffsetProperty, newHorizontalOffset);
                popup.SetCurrentValue(Popup.VerticalOffsetProperty, newVerticalOffset);

                Debug.WriteLine(">>offset    >> ho: {0:F} \t vo: {1:F}", popup.HorizontalOffset, popup.VerticalOffset);
            }
        }
    }
}