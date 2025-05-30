#pragma warning disable 618
// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Interop;
    using System.Windows.Media;
    using ControlzEx.Internal;
    using ControlzEx.Native;
    using global::Windows.Win32;

    public class NonClientControlManager
    {
        private DependencyObject? trackedControl;

        public NonClientControlManager(Window window)
        {
            this.Owner = window ?? throw new ArgumentNullException(nameof(window));
            this.OwnerHandle = new WindowInteropHelper(this.Owner).Handle;
        }

        private Window Owner { get; }

        private IntPtr OwnerHandle { get; }

        public void ClearTrackedControl()
        {
            if (this.trackedControl is null)
            {
                return;
            }

            NonClientControlProperties.SetIsNCMouseOver(this.trackedControl, false);
            NonClientControlProperties.SetIsNCPressed(this.trackedControl, false);
            this.trackedControl = null;
        }

        public bool HoverTrackedControl(IntPtr lParam)
        {
            var controlUnderMouse = this.GetControlUnderMouse(lParam);
            if (controlUnderMouse == this.trackedControl)
            {
                return true;
            }

            if (this.trackedControl is not null)
            {
                NonClientControlProperties.SetIsNCMouseOver(this.trackedControl, false);
                NonClientControlProperties.SetIsNCPressed(this.trackedControl, false);
            }

            this.trackedControl = controlUnderMouse;

            if (this.trackedControl is not null)
            {
                NonClientControlProperties.SetIsNCMouseOver(this.trackedControl, true);
            }

            return true;
        }

        public bool PressTrackedControl(IntPtr lParam)
        {
            var controlUnderMouse = this.GetControlUnderMouse(lParam);
            if (controlUnderMouse != this.trackedControl)
            {
                this.HoverTrackedControl(lParam);
            }

            if (this.trackedControl is null)
            {
                return false;
            }

            NonClientControlProperties.SetIsNCPressed(this.trackedControl, true);

            var nonClientControlClickStrategy = NonClientControlProperties.GetClickStrategy(this.trackedControl);
            if (nonClientControlClickStrategy is NonClientControlClickStrategy.MouseEvent)
            {
                // Raising LBUTTONDOWN here automatically causes a LBUTTONUP to be raised by windows later correctly
                PInvoke.RaiseMouseMessage(this.OwnerHandle, WM.LBUTTONDOWN, default, lParam);

                return true;
            }

            return nonClientControlClickStrategy != NonClientControlClickStrategy.None;
        }

        public bool ClickTrackedControl(IntPtr lParam)
        {
            var controlUnderMouse = this.GetControlUnderMouse(lParam);
            if (controlUnderMouse != this.trackedControl)
            {
                return false;
            }

            if (this.trackedControl is null)
            {
                return false;
            }

            if (NonClientControlProperties.GetIsNCPressed(this.trackedControl) == false)
            {
                return false;
            }

            NonClientControlProperties.SetIsNCPressed(this.trackedControl, false);

            if (NonClientControlProperties.GetClickStrategy(this.trackedControl) is NonClientControlClickStrategy.AutomationPeer
                && this.trackedControl is UIElement uiElement
                && UIElementAutomationPeer.CreatePeerForElement(uiElement).GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProvider)
            {
                invokeProvider.Invoke();
            }

            return false;
        }

        public DependencyObject? GetControlUnderMouse(IntPtr lParam)
        {
            return GetControlUnderMouse(this.Owner, lParam);
        }

        public static DependencyObject? GetControlUnderMouse(Window owner, IntPtr lParam)
        {
            return GetControlUnderMouse(owner, lParam, out _);
        }

        public static DependencyObject? GetControlUnderMouse(Window owner, IntPtr lParam, out HT hitTestResult)
        {
            if (lParam == IntPtr.Zero
                || PresentationSource.FromVisual(owner) is null)
            {
                hitTestResult = HT.NOWHERE;
                return null;
            }

            var point = LogicalPointFromLParam(owner, lParam);

            if (owner.InputHitTest(point) is DependencyObject visualHit
                && NonClientControlProperties.GetHitTestResult(visualHit) is var res
                && res != HT.NOWHERE)
            {
                hitTestResult = res;

                // If the cursor is on the window edge we must not hit test controls.
                // Otherwise we have no chance to un-track controls when the cursor leaves the window.
                // This is left here in case someone uses this class without using PInvoke.TrackMouseEvent.
                if (owner.WindowState is not WindowState.Maximized
                    && hitTestResult is HT.MAXBUTTON or HT.MINBUTTON or HT.CLOSE)
                {
                    if (point.X.AreClose(0)
                        || point.X.AreClose(owner.Width)
                        || point.Y.AreClose(0)
                        || point.Y.AreClose(owner.Height))
                    {
                        hitTestResult = HT.NOWHERE;
                        return null;
                    }
                }

                DependencyObject control = visualHit;
                var currentControl = control;

                while (currentControl is not null)
                {
                    var valueSource = DependencyPropertyHelper.GetValueSource(currentControl, NonClientControlProperties.HitTestResultProperty);
                    if (valueSource.BaseValueSource is not BaseValueSource.Inherited and not BaseValueSource.Unknown)
                    {
                        control = currentControl;
                        break;
                    }

                    currentControl = GetVisualOrLogicalParent(currentControl);
                }

                return control;
            }

            hitTestResult = HT.NOWHERE;
            return null;

            static Point LogicalPointFromLParam(Window owner, IntPtr lParam)
            {
                var point2 = Utility.GetPoint(lParam);
                return owner.PointFromScreen(point2);
            }
        }

        private static DependencyObject? GetVisualOrLogicalParent(DependencyObject? sourceElement)
        {
            return sourceElement switch
            {
                null => null,
                Visual => VisualTreeHelper.GetParent(sourceElement) ?? LogicalTreeHelper.GetParent(sourceElement),
                _ => LogicalTreeHelper.GetParent(sourceElement)
            };
        }
    }
}