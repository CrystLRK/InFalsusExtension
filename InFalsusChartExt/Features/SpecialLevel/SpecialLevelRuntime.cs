using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
#if MELONLOADER
using Il2Cpp;
using Il2Cppifapp.Game.Data;
using Il2Cppifapp.Game.Scenes;
using Il2Cppifapp.Game.Scenes.Game.UI;
using Il2Cppifapp.Game.Scenes.SongSelect;
using C2D = Il2Cppifapp.Game.UI.Common.Constrained2D;
using TransitionLayer = Il2Cpp.SongTransitionLayer;
using PackSongCardMember = Il2Cppifapp.Game.PackSongCardMember;
#elif BEPINEX
using ifapp.Game.Data;
using ifapp.Game.Scenes;
using ifapp.Game.Scenes.Game.UI;
using ifapp.Game.Scenes.SongSelect;
using C2D = ifapp.Game.UI.Common.Constrained2D;
using TransitionLayer = global::SongTransitionLayer;
using PackSongCardMember = ifapp.Game.PackSongCardMember;
#endif

namespace InFalsusChartExt
{
    /// <summary>
    /// Draws <c>SongChartInfo.LevelSectionIndicator</c> wherever the game
    /// prints a difficulty number. Every hook is a postfix on a normal managed
    /// UI refresh; the original method, rating, chart data and judgements are
    /// left untouched.
    /// </summary>
    internal static class SpecialLevelRuntime
    {
        private struct FontState
        {
            public Il2CppFastText.Data.FastTextFontFamily Family;
            public int Localization;
        }

        private static readonly Dictionary<IntPtr, FontState> Originals =
            new Dictionary<IntPtr, FontState>();
        private static readonly Dictionary<IntPtr, string> Applied =
            new Dictionary<IntPtr, string>();
        private static Il2CppFastText.FastText _lastDonor;
        private sealed class PendingLevel
        {
            public SongSelectDifficultyButton Button;
            public ChartDifficultyFlag Difficulty;
            public int Level;
            public long At;
        }
        private static readonly List<PendingLevel> Pending =
            new List<PendingLevel>();
        private const long PendingLifetime = 800;
        private const int PendingLimit = 12;
        private static SongInfo _currentSong;
        private static SongInfoContainer _hud;
        private static TransitionLayer _transition;
        private static ResultsJacketContainer _results;
        private static int _currentSlot = -1;
        private static bool _hasCurrent;
        private static int _logs;
        private static int _cardLogs;
        private static int _errors;
        private static int _lastGuiFrame = -1;
        private static int _hudDiag;
        private static bool _lateInstalled;

        internal static void ResetForScene()
        {
            Originals.Clear();
            Applied.Clear();
            Pending.Clear();
            _hud = null;
            _transition = null;
            _results = null;
            // Selection state must survive the song-select -> gameplay
            // scene transition; the next selection overwrites it.
        }

        internal static void Tick()
        {
            if (!_hasCurrent) return;
            try
            {
                TickHud();
                TickTransition();
                TickResults();
            }
            catch (Exception e)
            {
                Guard("special-level tick", e);
            }
        }

        internal static void InstallLateApply()
        {
            if (_lateInstalled) return;
            try
            {
                Canvas.willRenderCanvases +=
                    DelegateSupport.ConvertDelegate<
                        Canvas.WillRenderCanvases>(
                        new Action(OnWillRenderCanvases));
                _lateInstalled = true;
            }
            catch (Exception e)
            {
                Guard("late apply install", e);
            }
        }

        private static void OnWillRenderCanvases()
        {
            Tick();
        }

        internal static void OnGUI()
        {
            if (Time.frameCount == _lastGuiFrame) return;
            _lastGuiFrame = Time.frameCount;
            Tick();
        }

