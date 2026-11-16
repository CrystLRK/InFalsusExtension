using System;
using System.Collections.Generic;
using System.IO;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Install;
using InFalsusChartExt.CustomSong.Lifecycle;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#if MELONLOADER
using Il2Cppifapp.Game.Data;
#elif BEPINEX
using ifapp.Game.Data;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace InFalsusChartExt.CustomSong.Assets
{
    internal static class JacketMaterialService
    {
        private static readonly Dictionary<IntPtr, OwnedMaterial> Owned =
            new Dictionary<IntPtr, OwnedMaterial>();
        private static readonly Dictionary<IntPtr, Material>
            TemplateMaterials =
            new Dictionary<IntPtr, Material>();
        private static readonly HashSet<IntPtr> TemplateFailures =
            new HashSet<IntPtr>();
        private static readonly List<DeferredSong> Pending =
            new List<DeferredSong>();

        internal static void RegisterLibrary(
            SongData data,
            CustomLibraryPlan plan)
        {
            Pending.Clear();
            if (!JacketAddressablesNativeHook.Install())
            {
                ChartExtMod.Log.Error(
                    "[CustomSong] custom jackets disabled because " +
                    "the marker hook is unavailable.");
                StartupLoadingScreen.MarkJacketsReady();
                return;
            }
            foreach (CustomPackPlan pack in plan.Packs)
            {
                foreach (CustomSongPlan song in pack.Songs)
                {
                    if (!HasAnyJacket(song))
                    {
                        continue;
                    }
                    Pending.Add(new DeferredSong(data, song));
                }
            }
            if (Pending.Count == 0)
                StartupLoadingScreen.MarkJacketsReady();
            else
                ChartExtMod.Log.Msg(
                    $"[CustomSong] jacket loading started: " +
                    $"{Pending.Count} song(s)");
        }

        internal static void Tick()
        {
            if (Pending.Count == 0) return;
            const int MaxAttempts = 8;
            const int MaxApplied = 2;
            int attempts = 0;
            int applied = 0;
            SongData refreshData = null;
            int i = Pending.Count - 1;
            while (i >= 0 &&
                   attempts < MaxAttempts &&
                   applied < MaxApplied)
            {
                attempts++;
                DeferredSong pending = Pending[i];
                if (pending.Deadline < DateTime.UtcNow)
                {
                    ChartExtMod.Log.Warning(
                        $"[CustomSong] jacket skipped after timeout: " +
                        pending.Song.Config.Id);
                    Pending.RemoveAt(i);
                    i--;
                    continue;
                }
                try
                {
                    StartupLoadingScreen.SetStatus(
                        "LOADING JACKETS: " +
                        pending.Song.Config.Id);
                    if (TryApply(pending))
                    {
                        Pending.RemoveAt(i);
                        applied++;
                        refreshData = pending.Data;
                    }
                }
                catch (Exception exception)
                {
                    Pending.RemoveAt(i);
                    ChartExtMod.Log.Error(
                        $"[CustomSong] jacket failed for " +
                        $"'{pending.Song.Config.Id}': {exception}");
                }
                i--;
            }
            if (refreshData is not null)
                SongLibraryInstaller.RefreshSongDataCaches(refreshData);
            if (Pending.Count == 0)
            {
                StartupLoadingScreen.MarkJacketsReady();
                ChartExtMod.Log.Msg(
                    "[CustomSong] jacket loading complete");
            }
        }

        internal static void Dispose()
        {
            Pending.Clear();
            foreach (OwnedMaterial material in Owned.Values)
            {
                JacketAddressablesNativeHook.Unregister(
                    material.Reference);
                if (material.Material is not null)
                    UnityEngine.Object.Destroy(
                        material.Material);
                if (material.Texture is not null)
                    UnityEngine.Object.Destroy(
                        material.Texture);
            }
            Owned.Clear();
            TemplateMaterials.Clear();
            TemplateFailures.Clear();
            JacketAddressablesNativeHook.Dispose();
        }

        private static bool TryApply(DeferredSong pending)
        {
            SongData data = pending.Data;
            if (data is null || data.Pointer == IntPtr.Zero)
                return false;
            SongJacketEntry selector =
                FindSelector(data, pending.Song.TemplateSongId);
            Material largeTemplate =
                Resolve(selector.JacketLargeMaterial);
            Material smallTemplate =
                Resolve(selector.JacketSmallMaterial);
            if (largeTemplate is null || smallTemplate is null)
                return false;

            var largeByPath =
                new Dictionary<string, PendingReference>(
                    StringComparer.OrdinalIgnoreCase);
            var smallByPath =
                new Dictionary<string, PendingReference>(
                    StringComparer.OrdinalIgnoreCase);
            bool committed = false;
            try
            {
                var largeBySlot =
                    new PendingReference[4];
                var smallBySlot =
                    new PendingReference[4];
                for (int slot = 0; slot < 4; slot++)
                {
                    largeBySlot[slot] = CreatePending(
                        pending.Song.JacketLargeFor(slot),
                        largeTemplate,
                        largeByPath);
                    smallBySlot[slot] = CreatePending(
                        pending.Song.JacketSmallFor(slot),
                        smallTemplate,
                        smallByPath);
                }
                PendingReference selectorLarge =
                    largeBySlot[0] ?? FirstPending(largeBySlot);
                PendingReference selectorSmall =
                    smallBySlot[0] ?? FirstPending(smallBySlot);
                AssetReferenceT<Material> largeReference =
                    selectorLarge?.Reference ??
                    selector.JacketLargeMaterial;
                AssetReferenceT<Material> smallReference =
                    selectorSmall?.Reference ??
                    selector.JacketSmallMaterial;

                var selectorEntries =
                    data.songIdJacketMaterials;
                int selectorIndex = FindSongEntry(
                    selectorEntries, pending.Song.RuntimeSongId);
                if (selectorIndex < 0)
                    throw new InvalidOperationException(
                        "custom song jacket slot is missing");
                SongJacketEntry selectorEntry =
                    selectorEntries[selectorIndex];
                selectorEntry.JacketLargeMaterial = largeReference;
                selectorEntry.JacketSmallMaterial = smallReference;
                selectorEntries[selectorIndex] = selectorEntry;
                data.songIdJacketMaterials = selectorEntries;

                var chartEntries =
                    data.chartIdJacketMaterials;
                foreach (CustomChartPlan chart in pending.Song.Charts)
                {
                    string parserStem =
                        Path.GetFileNameWithoutExtension(
                            chart.ParserKey);
                    foreach (string chartId in new[]
                             {
                                 chart.LogicalChartKey,
                                 parserStem,
                             })
                    {
                        int index = FindChartEntry(
                            chartEntries, chartId);
                        if (index < 0)
                            throw new InvalidOperationException(
                                "custom chart jacket slot is missing: " +
                                chartId);
                        SongDataChartJacketEntry entry =
                            chartEntries[index];
                        PendingReference chartLarge =
                            largeBySlot[chart.Slot] ??
                            selectorLarge;
                        PendingReference chartSmall =
                            smallBySlot[chart.Slot] ??
                            selectorSmall;
                        entry.JacketLargeMaterial =
                            chartLarge?.Reference ??
                            selector.JacketLargeMaterial;
                        entry.JacketSmallMaterial =
                            chartSmall?.Reference ??
                            selector.JacketSmallMaterial;
                        chartEntries[index] = entry;
                    }
                }
                data.chartIdJacketMaterials = chartEntries;
                foreach (PendingReference value
                         in largeByPath.Values)
                    Register(value);
                foreach (PendingReference value
                         in smallByPath.Values)
                    Register(value);
                committed = true;
                return true;
            }
            finally
            {
                if (!committed)
                {
                    foreach (PendingReference value
                             in largeByPath.Values)
                        UnregisterAndDispose(value);
                    foreach (PendingReference value
                             in smallByPath.Values)
                        UnregisterAndDispose(value);
                }
            }
        }

        private static bool HasAnyJacket(CustomSongPlan song)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                if (!string.IsNullOrWhiteSpace(
                        song.JacketLargeFor(slot)) ||
                    !string.IsNullOrWhiteSpace(
                        song.JacketSmallFor(slot)))
                    return true;
            }
            return false;
        }

        private static PendingReference FirstPending(
            PendingReference[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (values[i] is not null)
                    return values[i];
            return null;
        }

        private static PendingReference CreatePending(
            string path,
            Material template,
            Dictionary<string, PendingReference> cache)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            if (cache.TryGetValue(path, out PendingReference existing))
                return existing;
            PendingReference created = CreatePending(path, template);
            cache[path] = created;
            return created;
        }

        private static PendingReference CreatePending(
            string path,
            Material template)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            string marker = "ifce-" + Guid.NewGuid().ToString("N");
            Texture2D texture = PngImage.CreateTexture(
                File.ReadAllBytes(path));
            Material material = null;
            AssetReferenceT<Material> reference = null;
            try
            {
                material = new Material(template);
                material.mainTexture = texture;
                reference =
                    new AssetReferenceT<Material>(marker);
                object handleRoot =
                    JacketAddressablesNativeHook.Register(
                        reference,
                        material,
                        out JacketAddressablesNativeHook
                            .NativeAsyncOperationHandle handle);
                return new PendingReference(
                    reference,
                    material,
                    texture,
                    handleRoot,
                    handle);
            }
            catch
            {
                JacketAddressablesNativeHook.Unregister(reference);
                if (material is not null)
                    UnityEngine.Object.Destroy(material);
                UnityEngine.Object.Destroy(texture);
                throw;
            }
        }

        private static void Register(PendingReference pending)
        {
            if (pending is null) return;
            Owned[pending.Material.Pointer] = new OwnedMaterial(
                pending.Reference,
                pending.Material,
                pending.Texture,
                pending.HandleRoot);
            ChartExtMod.Log.Msg(
                "[CustomSong] registered native jacket marker " +
                Owned.Count + ".");
        }

        private static void UnregisterAndDispose(
            PendingReference pending)
        {
            if (pending is null) return;
            if (pending.Material is not null)
            {
                Owned.Remove(pending.Material.Pointer);
            }
            pending.Dispose();
        }

        private static Material Resolve(
            AssetReferenceT<Material> reference)
        {
            if (reference is null) return null;
            IntPtr key = reference.Pointer;
            if (key == IntPtr.Zero) return null;
            if (TemplateMaterials.TryGetValue(
                    key, out Material cached))
            {
                return cached;
            }
            try
            {
                Material asset = reference.Asset as Material;
                if (asset is not null &&
                    asset.Pointer != IntPtr.Zero)
                {
                    TemplateMaterials[key] = asset;
                    return asset;
                }
                if (TemplateFailures.Contains(key))
                    return null;
                AsyncOperationHandle<Material> operation =
                    reference.LoadAssetAsync();
                asset = operation.WaitForCompletion();
                if (asset is not null &&
                    asset.Pointer != IntPtr.Zero)
                {
                    TemplateMaterials[key] = asset;
                    return asset;
                }
                TemplateFailures.Add(key);
            }
            catch
            {
                TemplateFailures.Add(key);
            }
            return null;
        }

        private static SongJacketEntry FindSelector(
            SongData data,
            ushort templateSongId)
        {
            var entries = data.songIdJacketMaterials;
            int index = FindSongEntry(entries, templateSongId);
            if (index < 0)
                throw new InvalidOperationException(
                    "template selector jacket is missing");
            return entries[index];
        }

        private static int FindSongEntry(
            Il2CppReferenceArray<SongJacketEntry> entries,
            ushort songId)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].SongId.Value == songId)
                    return i;
            return -1;
        }

        private static int FindChartEntry(
            Il2CppReferenceArray<SongDataChartJacketEntry> entries,
            string chartId)
        {
            for (int i = 0; i < entries.Length; i++)
                if (string.Equals(
                        entries[i].ChartId,
                        chartId,
                        StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private sealed class DeferredSong
        {
            internal DeferredSong(
                SongData data,
                CustomSongPlan song)
            {
                Data = data;
                Song = song;
                Deadline = DateTime.UtcNow.AddSeconds(30);
            }

            internal SongData Data { get; }
            internal CustomSongPlan Song { get; }
            internal DateTime Deadline { get; }
        }

        private sealed class OwnedMaterial
        {
            internal OwnedMaterial(
                AssetReferenceT<Material> reference,
                Material material,
                Texture2D texture,
                object handleRoot)
            {
                Reference = reference;
                Material = material;
                Texture = texture;
                HandleRoot = handleRoot;
            }

            internal AssetReferenceT<Material> Reference { get; }
            internal Material Material { get; }
            internal Texture2D Texture { get; }
            internal object HandleRoot { get; }
        }

        private sealed class PendingReference
        {
            internal PendingReference(
                AssetReferenceT<Material> reference,
                Material material,
                Texture2D texture,
                object handleRoot,
                JacketAddressablesNativeHook
                    .NativeAsyncOperationHandle handle)
            {
                Reference = reference;
                Material = material;
                Texture = texture;
                HandleRoot = handleRoot;
                Handle = handle;
            }

            internal AssetReferenceT<Material> Reference { get; }
            internal Material Material { get; }
            internal Texture2D Texture { get; }
            internal object HandleRoot { get; }
            internal JacketAddressablesNativeHook
                .NativeAsyncOperationHandle Handle { get; }

            internal void Dispose()
            {
                JacketAddressablesNativeHook.Unregister(
                    Reference);
                if (Material is not null)
                    UnityEngine.Object.Destroy(Material);
                if (Texture is not null)
                    UnityEngine.Object.Destroy(Texture);
            }
        }
    }

}
