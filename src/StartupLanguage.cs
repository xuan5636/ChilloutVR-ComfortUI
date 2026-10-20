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
            var parts = (locale ?? "").Trim().Replace('_', '-').ToLowerInvariant().Split('-');
            var primary = parts[0];
            if (primary == "zh")
            {
                // An explicit script takes precedence over the region (e.g. zh-Hans-HK).
                if (Array.IndexOf(parts, "hant") >= 0) return "zh-Hant";
                if (Array.IndexOf(parts, "hans") >= 0) return "zh";
                return Array.IndexOf(parts, "tw") >= 0 || Array.IndexOf(parts, "hk") >= 0 || Array.IndexOf(parts, "mo") >= 0 ? "zh-Hant" : "zh";
            }
            if (primary == "pt") return "pt-BR";
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