        private static void TickHud()
        {
            try
            {
                if (_hud == null ||
                    _hud.Pointer == IntPtr.Zero ||
                    !_hud.gameObject.activeInHierarchy)
                {
                    if ((Time.frameCount & 15) != 0) return;
                    var huds = UnityEngine.Object.FindObjectsOfType<
                        SongInfoContainer>();
                    _hud = null;
                    if (huds != null)
                    {
                        for (int i = 0; i < huds.Length; i++)
                        {
                            SongInfoContainer candidate = huds[i];
                            if (candidate != null &&
                                candidate.Pointer != IntPtr.Zero &&
                                candidate.gameObject.activeInHierarchy)
                            {
                                _hud = candidate;
                                break;
                            }
                        }
                    }
                }
                if (_hud == null) return;
                if (_hudDiag++ < 4)
                {
                    var levels = _hud.difficultyLevelText;
                    int length = levels?.Length ?? 0;
                    for (int i = 0; i < length; i++)
                    {
                        var text = levels[i];
                        if (text == null) continue;
                        string value = "";
                        string family = "";
                        try { value = text.TextParameters.Param0_String; } catch { }
                        try { family = text.fontFamilyAsset?.name; } catch { }
                        ChartExtMod.Log.Msg(
                            $"[ChartExt] sp hud diag[{i}] text='{value}' " +
                            $"family='{family}' active={text.gameObject.activeInHierarchy}");
                    }
                }
                ApplyTextArray(
                    _hud.difficultyLevelText,
                    _currentSong, _currentSlot,
                    DonorFromHud(_hud), true);
            }
            catch (Exception e)
            {
                Guard("hud tick", e);
            }
        }

        private static void TickTransition()
        {
            try
            {
                if (_transition == null ||
                    _transition.Pointer == IntPtr.Zero ||
                    !_transition.gameObject.activeInHierarchy)
                {
                    if ((Time.frameCount & 15) != 0) return;
                    var layers = UnityEngine.Object.FindObjectsOfType<
                        TransitionLayer>();
                    _transition = null;
                    if (layers != null)
                    {
                        for (int i = 0; i < layers.Length; i++)
                        {
                            TransitionLayer candidate = layers[i];
                            if (candidate != null &&
                                candidate.Pointer != IntPtr.Zero &&
                                candidate.gameObject.activeInHierarchy)
                            {
                                _transition = candidate;
                                break;
                            }
                        }
                    }
                }
                if (_transition == null) return;
                var elements = _transition.difficultyElements;
                ApplyConstrainedArray(
                    elements.LevelTextValue,
                    _currentSong, _currentSlot,
                    DonorFromTransition(_transition), true);
            }
            catch (Exception e)
            {
                Guard("transition tick", e);
            }
        }

        private static void TickResults()
        {
            try
            {
                if (_results == null ||
                    _results.Pointer == IntPtr.Zero ||
                    !_results.gameObject.activeInHierarchy)
                {
                    if ((Time.frameCount & 15) != 0) return;
                    var containers = UnityEngine.Object.FindObjectsOfType<
                        ResultsJacketContainer>();
                    _results = null;
                    if (containers != null)
                    {
                        for (int i = 0; i < containers.Length; i++)
                        {
                            ResultsJacketContainer candidate = containers[i];
                            if (candidate != null &&
                                candidate.Pointer != IntPtr.Zero &&
                                candidate.gameObject.activeInHierarchy)
                            {
                                _results = candidate;
                                break;
                            }
                        }
                    }
                    if (_results == null)
                    {
                        var scenes = UnityEngine.Object.FindObjectsOfType<
                            ResultsScene>();
                        if (scenes != null)
                        {
                            for (int i = 0; i < scenes.Length; i++)
                            {
                                ResultsScene scene = scenes[i];
                                if (scene == null ||
                                    scene.Pointer == IntPtr.Zero ||
                                    !scene.gameObject.activeInHierarchy)
                                    continue;
                                _results = scene.resultsJacketContainer;
                                if (_results != null) break;
                            }
                        }
                    }
                }
                if (_results == null) return;
                Il2CppFastText.FastText donor =
                    DonorFromResults(_results);
                ApplyConstrained(
                    _results.coloredDifficultyNumber,
                    _currentSong, _currentSlot, donor, true);
                ApplyConstrainedArray(
                    _results.textsC2d,
                    _currentSong, _currentSlot, donor, true);
                ApplyTextArray(
                    _results.texts,
                    _currentSong, _currentSlot, donor);
            }
            catch (Exception e)
            {
                Guard("results tick", e);
            }
        }

