using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace TouchCloudPad
{
    /// <summary>One saved cloud-game account (cookies live inside its Chrome profile).</summary>
    [DataContract]
    public class AccountProfile
    {
        [DataMember] public string Name = "账号";
        [DataMember] public string GameId = "wuwaves";   // wuwaves | hsr
        [DataMember] public string Url = "";             // cloud game login page
        [DataMember] public string ProfileDir = "";      // chrome --user-data-dir
        [DataMember] public string ChromePath = "";      // optional custom chrome exe
    }

    /// <summary>Global + per-skin configuration, persisted as JSON next to the exe.</summary>
    [DataContract]
    public class Config
    {
        [DataMember] public string SkinId = "wuwaves";
        [DataMember] public bool AlwaysOnTop = true;
        [DataMember] public double Opacity = 0.92;
        [DataMember] public int CameraSensitivity = 26;   // touch px -> stick deflection
        [DataMember] public double WindowLeft = 60;
        [DataMember] public double WindowTop = 120;

        [DataMember] public Dictionary<string, SkinSettings> Skins = new Dictionary<string, SkinSettings>();
        [DataMember] public List<AccountProfile> Profiles = new List<AccountProfile>();
        [DataMember] public int ActiveProfile = -1;

        // ---- paths ----
        public static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "TouchCloudPad");
                return Path.Combine(dir, "config.json");
            }
        }

        /// <summary>Fallback location: next to the exe (portable mode).</summary>
        public static string ExeConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"); }
        }

        public static string DefaultProfileRoot
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "TouchCloudPad", "Profiles");
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        public static Config Load()
        {
            string path = File.Exists(ConfigPath) ? ConfigPath : File.Exists(ExeConfigPath) ? ExeConfigPath : null;
            try
            {
                if (path != null)
                {
                    var ser = new DataContractJsonSerializer(typeof(Config));
                    using (var fs = File.OpenRead(path))
                        return (Config)ser.ReadObject(fs);
                }
            }
            catch { /* corrupted/absent -> defaults */ }
            var cfg = new Config();
            cfg.Skins["wuwaves"] = new SkinSettings();
            cfg.Skins["hsr"] = new SkinSettings();
            cfg.Skins["zzz"] = new SkinSettings();
            return cfg;
        }

        public void Save()
        {
            try
            {
                SaveTo(ConfigPath);
            }
            catch
            {
                try { SaveTo(ExeConfigPath); }
                catch { }
            }
        }

        private void SaveTo(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var ser = new DataContractJsonSerializer(typeof(Config));
            using (var fs = File.Create(path))
            using (var writer = System.Runtime.Serialization.Json.JsonReaderWriterFactory
                   .CreateJsonWriter(fs, new System.Text.UTF8Encoding(false), true, true, "  "))
            {
                ser.WriteObject(writer, this);
                writer.Flush();
            }
        }
    }
}
