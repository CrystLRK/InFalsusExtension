using System;
using System.Collections.Generic;
using System.IO;
using InFalsusChartExt.CustomSong.Discovery;
using InFalsusChartExt.CustomSong.Assets;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#if MELONLOADER
using Il2Cppifapp.Game;
using Il2Cppifapp.Game.Data;
using Il2CppStr;
#elif BEPINEX
using ifapp.Game;
using ifapp.Game.Data;
using Str;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace InFalsusChartExt.CustomSong.Install
{
    internal static class PackAssetInstaller
    {
        private static bool _applied;
        private static IntPtr _appliedAssets;
        private static int _appliedLanguage = int.MinValue;
        private static readonly System.Collections.Generic.List<Owned>
            OwnedMaterials =
                new System.Collections.Generic.List<Owned>();

        internal static void Apply(
            PackSelectSceneAssets assets,
            CustomLibraryPlan plan)
        {
            if (assets is null || plan is null)
                return;
            if (_applied &&
                _appliedAssets == assets.Pointer &&
                _appliedLanguage == CurrentLanguage())
            {
                return;
            }
            ReleaseOwnedMaterials();

            Il2CppReferenceArray<
                PackSelectSceneAssets.SongSelectPackAssets> source =
                assets.packToAssets;
            if (source is null || source.Length == 0)
            {
                throw new InvalidOperationException(
                    "PackSelectSceneAssets has no stock styles");
            }

            int highestPack = 0;
            for (int i = 0; i < plan.Packs.Count; i++)
                highestPack = Math.Max(
                    highestPack,
                    plan.Packs[i].RuntimePackId);
            int length = Math.Max(source.Length, highestPack + 1);
            var next = new Il2CppReferenceArray<
                PackSelectSceneAssets.SongSelectPackAssets>(length);
            for (int i = 0; i < source.Length; i++)
                next[i] = source[i];

            foreach (CustomPackPlan pack in plan.Packs)
            {
                int styleIndex = FindStyleIndex(source, pack);
                PackSelectSceneAssets.SongSelectPackAssets style =
                    source[styleIndex];
                ApplyVisuals(
                    ref style,
                    pack,
                    source[styleIndex],
                    CurrentLanguage());
                next[pack.RuntimePackId] = style;
            }

            assets.packToAssets = next;
            _applied = true;
            _appliedAssets = assets.Pointer;
            _appliedLanguage = CurrentLanguage();
        }

        private static int FindStyleIndex(
            Il2CppReferenceArray<
                PackSelectSceneAssets.SongSelectPackAssets> styles,
            CustomPackPlan pack)
        {
            string slug = string.IsNullOrWhiteSpace(
                pack.Config.StylePackSlug)
                ? "act-4"
                : pack.Config.StylePackSlug;
            int stockPackId = FindStockPackId(slug);
            if (stockPackId >= 0 &&
                stockPackId < styles.Length)
            {
                return stockPackId;
            }
            if (styles.Length == 1)
                return 0;

            int fallback = ParseTrailingNumber(slug);
            if (fallback < 0 ||
                fallback >= styles.Length)
            {
                fallback = Math.Min(
                    Math.Max(0, pack.Config.SortOrder),
                    styles.Length - 1);
            }
            if (fallback < 0) fallback = 0;
            ChartExtMod.Log.Warning(
                $"[CustomSong] pack '{pack.Config.Id}' style " +
                $"'{slug}' resolved to stock style index {fallback}");
            return fallback;
        }

        private static int FindStockPackId(string slug)
        {
            Il2CppReferenceArray<Object> objects =
                Resources.FindObjectsOfTypeAll(
                    Il2CppType.Of<DataAccess>());
            for (int i = 0; i < objects.Length; i++)
            {
                DataAccess access =
                    objects[i].TryCast<DataAccess>();
                PackData packs = access?.PackData;
                if (packs?.PackInfo is null)
                    continue;
                for (int j = 0; j < packs.PackInfo.Length; j++)
                {
                    PackInfo info = packs.PackInfo[j];
                    if (string.Equals(
                            info.Slug,
                            slug,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return info.Id.Value;
                    }
                }
            }
            return -1;
        }

        internal static void Dispose()
        {
            ReleaseOwnedMaterials();
            _applied = false;
            _appliedAssets = IntPtr.Zero;
            _appliedLanguage = int.MinValue;
        }

        private static void ReleaseOwnedMaterials()
        {
            for (int i = 0; i < OwnedMaterials.Count; i++)
            {
                if (OwnedMaterials[i].Material is not null)
                    Object.Destroy(OwnedMaterials[i].Material);
                if (OwnedMaterials[i].Texture is not null)
                    Object.Destroy(OwnedMaterials[i].Texture);
            }
            OwnedMaterials.Clear();
        }

        private static void ApplyVisuals(
            ref PackSelectSceneAssets.SongSelectPackAssets style,
            CustomPackPlan pack,
            PackSelectSceneAssets.SongSelectPackAssets template,
            int language)
        {
            Material source = FirstMaterial(template);
            if (source is null)
            {
                ChartExtMod.Log.Warning(
                    "[CustomSong] pack jacket fallback has no " +
                    "template material");
                return;
            }

            var built = new Dictionary<string, Owned>(
                StringComparer.Ordinal);
            foreach (string key in VisualKeys)
            {
                string path = pack.Visual(key, language);
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                Owned owned = Build(source, path);
                built[key] = owned;
                OwnedMaterials.Add(owned);
            }

            Material MaterialFor(string key) =>
                built.TryGetValue(key, out Owned value)
                    ? value.Material
                    : null;

            Material songSelect = MaterialFor("songSelect");
            if (songSelect is not null)
            {
                style.SongSelectLargeNonCompleted = songSelect;
                style.SongSelectLargeCompleted = songSelect;
            }
            Material songSelectHover = MaterialFor("songSelectHover");
            if (songSelectHover is not null)
            {
                style.SongSelectLargeNonCompletedHover =
                    songSelectHover;
                style.SongSelectLargeCompletedHover =
                    songSelectHover;
            }
            Material selected = MaterialFor("selected");
            if (selected is not null)
            {
                style.PackSelectLargeNonCompleted = selected;
                style.PackSelectLargeCompleted = selected;
            }
            Material selectedHover = MaterialFor("selectedHover");
            if (selectedHover is not null)
            {
                style.PackSelectLargeNonCompletedHover =
                    selectedHover;
                style.PackSelectLargeCompletedHover =
                    selectedHover;
            }
            Material deselected = MaterialFor("deselected");
            if (deselected is not null)
            {
                style.PackSelectNonCompleted = deselected;
                style.PackSelectCompleted = deselected;
            }
            Material deselectedHover =
                MaterialFor("deselectedHover");
            if (deselectedHover is not null)
            {
                style.PackSelectNonCompletedHover =
                    deselectedHover;
                style.PackSelectCompletedHover =
                    deselectedHover;
            }
            Material outline = MaterialFor("outline");
            if (outline is not null)
            {
                style.OutlineNonCompleted = outline;
                style.OutlineCompleted = outline;
            }

            if (built.TryGetValue("songSelect", out Owned backdrop))
            {
                style.BackingImageNonCompleted =
                    backdrop.Material;
                style.BackingImageCompleted =
                    backdrop.Material;
            }
        }

        private static Owned Build(
            Material source,
            string path)
        {
            Texture2D texture = null;
            Material material = null;
            try
            {
                texture = PngImage.CreateTexture(
                    File.ReadAllBytes(path));
                material = new Material(source);
                material.mainTexture = texture;
                return new Owned(material, texture);
            }
            catch
            {
                if (material is not null)
                    Object.Destroy(material);
                if (texture is not null)
                    Object.Destroy(texture);
                throw;
            }
        }

        private static readonly string[] VisualKeys =
        {
            "songSelect",
            "songSelectHover",
            "selected",
            "selectedHover",
            "deselected",
            "deselectedHover",
            "outline",
        };

        private static int CurrentLanguage()
        {
            try
            {
                switch ((int)Strings.GetCurrentLocalization())
                {
                    case 0: return 2;
                    case 1: return 3;
                    case 2: return 4;
                    case 3: return 0;
                    case 4: return 1;
                    default: return CustomSongSchema.FallbackLanguage;
                }
            }
            catch
            {
                return CustomSongSchema.FallbackLanguage;
            }
        }

        private static Material FirstMaterial(
            PackSelectSceneAssets.SongSelectPackAssets style)
        {
            foreach (Material material in new[]
                     {
                         style.PackSelectLargeNonCompleted,
                         style.PackSelectNonCompleted,
                         style.BackingImageNonCompleted,
                         style.OutlineNonCompleted,
                         style.SongSelectLargeNonCompleted,
                     })
            {
                if (material is not null)
                    return material;
            }
            return null;
        }

        private static int ParseTrailingNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return -1;
            int end = value.Length - 1;
            while (end >= 0 && char.IsDigit(value[end]))
                end--;
            if (end == value.Length - 1)
                return -1;
            return int.TryParse(
                value.Substring(end + 1),
                out int parsed)
                ? parsed
                : -1;
        }

        private sealed class Owned
        {
            internal Owned(Material material, Texture2D texture)
            {
                Material = material;
                Texture = texture;
            }

            internal Material Material { get; }
            internal Texture2D Texture { get; }
        }
    }
}
