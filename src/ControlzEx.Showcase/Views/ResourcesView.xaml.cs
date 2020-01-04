namespace ControlzEx.Showcase.Views
{
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using ControlzEx.Theming;

    public partial class ResourcesView
    {
        public ResourcesView()
        {
            this.InitializeComponent();

            this.ThemeAnalyzers = new ObservableCollection<ThemeResource>();

            var view = CollectionViewSource.GetDefaultView(this.ThemeAnalyzers);
            view.SortDescriptions.Add(new SortDescription(nameof(ThemeResource.Key), ListSortDirection.Ascending));

            this.UpdateThemeAnalyzers(ThemeManager.DetectTheme());

            ThemeManager.ThemeChanged += this.ThemeManager_ThemeChanged;
        }

        public static readonly DependencyProperty ThemeAnalyzersProperty = DependencyProperty.Register(
            nameof(ThemeAnalyzers), typeof(ObservableCollection<ThemeResource>), typeof(ResourcesView), new PropertyMetadata(default(ObservableCollection<ThemeResource>)));

        public ObservableCollection<ThemeResource> ThemeAnalyzers
        {
            get { return (ObservableCollection<ThemeResource>)this.GetValue(ThemeAnalyzersProperty); }
            set { this.SetValue(ThemeAnalyzersProperty, value); }
        }

        public class ThemeResource
        {
            public ThemeResource(ResourceDictionary resourceDictionary, DictionaryEntry dictionaryEntry)
                : this(resourceDictionary, dictionaryEntry.Key.ToString(), dictionaryEntry.Value)
            {
            }

            public ThemeResource(ResourceDictionary resourceDictionary, string key, object value)
            {
                this.Source = resourceDictionary.Source?.ToString() ?? "Runtime";
                this.Key = key;

                this.Value = value switch
                {
                    Color color => new Rectangle { Fill = new SolidColorBrush(color) },
                    Brush brush => new Rectangle { Fill = brush },
                    _ => null
                };

                this.StringValue = value.ToString();
            }

            public string Source { get; }

            public string Key { get; }

            public object Value { get; }

            public string StringValue { get; }
        }

        private void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            this.UpdateThemeAnalyzers(e.NewTheme);
        }

        private void UpdateThemeAnalyzers(Theme theme)
        {
            this.ThemeAnalyzers.Clear();

            if (theme is null)
            {
                return;
            }

            foreach (var resourceDictionary in theme.Resources)
            {
                foreach (DictionaryEntry dictionaryEntry in resourceDictionary)
                {
                    this.ThemeAnalyzers.Add(new ThemeResource(resourceDictionary, dictionaryEntry));
                }
            }
        }
    }
}