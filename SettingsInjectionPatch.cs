using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace ScavSetLib
{
    [HarmonyPatch]
    public static class SettingsInjectionPatch
    {
        public static void InjectIntoList(List<Setting> list)
        {
            if (list == null) return;

            var nameField = typeof(Setting).GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var def in SettingsManager.Definitions)
            {
                bool alreadyExists = false;
                Setting? existingSetting = null;

                foreach (var s in list)
                {
                    if (s == null) continue;
                    if (nameField != null)
                    {
                        string? name = nameField.GetValue(s) as string;
                        if (name == def.Name)
                        {
                            alreadyExists = true;
                            existingSetting = s;
                            break;
                        }
                    }
                }

                if (!alreadyExists)
                {
                    // Create new setting instance and add it
                    Setting newSetting = def.CreateSettingInstance();
                    list.Add(newSetting);
                }
                else if (existingSetting != null)
                {
                    // Sync value in case it was changed externally
                    def.SyncValueFromSource(existingSetting);
                }
            }
        }

        [HarmonyPatch(typeof(Settings), "DefaultSettings")]
        [HarmonyPostfix]
        public static void DefaultSettings_Postfix(ref List<Setting> __result)
        {
            if (__result != null)
            {
                InjectIntoList(__result);
            }
        }

        [HarmonyPatch(typeof(Settings), "EnsureLoaded")]
        [HarmonyPostfix]
        public static void EnsureLoaded_Postfix()
        {
            var settingsField = typeof(Settings).GetField("settings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (settingsField != null)
            {
                var settingsList = settingsField.GetValue(null) as List<Setting>;
                if (settingsList != null)
                {
                    InjectIntoList(settingsList);
                }
            }
        }

        [HarmonyPatch(typeof(Locale), "GetString")]
        [HarmonyPrefix]
        public static bool GetString_Prefix(string str, int type, ref string __result)
        {
            if (str != null && SettingsManager.TryGetLocalization(str, out string localized))
            {
                __result = localized;
                return false; // Skip original game lookup entirely
            }
            return true; // Proceed with native localization lookup
        }

        [HarmonyPatch(typeof(Locale), "GetOther")]
        [HarmonyPrefix]
        public static bool GetOther_Prefix(string str, ref string __result)
        {
            if (str != null && SettingsManager.TryGetLocalization(str, out string localized))
            {
                __result = localized;
                return false; // Skip original game lookup entirely
            }
            return true; // Proceed with native localization lookup
        }
    }
}
