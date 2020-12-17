namespace ControlzEx.Internal
{
    using System.Reflection;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;

    internal static class SelectorAutomationPeerExtensions
    {
        private static readonly MethodInfo? RaiseSelectionEventsMethodInfo = typeof(SelectorAutomationPeer).GetMethod("RaiseSelectionEvents", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static void RaiseSelectionEvents(this SelectorAutomationPeer selectorAutomationPeer, SelectionChangedEventArgs e)
        {
            RaiseSelectionEventsMethodInfo?.Invoke(selectorAutomationPeer, new object[] { e });
        }
    }
}