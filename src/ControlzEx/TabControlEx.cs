namespace ControlzEx
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;

    /// <summary>
    /// The standard WPF TabControl is quite bad in the fact that it only
    /// even contains the current TabItem in the VisualTree, so if you
    /// have complex views it takes a while to re-create the view each tab
    /// selection change.Which makes the standard TabControl very sticky to
    /// work with. This class along with its associated ControlTemplate
    /// allow all TabItems to remain in the VisualTree without it being Sticky.
    /// It does this by keeping all TabItem content in the VisualTree but
    /// hides all inactive TabItem content, and only keeps the active TabItem
    /// content shown.
    /// 
    /// Acknowledgement
    ///     Eric Burke
    ///         http://eric.burke.name/dotnetmania/2009/04/26/22.09.28
    ///     Sacha Barber: https://sachabarbs.wordpress.com/about-me/
    ///         http://stackoverflow.com/a/10210889/920384
    ///     http://stackoverflow.com/a/7838955/920384
    /// </summary>
    [TemplatePart(Name = "PART_ItemsHolder", Type = typeof(Panel))]    
    public class TabControlEx : TabControl
    {
        private Panel itemsHolder;        

        public static readonly DependencyProperty ChildContentVisibilityProperty
            = DependencyProperty.Register(nameof(ChildContentVisibility), typeof(Visibility), typeof(TabControlEx), new PropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty TabPanelVisibilityProperty =
            DependencyProperty.Register(nameof(TabPanelVisibility), typeof(Visibility), typeof(TabControlEx), new PropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty OwningTabItemProperty = DependencyProperty.RegisterAttached("OwningTabItem", typeof(TabItem), typeof(TabControlEx), new PropertyMetadata(default(TabItem)));

        [AttachedPropertyBrowsableForType(typeof(ContentPresenter))]
        public static TabItem GetOwningTabItem(DependencyObject element)
        {
            return (TabItem)element.GetValue(OwningTabItemProperty);
        }

        public static void SetOwningTabItem(DependencyObject element, TabItem value)
        {
            element.SetValue(OwningTabItemProperty, value);
        }

        public static readonly DependencyProperty MoveFocusToContentWhenSelectionChangesProperty = DependencyProperty.Register(nameof(MoveFocusToContentWhenSelectionChanges), typeof(bool), typeof(TabControlEx), new PropertyMetadata(default(bool)));

        public bool MoveFocusToContentWhenSelectionChanges
        {
            get => (bool)this.GetValue(MoveFocusToContentWhenSelectionChangesProperty);
            set => this.SetValue(MoveFocusToContentWhenSelectionChangesProperty, value);
        }

        static TabControlEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TabControlEx), new FrameworkPropertyMetadata(typeof(TabControlEx)));
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public TabControlEx()
        {
            this.Loaded += this.HandleTabControlExLoaded;
            this.Unloaded += this.HandleTabControlExUnloaded;
        }

        /// <summary>
        /// Defines if the TabPanel (Tab-Header) are visible.
        /// </summary>
        public Visibility TabPanelVisibility
        {
            get => (Visibility)this.GetValue(TabPanelVisibilityProperty);

            set => this.SetValue(TabPanelVisibilityProperty, value);
        }

        /// <summary>
        /// Gets or sets the child content visibility.
        /// </summary>
        /// <value>
        /// The child content visibility.
        /// </value>
        public Visibility ChildContentVisibility
        {
            get => (Visibility)this.GetValue(ChildContentVisibilityProperty);
            set => this.SetValue(ChildContentVisibilityProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.itemsHolder = this.Template.FindName("PART_ItemsHolder", this) as Panel;            

            this.RefreshItemsHolder();
        }

        /// <inheritdoc />
        protected override void OnItemContainerStyleChanged(Style oldItemContainerStyle, Style newItemContainerStyle)
        {
            base.OnItemContainerStyleChanged(oldItemContainerStyle, newItemContainerStyle);

            this.RefreshItemsHolder();
        }

        /// <inheritdoc />
        protected override void OnItemContainerStyleSelectorChanged(StyleSelector oldItemContainerStyleSelector, StyleSelector newItemContainerStyleSelector)
        {
            base.OnItemContainerStyleSelectorChanged(oldItemContainerStyleSelector, newItemContainerStyleSelector);

            this.RefreshItemsHolder();
        }

        /// <summary>
        /// When the items change we remove any generated panel children and add any new ones as necessary.
        /// </summary>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (this.itemsHolder == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    this.RefreshItemsHolder();
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            var contentPresenter = this.FindChildContentPresenter(item);

                            if (contentPresenter != null)
                            {
                                this.itemsHolder.Children.Remove(contentPresenter);
                            }
                        }
                    }

                    // don't do anything with new items because we don't want to
                    // create visuals that aren't being shown yet
                    this.UpdateSelectedItem();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException("Replace not implemented yet");
            }
        }

        /// <summary>
        /// Update the visible child in the ItemsHolder.
        /// </summary>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            this.UpdateSelectedItem();
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // We need this to prevent the base class to always accept CTRL + TAB navigation regardless of which keyboard navigation mode is set for this control
            if (e.Key == Key.Tab
                && (e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                var controlTabNavigation = KeyboardNavigation.GetControlTabNavigation(this);

                if (controlTabNavigation != KeyboardNavigationMode.Cycle
                    && controlTabNavigation != KeyboardNavigationMode.Continue)
                {
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            var automationPeer = new TabControlExAutomationPeer(this);
            return automationPeer;
        }

        /// <summary>
        /// Copied from <see cref="TabControl"/>. wish it were protected in that class instead of private.
        /// </summary>
        protected object GetSelectedItem()
        {
            var selectedItem = this.SelectedItem;

            if (selectedItem == null)
            {
                return null;
            }

            return selectedItem as TabItem
                ?? this.ItemContainerGenerator.ContainerFromIndex(this.SelectedIndex) as TabItem;
        }

        /// <summary>
        /// Clears all current children and calls <see cref="UpdateSelectedItem"/> afterwards.
        /// </summary>
        private void RefreshItemsHolder()
        {
            this.itemsHolder?.Children.Clear();

            this.UpdateSelectedItem();
        }

        private void HandleTabControlExLoaded(object sender, RoutedEventArgs e)
        {
            this.RefreshItemsHolder();
        }

        private void HandleTabControlExUnloaded(object sender, RoutedEventArgs e)
        {
            this.itemsHolder?.Children.Clear();
        }

        /// <summary>
        /// Generate a ContentPresenter for the selected item.
        /// </summary>
        private void UpdateSelectedItem()
        {
            if (this.itemsHolder == null)
            {
                return;
            }

            // generate a ContentPresenter if necessary
            var selectedItem = this.GetSelectedItem();

            if (selectedItem != null)
            {
                this.CreateChildContentPresenter(selectedItem);
            }

            // show the right child
            foreach (ContentPresenter contentPresenter in this.itemsHolder.Children)
            {
                var tabItem = GetOwningTabItem(contentPresenter);
                contentPresenter.Visibility = tabItem.IsSelected
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                if (this.MoveFocusToContentWhenSelectionChanges
                    && contentPresenter.Visibility == Visibility.Visible
                    && contentPresenter.IsKeyboardFocusWithin == false)
                {
                    var presenter = contentPresenter;

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                                                (Action)(() =>
                                                         {
                                                             tabItem.BringIntoView();
                                                             presenter.BringIntoView();

                                                             if (presenter.IsKeyboardFocusWithin == false)
                                                             {
                                                                 presenter.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                                                             }
                                                         }));
                }
            }
        }

        /// <summary>
        /// Create the child ContentPresenter for the given item (could be data or a TabItem) if none exists.
        /// </summary>
        private void CreateChildContentPresenter(object item)
        {
            if (item == null)
            {
                return;
            }

            if (this.FindChildContentPresenter(item) != null)
            {
                return;
            }

            // the actual child to be added.
            var contentPresenter = new ContentPresenter
            {
                Content = item is TabItem tabItem ? tabItem.Content : item,
                Visibility = Visibility.Collapsed,
                ContentTemplate = this.ContentTemplate,
                ContentTemplateSelector = this.ContentTemplateSelector,
                ContentStringFormat = this.ContentStringFormat
            };

            var owningTabItem = item as TabItem ?? (TabItem)this.ItemContainerGenerator.ContainerFromItem(item);
            
            if (owningTabItem == null)
            {
                throw new Exception("No owning TabItem could be found.");
            }

            SetOwningTabItem(contentPresenter, owningTabItem);

            this.itemsHolder.Children.Add(contentPresenter);
        }

        /// <summary>
        /// Find the <see cref="ContentPresenter"/> for the given object. Data could be a TabItem or a piece of data.
        /// </summary>
        public ContentPresenter FindChildContentPresenter(object data)
        {
            if (data == null)
            {
                return null;
            }

            if (this.itemsHolder == null)
            {
                return null;
            }

            var tabItem = data as TabItem ?? this.ItemContainerGenerator.ContainerFromItem(data) as TabItem;

            var contentPresenters = this.itemsHolder.Children
                .OfType<ContentPresenter>();

            if (tabItem != null)
            {
                return contentPresenters
                    .FirstOrDefault(contentPresenter => ReferenceEquals(GetOwningTabItem(contentPresenter), tabItem));
            }

            return contentPresenters
                .FirstOrDefault(contentPresenter => ReferenceEquals(contentPresenter.Content, data));
        }
    }

    /// <summary>
    /// Automation-Peer for <see cref="TabControlEx"/>.
    /// </summary>
    public class TabControlExAutomationPeer : TabControlAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance.
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

    /// <summary>
    /// Automation-Peer for <see cref="TabItem"/> in <see cref="TabControlEx"/>.
    /// </summary>
    public class TabItemExAutomationPeer : TabItemAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance.
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

            var contentHost = parentTabControl.FindChildContentPresenter(tabItem.Content);

            if (contentHost != null)
            {
                var contentHostPeer = new FrameworkElementAutomationPeer(contentHost);
                var contentChildren = contentHostPeer.GetChildren();

                if (contentChildren != null)
                {
                    if (headerChildren == null)
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
        /// Gets the real tab item.
        /// </summary>
        private UIElement GetWrapper()
        {
            var itemsControlAutomationPeer = this.ItemsControlAutomationPeer;

            var owner = (TabControlEx)itemsControlAutomationPeer?.Owner;

            if (owner == null)
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