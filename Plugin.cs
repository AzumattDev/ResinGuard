using System;
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

namespace ResinGuard
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ResinGuardPlugin : BaseUnityPlugin
    {
        internal const string ModName = "ResinGuard";
        internal const string ModVersion = "1.1.0";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private static string ConfigFileName = $"{ModGUID}.cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource ResinGuardLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion, ModRequired = false };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            DecayTime = config("1 - General", "Decay Time", 3600f, "The time it takes for resin to decay in seconds. (Realtime)");
            EnableVisualUpdates = Config.Bind("2 - Visual", "Enable Visual Updates", Toggle.On, "Enable or disable visual updates when resin or tar is applied.");
            EnableVisualUpdates.SettingChanged += (sender, args) => { ResinProtection.ForceUpdateVisuals(); };
            MaxResin = config("3 - Protection", "Max Resin", 10, "The maximum amount of resin a piece can have. WARNING, the higher the number the more health the piece will have. By default, it's balanced to double the health of the piece.");
            MaxResin.SettingChanged += (sender, args) => { ResinProtection.UpdateProtectionValues(); };
            RepairWhenProtectionApplied = config("3 - Protection", "Repair When Protection Applied", Toggle.On, "If on, the piece will be repaired when resin or tar is applied. This is balanced because technically it's costing more to repair it than if you used your hammer.");

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


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<float> DecayTime = null!;
        public static ConfigEntry<Toggle> EnableVisualUpdates = null!;
        public static ConfigEntry<int> MaxResin = null!;
        public static ConfigEntry<Toggle> RepairWhenProtectionApplied = null!;
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

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() => $"# Acceptable values: {string.Join(", ", UnityInput.Current.SupportedKeyCodes)}";
        }

        #endregion
    }

    public static class KeyboardExtensions
    {
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }
    }
}