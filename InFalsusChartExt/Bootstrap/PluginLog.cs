using System;
#if MELONLOADER
using MelonLoader;
#elif BEPINEX
using BepInEx.Logging;
#endif

namespace InFalsusChartExt
{
    /// <summary>
    /// Keep the existing Msg/Warning/Error call sites while allowing either
    /// MelonLoader or BepInEx to own the underlying logger.
    /// </summary>
    internal sealed class PluginLog
    {
#if MELONLOADER
        private readonly MelonLogger.Instance _logger;

        internal PluginLog(MelonLogger.Instance logger)
        {
            _logger = logger;
        }
#elif BEPINEX
        private readonly ManualLogSource _logger;

        internal PluginLog(ManualLogSource logger)
        {
            _logger = logger;
        }
#endif

        internal void Msg(string message)
        {
            if (_logger is null || message is null) return;
#if MELONLOADER
            _logger.Msg(message);
#elif BEPINEX
            _logger.LogMessage(message);
#endif
        }

        internal void Warning(string message)
        {
            if (_logger is null || message is null) return;
#if MELONLOADER
            _logger.Warning(message);
#elif BEPINEX
            _logger.LogWarning(message);
#endif
        }

        internal void Error(string message)
        {
            if (_logger is null || message is null) return;
#if MELONLOADER
            _logger.Error(message);
#elif BEPINEX
            _logger.LogError(message);
#endif
        }
    }
}
