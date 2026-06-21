using System;
using System.Collections.Generic;
using System.Reflection;

namespace ScavSetLib
{
    /// <summary>
    /// Base class representing a custom registered setting definition.
    /// </summary>
    public abstract class SettingDefinition
    {
        public string Name { get; set; } = string.Empty;
        public Setting.SettingCategory Category { get; set; }
        public string CleanName { get; set; } = string.Empty;

        public abstract Setting CreateSettingInstance();
        public abstract void SyncValueFromSource(Setting setting);

        protected void PopulateBaseFields(Setting setting)
        {
            var nameField = typeof(Setting).GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var categoryField = typeof(Setting).GetField("category", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (nameField != null) nameField.SetValue(setting, Name);
            if (categoryField != null) categoryField.SetValue(setting, Category);
        }
    }

    /// <summary>
    /// Dropdown choice setting definition.
    /// </summary>
    public class DropdownSettingDefinition : SettingDefinition
    {
        public string[] Choices { get; set; } = Array.Empty<string>();
        public int DefaultValue { get; set; }
        public Action<int>? OnApply { get; set; }
        public Func<int>? ValueGetter { get; set; }
        public string[]? CleanChoiceNames { get; set; }

        public override Setting CreateSettingInstance()
        {
            var dropdown = new SettingDropdown
            {
                choices = Choices,
                value = ValueGetter != null ? ValueGetter() : DefaultValue
            };
            PopulateBaseFields(dropdown);

            var applyField = typeof(Setting).GetField("apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (applyField != null)
            {
                applyField.SetValue(dropdown, new Action(() => OnApply?.Invoke(dropdown.value)));
            }

            return dropdown;
        }

        public override void SyncValueFromSource(Setting setting)
        {
            if (setting is SettingDropdown dropdown && ValueGetter != null)
            {
                dropdown.value = ValueGetter();
            }
        }
    }

    /// <summary>
    /// Float slider setting definition.
    /// </summary>
    public class FloatSettingDefinition : SettingDefinition
    {
        public float Min { get; set; }
        public float Max { get; set; }
        public float DefaultValue { get; set; }
        public Func<float, string>? FormatValue { get; set; }
        public Action<float>? OnApply { get; set; }
        public Func<float>? ValueGetter { get; set; }

        public override Setting CreateSettingInstance()
        {
            var slider = new SettingFloat
            {
                min = Min,
                max = Max,
                value = ValueGetter != null ? ValueGetter() : DefaultValue,
                formatValue = FormatValue
            };
            PopulateBaseFields(slider);

            var applyField = typeof(Setting).GetField("apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (applyField != null)
            {
                applyField.SetValue(slider, new Action(() => OnApply?.Invoke(slider.value)));
            }

            return slider;
        }

        public override void SyncValueFromSource(Setting setting)
        {
            if (setting is SettingFloat slider && ValueGetter != null)
            {
                slider.value = ValueGetter();
            }
        }
    }

    /// <summary>
    /// Integer slider setting definition.
    /// </summary>
    public class IntSettingDefinition : SettingDefinition
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int DefaultValue { get; set; }
        public Action<int>? OnApply { get; set; }
        public Func<int>? ValueGetter { get; set; }

        public override Setting CreateSettingInstance()
        {
            var slider = new SettingInt
            {
                min = Min,
                max = Max,
                value = ValueGetter != null ? ValueGetter() : DefaultValue
            };
            PopulateBaseFields(slider);

            var applyField = typeof(Setting).GetField("apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (applyField != null)
            {
                applyField.SetValue(slider, new Action(() => OnApply?.Invoke(slider.value)));
            }

            return slider;
        }

        public override void SyncValueFromSource(Setting setting)
        {
            if (setting is SettingInt slider && ValueGetter != null)
            {
                slider.value = ValueGetter();
            }
        }
    }

    /// <summary>
    /// Boolean toggle setting definition.
    /// </summary>
    public class BoolSettingDefinition : SettingDefinition
    {
        public bool DefaultValue { get; set; }
        public Action<bool>? OnApply { get; set; }
        public Func<bool>? ValueGetter { get; set; }

        public override Setting CreateSettingInstance()
        {
            var toggle = new SettingBool
            {
                value = ValueGetter != null ? ValueGetter() : DefaultValue
            };
            PopulateBaseFields(toggle);

            var applyField = typeof(Setting).GetField("apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (applyField != null)
            {
                applyField.SetValue(toggle, new Action(() => OnApply?.Invoke(toggle.value)));
            }

            return toggle;
        }

        public override void SyncValueFromSource(Setting setting)
        {
            if (setting is SettingBool toggle && ValueGetter != null)
            {
                toggle.value = ValueGetter();
            }
        }
    }

