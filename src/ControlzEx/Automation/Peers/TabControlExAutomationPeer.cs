namespace ControlzEx.Automation.Peers
{
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using ControlzEx.Controls;

    /// <summary>
    ///     Automation-Peer for <see cref="TabControlEx" />.
    /// </summary>
    public class TabControlExAutomationPeer : TabControlAutomationPeer
    {
        /// <summary>
        ///     Initializes a new instance.
        /// </summary>
        public TabControlExAutomationPeer(TabControl owner)
            : base(owner)
        {
        }

        /// <inheritdoc />
        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new TabItemExAutomationPeer(item, this);
        }
    }
}