        private static int SlotOf(ChartDifficultyFlag difficulty)
        {
            switch (difficulty)
            {
                case ChartDifficultyFlag.Minimal: return 0;
                case ChartDifficultyFlag.Evolved: return 1;
                case ChartDifficultyFlag.Ultimate: return 2;
                case ChartDifficultyFlag.Forbidden: return 3;
                default: return -1;
            }
        }

        private static SongChartInfo ChartOf(
            SongInfo song,
            int slot)
        {
            try
            {
                var charts = song.ChartInfos;
                if (charts == null || slot < 0 || slot >= charts.Length)
                    return null;
                return charts[slot];
            }
            catch
            {
                return null;
            }
        }

        private static string SpecialOf(SongInfo song, int slot)
        {
            if (!IsCustomSong(song)) return null;
            SongChartInfo chart = ChartOf(song, slot);
            if (chart == null) return null;
            if (!chart.Available) return null;
            string special = chart.LevelSectionIndicator;
            if (string.IsNullOrWhiteSpace(special)) return null;
            if (special == chart.Rating.ToString(
                    System.Globalization.CultureInfo.InvariantCulture))
                return null;
            return special;
        }

        private static bool IsCustomSong(SongInfo song)
        {
            try
            {
                return CustomSong.Lifecycle.CustomSongRuntime
                    .TryGetSongByRuntimeId(
                        (ushort)song.Id.Value, out _);
            }
            catch
            {
                return false;
            }
        }

