using System.Collections.Generic;
using Newtonsoft.Json;
using CitizenFX.Core.Native;

namespace vox.opticom
{
    public static class ConfigLoader
    {
        public static Config Config { get; private set; } = new Config();

        public static bool UseWhitelist => Config.UseWhitelist;
        public static List<string> WhitelistedVehicles => Config.WhitelistedVehicles;
        public static bool Debug => Config.Debug;

        static ConfigLoader()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            string json = API.LoadResourceFile(API.GetCurrentResourceName(), "config.json");
            if (!string.IsNullOrEmpty(json))
            {
                Config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
            }
        }
    }
}