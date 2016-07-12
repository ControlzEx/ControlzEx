using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ControlzEx
{
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
        public static readonly DependencyProperty ChildContentVisibilityProperty
            = DependencyProperty.Register(nameof(ChildContentVisibility),
                                          typeof(Visibility),
                                          typeof(TabControlEx),
                                          new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Gets or sets the child content visibility.
        /// </summary>
        /// <value>
        /// The child content visibility.
        /// </value>
        public Visibility ChildContentVisibility
        {
            get { return (Visibility)this.GetValue(ChildContentVisibilityProperty); }
            set { this.SetValue(ChildContentVisibilityProperty, value); }
        }

        public TabControlEx()
        {
            // this is necessary so that we get the initial databound selected item
            this.ItemContainerGenerator.StatusChanged += this.ItemContainerGenerator_StatusChanged;
            this.Loaded += TabControlEx_Loaded;
        }

        /// <summary>
        /// if containers are done, generate the selected item
        /// </summary>
        /// <param name = "sender"></param>
        /// <param name = "e"></param>
        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (this.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                this.ItemContainerGenerator.StatusChanged -= this.ItemContainerGenerator_StatusChanged;
                this.UpdateSelectedItem();
            }
        }

        /// <summary>
        /// in some scenarios we need to update when loaded in case the ApplyTemplate happens before the databind.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControlEx_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSelectedItem();
        }

        /// <summary>
        /// get the ItemsHolder and generate any children
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this._itemsHolder = this.GetTemplateChild("PART_ItemsHolder") as Panel;
            this.UpdateSelectedItem();
        }

        /// <summary>
        /// when the items change we remove any generated panel children and add any new ones as necessary
        /// </summary>
        /// <param name = "e"></param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (this._itemsHolder == null)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    this._itemsHolder.Children.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            ContentPresenter cp = this.FindChildContentPresenter(item);
                            if (cp != null)
                            {
                                this._itemsHolder.Children.Remove(cp);
                            }
                        }
                    }

                    // don't do anything with new items because we don't want to
                    // create visuals that aren't being shown

                    this.UpdateSelectedItem();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    throw new NotImplementedException("Replace not implemented yet");
            }
        }

        /// <summary>
        /// update the visible child in the ItemsHolder
        /// </summary>
        /// <param name = "e"></param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            this.UpdateSelectedItem();
        }

        /// <summary>
        /// generate a ContentPresenter for the selected item
        /// </summary>
        private void UpdateSelectedItem()
        {
            if (this._itemsHolder == null)
            {
                return;
            }

            // generate a ContentPresenter if necessary
            TabItem item = this.GetSelectedTabItem();
            if (item != null)
            {
                this.CreateChildContentPresenter(item);
            }

            // show the right child
            foreach (ContentPresenter child in this._itemsHolder.Children)
            {
                child.Visibility = ((child.Tag as TabItem).IsSelected) ? Visibility.Visible : this.ChildContentVisibility;
            }
        }

        /// <summary>
        /// create the child ContentPresenter for the given item (could be data or a TabItem)
        /// </summary>
        /// <param name = "item"></param>
        /// <returns></returns>
        private ContentPresenter CreateChildContentPresenter(object item)
        {
            if (item == null)
            {
                return null;
            }

            ContentPresenter cp = this.FindChildContentPresenter(item);

            if (cp != null)
            {
                return cp;
            }

            // the actual child to be added.  cp.Tag is a reference to the TabItem
            var tabItem = item as TabItem;
            cp = new ContentPresenter();
            cp.Content = tabItem != null ? tabItem.Content : item;
            cp.ContentTemplate = this.SelectedContentTemplate;
            cp.ContentTemplateSelector = this.SelectedContentTemplateSelector;
            cp.ContentStringFormat = this.SelectedContentStringFormat;
            cp.Visibility = this.ChildContentVisibility;
            cp.Tag = tabItem ?? this.ItemContainerGenerator.ContainerFromItem(item);
            this._itemsHolder.Children.Add(cp);
            return cp;
        }

        /// <summary>
        /// Find the CP for the given object.  data could be a TabItem or a piece of data
        /// </summary>
        /// <param name = "data"></param>
        /// <returns></returns>
        private ContentPresenter FindChildContentPresenter(object data)
        {
            if (data is TabItem)
            {
                data = ((TabItem)data).Content;
            }

            if (data == null)
            {
                return null;
            }

            if (this._itemsHolder == null)
            {
                return null;
            }

            foreach (ContentPresenter cp in this._itemsHolder.Children)
            {
                if (cp.Content == data)
                {
                    return cp;
                }
            }

            return null;
        }

        /// <summary>
        /// copied from TabControl; wish it were protected in that class instead of private
        /// </summary>
        /// <returns></returns>
        protected TabItem GetSelectedTabItem()
        {
            object selectedItem = base.SelectedItem;
            if (selectedItem == null)
            {
                return null;
            }
            TabItem item = selectedItem as TabItem;
            if (item == null)
            {
                item = base.ItemContainerGenerator.ContainerFromIndex(base.SelectedIndex) as TabItem;
            }
            return item;
        }

        private Panel _itemsHolder = null;
    }
}