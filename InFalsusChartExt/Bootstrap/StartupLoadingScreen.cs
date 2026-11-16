using System;
using System.Threading;
using InFalsusChartExt.CustomSong.Config;
using UnityEngine;
using UnityEngine.UI;
#if MELONLOADER
using Il2Cppifapp.Game;
#elif BEPINEX
using ifapp.Game;
#endif

namespace InFalsusChartExt
{
    /// <summary>
    /// Full-screen startup cover. It is drawn before the original UI is
    /// visible, reports migration/library preparation in English, and blocks
    /// IMGUI mouse events until the main menu is reached.
    /// </summary>
    internal static class StartupLoadingScreen
    {
        private const float MinimumSeconds = 0.75f;
        private const float SafetySeconds = 60f;

        private static readonly object Gate = new object();
        private static volatile string _status =
            "INITIALIZING...";
        private static volatile bool _migrationDone;
        private static volatile bool _libraryReady;
        private static volatile bool _jacketsReady;
        private static volatile bool _mainMenuSeen;
        private static volatile bool _nonStartupSceneSeen;
        private static volatile bool _hidden;
        private static bool _started;
        private static float _firstGuiAt = -1f;
        private static GUIStyle _titleStyle;
        private static GUIStyle _statusStyle;
        private static GUIStyle _hintStyle;
        private static GUIStyle _coverStyle;
        private static bool _coverFailureLogged;
        private static GameObject _canvasRoot;
        private static Text _canvasStatus;
        private static bool _canvasFailed;
        private static float _visibleSince = -1f;

        internal static bool Visible => !_hidden;

        internal static void Begin()
        {
            lock (Gate)
            {
                if (_started)
                    return;
                _started = true;
            }

            SetStatus("PREPARING CHART STORAGE...");
            var thread = new Thread(Migrate)
            {
                IsBackground = true,
                Name = "IFCE startup migration",
            };
            thread.Start();
        }

        internal static void SetStatus(string status)
        {
            if (!string.IsNullOrWhiteSpace(status))
                _status = status;
        }

        internal static void MarkLibraryReady()
        {
            _libraryReady = true;
            if (!_jacketsReady)
                SetStatus("LOADING JACKETS...");
        }

        internal static void MarkJacketsReady()
        {
            _jacketsReady = true;
            SetStatus("READY");
        }

        internal static void OnSceneLoaded(string sceneName)
        {
            try
            {
                ChartExtMod.Log.Msg(
                    "[Startup] scene loaded: " +
                    (sceneName ?? "<null>"));
            }
            catch
            {
            }
            if (IsMainMenu(sceneName))
                _mainMenuSeen = true;
            if (!string.Equals(
                    sceneName,
                    "StartUpScene",
                    StringComparison.OrdinalIgnoreCase))
                _nonStartupSceneSeen = true;
        }

        internal static void Tick()
        {
            if (_hidden)
            {
                DestroyCanvas();
                return;
            }
            EnsureCanvas();
            float now = Time.realtimeSinceStartup;
            if (_canvasRoot is not null)
            {
                if (_visibleSince < 0f)
                    _visibleSince = now;
                if (_canvasStatus is not null)
                    _canvasStatus.text = _status;
            }
            float since = _canvasRoot is not null
                ? _visibleSince
                : _firstGuiAt;
            if (since < 0f)
                return;
            float elapsed = now - since;
            if (elapsed >= SafetySeconds)
            {
                _hidden = true;
                try
                {
                    ChartExtMod.Log.Warning(
                        "[Startup] loading cover timed out; " +
                        "continuing without it");
                }
                catch
                {
                }
                return;
            }
            if (_mainMenuSeen &&
                _migrationDone &&
                _libraryReady &&
                _jacketsReady &&
                !IsStartUpSceneActive() &&
                elapsed >= MinimumSeconds)
            {
                _hidden = true;
            }
            else if (_nonStartupSceneSeen &&
                     _migrationDone &&
                     _libraryReady &&
                     _jacketsReady &&
                     !IsStartUpSceneActive() &&
                     elapsed >= MinimumSeconds)
            {
                _hidden = true;
            }
        }

        internal static void OnGUI()
        {
            if (_hidden)
                return;
            if (_canvasRoot is not null)
            {
                ConsumeKeyEvents();
                return;
            }
            if (_firstGuiAt < 0f)
                _firstGuiAt = Time.realtimeSinceStartup;
            int width = Screen.width;
            int height = Screen.height;
            if (width <= 0 || height <= 0)
                return;

            int oldDepth = GUI.depth;
            Color oldColor = GUI.color;
            GUI.depth = -3000;
            try
            {
                var full = new Rect(0f, 0f, width, height);
                GUI.color = Color.black;
                try
                {
                    GUI.Box(
                        full, GUIContent.none, _coverStyle);
                }
                catch (Exception exception)
                {
                    if (!_coverFailureLogged)
                    {
                        _coverFailureLogged = true;
                        try
                        {
                            ChartExtMod.Log.Error(
                                "[Startup] cover draw failed: " +
                                exception);
                        }
                        catch
                        {
                        }
                    }
                    try
                    {
                        GUI.Box(full, GUIContent.none);
                    }
                    catch
                    {
                    }
                }
                GUI.color = oldColor;

                // A full-screen control consumes IMGUI mouse events while
                // the cover is up.
                try
                {
                    GUI.Button(
                        full, GUIContent.none, GUIStyle.none);
                }
                catch
                {
                }

                ConsumeKeyEvents();

                EnsureStyles();
                float center = height * 0.5f;
                GUI.Label(
                    new Rect(
                        0f,
                        center - 82f,
                        width,
                        44f),
                    "INFALSUS EXTENSION",
                    _titleStyle);
                GUI.Label(
                    new Rect(
                        0f,
                        center - 24f,
                        width,
                        36f),
                    _status ?? "LOADING...",
                    _statusStyle);
                GUI.Label(
                    new Rect(
                        0f,
                        center + 34f,
                        width,
                        24f),
                    "PLEASE WAIT",
                    _hintStyle);
            }
            finally
            {
                GUI.color = oldColor;
                GUI.depth = oldDepth;
            }
        }