        private static string CurrentText(
            Il2CppFastText.FastText text)
        {
            try
            {
                return text.TextParameters.Param0_String;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasNonAscii(string value)
        {
            for (int i = 0; i < value.Length; i++)
                if (value[i] > 0x7E) return true;
            return false;
        }

        private static void ApplyText(
            Il2CppFastText.FastText text,
            C2D constrained,
            SongInfo song,
            int slot,
            Il2CppFastText.FastText donor,
            bool force = false)
        {
            if (text == null || text.Pointer == IntPtr.Zero) return;
            if (donor == null)
                donor = _lastDonor;
            SongChartInfo chart = ChartOf(song, slot);
            string special = SpecialOf(song, slot);
            if (special == null)
            {
                string restoreRating = chart != null
                    ? chart.Rating.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : null;
                if (Applied.TryGetValue(
                        text.Pointer, out string previousText))
                {
                    // Prefer the current difficulty's own rating. A reused
                    // text component may have captured null/empty before the
                    // label was initialized; restoring "" made the pack card
                    // show nothing at all.
                    string restore = restoreRating;
                    if (string.IsNullOrEmpty(restore))
                        restore = previousText;
                    try
                    {
                        if (!string.IsNullOrEmpty(restore))
                        {
                            text.SetTextNonLocalized(
                                restore,
                                Il2CppFastText.FastText.UpdateType.Default);
                            text.EnsureDirtyFlagsProcessed();
                            if (constrained != null)
                                constrained._qQA();
                        }
                    }
                    catch (Exception e)
                    {
                        Guard("label restore", e);
                    }
                    Applied.Remove(text.Pointer);
                }
                RestoreFont(text);
                return;
            }

            string current = CurrentText(text);
            string rating = chart != null
                ? chart.Rating.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                : null;
            if (!force &&
                (current == null ||
                 (current != rating && current != special)))
                return;

            if (current != special)
            {
                if (Applied.Count >= 64) Applied.Clear();
                Applied[text.Pointer] = current ?? string.Empty;
            }
            else if (!Applied.ContainsKey(text.Pointer))
            {
                if (Applied.Count >= 64) Applied.Clear();
                Applied[text.Pointer] = rating ?? string.Empty;
            }

            if (HasNonAscii(special) && donor != null)
            {
                SwapFont(text, donor);
                _lastDonor = donor;
            }
            else
                RestoreFont(text);

            try
            {
                text.SetTextNonLocalized(
                    special,
                    Il2CppFastText.FastText.UpdateType.Default);
                text.EnsureDirtyFlagsProcessed();
            }
            catch (Exception e)
            {
                Guard("label text", e);
            }
            if (constrained != null)
            {
                try { constrained._qQA(); } catch { }
            }
            if (_logs++ < 5)
            {
                ChartExtMod.Log.Msg(
                    $"[ChartExt] sp '{special}' slot={slot}");
            }
        }

        private static void SwapFont(
            Il2CppFastText.FastText text,
            Il2CppFastText.FastText donor)
        {
            try
            {
                if (text.fontFamilyAsset == donor.fontFamilyAsset)
                    return;
                IntPtr key = text.Pointer;
                if (!Originals.ContainsKey(key))
                {
                    if (Originals.Count >= 64) Originals.Clear();
                    Originals[key] = new FontState
                    {
                        Family = text.fontFamilyAsset,
                        Localization = (int)text.targetLocalization,
                    };
                }
                SetFamily(text, donor.fontFamilyAsset);
                text.targetLocalization =
                    (Il2CppStr.Strings.Localization)
                    (int)donor.targetLocalization;
            }
            catch (Exception e)
            {
                Guard("label font", e);
            }
        }

        private static void RestoreFont(
            Il2CppFastText.FastText text)
        {
            try
            {
                if (!Originals.TryGetValue(
                        text.Pointer, out FontState state))
                    return;
                SetFamily(text, state.Family);
                text.targetLocalization =
                    (Il2CppStr.Strings.Localization)state.Localization;
                Originals.Remove(text.Pointer);
            }
            catch (Exception e)
            {
                Guard("label font restore", e);
            }
        }

        private static void SetFamily(
            Il2CppFastText.FastText text,
            Il2CppFastText.Data.FastTextFontFamily family)
        {
            try
            {
                text.SetFontFamilyAsset(family);
            }
            catch
            {
                try { text.fontFamilyAsset = family; } catch { }
            }
        }

        private static void ApplyConstrained(
            C2D constrained,
            SongInfo song,
            int slot,
            Il2CppFastText.FastText donor,
            bool force = false)
        {
            if (constrained == null) return;
            Il2CppFastText.FastText text = null;
            try { text = constrained._i; } catch { }
            if (text == null) return;
            ApplyText(text, constrained, song, slot, donor, force);
        }

        private static void ApplyConstrainedArray(
            Il2CppInterop.Runtime.InteropTypes.Arrays
                .Il2CppReferenceArray<C2D> values,
            SongInfo song,
            int slot,
            Il2CppFastText.FastText donor,
            bool force = false)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                ApplyConstrained(values[i], song, slot, donor, force);
        }

        private static void ApplyTextArray(
            Il2CppInterop.Runtime.InteropTypes.Arrays
                .Il2CppReferenceArray<Il2CppFastText.FastText> values,
            SongInfo song,
            int slot,
            Il2CppFastText.FastText donor,
            bool force = false)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                ApplyText(values[i], null, song, slot, donor, force);
        }

        private static Il2CppFastText.FastText DonorFromCard(
            LargeSongCard card)
        {
            if (card == null) return null;
            try
            {
                var artists = card.artistNames;
                if (artists != null)
                    for (int i = 0; i < artists.Length; i++)
                        if (artists[i] != null) return artists[i];
            }
            catch { }
            try
            {
                var names = card.songNames;
                if (names != null)
                    for (int i = 0; i < names.Length; i++)
                        if (names[i] != null) return names[i];
            }
            catch { }
            return _lastDonor;
        }

