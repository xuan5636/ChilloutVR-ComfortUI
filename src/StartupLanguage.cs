using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ComfortUI
{
    internal static class StartupLanguage
    {
        [DllImport("kernel32.dll")] private static extern ushort GetUserDefaultUILanguage();

        internal static string FromLocale(string locale)
        {
            var primary = (locale ?? "").Trim().Replace('_', '-').Split('-')[0].ToLowerInvariant();
            return LanguageSupport.Supported(primary) ? primary : "en";
        }

        internal static string Choose(bool hasSavedPreferences, string savedLanguage, Func<string> systemLocale)
        {
            // An upgrade or a user selection always wins over automatic detection.
            if (hasSavedPreferences) return savedLanguage;
            try { return FromLocale(systemLocale()); }
            catch { return "en"; }
        }

        internal static string SystemLocale()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try { return CultureInfo.GetCultureInfo(GetUserDefaultUILanguage()).Name; }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }
                catch (CultureNotFoundException) { }
            }
            return CultureInfo.CurrentUICulture.Name;
        }
    }
}
