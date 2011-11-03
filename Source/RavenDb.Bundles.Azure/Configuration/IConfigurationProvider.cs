using System;

namespace RavenDb.Bundles.Azure.Configuration
{
    public interface IConfigurationProvider
    {
        string GetSetting(string key);
    }

    public static class ConfigurationProviderExtensions
    {
        public static TValue GetSetting<TValue>(this IConfigurationProvider configurationProvider, string key,TValue defaultValue)
        {
            var rawValue = configurationProvider.GetSetting(key);

            if (rawValue != null)
            {
                return (TValue) Convert.ChangeType(rawValue, typeof (TValue));
            }

            return defaultValue;
        }
    }
}