    /// <summary>
    /// The public facing registration API for custom native settings.
    /// </summary>
    public static class SettingsManager
    {
        private static readonly List<SettingDefinition> _definitions = new List<SettingDefinition>();
        private static readonly Dictionary<string, string> _localizationMap = new Dictionary<string, string>(StringComparer.Ordinal);

        public static IReadOnlyList<SettingDefinition> Definitions => _definitions;

        /// <summary>
        /// Register a custom dropdown choice setting into the native settings menus.
        /// </summary>
        public static void RegisterDropdown(
            string name,
            Setting.SettingCategory category,
            string[] choices,
            int defaultValue,
            Action<int> onApply,
            Func<int> valueGetter,
            string cleanName,
            string[]? cleanChoiceNames = null,
            string description = "")
        {
            var def = new DropdownSettingDefinition
            {
                Name = name,
                Category = category,
                Choices = choices,
                DefaultValue = defaultValue,
                OnApply = onApply,
                ValueGetter = valueGetter,
                CleanName = cleanName,
                CleanChoiceNames = cleanChoiceNames
            };
            _definitions.Add(def);

            // Populate the clean string mappings
            _localizationMap["gameset" + name] = cleanName;
            _localizationMap["gameset" + name + "dsc"] = description;
            if (choices != null)
            {
                for (int i = 0; i < choices.Length; i++)
                {
                    string rawChoice = choices[i];
                    string cleanChoice = (cleanChoiceNames != null && i < cleanChoiceNames.Length) ? cleanChoiceNames[i] : rawChoice;
                    _localizationMap["gameset" + name + rawChoice] = cleanChoice;
                }
            }
        }

        /// <summary>
        /// Register a custom float slider setting into the native settings menus.
        /// </summary>
        public static void RegisterFloat(
            string name,
            Setting.SettingCategory category,
            float min,
            float max,
            float defaultValue,
            Action<float> onApply,
            Func<float> valueGetter,
            string cleanName,
            Func<float, string>? formatValue = null,
            string description = "")
        {
            var def = new FloatSettingDefinition
            {
                Name = name,
                Category = category,
                Min = min,
                Max = max,
                DefaultValue = defaultValue,
                OnApply = onApply,
                ValueGetter = valueGetter,
                CleanName = cleanName,
                FormatValue = formatValue
            };
            _definitions.Add(def);

            _localizationMap["gameset" + name] = cleanName;
            _localizationMap["gameset" + name + "dsc"] = description;
        }

        /// <summary>
        /// Register a custom int slider setting into the native settings menus.
        /// </summary>
        public static void RegisterInt(
            string name,
            Setting.SettingCategory category,
            int min,
            int max,
            int defaultValue,
            Action<int> onApply,
            Func<int> valueGetter,
            string cleanName,
            string description = "")
        {
            var def = new IntSettingDefinition
            {
                Name = name,
                Category = category,
                Min = min,
                Max = max,
                DefaultValue = defaultValue,
                OnApply = onApply,
                ValueGetter = valueGetter,
                CleanName = cleanName
            };
            _definitions.Add(def);

            _localizationMap["gameset" + name] = cleanName;
            _localizationMap["gameset" + name + "dsc"] = description;
        }

        /// <summary>
        /// Register a custom boolean toggle setting into the native settings menus.
        /// </summary>
        public static void RegisterBool(
            string name,
            Setting.SettingCategory category,
            bool defaultValue,
            Action<bool> onApply,
            Func<bool> valueGetter,
            string cleanName,
            string description = "")
        {
            var def = new BoolSettingDefinition
            {
                Name = name,
                Category = category,
                DefaultValue = defaultValue,
                OnApply = onApply,
                ValueGetter = valueGetter,
                CleanName = cleanName
            };
            _definitions.Add(def);

            _localizationMap["gameset" + name] = cleanName;
            _localizationMap["gameset" + name + "dsc"] = description;
        }

        /// <summary>
        /// Attempts to get a registered translation for the given localized settings key.
        /// </summary>
        public static bool TryGetLocalization(string key, out string localizedValue)
        {
            return _localizationMap.TryGetValue(key, out localizedValue);
        }
    }
}
