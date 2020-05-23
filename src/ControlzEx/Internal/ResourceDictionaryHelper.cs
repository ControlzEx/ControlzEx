#nullable enable
namespace ControlzEx.Internal
{
    using System.Windows;
    using JetBrains.Annotations;

    public static class ResourceDictionaryHelper
    {
        /// <summary>
        /// Gets the value associated with <paramref name="key"/> directly from <paramref name="resourceDictionary"/>.
        /// </summary>
        public static object? GetValueFromKey([NotNull] ResourceDictionary resourceDictionary, object key)
        {
            foreach (var resourceKey in resourceDictionary.Keys)
            {
                if (key.Equals(resourceKey))
                {
                    try
                    {
                        return resourceDictionary[resourceKey];
                    }
                    catch
                    {
                        // ignored
                        // if we get an exception here the resource dictionary is, most likely, malformed
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if <paramref name="resourceDictionary"/> directly contains <paramref name="key"/>.
        /// </summary>
        public static bool ContainsKey(ResourceDictionary resourceDictionary, object key)
        {
            foreach (var resourceKey in resourceDictionary.Keys)
            {
                if (key.Equals(resourceKey))
                {
                    return true;
                }
            }

            return false;
        }
    }
}