namespace ControlzEx.Controls
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;
    using ControlzEx.Automation.Peers;

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

            this.ClearItemsHolder();

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

            if (this.itemsHolder is null)
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
                    if (e.OldItems is null == false)
                    {
                        foreach (var item in e.OldItems)
                        {
                            var contentPresenter = this.FindChildContentPresenter(item);

                            if (contentPresenter is null == false)
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
                    // Replace is not really implemented yet
                    this.RefreshItemsHolder();
                    break;
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
        public TabItem GetSelectedTabItem()
        {
            var selectedItem = this.SelectedItem;
            if (selectedItem is null)
            {
                return null;
            }

            var tabItem = selectedItem as TabItem;
            if (tabItem is null)
            {
                tabItem = this.ItemContainerGenerator.ContainerFromIndex(this.SelectedIndex) as TabItem;

                if (tabItem is null
                    // is this really the container we wanted?
                    || ReferenceEquals(selectedItem, this.ItemContainerGenerator.ItemFromContainer(tabItem)) == false)
                {
                    tabItem = this.ItemContainerGenerator.ContainerFromItem(selectedItem) as TabItem;
                }
            }

            return tabItem;
        }

        private void ClearItemsHolder()
        {
            if (this.itemsHolder is null)
            {
                return;
            }

            foreach (var itemsHolderChild in this.itemsHolder.Children)
            {
                var contentPresenter = itemsHolderChild as ContentPresenter;

                contentPresenter?.ClearValue(OwningTabItemProperty);
            }

            this.itemsHolder.Children.Clear();
        }

        /// <summary>
        /// Clears all current children by calling <see cref="ClearItemsHolder"/> and calls <see cref="UpdateSelectedItem"/> afterwards.
        /// </summary>
        private void RefreshItemsHolder()
        {
            this.ClearItemsHolder();

            this.UpdateSelectedItem();
        }

        private void HandleTabControlExLoaded(object sender, RoutedEventArgs e)
        {
            this.RefreshItemsHolder();
        }

        private void HandleTabControlExUnloaded(object sender, RoutedEventArgs e)
        {
            this.ClearItemsHolder();
        }

        /// <summary>
        /// Generate a ContentPresenter for the selected item.
        /// </summary>
        private void UpdateSelectedItem()
        {
            if (this.itemsHolder is null)
            {
                return;
            }

            var selectedItem = this.GetSelectedTabItem();

            if (selectedItem is null == false)
            {
                // generate a ContentPresenter if necessary
                this.CreateChildContentPresenter(selectedItem);
            }

            // show the right child
            foreach (ContentPresenter contentPresenter in this.itemsHolder.Children)
            {
                var tabItem = GetOwningTabItem(contentPresenter);

                // Hide all non selected items and show the selected item
                contentPresenter.Visibility = tabItem.IsSelected
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                if (this.MoveFocusToContentWhenSelectionChanges)
                {
                    this.MoveFocusToContent(contentPresenter, tabItem);
                }
            }
        }

        /// <summary>
        /// Create the child ContentPresenter for the given item (could be data or a TabItem) if none exists.
        /// </summary>
        private void CreateChildContentPresenter(object item)
        {
            if (item is null)
            {
                return;
            }

            // Jump out if we already created a ContentPresenter for this item
            if (this.FindChildContentPresenter(item) is null == false)
            {
                return;
            }

            // the actual child to be added
            var contentPresenter = new ContentPresenter
            {
                Content = item is TabItem tabItem ? tabItem.Content : item,
                Visibility = Visibility.Collapsed,
                ContentTemplate = this.ContentTemplate,
                ContentTemplateSelector = this.ContentTemplateSelector,
                ContentStringFormat = this.ContentStringFormat
            };

            var owningTabItem = item as TabItem ?? (TabItem)this.ItemContainerGenerator.ContainerFromItem(item);
            
            if (owningTabItem is null)
            {
                throw new Exception("No owning TabItem could be found.");
            }

            SetOwningTabItem(contentPresenter, owningTabItem);

            this.itemsHolder.Children.Add(contentPresenter);
        }

        /// <summary>
        /// Find the <see cref="ContentPresenter"/> for the given object. Data could be a TabItem or a piece of data.
        /// </summary>
        public ContentPresenter FindChildContentPresenter(object item)
        {
            if (item is null)
            {
                return null;
            }

            if (this.itemsHolder is null)
            {
                return null;
            }

            var tabItem = item as TabItem ?? (TabItem)this.ItemContainerGenerator.ContainerFromItem(item);

            var contentPresenters = this.itemsHolder.Children
                .OfType<ContentPresenter>();

            if (tabItem is null == false)
            {
                return contentPresenters
                    .FirstOrDefault(contentPresenter => ReferenceEquals(GetOwningTabItem(contentPresenter), tabItem));
            }

            return contentPresenters
                .FirstOrDefault(contentPresenter => ReferenceEquals(contentPresenter.Content, item));
        }

        private void MoveFocusToContent(ContentPresenter contentPresenter, TabItem tabItem)
        {
            // Do nothing if the item is not visible or already has keyboard focus
            if (contentPresenter.Visibility != Visibility.Visible
                || contentPresenter.IsKeyboardFocusWithin)
            {
                return;
            }

            var presenter = contentPresenter;

            this.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)(() =>
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