using Microsoft.Windows.ApplicationModel.Resources;

namespace wordpadneo.Helpers
{
    internal static class LocalizationHelper
    {
        private static readonly ResourceLoader _resourceLoader = new();

        public static string GetString(string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                return string.Empty;
            }

            string value = _resourceLoader.GetString(resourceKey);
            return string.IsNullOrWhiteSpace(value) ? resourceKey : value;
        }
    }
}
