namespace ControlzEx.Tests.Controls
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Controls;
    using ControlzEx.Controls;
    using ControlzEx.Tests.TestClasses;
    using NUnit.Framework;

    [TestFixture]
    public class TabControlExTests
    {
        [Test]
        public void TestAddRemoveInsertWithItemsSource()
        {
            var items = new ObservableCollection<string>
            {
                "1",
                "2",
                "3"
            };

            var tabControl = new TabControlEx
            {
                ItemsSource = items
            };

            using (new TestWindow(tabControl))
            {
                var itemsPanel = (Panel)tabControl.GetType().GetField("itemsHolder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tabControl);

                var tabItemsInitial = GetTabItems(tabControl);

                Assert.That(tabItemsInitial, Has.Count.EqualTo(3));

                Assert.That(itemsPanel.Children.Count, Is.EqualTo(1));

                foreach (var tabItem in tabItemsInitial)
                {
                    tabItem.IsSelected = true;
                    UITestHelper.DoEvents();
                }

                Assert.That(itemsPanel.Children.Count, Is.EqualTo(3));

                items.RemoveAt(1);
                items.Insert(1, "2");

                UITestHelper.DoEvents();

                Assert.That(itemsPanel.Children.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void TestItemContainerGeneratorRefresh()
        {
            var items = new ObservableCollection<string>
            {
                "1",
                "2",
                "3"
            };

            var tabControl = new TabControlEx
            {
                ItemsSource = items
            };

            using (new TestWindow(tabControl))
            {
                var itemsPanel = (Panel)tabControl.GetType().GetField("itemsHolder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tabControl);

                var tabItemsInitial = GetTabItems(tabControl);

                Assert.That(tabItemsInitial, Has.Count.EqualTo(3));

                Assert.That(itemsPanel.Children.Count, Is.EqualTo(1));

                tabControl.ItemContainerGenerator.GetType().GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tabControl.ItemContainerGenerator, null);

                UITestHelper.DoEvents();

                Assert.That(itemsPanel.Children.Count, Is.EqualTo(1));
            }
        }

        private static IList<TabItem> GetTabItems(TabControlEx tabControl)
        {
            return Enumerable.Range(0, tabControl.ItemContainerGenerator.Items.Count).Select(x => tabControl.ItemContainerGenerator.ContainerFromIndex(x))
                             .Cast<TabItem>()
                             .ToList();
        }
    }
}