        private static void Migrate()
        {
            try
            {
                LegacyConfigMigrator.EnsureMigrated(
                    out string report,
                    SetStatus);
                if (!string.IsNullOrWhiteSpace(report))
                {
                    ChartExtMod.Log.Msg(
                        "[Startup] migration report:\n" +
                        report);
                }
            }
            catch (Exception exception)
            {
                SetStatus("MIGRATION FAILED - SEE LOG");
                try
                {
                    ChartExtMod.Log.Error(
                        "[Startup] migration failed: " +
                        exception);
                }
                catch
                {
                }
            }
            finally
            {
                _migrationDone = true;
                if (!string.Equals(
                        _status,
                        "MIGRATION FAILED - SEE LOG",
                        StringComparison.Ordinal))
                {
                    SetStatus("CHECKING CUSTOM LIBRARY...");
                }
            }
        }

        private static void EnsureCanvas()
        {
            if (_canvasFailed || _canvasRoot is not null)
                return;
            try
            {
                Canvas canvas = RuntimeCanvasUi.CreateCanvas(
                    "IFCE_StartupCover", short.MaxValue);
                _canvasRoot = canvas.gameObject;
                RuntimeCanvasUi.CreateFullScreenPanel(
                    canvas.transform, Color.black);
                RuntimeCanvasUi.CreateCenteredText(
                    canvas.transform,
                    "INFALSUS EXTENSION",
                    34,
                    new Vector2(1400f, 56f),
                    104f);
                _canvasStatus = RuntimeCanvasUi.CreateCenteredText(
                    canvas.transform,
                    _status,
                    20,
                    new Vector2(1400f, 40f),
                    34f);
                RuntimeCanvasUi.CreateCenteredText(
                    canvas.transform,
                    "PLEASE WAIT",
                    14,
                    new Vector2(1400f, 28f),
                    -34f);
                _visibleSince = Time.realtimeSinceStartup;
                ChartExtMod.Log.Msg(
                    "[Startup] canvas cover created");
            }
            catch (Exception exception)
            {
                _canvasFailed = true;
                try
                {
                    ChartExtMod.Log.Error(
                        "[Startup] canvas cover failed; using IMGUI: " +
                        exception);
                }
                catch
                {
                }
            }
        }

        private static void DestroyCanvas()
        {
            if (_canvasRoot is null)
                return;
            UnityEngine.Object.Destroy(_canvasRoot);
            _canvasRoot = null;
            _canvasStatus = null;
        }

        private static void ConsumeKeyEvents()
        {
            Event current = Event.current;
            if (current is not null &&
                (current.type == EventType.KeyDown ||
                 current.type == EventType.KeyUp ||
                 current.type == EventType.ScrollWheel))
                current.Use();
        }

        private static bool IsMainMenu(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;
            return sceneName.IndexOf(
                       "Title",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sceneName.IndexOf(
                       "MainMenu",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsStartUpSceneActive()
        {
            try
            {
                var scenes =
                    UnityEngine.Resources
                        .FindObjectsOfTypeAll<StartUpScene>();
                if (scenes is null)
                    return false;
                for (int i = 0; i < scenes.Length; i++)
                {
                    StartUpScene scene = scenes[i];
                    if (scene is null)
                        continue;
                    GameObject target = scene.gameObject;
                    if (target is not null &&
                        target.activeInHierarchy)
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private static void EnsureStyles()
        {
            if (_coverStyle is null)
            {
                _coverStyle = new GUIStyle
                {
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                };
                _coverStyle.normal.background =
                    Texture2D.whiteTexture;
            }
            if (_titleStyle is null)
            {
                _titleStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 30,
                    fontStyle = FontStyle.Bold,
                };
                _titleStyle.normal.textColor =
                    new Color(0.95f, 0.95f, 0.95f, 1f);
            }
            if (_statusStyle is null)
            {
                _statusStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                    fontStyle = FontStyle.Normal,
                };
                _statusStyle.normal.textColor =
                    new Color(0.82f, 0.82f, 0.82f, 1f);
            }
            if (_hintStyle is null)
            {
                _hintStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                };
                _hintStyle.normal.textColor =
                    new Color(0.55f, 0.55f, 0.55f, 1f);
            }
        }
    }
}
