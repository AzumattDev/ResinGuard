using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ResinGuard
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ResinGuardPlugin : BaseUnityPlugin
    {
        internal const string ModName = "ResinGuard";
        internal const string ModVersion = "1.2.1";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private static string ConfigFileName = $"{ModGUID}.cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        private static string ExclusionFileName = $"{ModGUID}_ExcludedPieces.yml";
        private static string ExclusionFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ExclusionFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource ResinGuardLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion, ModRequired = false };
        internal static HashSet<string> excludedResinPieces = [];
        internal static HashSet<string> excludedTarPieces = [];
        private static CustomSyncedValue<string> yamlConfigContent = new(ConfigSync, $"{ModGUID}_ExcludedPiecesYAML", "");

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            if (!File.Exists(ExclusionFileFullPath))
            {
                WriteConfigFileFromResource(ExclusionFileFullPath);
            }

            yamlConfigContent.ValueChanged += LoadExcludedPiecesFromYaml;
            yamlConfigContent.AssignLocalValue(File.ReadAllText(ExclusionFileFullPath));

            LoadExcludedPiecesFromYaml();

            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            DecayTime = config("1 - General", "Decay Time", 3600f, "The time it takes for resin to decay in seconds. (Realtime)");
            ShowWrongItemMessage = config("1 - General", "Show Wrong Item Message", Toggle.Off, "If on, a message will be shown when you try to use an item that isn't resin or tar when hovering over a piece.", false);
            EnableVisualUpdates = Config.Bind("2 - Visual", "Enable Visual Updates", Toggle.On, "Enable or disable visual updates when resin or tar is applied.");
            EnableVisualUpdates.SettingChanged += (sender, args) => { ResinProtection.ForceUpdateVisuals(); };
            MaxResin = config("3 - Protection", "Max Resin", 10, "The maximum amount of resin a piece can have. WARNING, the higher the number the more health the piece will have. By default, it's balanced to double the health of the piece.");
            MaxResin.SettingChanged += (sender, args) => { ResinProtection.UpdateProtectionValues(); };
            RepairWhenProtectionApplied = config("3 - Protection", "Repair When Protection Applied", Toggle.On, "If on, the piece will be repaired when resin or tar is applied. This is balanced because technically it's costing more to repair it than if you used your hammer.");
            ResinColor = config("3 - Protection", "Resin Color", Color.yellow, "Visual color of the piece when resin is applied.", false);
            TarColor = config("3 - Protection", "Tar Color", Color.gray, "Visual color of the piece when tar is applied.", false);


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
            Config.Save();
            Config.SaveOnConfigSet = saveOnSet;
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;

            FileSystemWatcher yamlwatcher = new(Paths.ConfigPath, ExclusionFileName);
            yamlwatcher.Changed += OnConfigFileChanged;
            yamlwatcher.Created += OnConfigFileChanged;
            yamlwatcher.Renamed += OnConfigFileChanged;
            yamlwatcher.IncludeSubdirectories = true;
            yamlwatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            yamlwatcher.EnableRaisingEvents = true;
        }


        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                ResinGuardLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                ResinGuardLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ResinGuardLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private static void LoadExcludedPiecesFromYaml()
        {
            if (string.IsNullOrEmpty(yamlConfigContent.Value))
                return;

            using StringReader reader = new StringReader(yamlConfigContent.Value);
            IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

            Dictionary<string, List<string>> yamlConfig = deserializer.Deserialize<Dictionary<string, List<string>>>(reader);
            if (yamlConfig != null)
            {
                if (yamlConfig.TryGetValue("Resin", out List<string>? resin))
                {
                    excludedResinPieces = [..resin];
                }

                if (yamlConfig.TryGetValue("Tar", out List<string>? tar))
                {
                    excludedTarPieces = [..tar];
                }
            }
        }

        private static void WriteConfigFileFromResource(string configFilePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"{ModName}.{ExclusionFileName}";

            using Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                throw new FileNotFoundException($"Resource '{resourceName}' not found in the assembly.");
            }

            using StreamReader reader = new StreamReader(resourceStream);
            string contents = reader.ReadToEnd();

            File.WriteAllText(configFilePath, contents);
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ExclusionFileFullPath)) return;
            try
            {
                yamlConfigContent.AssignLocalValue(File.ReadAllText(ExclusionFileFullPath));
            }
            catch
            {
                ResinGuardLogger.LogError($"There was an issue loading your {ExclusionFileName}");
                ResinGuardLogger.LogError("Please check your config entries for spelling and format!");
            }

            LoadExcludedPiecesFromYaml();
        }

        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<float> DecayTime = null!;
        public static ConfigEntry<Toggle> ShowWrongItemMessage = null!;
        public static ConfigEntry<Toggle> EnableVisualUpdates = null!;
        public static ConfigEntry<int> MaxResin = null!;
        public static ConfigEntry<Toggle> RepairWhenProtectionApplied = null!;
        public static ConfigEntry<Color> ResinColor = null!;
        public static ConfigEntry<Color> TarColor = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        #endregion
    }
}