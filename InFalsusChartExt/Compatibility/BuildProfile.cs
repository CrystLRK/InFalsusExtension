// Game build compatibility profile and capability gates.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace InFalsusChartExt
{
    [Flags]
    internal enum NativeCapability : uint
    {
        None = 0,
        CapacityPatch = 1u << 0,
        ChartIngress = 1u << 1,
        TimingGroup = 1u << 2,
        SkyBlock = 1u << 3,
        BombVisual = 1u << 4,
        BombJudge = 1u << 5,
        Autoplay = 1u << 6,
        SkyColor = 1u << 7,
        SceneControl = 1u << 8,
        CustomSong = 1u << 9,
        Background = 1u << 10,
        JacketBridge = 1u << 11,
        TrackMove = 1u << 12,
        HideGroup = 1u << 13,
        NormalSkyEdge = 1u << 14,
        VisualNotes = 1u << 15,
        AudioOffset = 1u << 16,
        NativeRead = 1u << 17,
        SkyFeedback = 1u << 18,
        CustomScores = 1u << 19,
        AutoplayScoreGuard = 1u << 20,
        CustomDifficultyUnlock = 1u << 21,
    }

    internal enum NativeBuildKind
    {
        Unknown,
        Current,
        NewUnvalidated,
    }

    internal sealed class GameBuildProfile
    {
        internal string Id;
        internal NativeBuildKind Kind;
        internal string GameAssemblyHash;
        internal string MetadataHash;
        internal NativeCapability Capabilities;
        internal bool CoreAbiValidated;
        internal NativeAbiProfile Abi;

        internal bool Supports(NativeCapability capability)
            => (Capabilities & capability) == capability;

        internal bool SupportsCapacityPatch
            => Supports(NativeCapability.CapacityPatch);
        internal bool SupportsBackgroundNativeHook
            => Supports(NativeCapability.Background);
    }

    /// <summary>
    /// One process-wide description of the native game build. Feature modules
    /// consult this boundary before installing build-sensitive native hooks.
    /// </summary>
    internal static class BuildProfile
    {
        private static readonly Dictionary<string, string> HashCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> DisableReasons =
            new List<string>();
        private static GameBuildProfile _current;
        private static bool _logged;

        internal static GameBuildProfile Current
        {
            get
            {
                if (_current == null) _current = Detect();
                return _current;
            }
        }

        internal static void Initialize()
        {
            GameBuildProfile profile = Current;
            if (_logged) return;
            _logged = true;

            if (profile.Abi is not null &&
                profile.Capabilities != NativeCapability.None &&
                !NativeLayoutCatalog.ValidateCurrentBuild(out string validationError))
            {
                profile.Capabilities = NativeCapability.None;
                ChartExtMod.Log.Error(
                    "[BuildProfile] core ABI validation failed; native-write " +
                    "features disabled:\n" + validationError);
            }
            if (profile.Abi is null &&
                !string.IsNullOrWhiteSpace(
                    NativeAbiProfileStore.Error))
            {
                ChartExtMod.Log.Error(
                    "[BuildProfile] no usable ABI profile: " +
                    NativeAbiProfileStore.Error);
            }
            for (int i = 0; i < DisableReasons.Count; i++)
            {
                ChartExtMod.Log.Error(
                    "[BuildProfile] capability disable reason: " +
                    DisableReasons[i]);
            }
            DisableReasons.Clear();

            string message =
                $"[BuildProfile] id={profile.Id}, kind={profile.Kind}, " +
                $"game={ShortHash(profile.GameAssemblyHash)}, " +
                $"metadata={ShortHash(profile.MetadataHash)}, " +
                $"coreAbi={(profile.CoreAbiValidated ? "valid" : "pending")}, " +
                $"capabilities=0x{(uint)profile.Capabilities:X8}, " +
                $"capacity={profile.SupportsCapacityPatch}, " +
                $"chartIngress={Supports(NativeCapability.ChartIngress)}, " +
                $"timingGroup={Supports(NativeCapability.TimingGroup)}, " +
                $"skyBlock={Supports(NativeCapability.SkyBlock)}, " +
                $"bombVisual={Supports(NativeCapability.BombVisual)}, " +
                $"bombJudge={Supports(NativeCapability.BombJudge)}, " +
                $"autoplay={Supports(NativeCapability.Autoplay)}, " +
                $"skyColor={Supports(NativeCapability.SkyColor)}, " +
                $"sceneControl={Supports(NativeCapability.SceneControl)}, " +
                $"customSong={Supports(NativeCapability.CustomSong)}, " +
                $"background={Supports(NativeCapability.Background)}, " +
                $"jacketBridge={Supports(NativeCapability.JacketBridge)}, " +
                $"trackMove={Supports(NativeCapability.TrackMove)}, " +
                $"hideGroup={Supports(NativeCapability.HideGroup)}, " +
                $"normalSkyEdge={Supports(NativeCapability.NormalSkyEdge)}, " +
                $"visualNotes={Supports(NativeCapability.VisualNotes)}, " +
                $"audioOffset={Supports(NativeCapability.AudioOffset)}, " +
                $"nativeRead={Supports(NativeCapability.NativeRead)}, " +
                $"skyFeedback={Supports(NativeCapability.SkyFeedback)}, " +
                $"customScores={Supports(NativeCapability.CustomScores)}, " +
                $"autoplayScoreGuard={Supports(NativeCapability.AutoplayScoreGuard)}, " +
                $"customDifficultyUnlock={Supports(NativeCapability.CustomDifficultyUnlock)}";
            if (profile.Kind == NativeBuildKind.Unknown)
                ChartExtMod.Log.Warning(message);
            else
                ChartExtMod.Log.Msg(message);
        }

        internal static void MarkCoreAbiValidated()
        {
            if (Current.CoreAbiValidated) return;
            Current.CoreAbiValidated = true;
            try
            {
                ChartExtMod.Log.Msg("[BuildProfile] core ABI layout validated");
            }
            catch { }
        }

        internal static bool Supports(NativeCapability capability)
            => Current.Supports(capability);

        internal static bool SupportsNativeRead
            => Supports(NativeCapability.NativeRead);

        internal static void DisableCapabilities(
            NativeCapability capabilities,
            string reason)
        {
            if (capabilities == NativeCapability.None)
                return;
            GameBuildProfile profile = Current;
            NativeCapability removed =
                profile.Capabilities & capabilities;
            if (removed == NativeCapability.None)
                return;
            profile.Capabilities &= ~capabilities;
            DisableReasons.Add(reason);
            try
            {
                ChartExtMod.Log.Error(
                    "[BuildProfile] disabled capabilities " +
                    $"0x{(uint)removed:X8}: {reason}");
            }
            catch
            {
            }
        }

        private static GameBuildProfile Detect()
        {
            string root = GameRoot();
            string gameAssembly = Path.Combine(root, "GameAssembly.dll");
            string metadata = FindMetadata(root);
            string gameHash = HashFile(gameAssembly);
            string metadataHash = HashFile(metadata);

            NativeAbiProfile abi =
                NativeAbiProfileStore.Select(
                    gameHash, metadataHash);
            if (abi is not null)
            {
                return new GameBuildProfile
                {
                    Id = abi.Id,
                    Kind = ParseKind(abi.Kind),
                    GameAssemblyHash = gameHash,
                    MetadataHash = metadataHash,
                    Capabilities = abi.Capabilities,
                    Abi = abi,
                };
            }

            return new GameBuildProfile
            {
                Id = "unknown",
                Kind = NativeBuildKind.Unknown,
                GameAssemblyHash = gameHash,
                MetadataHash = metadataHash,
                Capabilities = NativeCapability.None,
            };
        }

        private static NativeBuildKind ParseKind(string kind)
        {
            if (string.Equals(
                    kind,
                    "current",
                    StringComparison.OrdinalIgnoreCase))
            {
                return NativeBuildKind.Current;
            }
            if (string.Equals(
                    kind,
                    "new",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    kind,
                    "newUnvalidated",
                    StringComparison.OrdinalIgnoreCase))
            {
                return NativeBuildKind.NewUnvalidated;
            }
            return NativeBuildKind.Unknown;
        }

        private static string GameRoot()
        {
            try
            {
                string mods = Path.GetDirectoryName(
                    typeof(ChartExtMod).Assembly.Location);
                return Path.GetFullPath(Path.Combine(mods, ".."));
            }
            catch
            {
                return Environment.CurrentDirectory;
            }
        }

        private static string FindMetadata(string root)
        {
            try
            {
                string direct = Path.Combine(
                    root, "infalsus_Data", "il2cpp_data",
                    "Metadata", "global-metadata.dat");
                if (File.Exists(direct)) return direct;
                foreach (string dataDir in Directory.GetDirectories(
                             root, "*_Data"))
                {
                    string candidate = Path.Combine(
                        dataDir, "il2cpp_data", "Metadata",
                        "global-metadata.dat");
                    if (File.Exists(candidate)) return candidate;
                }
            }
            catch { }
            return string.Empty;
        }

        private static string HashFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return string.Empty;
            if (HashCache.TryGetValue(path, out string cached))
                return cached;
            try
            {
                using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using SHA256 sha = SHA256.Create();
                string hash = Convert.ToHexString(sha.ComputeHash(stream));
                HashCache[path] = hash;
                return hash;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ShortHash(string value)
            => string.IsNullOrEmpty(value)
                ? "unavailable"
                : value.Substring(0, Math.Min(12, value.Length));
    }
}
