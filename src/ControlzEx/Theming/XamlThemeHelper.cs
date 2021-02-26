#nullable enable
namespace ControlzEx.Theming
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class XamlThemeHelper
    {
        /// <summary>
        /// Works around an issue in the XamlReader.
        /// Without this fix the XamlReader would not be able to read the XAML we produced earlier because it does not know where to look for the types.
        /// The real issue is that we can't use the full namespace, with assembly hint, at compile time of the original project because said assembly does not yet exist and would cause a compile time error.
        /// Hence we have to use this workaround to enable both.
        /// The issue 
        /// </summary>
        /// <returns>The fixed version of <paramref name="xamlContent"/>.</returns>
        /// <example>
        /// If you have the following in your XAML file:
        /// xmlns:markup="clr-namespace:MahApps.Metro.Markup"
        /// xmlns:markupWithAssembly="clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro"
        /// It get's converted to:
        /// xmlns:markup="clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro"
        /// xmlns:markupWithAssembly="clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro"
        /// </example>
        public static string FixXamlReaderXmlNsIssue(string xamlContent)
        {
            // Check if we have to fix something
            if (xamlContent.IndexOf("WithAssembly=\"", StringComparison.Ordinal) < 0)
            {
                return xamlContent;
            }

            // search for namespace names with suffix "WithAssembly"
            // example: xmlns:markupWithAssembly="clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro"
            var withAssemblyMatches = Regex.Matches(xamlContent, @"\s*xmlns:(?<namespace_name>.+?)WithAssembly=(?<namespace>"".+?"")");

            foreach (var withAssemblyMatch in withAssemblyMatches.OfType<Match>())
            {
                // search for namespaces that are the same without suffix "WithAssembly"
                // example: xmlns:markup="clr-namespace:MahApps.Metro.Markup"
                var originalMatches = Regex.Matches(xamlContent, $@"\s*xmlns:({withAssemblyMatch.Groups["namespace_name"].Value})=(?<namespace>"".+?"")");

                foreach (var originalMatch in originalMatches.OfType<Match>())
                {
                    // replace the used namespace value of the namespaces without the suffix with the content from the namespace with suffix
                    // example result: xmlns:markup="clr-namespace:MahApps.Metro.Markup;assembly=MahApps.Metro"
                    xamlContent = xamlContent.Replace(originalMatch.Groups["namespace"].Value, withAssemblyMatch.Groups["namespace"].Value);
                }
            }

            return xamlContent;
        }
    }
}