using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ComfortUI
{
    // Only new keys entered in this mod are stored here, protected for this Windows user.
    // They never enter Preferences, Cohtml bootstrap scripts or diagnostic logs.
    internal sealed class TranslationSecrets
    {
        private readonly string path;
        private JObject values = new JObject();
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ComfortUI.translation-credentials.v1");
        internal TranslationSecrets(string file)
        {
            path = file;
            if (!File.Exists(path)) return;
            var protectedBytes = File.ReadAllBytes(path);
            if (protectedBytes.Length > 16384) throw new InvalidDataException();
            var data = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            try { values = JObject.Parse(Encoding.UTF8.GetString(data)); }
            finally { Array.Clear(data, 0, data.Length); }
        }
        private static string Scope(string provider, string endpoint)
        {
            return provider == "microsoft" ? "microsoft-global" : endpoint.TrimEnd('/');
        }
        internal string Get(string provider, string endpoint)
        {
            var value = values[provider] as JObject;
            return (string)value?["scope"] == Scope(provider, endpoint) ? (string)value["key"] ?? "" : "";
        }
        internal void Save(string provider, string endpoint, string key)
        {
            if (provider != "ai" && provider != "microsoft" && provider != "libre" || !TranslationProviders.ValidValue(key, 1024)) throw new FormatException();
            if (provider != "microsoft" && !TranslationProviders.ValidEndpoint(endpoint)) throw new FormatException();
            var next = (JObject)values.DeepClone();
            if (key.Length == 0) next.Remove(provider);
            else next[provider] = new JObject { ["scope"] = Scope(provider, endpoint), ["key"] = key };
            var bytes = Encoding.UTF8.GetBytes(next.ToString(Newtonsoft.Json.Formatting.None));
            byte[] protectedBytes;
            try { protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, protectedBytes);
                if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
                values = next;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
