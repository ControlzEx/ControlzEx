namespace ControlzEx.Automation.Peers
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using ControlzEx.Controls;

    /// <summary>
    ///     Automation-Peer for <see cref="TabItem" /> in <see cref="TabControlEx" />.
    /// </summary>
    public class TabItemExAutomationPeer : TabItemAutomationPeer
    {
        /// <summary>
        ///     Initializes a new instance.
        /// </summary>
        public TabItemExAutomationPeer(object owner, TabControlAutomationPeer tabControlAutomationPeer)
            : base(owner, tabControlAutomationPeer)
        {
        }

        /// <inheritdoc />
        protected override List<AutomationPeer> GetChildrenCore()
        {
            // Call the base in case we have children in the header
            var headerChildren = base.GetChildrenCore();

            // Only if the TabItem is selected we need to add its visual children
            if (!(this.GetWrapper() is TabItem tabItem)
                || tabItem.IsSelected == false)
            {
                return headerChildren;
            }

            if (!(this.ItemsControlAutomationPeer.Owner is TabControlEx parentTabControl))
            {
                return headerChildren;
            }

            var contentHost = parentTabControl.FindChildContentPresenter(tabItem.Content, tabItem);

            if (contentHost is not null)
            {
                var contentHostPeer = new FrameworkElementAutomationPeer(contentHost);
                var contentChildren = contentHostPeer.GetChildren();

                if (contentChildren is not null)
                {
                    if (headerChildren is null)
                    {
                        headerChildren = contentChildren;
                    }
                    else
                    {
                        headerChildren.AddRange(contentChildren);
                    }
                }
            }

            return headerChildren;
        }

        /// <summary>
        ///     Gets the real tab item.
        /// </summary>
        private UIElement? GetWrapper()
        {
            var itemsControlAutomationPeer = this.ItemsControlAutomationPeer;

            var owner = (TabControlEx?)itemsControlAutomationPeer?.Owner;

            if (owner is null)
            {
                return null;
            }

            if (owner.IsItemItsOwnContainer(this.Item))
            {
                return this.Item as UIElement;
            }

            return owner.ItemContainerGenerator.ContainerFromItem(this.Item) as UIElement;
        }
    }
}