        private static Il2CppFastText.FastText DonorFromHud(
            SongInfoContainer hud)
        {
            if (hud == null) return null;
            try
            {
                var artists = hud.songArtist;
                if (artists != null)
                    for (int i = 0; i < artists.Length; i++)
                        if (artists[i] != null) return artists[i];
            }
            catch { }
            try
            {
                var titles = hud.songTitle;
                if (titles != null)
                    for (int i = 0; i < titles.Length; i++)
                        if (titles[i] != null && titles[i]._i != null)
                            return titles[i]._i;
            }
            catch { }
            return null;
        }

        private static Il2CppFastText.FastText DonorFromTransition(
            TransitionLayer layer)
        {
            if (layer == null) return null;
            try
            {
                var artists = layer.artist;
                if (artists != null)
                    for (int i = 0; i < artists.Length; i++)
                        if (artists[i] != null && artists[i]._i != null)
                            return artists[i]._i;
            }
            catch { }
            try
            {
                var titles = layer.songTitle;
                if (titles != null)
                    for (int i = 0; i < titles.Length; i++)
                        if (titles[i] != null && titles[i]._i != null)
                            return titles[i]._i;
            }
            catch { }
            return null;
        }

        private static Il2CppFastText.FastText DonorFromSmall(
            SmallSongCard card)
        {
            if (card == null) return null;
            try { if (card.artistName != null) return card.artistName; }
            catch { }
            try { if (card.songName != null) return card.songName; }
            catch { }
            return null;
        }

        private static Il2CppFastText.FastText DonorFromResults(
            ResultsJacketContainer results)
        {
            if (results == null) return null;
            try
            {
                var texts = results.texts;
                if (texts != null)
                    for (int i = 0; i < texts.Length; i++)
                        if (texts[i] != null) return texts[i];
            }
            catch { }
            return null;
        }

        private static Il2CppFastText.FastText DonorFromPackCard(
            PackSongCardMember card)
        {
            if (card == null) return null;
            try
            {
                var names = card.songNameTexts;
                if (names != null)
                    for (int i = 0; i < names.Length; i++)
                        if (names[i] != null && names[i]._i != null)
                            return names[i]._i;
            }
            catch { }
            try
            {
                var artists = card.artistNameTexts;
                if (artists != null)
                    for (int i = 0; i < artists.Length; i++)
                        if (artists[i] != null && artists[i]._i != null)
                            return artists[i]._i;
            }
            catch { }
            return _lastDonor;
        }

        private static void ApplyPackCard(
            PackSongCardMember card,
            SongInfo song,
            int slot)
        {
            if (card == null) return;
            int effectiveSlot = EffectiveSlot(song, slot);
            C2D rating = null;
            C2D constrained = null;
            try { rating = card.ratingText; } catch { }
            try { constrained = card.constrained2D; } catch { }
            Il2CppFastText.FastText donor = DonorFromPackCard(card);
            if (rating != null)
                ApplyConstrained(
                    rating, song, effectiveSlot, donor, true);
            if (constrained != null &&
                (rating == null ||
                 constrained.Pointer != rating.Pointer))
                ApplyConstrained(
                    constrained, song, effectiveSlot, donor, true);
        }

        private static int EffectiveSlot(SongInfo song, int slot)
        {
            SongChartInfo current = ChartOf(song, slot);
            if (current != null && current.Available)
                return slot;
            for (int i = 0; i < 4; i++)
            {
                SongChartInfo candidate = ChartOf(song, i);
                if (candidate != null && candidate.Available)
                    return i;
            }
            return slot;
        }

