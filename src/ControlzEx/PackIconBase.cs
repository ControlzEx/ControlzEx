namespace ControlzEx
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public abstract class PackIconBase : Control
    {
        internal abstract void UpdateData();
    }

    /// <summary>
    /// Base class for creating an icon control for icon packs.
    /// </summary>
    public abstract class PackIconBase<TKind> : PackIconBase
        where TKind : notnull
    {
        private static Lazy<IDictionary<TKind, string>>? dataIndex;

        /// <summary>Creates a new instance.</summary>
        /// <param name="dataIndexFactory">
        /// Inheritors should provide a factory for setting up the path data index (per icon kind).
        /// The factory will only be utilized once, across all closed instances (first instantiation wins).
        /// </param>
        protected PackIconBase(Func<IDictionary<TKind, string>> dataIndexFactory)
        {
            if (dataIndexFactory is null)
            {
                throw new ArgumentNullException(nameof(dataIndexFactory));
            }

            if (dataIndex is null)
            {
                dataIndex = new Lazy<IDictionary<TKind, string>>(dataIndexFactory);
            }
        }

        /// <summary>Identifies the <see cref="Kind"/> dependency property.</summary>
        public static readonly DependencyProperty KindProperty
            = DependencyProperty.Register(nameof(Kind),
                                          typeof(TKind),
                                          typeof(PackIconBase<TKind>),
                                          new PropertyMetadata(default(TKind), OnKindChanged));

        private static void OnKindChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ((PackIconBase<TKind>)dependencyObject).UpdateData();
        }

        /// <summary>
        /// Gets or sets the icon to display.
        /// </summary>
        public TKind Kind
        {
            get { return (TKind)this.GetValue(KindProperty); }
            set { this.SetValue(KindProperty, value); }
        }

        private static readonly DependencyPropertyKey DataPropertyKey
            = DependencyProperty.RegisterReadOnly(nameof(Data),
                                                  typeof(string),
                                                  typeof(PackIconBase<TKind>),
                                                  new PropertyMetadata(string.Empty));

        // ReSharper disable once StaticMemberInGenericType

        /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
        public static readonly DependencyProperty DataProperty = DataPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the icon path data for the current <see cref="Kind"/>.
        /// </summary>
        [TypeConverter(typeof(GeometryConverter))]
#pragma warning disable WPF0012 // CLR property type should match registered type.
        public string? Data
#pragma warning restore WPF0012 // CLR property type should match registered type.
        {
            get { return (string?)this.GetValue(DataProperty); }
            private set { this.SetValue(DataPropertyKey, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.UpdateData();
        }

        internal override void UpdateData()
        {
            string? data = null;
            dataIndex?.Value?.TryGetValue(this.Kind, out data);
            this.Data = data;
        }
    }
}