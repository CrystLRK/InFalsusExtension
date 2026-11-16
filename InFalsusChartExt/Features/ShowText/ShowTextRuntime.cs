using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace InFalsusChartExt
{
    /// <summary>
    /// showtext：按谱面时间在屏幕上显示文本。
    ///
    /// 文本与样式存放在与谱面同级的 <c>&lt;谱名&gt;.text.json</c>（也兼容
    /// <c>&lt;谱名&gt;.spc.text.json</c>）：
    /// <code>
    /// { "texts": [ { "id": 0, "text": "hello", "x": 0.5, "y": 0.8, "size": 6,
    ///               "duration": 2000, "fadeIn": 300, "fadeOut": 300,
    ///               "color": "#7FD4FF" }, ... ] }
    /// </code>
    /// x/y 为 0..1 归一化屏幕坐标（y 从上往下）。语句里的 time 决定出现时间。
    ///
    /// 第一版用 IMGUI 绘制 + OS 动态字体（缺字形自动回退）；游戏内 TMP 字体克隆
    /// 作为后续升级项，不影响语句/文件格式。
    /// </summary>
    internal static class ShowTextRuntime
    {
        private sealed class Style
        {
            public string Text;
            public double Duration = 2000;
            public double FadeIn = 300;
            public double FadeOut = 300;
            public float X = 0.5f;
            public float Y = 0.5f;
            public int Size = 6;
            public Color Color = Color.white;
            public Color Color2 = Color.white;
            public float Scale = 1f;
            public bool Shadow = true;
            public Color ShadowColor = new Color(0f, 0f, 0f, 1f);
            public float ShadowAlpha = 0.55f;
            public float ShadowScaleX = 1.8f;
            public float ShadowScaleY = 2.8f;
            public float ShadowOffsetY = 0f;
            public float Spacing;
            public float Amp;
            public float Hz;
            public float Rot;
            public float Stagger;
            public float Pop = 1f;
        }

        private static readonly Dictionary<long, Style> Styles = new Dictionary<long, Style>();
        private static readonly List<(double Time, int Order, long Id)>
            Sorted = new List<(double, int, long)>();
        private static string _loadedFor;
        private static string _ignoredFor;
        private static Font _font;
        private static bool _fontFailed;
        private static GUIStyle _style;
        private static int _logs;
        private static int _errors;
        private static int _drawErrors;
        private static readonly HashSet<long> DrawLogged =
            new HashSet<long>();
        private static string _failedFor;
        private static string _fatalFor;
        private static string _fatalTitle;
        private static string _fatalDetail;

        private sealed class FastEntry
        {
            public GameObject Go;
            public Il2CppFastText.FastText Text;
            public RectTransform Rect;
            public string LastText;
            public float LastSize;
            public Color LastColor;
            public Color LastColor2;
            public GameObject Shadow;
            public MeshRenderer ShadowRenderer;
            public Material ShadowMaterial;
            public Vector3 BaseScale;
            public double StartMs;
            public double DurationMs;
        }

        private static readonly Dictionary<long, FastEntry> Fast =
            new Dictionary<long, FastEntry>();
        private static readonly List<GameObject> AllFastObjects = new List<GameObject>();
        private static bool _fastTried;
        private static bool _fastFailed;
        private static bool _fastDisabled;
        private static int _zeroBoundsCount;
        private static Il2CppFastText.FastText _fastTemplate;
        private static Il2CppFastText.Data.FastTextFontFamily _customFamily;
        private static bool _fontsTried;
        private static Transform _fastRoot;
        private static Canvas _fastCanvas;
        private static Camera _fastCamera;
        private static float _fastDepth = 1f;
        private static Mesh _shadowMesh;
        private static Texture2D _shadowTexture;
        private static Material _shadowSourceMaterial;
        private static readonly HashSet<long> ActiveFastIds =
            new HashSet<long>();
        private static readonly List<long> ActiveFastOrder =
            new List<long>();
        private static int _sortedCursor;
        private static double _lastDrawNow = double.NaN;

        internal static void EnsureLoaded()
        {
            try
            {
                string key = ChartExtState.LastChartName;
                if (ChartExtState.ShowTextEntries.Count == 0)
                {
                    // A chart without showtext must also tear down the previous
                    // chart's cached styles and live FastText objects.
                    if (_loadedFor != null || Styles.Count > 0 || Sorted.Count > 0 ||
                        Fast.Count > 0)
                        ResetForChartSwitch();
                    return;
                }
                if (!string.IsNullOrEmpty(_fatalDetail))
                {
                    if (!string.Equals(
                            _fatalFor,
                            ChartExtState.LastChartName,
                            StringComparison.Ordinal))
                        ResetForChartSwitch();
                    return;
                }
                if (string.Equals(_loadedFor, key, StringComparison.Ordinal))
                {
                    if (Styles.Count > 0) return;
                    if (string.Equals(_failedFor, key, StringComparison.Ordinal) ||
                        string.Equals(_ignoredFor, key, StringComparison.Ordinal))
                        return;
                }
                ResetForChartSwitch();
                _loadedFor = key;

                InFalsusChartExt.CustomSong.Discovery.CustomSongPlan song =
                    null;
                InFalsusChartExt.CustomSong.Discovery.CustomChartPlan
                    selectedChart = null;
                try
                {
                    InFalsusChartExt.CustomSong.Lifecycle
                        .CustomSongRuntime.TryGetChartByAnyKey(
                            key,
                            out InFalsusChartExt.CustomSong.Discovery
                                .CustomChartPlan chart);
                    selectedChart = chart;
                    song = chart?.Song;
                }
                catch { }

                var names = new List<string>();
                if (song != null &&
                    selectedChart is not null)
                {
                    string customStem = Path.GetFileNameWithoutExtension(
                        selectedChart.SourcePath);
                    names.Add(customStem + ".text.json");
                    names.Add(customStem + ".spc.text.json");
                }
                string stem = Path.GetFileNameWithoutExtension(key ?? "");
                string fallback = stem + ".text.json";
                if (!names.Contains(fallback, StringComparer.OrdinalIgnoreCase))
                    names.Add(fallback);
                string keyFallback = (key ?? "") + ".text.json";
                if (!names.Contains(keyFallback, StringComparer.OrdinalIgnoreCase))
                    names.Add(keyFallback);

                string found = song?.Directory == null
                    ? null
                    : names
                        .Select(name => Path.Combine(song.Directory, name))
                        .FirstOrDefault(File.Exists);
                if (found == null)
                {
                    MarkIgnored(
                        key,
                        "no text sidecar in '" +
                        (song?.Directory ?? "(unknown)") + "'");
                    return;
                }

                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(found));
                JsonElement arr = doc.RootElement;
                if (arr.ValueKind == JsonValueKind.Object && arr.TryGetProperty("texts", out JsonElement texts))
                    arr = texts;
                if (arr.ValueKind != JsonValueKind.Array)
                {
                    MarkIgnored(key, "sidecar root has no texts array");
                    return;
                }
                if (arr.GetArrayLength() == 0)
                {
                    MarkIgnored(key, "sidecar texts array is empty");
                    return;
                }

                foreach (JsonElement el in arr.EnumerateArray())
                {
                    long id = el.TryGetProperty("id", out JsonElement eid) && eid.TryGetInt64(out long idv) ? idv : -1;
                    if (id < 0) continue;
                    var st = new Style();
                    if (el.TryGetProperty("text", out JsonElement et)) st.Text = et.GetString();
                    if (el.TryGetProperty("duration", out JsonElement ed)) st.Duration = ed.GetDouble();
                    if (el.TryGetProperty("fadeIn", out JsonElement ef)) st.FadeIn = ef.GetDouble();
                    if (el.TryGetProperty("fadeOut", out JsonElement efo)) st.FadeOut = efo.GetDouble();
                    if (el.TryGetProperty("x", out JsonElement ex)) st.X = ex.GetSingle();
                    if (el.TryGetProperty("y", out JsonElement ey)) st.Y = ey.GetSingle();
                    if (el.TryGetProperty("size", out JsonElement es)) st.Size = es.GetInt32();
                    if (el.TryGetProperty("color", out JsonElement ec) && ec.ValueKind == JsonValueKind.String)
                        st.Color = ParseColor(ec.GetString());
                    if (!el.TryGetProperty("fill2", out _))
                        st.Color2 = st.Color;
                    if (el.TryGetProperty("scale", out JsonElement esc)) st.Scale = esc.GetSingle();
                    if (el.TryGetProperty("fill2", out JsonElement ef2) && ef2.ValueKind == JsonValueKind.String)
                        st.Color2 = ParseColor(ef2.GetString());
                    if (el.TryGetProperty("spacing", out JsonElement esp)) st.Spacing = esp.GetSingle();
                    if (el.TryGetProperty("amp", out JsonElement eam)) st.Amp = eam.GetSingle();
                    if (el.TryGetProperty("hz", out JsonElement ehz)) st.Hz = ehz.GetSingle();
                    if (el.TryGetProperty("rot", out JsonElement ero)) st.Rot = ero.GetSingle();
                    if (el.TryGetProperty("stagger", out JsonElement est)) st.Stagger = est.GetSingle();
                    if (el.TryGetProperty("pop", out JsonElement epop)) st.Pop = epop.GetSingle();
                    if (el.TryGetProperty("shadowColor", out JsonElement esc2) && esc2.ValueKind == JsonValueKind.String)
                        st.ShadowColor = ParseColor(esc2.GetString());
                    if (el.TryGetProperty("shadow", out JsonElement esh)) st.Shadow = esh.GetBoolean();
                    if (el.TryGetProperty("shadowAlpha", out JsonElement esa)) st.ShadowAlpha = esa.GetSingle();
                    if (el.TryGetProperty("shadowScaleX", out JsonElement essx)) st.ShadowScaleX = essx.GetSingle();
                    if (el.TryGetProperty("shadowScaleY", out JsonElement essy)) st.ShadowScaleY = essy.GetSingle();
                    if (el.TryGetProperty("shadowOffsetY", out JsonElement esoy)) st.ShadowOffsetY = esoy.GetSingle();
                    Styles[id] = st;
                }

                var missingIds = new List<long>();
                for (int i = 0; i < ChartExtState.ShowTextEntries.Count; i++)
                {
                    ChartExtState.ShowTextEntry entry = ChartExtState.ShowTextEntries[i];
                    if (entry.Size > 0 && Styles.TryGetValue(entry.Id, out Style overrideStyle))
                        overrideStyle.Size = entry.Size;
                    else if (!Styles.ContainsKey(entry.Id))
                        missingIds.Add(entry.Id);
                    Sorted.Add((
                        entry.TimeMs,
                        entry.SourceOrder,
                        entry.Id));
                }
                if (missingIds.Count > 0)
                {
                    _failedFor = key;
                    Fail(
                        "showtext 文本 id 缺失",
                        $"谱面：{key}\n文本文件：{found}\n缺失 id：{string.Join(", ", missingIds)}");
                    return;
                }
                Sorted.Sort((a, b) =>
                {
                    int time = a.Time.CompareTo(b.Time);
                    return time != 0
                        ? time
                        : a.Order.CompareTo(b.Order);
                });
                ResetFastSchedule();
                ChartExtMod.Log.Msg($"[ChartExt] showtext: loaded {Styles.Count} texts from '{Path.GetFileName(found)}'");
            }
            catch (Exception e)
            {
                _failedFor = ChartExtState.LastChartName;
                Fail(
                    "showtext 配置无效",
                    $"谱面：{ChartExtState.LastChartName}\n错误：{e.Message}");
            }
        }

        /// <summary>
        /// Drops every chart-scoped style and visual. Called synchronously when
        /// ChartExtState starts a new chart, and again defensively when a chart
        /// without showtext is encountered.
        /// </summary>
        internal static void ResetForChartSwitch()
        {
            _loadedFor = null;
            _ignoredFor = null;
            _failedFor = null;
            _fatalFor = null;
            _fatalTitle = null;
            _fatalDetail = null;
            Styles.Clear();
            Sorted.Clear();
            ResetFastSchedule();
            ResetFast();
            _fastDisabled = false;
            _zeroBoundsCount = 0;
        }

        internal static void ReportFatal(string title, string detail)
        {
            ReportFatal(title, detail, null);
        }

        internal static void ReportFatal(
            string title,
            string detail,
            string chartKey)
        {
            Fail(title, detail, chartKey);
        }

        internal static void DisposeResources()
        {
            ResetForChartSwitch();
            try
            {
                if (_shadowMesh != null)
                    UnityEngine.Object.Destroy(_shadowMesh);
            }
            catch { }
            try
            {
                if (_shadowTexture != null)
                    UnityEngine.Object.Destroy(_shadowTexture);
            }
            catch { }
            try
            {
                if (_shadowSourceMaterial != null)
                    UnityEngine.Object.Destroy(_shadowSourceMaterial);
            }
            catch { }
            _shadowMesh = null;
            _shadowTexture = null;
            _shadowSourceMaterial = null;
        }

        internal static void OnGUI()
        {
            try
            {
                if (!string.IsNullOrEmpty(_fatalDetail) &&
                    string.Equals(
                        _fatalFor,
                        ChartExtState.LastChartName,
                        StringComparison.Ordinal))
                {
                    DrawFatalError();
                    return;
                }
                if (ChartExtState.ShowTextEntries.Count == 0)
                {
                    if (_loadedFor != null || Styles.Count > 0 || Sorted.Count > 0 ||
                        Fast.Count > 0)
                        ResetForChartSwitch();
                    return;
                }
                if (!string.Equals(_loadedFor, ChartExtState.LastChartName,
                        StringComparison.Ordinal))
                {
                    ResetForChartSwitch();
                    return;
                }
                if (Sorted.Count == 0 || Styles.Count == 0) return;
                double nowMs = NowMs();
                if (double.IsNaN(nowMs)) return;
                if (!_fastDisabled && EnsureFastRenderer())
                {
                    DrawFast(nowMs);
                    return;
                }
                if (_font == null && !_fontFailed)
                {
                    // 逐级回退：单名字体（前一版的字符串数组重载在运行期不存在）→ 系统 Arial → 内置字体。
                    try
                    {
                        var names =
                            new Il2CppInterop.Runtime.InteropTypes.Arrays
                                .Il2CppStringArray(new[]
                            {
                                "Microsoft YaHei",
                                "Meiryo",
                                "Arial",
                            });
                        _font = Font.CreateDynamicFontFromOSFont(names, 64);
                    }
                    catch { }
                    try { _font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 48); } catch { }
                    if (_font == null) { try { _font = Font.CreateDynamicFontFromOSFont("Meiryo", 48); } catch { } }
                    if (_font == null) { try { _font = Font.CreateDynamicFontFromOSFont("Arial", 48); } catch { } }
                    if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                    if (_font == null) { try { _font = GUI.skin != null ? GUI.skin.label.font : null; } catch { } }
                    if (_font == null)
                    {
                        _fontFailed = true;
                        if (_errors < 3) { _errors++; ChartExtMod.Log.Warning("[ChartExt] showtext: no usable font; text disabled"); }
                    }
                }
                if (_font == null) return;
                if (_style == null)
                {
                    _style = new GUIStyle
                    {
                        alignment = TextAnchor.MiddleCenter,
                        richText = false,
                    };
                    _style.font = _font;
                }

                int oldDepth = GUI.depth;
                GUI.depth = -1000;
                for (int i = 0; i < Sorted.Count; i++)
                {
                    (double time, int sourceOrder, long id) =
                        Sorted[i];
                    _ = sourceOrder;
                    if (!Styles.TryGetValue(id, out Style st)) continue;
                    double duration = st.Duration <= 0 ? 1e12 : st.Duration;
                    double rel = nowMs - time;
                    if (rel < 0 || rel > duration) continue;
                    float alpha = 1f;
                    if (st.FadeIn > 0 && rel < st.FadeIn) alpha = (float)(rel / st.FadeIn);
                    if (st.FadeOut > 0 && duration - rel < st.FadeOut)
                        alpha = Math.Min(alpha, (float)((duration - rel) / st.FadeOut));
                    alpha = Math.Clamp(alpha, 0f, 1f);
                    DrawImGuiLine(st, rel, duration, alpha, nowMs);
                }
                GUI.depth = oldDepth;
            }
            catch (Exception e)
            {
                // 绘制异常只在最初几次记录，避免逐帧刷日志（历史实测每帧数百条）。
                if (_drawErrors < 3) { _drawErrors++; ChartExtMod.Log.Error("[ChartExt] showtext draw failed: " + e); }
            }
        }

        private static void DrawImGuiLine(
            Style st,
            double rel,
            double duration,
            float alpha,
            double nowMs)
        {
            string text = st.Text ?? string.Empty;
            if (text.Length == 0) return;
            bool perGlyph = st.Stagger > 0f ||
                            st.Amp > 0.001f ||
                            Math.Abs(st.Rot) > 0.001f ||
                            st.Pop > 1.001f;
            if (!perGlyph)
            {
                Color color = st.Color;
                color.a *= alpha;
                _style.fontSize = st.Size;
                _style.normal.textColor = color;
                float width = Screen.width * 0.9f;
                GUI.Label(
                    new Rect(
                        st.X * Screen.width - width * 0.5f,
                        st.Y * Screen.height,
                        width,
                        st.Size * 1.8f),
                    text,
                    _style);
                return;
            }

            var glyphs = new List<string>(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length &&
                    char.IsLowSurrogate(text[i + 1]))
                {
                    glyphs.Add(text.Substring(i, 2));
                    i++;
                }
                else
                {
                    glyphs.Add(c.ToString());
                }
            }
            var advances = new float[glyphs.Count];
            for (int i = 0; i < glyphs.Count; i++)
                advances[i] = GlyphAdvance(glyphs[i], st.Size);

            float maxWidth = Screen.width * 0.9f;
            var lines = new List<(int Start, int Count, float Width)>();
            int first = 0;
            float cursor = 0f;
            for (int i = 0; i < glyphs.Count; i++)
            {
                float width = advances[i] + (i > first ? st.Spacing : 0f);
                if (i > first && cursor + width > maxWidth &&
                    lines.Count + 1 < 4)
                {
                    lines.Add((first, i - first, cursor));
                    first = i;
                    cursor = 0f;
                    width = advances[i];
                }
                cursor += width;
            }
            if (first < glyphs.Count)
                lines.Add((first, glyphs.Count - first, cursor));
            if (lines.Count == 0) return;

            float lineHeight = st.Size * 1.18f;
            float centerX = st.X * Screen.width;
            float centerY = st.Y * Screen.height;
            int sourceIndex = 0;
            for (int row = 0; row < lines.Count; row++)
            {
                (int start, int count, float width) = lines[row];
                float x = centerX - width * 0.5f;
                float y = centerY +
                    (row - (lines.Count - 1) * 0.5f) * lineHeight;
                for (int k = 0; k < count; k++, sourceIndex++)
                {
                    int index = start + k;
                    string glyph = glyphs[index];
                    float advance = advances[index];
                    if (glyph == " " || glyph == "\u3000")
                    {
                        x += advance + st.Spacing;
                        continue;
                    }
                    double glyphRel = rel -
                        (st.Stagger > 0f
                            ? sourceIndex * st.Stagger
                            : 0.0);
                    if (glyphRel < 0) continue;
                    float appear = st.FadeIn > 0
                        ? Mathf.Clamp01((float)glyphRel /
                                        (float)st.FadeIn)
                        : 1f;
                    float glyphAlpha = Math.Min(alpha, appear);
                    if (glyphAlpha <= 0.004f)
                    {
                        x += advance + st.Spacing;
                        continue;
                    }
                    float ease = appear * appear *
                                 (3f - 2f * appear);
                    float pop = Mathf.Lerp(
                        Mathf.Max(1f, st.Pop), 1f, ease);
                    float phase = (float)(nowMs / 1000.0) *
                        2f * Mathf.PI * st.Hz +
                        sourceIndex * 1.7f;
                    float dx = Mathf.Sin(phase) * st.Amp;
                    float dy = Mathf.Sin(phase * 1.31f + 0.7f) *
                        st.Amp * 0.62f;
                    float rotation = Mathf.Sin(
                        phase * 0.9f + sourceIndex * 0.6f) * st.Rot;
                    Color color = st.Color;
                    color.a *= glyphAlpha;
                    Color shadow = st.ShadowColor;
                    shadow.a *= glyphAlpha;
                    DrawImGuiGlyph(
                        glyph,
                        x + advance * 0.5f + dx,
                        y + dy,
                        st.Size * pop,
                        advance,
                        rotation,
                        color,
                        st.Shadow ? shadow : Color.clear,
                        st.ShadowOffsetY);
                    x += advance + st.Spacing;
                }
            }
        }

        private static void DrawImGuiGlyph(
            string glyph,
            float centerX,
            float centerY,
            float size,
            float advance,
            float rotation,
            Color color,
            Color shadow,
            float shadowOffsetY)
        {
            if (_style == null) return;
            _style.fontSize = Mathf.Max(1, Mathf.RoundToInt(size));
            Matrix4x4 oldMatrix = GUI.matrix;
            try
            {
                GUI.matrix = Matrix4x4.TRS(
                    new Vector3(centerX, centerY, 0f),
                    Quaternion.Euler(0f, 0f, rotation),
                    Vector3.one);
                float width = Mathf.Max(size * 1.5f, advance);
                Rect rect = new Rect(
                    -width * 0.5f,
                    -size * 0.62f,
                    width,
                    size * 1.24f);
                if (shadow.a > 0.001f)
                {
                    _style.normal.textColor = shadow;
                    GUI.Label(
                        new Rect(
                            rect.x,
                            rect.y - shadowOffsetY,
                            rect.width,
                            rect.height),
                        glyph,
                        _style);
                }
                _style.normal.textColor = color;
                GUI.Label(rect, glyph, _style);
            }
            catch (Exception e)
            {
                if (_drawErrors < 5)
                {
                    _drawErrors++;
                    ChartExtMod.Log.Error(
                        "[ChartExt] showtext glyph draw failed: " +
                        e.Message);
                }
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
        }

        private static float GlyphAdvance(string glyph, float size)
        {
            if (string.IsNullOrEmpty(glyph)) return 0f;
            if (glyph == " " || glyph == "\u3000") return size * 0.32f;
            char c = glyph[0];
            if (c < 0x80)
            {
                if (char.IsDigit(c) || char.IsUpper(c))
                    return size * 0.62f;
                if (char.IsLower(c)) return size * 0.52f;
            }
            return size;
        }

        private static void Fail(
            string title,
            string detail,
            string chartKey = null)
        {
            if (string.IsNullOrEmpty(_fatalDetail))
            {
                _fatalFor = chartKey ??
                    ChartExtState.LastChartName;
                _fatalTitle = title;
                _fatalDetail = detail;
                ChartExtMod.Log.Error("[ChartExt] showtext fatal: " + detail.Replace('\n', ' '));
            }
            ResetFast();
            Styles.Clear();
            Sorted.Clear();
            ResetFastSchedule();
        }

        private static void MarkIgnored(
            string chartKey,
            string reason)
        {
            _ignoredFor = chartKey;
            Styles.Clear();
            Sorted.Clear();
            ResetFastSchedule();
            try
            {
                ChartExtMod.Log.Warning(
                    $"[ChartExt] showtext ignored for key='" +
                    $"{chartKey}': {reason}");
            }
            catch
            {
            }
        }

        private static void DrawFatalError()
        {
            int oldDepth = GUI.depth;
            GUI.depth = -2000;
            var panel = new Rect(
                Math.Max(30f, Screen.width * 0.16f),
                Math.Max(30f, Screen.height * 0.18f),
                Screen.width * 0.68f,
                Screen.height * 0.64f);
            GUI.Box(panel, _fatalTitle);
            GUI.Label(
                new Rect(panel.x + 32f, panel.y + 52f, panel.width - 64f, panel.height - 150f),
                _fatalDetail);
            if (GUI.Button(
                    new Rect(panel.center.x - 120f, panel.yMax - 78f, 240f, 42f),
                    "退出游戏"))
            {
                Application.Quit();
            }
            GUI.depth = oldDepth;
        }

        private static double NowMs()
        {
            var judge = Il2Cppifapp.Game.Scenes.GameScene._DN;
            if (judge == null || judge.Pointer == IntPtr.Zero) return double.NaN;
            return judge._c * 1000.0;
        }

        private static bool EnsureFastRenderer()
        {
            if (_fastTried) return !_fastFailed;
            _fastTried = true;
            try
            {
                Camera templateCamera = FindFastCamera();
                var all = UnityEngine.Object.FindObjectsOfType<Il2CppFastText.FastText>();
                Il2CppFastText.FastText best = null;
                int bestScore = int.MinValue;
                if (all != null)
                {
                    for (int i = 0; i < all.Length; i++)
                    {
                        var ft = all[i];
                        if (ft == null || ft.Pointer == IntPtr.Zero) continue;
                        int score = 0;
                        try
                        {
                            if (ft.fontFamilyAsset == null) continue;
                            if (!ft.gameObject.activeInHierarchy) continue;
                            Bounds templateBounds =
                                ft.meshRenderer.bounds;
                            float templateArea =
                                templateBounds.size.x *
                                templateBounds.size.y;
                            if (templateArea <= 0.000001f) continue;
                            score += Mathf.Min(
                                20,
                                (int)(Mathf.Log10(templateArea + 1f) * 10f));
                        }
                        catch { continue; }
                        try
                        {
                            string sample = ft.TextParameters.Param0_String;
                            if (!string.IsNullOrEmpty(sample) &&
                                HasNonAscii(sample))
                                score += 10;
                            string family = ft.fontFamilyAsset.name ?? "";
                            if (family.IndexOf("Stylised",
                                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                                family.IndexOf("Title",
                                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                                family.IndexOf("Song",
                                    StringComparison.OrdinalIgnoreCase) >= 0)
                                score += 10;
                            Vector3 localScale = ft.transform.localScale;
                            float extent = Mathf.Max(
                                Mathf.Abs(localScale.x),
                                Mathf.Abs(localScale.y));
                            if (extent < 0.05f) score -= 40;
                            else if (extent < 0.2f) score -= 10;
                            else if (extent <= 3f) score += 10;
                            if (templateCamera != null)
                            {
                                float depth = Vector3.Dot(
                                    ft.transform.position -
                                    templateCamera.transform.position,
                                    templateCamera.transform.forward);
                                if (depth < 1f) score -= 30;
                                else if (depth > 2f) score += 5;
                            }
                        }
                        catch { continue; }
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = ft;
                        }
                    }
                }
                _fastTemplate = best;
                if (!_fontsTried)
                {
                    _fontsTried = true;
                    string fontsDirectory =
                        FastTextFontSupport.FontsDirectory;
                    FastTextFontSupport.ExportBaseFont(fontsDirectory);
                    _customFamily = FastTextFontSupport.TryLoadCustomFamily(
                        best.fontFamilyAsset, fontsDirectory);
                }
                if (_fastTemplate == null)
                {
                    _fastFailed = true;
                    if (_logs < 3)
                    {
                        _logs++;
                        ChartExtMod.Log.Warning(
                            "[ChartExt] showtext: no live FastText template; using IMGUI font");
                    }
                    return false;
                }
                Canvas canvas = null;
                try { canvas = _fastTemplate.GetComponentInParent<Canvas>(); } catch { }
                _fastRoot = canvas != null ? canvas.transform : _fastTemplate.transform.parent;
                _fastCanvas = canvas;
                _fastCamera = templateCamera;
                if (_fastCamera != null)
                {
                    float depth = Vector3.Dot(
                        _fastTemplate.transform.position - _fastCamera.transform.position,
                        _fastCamera.transform.forward);
                    if (depth > 0.1f) _fastDepth = depth;
                }
                if (_fastRoot == null || _fastRoot.Pointer == IntPtr.Zero)
                {
                    _fastFailed = true;
                    return false;
                }
                string fontName = "";
                try { fontName = _fastTemplate.fontFamilyAsset.name; } catch { }
                float templateSize = 0f;
                try { templateSize = _fastTemplate.FontSize; } catch { }
                ChartExtMod.Log.Msg(
                    $"[ChartExt] showtext: FastText template='{_fastTemplate.name}' " +
                    $"font='{fontName}' canvas='{(canvas != null ? canvas.name : "?")}' " +
                    $"fontSize={templateSize:F2} scale={_fastTemplate.transform.localScale} " +
                    $"camera='{(_fastCamera != null ? _fastCamera.name : "?")}' " +
                    $"depth={_fastDepth:F2}");
                return true;
            }
            catch (Exception e)
            {
                _fastFailed = true;
                if (_logs < 3)
                {
                    _logs++;
                    ChartExtMod.Log.Warning(
                        "[ChartExt] showtext: FastText template failed: " + e.Message);
                }
                return false;
            }
        }

        private static void DrawFast(double nowMs)
        {
            if (!double.IsNaN(_lastDrawNow) &&
                nowMs + 0.5 < _lastDrawNow)
            {
                ResetFastSchedule();
            }
            _lastDrawNow = nowMs;

            while (_sortedCursor < Sorted.Count &&
                   Sorted[_sortedCursor].Time <= nowMs)
            {
                (double time, int sourceOrder, long id) =
                    Sorted[_sortedCursor++];
                _ = sourceOrder;
                if (!Styles.TryGetValue(id, out Style st))
                    continue;
                double duration = st.Duration <= 0 ? 1e12 : st.Duration;
                if (!Fast.TryGetValue(id, out FastEntry entry) ||
                    entry.Go == null || entry.Text == null)
                {
                    entry = CreateFast(id, st);
                    if (entry == null) continue;
                    Fast[id] = entry;
                }
                entry.StartMs = time;
                entry.DurationMs = duration;
                if (ActiveFastIds.Add(id))
                    ActiveFastOrder.Add(id);
            }

            for (int i = ActiveFastOrder.Count - 1; i >= 0; i--)
            {
                long id = ActiveFastOrder[i];
                FastEntry entry = Fast[id];
                if (nowMs - entry.StartMs <= entry.DurationMs)
                    continue;
                if (entry.Go != null)
                    entry.Go.SetActive(false);
                if (entry.Shadow != null)
                    entry.Shadow.SetActive(false);
                ActiveFastIds.Remove(id);
                ActiveFastOrder.RemoveAt(i);
            }

            for (int i = 0; i < ActiveFastOrder.Count; i++)
            {
                long id = ActiveFastOrder[i];
                if (!Styles.TryGetValue(id, out Style st) ||
                    !Fast.TryGetValue(id, out FastEntry entry) ||
                    entry.Go == null || entry.Text == null)
                    continue;
                double rel = nowMs - entry.StartMs;
                float alpha = 1f;
                if (st.FadeIn > 0 && rel < st.FadeIn) alpha = (float)(rel / st.FadeIn);
                if (st.FadeOut > 0 &&
                    entry.DurationMs - rel < st.FadeOut)
                    alpha = Math.Min(
                        alpha,
                        (float)((entry.DurationMs - rel) /
                                st.FadeOut));
                alpha = Math.Clamp(alpha, 0f, 1f);
                float appear = st.FadeIn > 0
                    ? Mathf.Clamp01((float)rel / (float)st.FadeIn)
                    : 1f;
                float pop = Mathf.Lerp(
                    Mathf.Max(1f, st.Pop), 1f,
                    appear * appear * (3f - 2f * appear));

                if (!entry.Go.activeSelf)
                    entry.Go.SetActive(true);
                entry.Go.transform.localScale = new Vector3(
                    entry.BaseScale.x * st.Scale * pop,
                    entry.BaseScale.y * st.Scale * pop,
                    entry.BaseScale.z);
                RectTransform rt = entry.Rect;
                PlaceFastText(entry, st);
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(Screen.width * 0.9f, st.Size * 1.8f);
                }
                string renderText = st.Text ?? string.Empty;
                if (st.Stagger > 0f)
                {
                    int visible = Mathf.Clamp(
                        Mathf.FloorToInt((float)rel / st.Stagger) + 1,
                        0, renderText.Length);
                    renderText = renderText.Substring(0, visible);
                }
                if (entry.LastText != renderText)
                {
                    SetFastText(entry.Text, renderText);
                    entry.LastText = renderText;
                }
                if (Math.Abs(entry.LastSize - st.Size) > 0.01f)
                {
                    entry.Text.SetFontSize(st.Size,
                        Il2CppFastText.FastText.UpdateType.Default);
                    entry.LastSize = st.Size;
                }
                try
                {
                    if (Math.Abs(entry.Text.characterSpacingAdjustment -
                            st.Spacing) > 0.01f)
                        entry.Text.characterSpacingAdjustment = st.Spacing;
                }
                catch { }
                Color c = st.Color;
                c.a *= alpha;
                Color c2 = st.Color2;
                c2.a *= alpha;
                if (entry.LastColor != c || entry.LastColor2 != c2)
                {
                    entry.Text.SetTextColor(c, c2,
                        Il2CppFastText.FastText.UpdateType.Default);
                    entry.LastColor = c;
                    entry.LastColor2 = c2;
                }
                try { entry.Text.EnsureDirtyFlagsProcessed(); } catch { }
                try
                {
                    Bounds textBounds = entry.Text.meshRenderer.bounds;
                    if (textBounds.size.x <= 0.0001f ||
                        textBounds.size.y <= 0.0001f)
                    {
                        _zeroBoundsCount++;
                        if (_zeroBoundsCount > 12)
                        {
                            _fastDisabled = true;
                            ResetFast();
                            if (_logs < 4)
                            {
                                _logs++;
                                ChartExtMod.Log.Warning(
                                    "[ChartExt] showtext: FastText produced " +
                                    "zero bounds; switching to IMGUI");
                            }
                            return;
                        }
                    }
                    else
                    {
                        _zeroBoundsCount = 0;
                    }
                    if (textBounds.size.x > 0.0001f && textBounds.size.y > 0.0001f)
                    {
                        Vector3 desired = entry.Go.transform.position;
                        entry.Go.transform.position += desired - textBounds.center;
                    }
                    if (DrawLogged.Count < 8 && DrawLogged.Add(id))
                    {
                        Vector3 viewport = _fastCamera != null
                            ? _fastCamera.WorldToViewportPoint(
                                entry.Go.transform.position)
                            : new Vector3(-1f, -1f, -1f);
                        ChartExtMod.Log.Msg(
                            $"[ChartExt] showtext draw id={id} " +
                            $"size={st.Size} scale={st.Scale:F2} " +
                            $"base={entry.BaseScale} " +
                            $"pos={entry.Go.transform.position} " +
                            $"bounds={textBounds.size} " +
                            $"viewport={viewport}");
                    }
                }
                catch { }
                if (st.Amp > 0.001f || Math.Abs(st.Rot) > 0.001f)
                {
                    float phase = (float)(nowMs / 1000.0) *
                        2f * Mathf.PI * st.Hz;
                    float dx = Mathf.Sin(phase) * st.Amp;
                    float dy = Mathf.Sin(phase * 1.31f + 0.7f) *
                        st.Amp * 0.62f;
                    try
                    {
                        if (rt != null)
                            rt.anchoredPosition += new Vector2(dx, -dy);
                        else
                        {
                            Camera camera = _fastCamera;
                            if (camera != null)
                            {
                                entry.Go.transform.position +=
                                    camera.transform.right * dx +
                                    camera.transform.up * dy;
                            }
                        }
                        entry.Go.transform.localRotation =
                            Quaternion.Euler(
                                0f, 0f,
                                Mathf.Sin(phase * 0.9f) * st.Rot);
                    }
                    catch { }
                }
                if (entry.Shadow != null)
                {
                    if (!st.Shadow)
                    {
                        entry.Shadow.SetActive(false);
                    }
                    else
                    {
                        entry.Shadow.SetActive(true);
                        try { entry.Text.EnsureDirtyFlagsProcessed(); } catch { }
                        try
                        {
                            Bounds bounds = entry.Text.meshRenderer.bounds;
                            if (bounds.size.x > 0.0001f && bounds.size.y > 0.0001f)
                            {
                                entry.Shadow.transform.position =
                                    bounds.center + new Vector3(0f, -st.ShadowOffsetY, 0f);
                                entry.Shadow.transform.rotation = entry.Text.transform.rotation;
                                entry.Shadow.transform.localScale = new Vector3(
                                    bounds.size.x * st.ShadowScaleX,
                                    bounds.size.y * st.ShadowScaleY, 1f);
                            }
                        }
                        catch { }
                        if (entry.ShadowMaterial != null)
                        {
                            Color shadowColor = st.ShadowColor;
                            shadowColor.a *= alpha;
                            entry.ShadowMaterial.color = shadowColor;
                            try { entry.ShadowMaterial.SetColor("_BaseColor", shadowColor); } catch { }
                        }
                    }
                }
            }
        }

        private static void ResetFastSchedule()
        {
            _sortedCursor = 0;
            _lastDrawNow = double.NaN;
            ActiveFastIds.Clear();
            ActiveFastOrder.Clear();
            foreach (FastEntry entry in Fast.Values)
            {
                if (entry.Go != null)
                    entry.Go.SetActive(false);
                if (entry.Shadow != null)
                    entry.Shadow.SetActive(false);
            }
        }

        private static FastEntry CreateFast(long id, Style st)
        {
            GameObject go = null;
            try
            {
                go = UnityEngine.Object.Instantiate(_fastTemplate.gameObject);
                go.name = "IFCE_ShowText_" + id;
                go.SetActive(false);
                go.transform.SetParent(_fastRoot, false);
                try
                {
                    var constraints = go.GetComponentsInChildren<
                        Il2Cppifapp.Game.UI.Common.Constrained2D>(true);
                    if (constraints != null)
                        for (int i = 0; i < constraints.Length; i++)
                            if (constraints[i] != null) constraints[i].enabled = false;
                }
                catch { }
                try
                {
                    var animators = go.GetComponentsInChildren<Animator>(true);
                    if (animators != null)
                        for (int i = 0; i < animators.Length; i++)
                            if (animators[i] != null) animators[i].enabled = false;
                }
                catch { }
                try
                {
                    var group = go.GetComponent<CanvasGroup>();
                    if (group != null) group.alpha = 1f;
                }
                catch { }
                Il2CppFastText.FastText text = go.GetComponent<Il2CppFastText.FastText>();
                if (text == null)
                {
                    UnityEngine.Object.Destroy(go);
                    return null;
                }
                text.alignment = Il2CppFastText.Data.AlignmentOptions.Center;
                if (_customFamily != null)
                    FastTextFontSupport.Apply(text, _customFamily);
                go.SetActive(true);
                text.SetFontSize(st.Size, Il2CppFastText.FastText.UpdateType.Default);
                text.SetTextColor(st.Color, Il2CppFastText.FastText.UpdateType.Default);
                SetFastText(text, st.Text ?? "");
                var entry = new FastEntry
                {
                    Go = go,
                    Text = text,
                    Rect = go.GetComponent<RectTransform>(),
                    LastText = st.Text,
                    LastSize = st.Size,
                    LastColor = st.Color,
                    BaseScale = go.transform.localScale,
                };
                AllFastObjects.Add(go);
                if (_logs < 6)
                {
                    _logs++;
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] showtext clone id={id} size={st.Size} " +
                        $"scale={st.Scale:F2} baseScale={entry.BaseScale}");
                }
                if (st.Shadow) CreateShadow(entry, id);
                return entry;
            }
            catch (Exception e)
            {
                DestroyFastOwned(go, null);
                if (_errors < 5)
                {
                    _errors++;
                    ChartExtMod.Log.Error("[ChartExt] showtext FastText create failed: " + e.Message);
                }
                return null;
            }
        }

        private static void PlaceFastText(FastEntry entry, Style st)
        {
            try
            {
                RectTransform rt = entry.Rect;
                Vector2 screen = new Vector2(
                    st.X * Screen.width,
                    (1f - st.Y) * Screen.height);
                if (rt != null && _fastCanvas != null)
                {
                    RectTransform canvasRect = _fastCanvas.GetComponent<RectTransform>();
                    Camera canvasCamera = _fastCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                        ? null
                        : _fastCanvas.worldCamera;
                    Vector2 local;
                    if (canvasRect != null &&
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvasRect, screen, canvasCamera, out local))
                    {
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = local;
                        return;
                    }
                }
                Camera camera = _fastCamera != null ? _fastCamera : FindFastCamera();
                if (camera != null)
                {
                    Vector3 world = camera.ViewportToWorldPoint(
                        new Vector3(st.X, 1f - st.Y, _fastDepth));
                    entry.Go.transform.position = world;
                    entry.Go.transform.rotation = camera.transform.rotation;
                    return;
                }
                if (rt != null)
                {
                    Vector2 anchor = new Vector2(st.X, 1f - st.Y);
                    rt.anchorMin = anchor;
                    rt.anchorMax = anchor;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }
            }
            catch { }
        }

        private static void SetFastText(
            Il2CppFastText.FastText text,
            string value)
        {
            try
            {
                Il2CppStr.TextParameters parameters =
                    Il2CppStr.TextParameters.CreateDirectString(
                        value ?? string.Empty);
                text.textParameters = parameters;
                text.EnsureDirtyFlagsProcessed();
            }
            catch
            {
                try
                {
                    text.SetTextNonLocalized(
                        value ?? string.Empty,
                        Il2CppFastText.FastText.UpdateType.Default);
                    text.EnsureDirtyFlagsProcessed();
                }
                catch { }
            }
        }

        private static Camera FindFastCamera()
        {
            try
            {
                Camera[] cameras = Camera.allCameras;
                if (cameras == null) return null;
                Camera best = null;
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera camera = cameras[i];
                    if (camera == null || !camera.isActiveAndEnabled) continue;
                    if (camera.cameraType != CameraType.Game) continue;
                    if ((camera.cullingMask & (1 << 7)) == 0) continue;
                    if (best == null || camera.depth > best.depth) best = camera;
                }
                return best;
            }
            catch { return null; }
        }

        private static bool HasNonAscii(string value)
        {
            for (int i = 0; i < value.Length; i++)
                if (value[i] > 0x7E) return true;
            return false;
        }

        private static void ResetFast()
        {
            int removed = 0;
            foreach (var pair in Fast)
            {
                try
                {
                    if (pair.Value.Shadow != null)
                    {
                        pair.Value.Shadow.SetActive(false);
                    }
                    if (pair.Value.ShadowMaterial != null)
                    {
                        UnityEngine.Object.Destroy(pair.Value.ShadowMaterial);
                    }
                    if (pair.Value.Go != null)
                    {
                        pair.Value.Go.SetActive(false);
                    }
                }
                catch { }
            }
            for (int i = 0; i < AllFastObjects.Count; i++)
            {
                GameObject go = AllFastObjects[i];
                if (go == null || go.Pointer == IntPtr.Zero) continue;
                try
                {
                    go.SetActive(false);
                    UnityEngine.Object.DestroyImmediate(go);
                    removed++;
                }
                catch { }
            }
            Fast.Clear();
            AllFastObjects.Clear();
            _fastTemplate = null;
            _fastRoot = null;
            _fastCanvas = null;
            _fastTried = false;
            _fastFailed = false;
            if (removed > 0 && _logs < 10)
            {
                _logs++;
                ChartExtMod.Log.Msg(
                    $"[ChartExt] showtext: cleared {removed} visual object(s) on chart switch");
            }
        }

        private static void CreateShadow(FastEntry entry, long id)
        {
            GameObject go = null;
            Material material = null;
            try
            {
                if (!EnsureShadowResources()) return;
                go = new GameObject("IFCE_ShowTextShadow_" + id);
                go.layer = entry.Text.gameObject.layer;
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = _shadowMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                material = new Material(_shadowSourceMaterial);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                try
                {
                    MeshRenderer textRenderer = entry.Text.meshRenderer;
                    if (textRenderer != null)
                    {
                        renderer.renderingLayerMask = textRenderer.renderingLayerMask;
                        renderer.sortingLayerID = textRenderer.sortingLayerID;
                        renderer.sortingOrder = textRenderer.sortingOrder - 1;
                    }
                }
                catch { }
                go.transform.SetParent(_fastRoot, false);
                try
                {
                    go.transform.SetSiblingIndex(entry.Text.transform.GetSiblingIndex());
                }
                catch { }
                entry.Shadow = go;
                entry.ShadowRenderer = renderer;
                entry.ShadowMaterial = material;
                AllFastObjects.Add(go);
            }
            catch (Exception e)
            {
                DestroyFastOwned(go, material);
                if (_errors < 5)
                {
                    _errors++;
                    ChartExtMod.Log.Error("[ChartExt] showtext shadow create failed: " + e.Message);
                }
            }
        }

        private static void DestroyFastOwned(
            GameObject gameObject,
            Material material)
        {
            try
            {
                if (material != null &&
                    material.Pointer != IntPtr.Zero)
                    UnityEngine.Object.Destroy(material);
            }
            catch { }
            try
            {
                if (gameObject != null &&
                    gameObject.Pointer != IntPtr.Zero)
                    UnityEngine.Object.Destroy(gameObject);
            }
            catch { }
        }

        private static bool EnsureShadowResources()
        {
            Material pendingMaterial = null;
            try
            {
                if (_shadowMesh == null) _shadowMesh = CreateShadowQuad();
                if (_shadowTexture == null) _shadowTexture = CreateShadowTexture(128);
                if (_shadowSourceMaterial == null)
                {
                    Shader shader = Shader.Find("Unlit/StenciledUnlitSprite");
                    if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    if (shader == null) return false;
                    pendingMaterial = new Material(shader)
                    {
                        name = "IFCE_ShowTextShadowMaterial",
                    };
                    pendingMaterial.mainTexture = _shadowTexture;
                    pendingMaterial.SetTexture("_MainTex", _shadowTexture);
                    pendingMaterial.SetTexture("_BaseMap", _shadowTexture);
                    pendingMaterial.SetColor("_Color", Color.black);
                    pendingMaterial.SetColor("_BaseColor", Color.black);
                    pendingMaterial.SetFloat("_Stencil", 0f);
                    pendingMaterial.SetFloat("_ZTest", 8f);
                    pendingMaterial.SetFloat("_ZWrite", 0f);
                    pendingMaterial.SetFloat("_Cull", 0f);
                    pendingMaterial.SetFloat("_SrcBlend", 5f);
                    pendingMaterial.SetFloat("_DstBlend", 10f);
                    pendingMaterial.SetFloat("_SrcBlendAlpha", 1f);
                    pendingMaterial.SetFloat("_DstBlendAlpha", 10f);
                    pendingMaterial.renderQueue = 3000;
                    _shadowSourceMaterial = pendingMaterial;
                    pendingMaterial = null;
                }
                return _shadowMesh != null && _shadowTexture != null &&
                       _shadowSourceMaterial != null;
            }
            catch
            {
                DestroyFastOwned(null, pendingMaterial);
                return false;
            }
        }

        private static Mesh CreateShadowQuad()
        {
            var mesh = new Mesh
            {
                name = "IFCE_ShowTextShadowQuad",
            };
            mesh.vertices = new Il2CppInterop.Runtime.InteropTypes.Arrays
                .Il2CppStructArray<Vector3>(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            });
            mesh.uv = new Il2CppInterop.Runtime.InteropTypes.Arrays
                .Il2CppStructArray<Vector2>(new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            });
            mesh.triangles = new Il2CppInterop.Runtime.InteropTypes.Arrays
                .Il2CppStructArray<int>(new[] { 0, 2, 1, 0, 3, 2 });
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D CreateShadowTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "IFCE_ShowTextShadowMask",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            texture.SetPixels(new Il2CppInterop.Runtime.InteropTypes.Arrays
                .Il2CppStructArray<Color>(pixels));
            texture.Apply(false, false);
            return texture;
        }

        private static Color ParseColor(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return Color.white;
                string h = s.TrimStart('#');
                if (h.Length == 6 || h.Length == 8)
                {
                    int r = Convert.ToInt32(h.Substring(0, 2), 16);
                    int g = Convert.ToInt32(h.Substring(2, 2), 16);
                    int b = Convert.ToInt32(h.Substring(4, 2), 16);
                    int a = h.Length == 8 ? Convert.ToInt32(h.Substring(6, 2), 16) : 255;
                    return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                }
            }
            catch { }
            return Color.white;
        }
    }
}
