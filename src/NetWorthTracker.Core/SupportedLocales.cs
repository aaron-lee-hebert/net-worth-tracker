namespace NetWorthTracker.Core;

public static class SupportedLocales
{
    public static readonly Dictionary<string, string> Locales = new()
    {
        { "en-US", "English (United States)" },
        { "en-GB", "English (United Kingdom)" },
        { "en-CA", "English (Canada)" },
        { "en-AU", "English (Australia)" }
    };

    public static readonly Dictionary<string, string> Currencies = new()
    {
        { "en-US", "USD" },
        { "en-GB", "GBP" },
        { "en-CA", "CAD" },
        { "en-AU", "AUD" }
    };

    public static bool IsSupported(string? locale)
    {
        return !string.IsNullOrEmpty(locale) && Locales.ContainsKey(locale);
    }

    public static string GetCurrency(string locale)
    {
        return Currencies.TryGetValue(locale, out var currency) ? currency : "USD";
    }

    public static string GetDisplayName(string locale)
    {
        return Locales.TryGetValue(locale, out var name) ? name : locale;
    }
}