        private static void RefreshScene(SongSelectScene scene)
        {
            if (scene == null) return;
            try
            {
                SongInfo song = scene._wr;
                int slot = SlotOf(scene._Sr);
                if (_cardLogs++ < 4)
                {
                    ChartExtMod.Log.Msg(
                        $"[ChartExt] sp selection id={song.Id.Value} " +
                        $"slot={slot} text='{SpecialOf(song, slot) ?? "(none)"}'");
                }
                _currentSong = song;
                _currentSlot = slot;
                _hasCurrent = true;
                LargeSongCard card = scene.largeSongCard;
                Il2CppFastText.FastText donor = DonorFromCard(card);
                if (card != null)
                    ApplyConstrainedArray(
                        card.difficultyNumbers, song, slot, donor, true);
                var buttons = scene.difficultyButtons;
                if (buttons != null)
                {
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        SongSelectDifficultyButton button = buttons[i];
                        if (button == null) continue;
                        int buttonSlot = SlotOf(button.difficulty);
                        ApplyTextArray(
                            button.difficultyNumberText,
                            song, buttonSlot, donor, true);
                        ApplyConstrainedArray(
                            button.difficultyNumberTextC2D,
                            song, buttonSlot, donor, true);
                    }
                }
            }
            catch (Exception e)
            {
                Guard("song-select refresh", e);
            }
        }

        private static void Guard(string what, Exception e)
        {
            if (_errors++ < 6)
                ChartExtMod.Log.Warning(
                    "[ChartExt] sp " + what + " failed: " +
                    e.GetType().Name + ": " + e.Message);
        }

        [HarmonyPatch(typeof(LargeSongCard), "_OS")]
        private static class LargeCardPatch
        {
            [HarmonyPostfix]
            private static void Postfix(LargeSongCard __instance)
            {
                try
                {
                    if (__instance == null) return;
                    Il2Cpp_K._SH selection = null;
                    try { selection = __instance._oy; } catch { }
                    if (selection == null)
                    {
                        try { selection = __instance._A; } catch { }
                    }
                    if (selection == null)
                    {
                        if (!_hasCurrent) return;
                        ApplyConstrainedArray(
                            __instance.difficultyNumbers,
                            _currentSong, _currentSlot,
                            DonorFromCard(__instance), true);
                        ApplyPending(_currentSong, _currentSlot);
                        return;
                    }
                    SongInfo song = selection._a;
                    int slot = SlotOf(selection._e);
                    if (_cardLogs++ < 8)
                    {
                        ChartExtMod.Log.Msg(
                            $"[ChartExt] sp card id={song.Id.Value} " +
                            $"slot={slot} text='{SpecialOf(song, slot) ?? "(none)"}'");
                    }
                    ApplyConstrainedArray(
                        __instance.difficultyNumbers,
                        song, slot, DonorFromCard(__instance), true);
                    _currentSong = song;
                    _currentSlot = slot;
                    _hasCurrent = true;
                    ApplyPending(song, slot);
                }
                catch (Exception e)
                {
                    Guard("large card", e);
                }
            }
        }

        [HarmonyPatch]
        private static class DifficultyButtonPatch
        {
            [HarmonyPrepare]
            private static bool Prepare()
            {
                return TargetMethod() != null;
            }

            [HarmonyTargetMethod]
            private static MethodBase TargetMethod()
            {
                // The method name depends on the game build, not the loader.
                // 1.0.5 uses _BN; latest uses _Sp.
                return AccessTools.Method(
                           typeof(SongSelectDifficultyButton), "_Sp") ??
                       AccessTools.Method(
                           typeof(SongSelectDifficultyButton), "_BN");
            }

            [HarmonyPostfix]
            private static void Postfix(
                SongSelectDifficultyButton __instance,
                object[] __args)
            {
                try
                {
                    if (__instance == null) return;
                    int level = 0;
                    if (__args != null)
                    {
                        for (int i = 0; i < __args.Length; i++)
                        {
                            if (__args[i] is int value)
                            {
                                level = value;
                                break;
                            }
                        }
                    }
                    if (Pending.Count >= PendingLimit)
                        Pending.RemoveAt(0);
                    Pending.Add(new PendingLevel
                    {
                        Button = __instance,
                        Difficulty = __instance.difficulty,
                        Level = level,
                        At = Environment.TickCount64,
                    });
                }
                catch (Exception e)
                {
                    Guard("difficulty button", e);
                }
            }
        }

