using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
#if MELONLOADER
using MelonLoader;
#elif BEPINEX
using BepInEx.Configuration;
#endif

namespace InFalsusChartExt
{
    /// <summary>
    /// Loader-neutral configuration facade. Sections and key names stay
    /// identical to the existing MelonPreferences layout.
    /// </summary>
    internal sealed class PluginConfig
    {
#if MELONLOADER
        internal PluginConfig()
        {
        }

        internal PluginConfigCategory Category(
            string section,
            string displayName)
            => new PluginConfigCategory(
                MelonPreferences.CreateCategory(section, displayName));
#elif BEPINEX
        private readonly ConfigFile _file;
        private readonly Dictionary<string, string> _legacyValues;

        internal PluginConfig(ConfigFile file, string legacyPath)
        {
            _file = file;
            _legacyValues =
                !File.Exists(file.ConfigFilePath) ||
                file.Count == 0
                    ? ReadLegacyValues(legacyPath)
                    : new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);
        }

        internal PluginConfigCategory Category(
            string section,
            string displayName)
            => new PluginConfigCategory(
                _file, section, _legacyValues);

        private static Dictionary<string, string> ReadLegacyValues(
            string path)
        {
            var result = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
                return result;

            string section = string.Empty;
            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 ||
                    line.StartsWith("#", StringComparison.Ordinal) ||
                    line.StartsWith(";", StringComparison.Ordinal))
                    continue;
                if (line.StartsWith("[", StringComparison.Ordinal) &&
                    line.EndsWith("]", StringComparison.Ordinal))
                {
                    section = line.Substring(
                        1, line.Length - 2).Trim();
                    continue;
                }

                int equals = line.IndexOf('=');
                if (equals <= 0) continue;
                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                if (key.Length == 0 || value.Length == 0) continue;
                if (value.StartsWith("{", StringComparison.Ordinal))
                    continue;
                if (value.Length >= 2 &&
                    value[0] == '"' &&
                    value[value.Length - 1] == '"')
                {
                    value = value.Substring(1, value.Length - 2);
                }
                result[section + "\n" + key] = value;
            }
            return result;
        }
#endif
    }

    internal sealed class PluginConfigCategory
    {
#if MELONLOADER
        private readonly MelonPreferences_Category _category;

        internal PluginConfigCategory(
            MelonPreferences_Category category)
        {
            _category = category;
        }

        internal PluginConfigEntry<T> Bind<T>(
            string key,
            T defaultValue,
            string displayName,
            string description = null)
        {
            MelonPreferences_Entry<T> entry =
                _category.CreateEntry(
                    key,
                    defaultValue,
                    displayName ?? key,
                    description ?? string.Empty);
            return new PluginConfigEntry<T>(
                () => entry.Value,
                value => entry.Value = value);
        }

        internal void Save()
        {
            _category?.SaveToFile();
        }
#elif BEPINEX
        private readonly ConfigFile _file;
        private readonly string _section;
        private readonly Dictionary<string, string> _legacyValues;

        internal PluginConfigCategory(
            ConfigFile file,
            string section,
            Dictionary<string, string> legacyValues)
        {
            _file = file;
            _section = section;
            _legacyValues = legacyValues;
        }

        internal PluginConfigEntry<T> Bind<T>(
            string key,
            T defaultValue,
            string displayName,
            string description = null)
        {
            T effective = defaultValue;
            if (_legacyValues is not null &&
                _legacyValues.TryGetValue(
                    _section + "\n" + key,
                    out string raw))
            {
                effective = ConvertLegacy(raw, defaultValue);
            }

            ConfigEntry<T> entry = _file.Bind(
                _section,
                key,
                effective,
                description ?? displayName ?? string.Empty);
            return new PluginConfigEntry<T>(
                () => entry.Value,
                value => entry.Value = value);
        }

        internal void Save()
        {
            _file?.Save();
        }

        private static T ConvertLegacy<T>(
            string raw,
            T defaultValue)
        {
            try
            {
                object value;
                if (typeof(T) == typeof(bool))
                    value = bool.Parse(raw);
                else if (typeof(T) == typeof(int))
                    value = int.Parse(
                        raw,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture);
                else if (typeof(T) == typeof(float))
                    value = float.Parse(
                        raw,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture);
                else if (typeof(T) == typeof(string))
                    value = raw;
                else
                    return defaultValue;
                return (T)value;
            }
            catch
            {
                return defaultValue;
            }
        }
#endif
    }

    internal sealed class PluginConfigEntry<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;

        internal PluginConfigEntry(
            Func<T> getter,
            Action<T> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        internal T Value
        {
            get => _getter();
            set => _setter(value);
        }
    }
}
