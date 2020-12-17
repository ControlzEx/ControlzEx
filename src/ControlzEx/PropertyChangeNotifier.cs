namespace ControlzEx
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;
    using JetBrains.Annotations;

    /// <summary>
    /// AddValueChanged of dependency property descriptor results in memory leak as you already know.
    /// So, as described here, you can create custom class PropertyChangeNotifier to listen
    /// to any dependency property changes.
    /// 
    /// This class takes advantage of the fact that bindings use weak references to manage associations
    /// so the class will not root the object who property changes it is watching. It also uses a WeakReference
    /// to maintain a reference to the object whose property it is watching without rooting that object.
    /// In this way, you can maintain a collection of these objects so that you can unhook the property
    /// change later without worrying about that collection rooting the object whose values you are watching.
    /// 
    /// Complete implementation can be found here: http://agsmith.wordpress.com/2008/04/07/propertydescriptor-addvaluechanged-alternative/
    /// </summary>
    public sealed class PropertyChangeNotifier : DependencyObject, IDisposable
    {
        private readonly WeakReference propertySource;

        public PropertyChangeNotifier(DependencyObject propertySource, string path)
            : this(propertySource, new PropertyPath(path))
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
            : this(propertySource, new PropertyPath(property))
        {
        }

        public PropertyChangeNotifier([NotNull] DependencyObject propertySource, [NotNull] PropertyPath property)
        {
            if (propertySource is null)
            {
                throw new ArgumentNullException(nameof(propertySource));
            }

            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            this.propertySource = new WeakReference(propertySource);
            var binding = new Binding { Path = property, Mode = BindingMode.OneWay, Source = propertySource };
            BindingOperations.SetBinding(this, ValueProperty, binding);
        }

        public DependencyObject? PropertySource
        {
            get
            {
                try
                {
                    // note, it is possible that accessing the target property
                    // will result in an exception so i’ve wrapped this check
                    // in a try catch
                    return this.propertySource.IsAlive ? this.propertySource.Target as DependencyObject : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
        public static readonly DependencyProperty ValueProperty
            = DependencyProperty.Register(nameof(Value),
                                          typeof(object),
                                          typeof(PropertyChangeNotifier),
                                          new FrameworkPropertyMetadata(null, OnValueChanged));

        /// <summary>
        /// Gets or sets the value of the watched property.
        /// </summary>
        /// <seealso cref="ValueProperty"/>
        [Description("Gets or sets the value of the watched property.")]
        [Category("Behavior")]
        [Bindable(true)]
        public object? Value
        {
            get { return (object?)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var notifier = (PropertyChangeNotifier)d;
            if (notifier.RaiseValueChanged)
            {
                notifier.ValueChanged?.Invoke(notifier.PropertySource, EventArgs.Empty);
            }
        }

        public event EventHandler? ValueChanged;

        public bool RaiseValueChanged { get; set; } = true;

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ValueProperty);
        }
    }
}