        private static void ApplyPending(SongInfo song, int slot)
        {
            long now = Environment.TickCount64;
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                PendingLevel pending = Pending[i];
                if (now - pending.At > PendingLifetime)
                {
                    Pending.RemoveAt(i);
                    continue;
                }
                if (pending.Button == null) continue;
                int pendingSlot = SlotOf(pending.Difficulty);
                if (pendingSlot != slot) continue;
                SongChartInfo chart = ChartOf(song, pendingSlot);
                if (chart == null || chart.Rating != pending.Level)
                    continue;
                SongSelectScene scene = FindScene();
                Il2CppFastText.FastText donor = scene != null
                    ? DonorFromCard(scene.largeSongCard)
                    : null;
                ApplyTextArray(
                    pending.Button.difficultyNumberText,
                    song, pendingSlot, donor, true);
                ApplyConstrainedArray(
                    pending.Button.difficultyNumberTextC2D,
                    song, pendingSlot, donor, true);
                Pending.RemoveAt(i);
            }
        }

        [HarmonyPatch(typeof(SongInfoContainer), "_QCA")]
        private static class HudPatch
        {
            [HarmonyPostfix]
            private static void Postfix(SongInfoContainer __instance)
            {
                try
                {
                    if (!_hasCurrent) return;
                    ApplyTextArray(
                        __instance.difficultyLevelText,
                        _currentSong, _currentSlot,
                        DonorFromHud(__instance), true);
                }
                catch (Exception e)
                {
                    Guard("hud postfix", e);
                }
            }
        }

        [HarmonyPatch(typeof(TransitionLayer), "_IA")]
        private static class TransitionPatch
        {
            [HarmonyPostfix]
            private static void Postfix(TransitionLayer __instance)
            {
                try
                {
                    if (!_hasCurrent) return;
                    var elements = __instance.difficultyElements;
                    ApplyConstrainedArray(
                        elements.LevelTextValue,
                        _currentSong, _currentSlot,
                        DonorFromTransition(__instance), true);
                }
                catch (Exception e)
                {
                    Guard("transition postfix", e);
                }
            }
        }

        [HarmonyPatch(typeof(TransitionLayer), "_jA")]
        private static class TransitionAfterDivePatch
        {
            [HarmonyPostfix]
            private static void Postfix(TransitionLayer __instance)
            {
                try
                {
                    if (!_hasCurrent) return;
                    var elements = __instance.difficultyElements;
                    ApplyConstrainedArray(
                        elements.LevelTextValue,
                        _currentSong, _currentSlot,
                        DonorFromTransition(__instance), true);
                }
                catch (Exception e)
                {
                    Guard("transition after dive postfix", e);
                }
            }
        }

        [HarmonyPatch(typeof(ResultsJacketContainer), "_Wm")]
        private static class ResultsPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ResultsJacketContainer __instance)
            {
                try
                {
                    if (!_hasCurrent) return;
                    Il2CppFastText.FastText donor =
                        DonorFromResults(__instance);
                    ApplyConstrained(
                        __instance.coloredDifficultyNumber,
                        _currentSong, _currentSlot, donor, true);
                    ApplyTextArray(
                        __instance.texts,
                        _currentSong, _currentSlot, donor);
                }
                catch (Exception e)
                {
                    Guard("results postfix", e);
                }
            }
        }

        [HarmonyPatch(typeof(SmallSongCard), "_tS")]
        private static class SmallCardPatch
        {
            [HarmonyPostfix]
            private static void Postfix(SmallSongCard __instance)
            {
                try
                {
                    if (__instance == null) return;
                    Il2Cpp_K._SH selection = null;
                    try { selection = __instance._py; } catch { }
                    if (selection == null) return;
                    SongInfo song = selection._a;
                    int slot = SlotOf(selection._e);
                    Il2CppFastText.FastText donor =
                        DonorFromSmall(__instance);
                    ApplyConstrained(
                        __instance.difficultyLevel,
                        song, slot, donor, true);
                    ApplyText(
                        __instance.sectionIndicatorText,
                        null, song, slot, donor, true);
                }
                catch (Exception e)
                {
                    Guard("small card", e);
                }
            }
        }

        [HarmonyPatch(typeof(PackSongCardMember), "_pc")]
        private static class PackSongCardPatch
        {
            [HarmonyPostfix]
            private static void Postfix(
                PackSongCardMember __instance,
                Il2Cpp_K._SH __1)
            {
                try
                {
                    if (__instance == null || __1 == null) return;
                    ApplyPackCard(__instance, __1._a, SlotOf(__1._e));
                }
                catch (Exception e)
                {
                    Guard("pack song card", e);
                }
            }
        }

        [HarmonyPatch(typeof(PackSongCardMember), "_ef")]
        private static class PackSongCardAltPatch
        {
            [HarmonyPostfix]
            private static void Postfix(
                PackSongCardMember __instance,
                Il2Cpp_K._SH __1)
            {
                try
                {
                    if (__instance == null || __1 == null) return;
                    ApplyPackCard(__instance, __1._a, SlotOf(__1._e));
                }
                catch (Exception e)
                {
                    Guard("pack song card alt", e);
                }
            }
        }

        [HarmonyPatch(typeof(PackSongCardMember), "_qc")]
        private static class PackSongCardDifficultyPatch
        {
            [HarmonyPostfix]
            private static void Postfix(PackSongCardMember __instance)
            {
                try
                {
                    if (__instance == null) return;
                    Il2Cpp_K._SH selection = null;
                    try { selection = __instance._if; } catch { }
                    if (selection == null) return;
                    ApplyPackCard(
                        __instance, selection._a, SlotOf(selection._e));
                }
                catch (Exception e)
                {
                    Guard("pack song card difficulty", e);
                }
            }
        }

        [HarmonyPatch(typeof(SongSelectScene), "_MN")]
        private static class SelectionPatch
        {
            [HarmonyPostfix]
            private static void Postfix(
                SongSelectScene __instance,
                SongId __0,
                ChartDifficultyFlag __1)
            {
                try
                {
                    _currentSong = __instance._wr;
                    _currentSlot = SlotOf(__1);
                    _hasCurrent = true;
                    RefreshScene(__instance);
                }
                catch (Exception e)
                {
                    Guard("selection song", e);
                }
            }
        }

        [HarmonyPatch(typeof(SongSelectScene), "_pN")]
        private static class SelectionDifficultyPatch
        {
            [HarmonyPostfix]
            private static void Postfix(SongSelectScene __instance)
            {
                try { RefreshScene(__instance); }
                catch (Exception e)
                {
                    Guard("selection difficulty", e);
                }
            }
        }

        [HarmonyPatch(typeof(SongSelectScene), "_uN")]
        private static class SelectionDifficultyAltPatch
        {
            [HarmonyPostfix]
            private static void Postfix(SongSelectScene __instance)
            {
                try { RefreshScene(__instance); }
                catch (Exception e)
                {
                    Guard("selection difficulty alt", e);
                }
            }
        }

        private static SongSelectScene FindScene()
        {
            try
            {
                var scenes = UnityEngine.Object.FindObjectsOfType<
                    SongSelectScene>();
                if (scenes == null) return null;
                for (int i = 0; i < scenes.Length; i++)
                    if (scenes[i] != null &&
                        scenes[i].Pointer != IntPtr.Zero &&
                        scenes[i].gameObject.activeInHierarchy)
                        return scenes[i];
            }
            catch { }
            return null;
        }
    }
}
