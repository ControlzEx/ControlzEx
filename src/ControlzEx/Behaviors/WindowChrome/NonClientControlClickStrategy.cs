// ReSharper disable once CheckNamespace
namespace ControlzEx.Behaviors
{
    using System.Windows.Automation.Provider;

    public enum NonClientControlClickStrategy
    {
        /// <summary>
        /// No click.
        /// </summary>
        None = 0,

        /// <summary>
        /// Uses simulated mouse events to click.
        /// </summary>
        MouseEvent = 1,

        /// <summary>
        /// Uses an <see cref="IInvokeProvider"/> from an <see cref="System.Windows.Automation.Peers.AutomationPeer"/> to click.
        /// </summary>
        AutomationPeer = 2
    }
}