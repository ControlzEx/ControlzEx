namespace ControlzEx.Internal
{
    using System.Reflection;

    internal static class FrameworkAppContextSwitches
    {
        private static readonly PropertyInfo? SelectionPropertiesCanLagBehindSelectionChangedEventPropertyInfo = typeof(System.Windows.Controls.TabControl).Assembly.GetType("MS.Internal.FrameworkAppContextSwitches")?.GetProperty("SelectionPropertiesCanLagBehindSelectionChangedEvent", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        // This default value is only relevant if the type/property we are looking for is not present in the current runtime version.
        // In https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/retargeting/4.7-4.7.1#selector-selectionchanged-event-and-selectedvalue-property
        // it states that the "new" behavior is enabled if the value for the switch is false, by using true here if the type is not found we respect that default value which means not using the "new" behavior.
        private const bool SelectionPropertiesCanLagBehindSelectionChangedEventDefaultValue = true;

        internal static bool SelectionPropertiesCanLagBehindSelectionChangedEvent => ((bool?)SelectionPropertiesCanLagBehindSelectionChangedEventPropertyInfo?.GetValue(null)).GetValueOrDefault(SelectionPropertiesCanLagBehindSelectionChangedEventDefaultValue);
    }
}