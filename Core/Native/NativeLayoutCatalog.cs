// Code organization: native ABI layout catalog. Runtime behavior is unchanged.
using System;
using System.Text;
using Il2CppInterop.Runtime;

namespace InFalsusChartExt
{
    /// <summary>
    /// Central native ABI values shared by raw-access modules. Values here are
    /// build-layout data, not feature policy. Known builds are validated field
    /// by field before features that write through these offsets are enabled.
    /// </summary>
    internal static class NativeLayoutCatalog
    {
        internal static readonly int SideCount = Abi("NativeLayoutCatalog", "SideCount");
        internal static readonly int NoteStride = Abi("NativeLayoutCatalog", "NoteStride");
        internal static readonly int BoxedValueHeader = Abi("NativeLayoutCatalog", "BoxedValueHeader");
        internal static readonly int ArrayLengthOffset = Abi("NativeLayoutCatalog", "ArrayLengthOffset");
        internal static readonly int ArrayDataOffset = Abi("NativeLayoutCatalog", "ArrayDataOffset");

        internal static class Runtime
        {
            internal static readonly int CachedPointer = Abi("Runtime", "CachedPointer");
            internal static readonly int ListItems = Abi("Runtime", "ListItems");
            internal static readonly int ListCount = Abi("Runtime", "ListCount");
            internal static readonly int MemoryObject = Abi("Runtime", "MemoryObject");
            internal static readonly int MemoryIndex = Abi("Runtime", "MemoryIndex");
            internal static readonly int MemoryLength = Abi("Runtime", "MemoryLength");
            internal static readonly int OperationInternalHandle = Abi("Runtime", "OperationInternalHandle");
            internal static readonly int OperationVersion = Abi("Runtime", "OperationVersion");
            internal static readonly int OperationPadding = Abi("Runtime", "OperationPadding");
            internal static readonly int OperationLocationName = Abi("Runtime", "OperationLocationName");
        }

        internal static class CompiledSide
        {
            internal static readonly int PositionArray = Abi("CompiledSide", "PositionArray");
            internal static readonly int MemoryObject = Abi("CompiledSide", "MemoryObject");
            internal static readonly int MemoryIndex = Abi("CompiledSide", "MemoryIndex");
            internal static readonly int MemoryCount = Abi("CompiledSide", "MemoryCount");
        }

        internal static class Note
        {
            internal static readonly int Id = Abi("Note", "Id");
            internal static readonly int Group = Abi("Note", "Group");
            internal static readonly int Side = Abi("Note", "Side");
            internal static readonly int Type = Abi("Note", "Type");
            internal static readonly int StartMs = Abi("Note", "StartMs");
            internal static readonly int EndMs = Abi("Note", "EndMs");
            internal static readonly int PackedStart = Abi("Note", "PackedStart");
            internal static readonly int PackedEnd = Abi("Note", "PackedEnd");
            internal static readonly int PackedStartWidth = Abi("Note", "PackedStartWidth");
            internal static readonly int PackedEndWidth = Abi("Note", "PackedEndWidth");
            internal static readonly int Lane = Abi("Note", "Lane");
            internal static readonly int StartCenter = Abi("Note", "StartCenter");
            internal static readonly int EndCenter = Abi("Note", "EndCenter");
            internal static readonly int StartWidth = Abi("Note", "StartWidth");
            internal static readonly int EndWidth = Abi("Note", "EndWidth");
            internal static readonly int RuntimeLead = Abi("Note", "RuntimeLead");
            internal static readonly int Consumed = Abi("Note", "Consumed");
            internal static readonly int Flags = Abi("Note", "Flags");
            internal static readonly int RecordStart = Abi("Note", "RecordStart");
            internal static readonly int RecordEnd = Abi("Note", "RecordEnd");
        }

        internal static class Compiled
        {
            internal static readonly int RecordDictionary = Abi("Compiled", "RecordDictionary");
            internal static readonly int WarpMemoryObject = Abi("Compiled", "WarpMemoryObject");
            internal static readonly int WarpMemoryIndex = Abi("Compiled", "WarpMemoryIndex");
            internal static readonly int WarpMemoryLength = Abi("Compiled", "WarpMemoryLength");
            internal static readonly int WarpEntryStride = Abi("Compiled", "WarpEntryStride");
            internal static readonly int WarpEntryEnd = Abi("Compiled", "WarpEntryEnd");
        }

        internal static class Record
        {
            internal static readonly int Stride = Abi("Record", "Stride");
            internal static readonly int GlobalIndex = Abi("Record", "GlobalIndex");
            internal static readonly int RelativeTime = Abi("Record", "RelativeTime");
            internal static readonly int Judgment = Abi("Record", "Judgment");
            internal static readonly int Final = Abi("Record", "Final");
            internal static readonly int ConversionMarker = Abi("Record", "ConversionMarker");
        }

        internal static class SkyJudge
        {
            // _pEA input-contact states and _qEA per-record continuation bits
            // consumed by _pz. Array data begins at ArrayDataOffset.
            internal static readonly int InputStates = Abi("SkyJudge", "InputStates");
            internal static readonly int RecordStates = Abi("SkyJudge", "RecordStates");
            internal static readonly int InputStateStride = Abi("SkyJudge", "InputStateStride");
            internal static readonly int InputHeld = Abi("SkyJudge", "InputHeld");
            internal static readonly int InputPressed = Abi("SkyJudge", "InputPressed");
            internal static readonly int InputConsumed = Abi("SkyJudge", "InputConsumed");
            internal static readonly int InputPressedAt = Abi("SkyJudge", "InputPressedAt");
            internal static readonly int RecordStateContinuation = Abi("SkyJudge", "RecordStateContinuation");
            // Current horizontal position of the native sky cursor.
            internal static readonly int CursorX = Abi("SkyJudge", "CursorX");
            // Backing field for _VD._c, the chart clock in seconds.
            internal static readonly int ChartTime = Abi("SkyJudge", "ChartTime");
            // _VD._OEA sky coverage protection and current space-covered state.
            internal static readonly int WarningTime = Abi("SkyJudge", "WarningTime");
            internal static readonly int PenaltyTime = Abi("SkyJudge", "PenaltyTime");
            internal static readonly int SpaceCovered = Abi("SkyJudge", "SpaceCovered");
            // Queue<ValueTuple<long,int,int,long,Vector3>> filled by _Oz
            // and consumed by _Tz before GameScene._Mk.
            internal static readonly int PendingPresentation = Abi("SkyJudge", "PendingPresentation");
        }

        internal static class GameSceneAudio
        {
            // Native _Sh event layout dispatched by GameScene._Mk.
            internal static readonly int ShStride = Abi("GameSceneAudio", "ShStride");
            internal static readonly int ShType = Abi("GameSceneAudio", "ShType");
            internal static readonly int ShBool = Abi("GameSceneAudio", "ShBool");
            internal static readonly int HoldBus = Abi("GameSceneAudio", "HoldBus");
            internal static readonly int HoldBusRunning = Abi("GameSceneAudio", "HoldBusRunning");
        }

        internal static class Track
        {
            internal static readonly int RightLaneElements = Abi("Track", "RightLaneElements");
            internal static readonly int RightLaneAnchor = Abi("Track", "RightLaneAnchor");
            internal static readonly int RightLaneRenderer = Abi("Track", "RightLaneRenderer");
            internal static readonly int LeftLaneRenderer = Abi("Track", "LeftLaneRenderer");
            internal static readonly int LeftLaneJudgementLine = Abi("Track", "LeftLaneJudgementLine");
            internal static readonly int MasterScene = Abi("Track", "MasterScene");
            internal static readonly int SkyCursor = Abi("Track", "SkyCursor");
            internal static readonly int SkyCursorRenderer = Abi("Track", "SkyCursorRenderer");
            internal static readonly int SkyGhostCursor = Abi("Track", "SkyGhostCursor");
            internal static readonly int CursorExtrasContainer = Abi("Track", "CursorExtrasContainer");
            internal static readonly int SkyJudgementLineGlowRenderer = Abi("Track", "SkyJudgementLineGlowRenderer");
            internal static readonly int NotesContainer = Abi("Track", "NotesContainer");
            internal static readonly int LeftWallRenderer = Abi("Track", "LeftWallRenderer");
            internal static readonly int RightWallRenderer = Abi("Track", "RightWallRenderer");
            internal static readonly int RuntimeRenderers = Abi("Track", "RuntimeRenderers");
            internal static readonly int HomePosition = Abi("Track", "HomePosition");
            internal static readonly int CenterRotation = Abi("Track", "CenterRotation");
            internal static readonly int HoldRotation = Abi("Track", "HoldRotation");
            internal static readonly int RestPosition = Abi("Track", "RestPosition");
            internal static readonly int RestRotation = Abi("Track", "RestRotation");
            internal static readonly int SideLeftPosition = Abi("Track", "SideLeftPosition");
            internal static readonly int SideLeftRotation = Abi("Track", "SideLeftRotation");
            internal static readonly int SideRightPosition = Abi("Track", "SideRightPosition");
            internal static readonly int SideRightRotation = Abi("Track", "SideRightRotation");
            internal static readonly int CenterScaleX = Abi("Track", "CenterScaleX");
            internal static readonly int CenterScaleY = Abi("Track", "CenterScaleY");
            internal static readonly int CenterScaleZ = Abi("Track", "CenterScaleZ");
            internal static readonly int SideScaleVector = Abi("Track", "SideScaleVector");
            internal static readonly int SideScaleY = Abi("Track", "SideScaleY");
            internal static readonly int SideScaleZ = Abi("Track", "SideScaleZ");
            internal static readonly int LaneCenters = Abi("Track", "LaneCenters");
            internal static readonly int TrackWidth = Abi("Track", "TrackWidth");
            internal static readonly int NoteWidthScale = Abi("Track", "NoteWidthScale");
            internal static readonly int BaseSpeed = Abi("Track", "BaseSpeed");
            internal static readonly int DepthFactor = Abi("Track", "DepthFactor");
            internal static readonly int LaneRenderers = Abi("Track", "LaneRenderers");
        }

        internal static class SkySafeArea
        {
            internal static readonly int MeshRenderer = Abi("SkySafeArea", "MeshRenderer");
            internal static readonly int ColorDetails = Abi("SkySafeArea", "ColorDetails");
            internal static readonly int NativeNoteId = Abi("SkySafeArea", "NativeNoteId");
        }

        internal static class SafeAreaColorDetails
        {
            internal static readonly int MainDefault = Abi("SafeAreaColorDetails", "MainDefault");
            internal static readonly int EdgeCenterDefault = Abi("SafeAreaColorDetails", "EdgeCenterDefault");
            internal static readonly int EdgeGlowDefault = Abi("SafeAreaColorDetails", "EdgeGlowDefault");
            internal static readonly int FrontBackDefault = Abi("SafeAreaColorDetails", "FrontBackDefault");
            internal static readonly int MainRed = Abi("SafeAreaColorDetails", "MainRed");
            internal static readonly int EdgeCenterRed = Abi("SafeAreaColorDetails", "EdgeCenterRed");
            internal static readonly int EdgeGlowRed = Abi("SafeAreaColorDetails", "EdgeGlowRed");
            internal static readonly int FrontBackRed = Abi("SafeAreaColorDetails", "FrontBackRed");
            internal static readonly int ColorStride = Abi("SafeAreaColorDetails", "ColorStride");
        }

        internal static class Background
        {
            internal static readonly int OutputQuad = Abi("Background", "OutputQuad");
            internal static readonly int ConstrainedMaterial = Abi("Background", "ConstrainedMaterial");
        }

        internal static class FlickNote
        {
            internal static readonly int BackingMeshRenderer = Abi("FlickNote", "BackingMeshRenderer");
        }

        internal static class TapNote
        {
            internal static readonly int MeshRenderer = Abi("TapNote", "MeshRenderer");
            internal static readonly int LeftGuideObjects = Abi("TapNote", "LeftGuideObjects");
            internal static readonly int RightGuideObjects = Abi("TapNote", "RightGuideObjects");
            internal static readonly int RuntimeMaterial = Abi("TapNote", "RuntimeMaterial");
        }

        internal static class Audio
        {
            internal static readonly int Channel = Abi("Audio", "Channel");
        }

        internal static class CustomSong
        {
            internal static readonly int SongArray = Abi("CustomSong", "SongArray");
            internal static readonly int SongIdJacketArray = Abi("CustomSong", "SongIdJacketArray");
            internal static readonly int ChartIdJacketArray = Abi("CustomSong", "ChartIdJacketArray");
            internal static readonly int SongIdJacketCache = Abi("CustomSong", "SongIdJacketCache");
            internal static readonly int ChartIdJacketCache = Abi("CustomSong", "ChartIdJacketCache");
            internal static readonly int AudioChannel = Abi("CustomSong", "AudioChannel");
            internal static readonly int AudioSound = Abi("CustomSong", "AudioSound");
            internal static readonly int AudioPreviewStart = Abi("CustomSong", "AudioPreviewStart");
            internal static readonly int AudioPreviewEnd = Abi("CustomSong", "AudioPreviewEnd");
            internal static readonly int AudioSelectorCallback = Abi("CustomSong", "AudioSelectorCallback");
            internal static readonly int AudioPaused = Abi("CustomSong", "AudioPaused");
            internal static readonly int AudioSkipPreviewSeek = Abi("CustomSong", "AudioSkipPreviewSeek");
            internal static readonly int AudioConfigured = Abi("CustomSong", "AudioConfigured");
        }

        internal static class Result
        {
            internal static readonly int Pace = Abi("Result", "Pace");
            internal static readonly int TapCounts = Abi("Result", "TapCounts");
            internal static readonly int HoldCounts = Abi("Result", "HoldCounts");
            internal static readonly int SkyAreaCounts = Abi("Result", "SkyAreaCounts");
            internal static readonly int FlickCounts = Abi("Result", "FlickCounts");
            internal static readonly int PlayerScore = Abi("Result", "PlayerScore");
        }

        internal static class Presentation
        {
            internal static readonly int Stride =
                Abi("Presentation", "Stride");
            internal static readonly int NoteId =
                Abi("Presentation", "NoteId");
            internal static readonly int RecordStart =
                Abi("Presentation", "RecordStart");
            internal static readonly int RecordCount =
                Abi("Presentation", "RecordCount");
            internal static readonly int RecordIndex =
                Abi("Presentation", "RecordIndex");
            internal static readonly int PositionX =
                Abi("Presentation", "PositionX");
            internal static readonly int PositionY =
                Abi("Presentation", "PositionY");
            internal static readonly int PositionZ =
                Abi("Presentation", "PositionZ");
            internal static readonly int EnqueueParameterSize =
                Abi("Presentation", "EnqueueParameterSize");

            internal static string EnqueueMethodName =>
                NativeAbiProfileStore.Selected?.Presentation
                    .EnqueueMethodName;
        }

        internal static class Capacity
        {
            internal static readonly int OriginalSideNote = Abi("Capacity", "OriginalSideNote");
            internal static readonly int OriginalAuxTable = Abi("Capacity", "OriginalAuxTable");
            internal static readonly int OriginalRecord = Abi("Capacity", "OriginalRecord");
            internal static readonly int OriginalJudgeState = Abi("Capacity", "OriginalJudgeState");
        }

        private static int Abi(string section, string key)
        {
            NativeAbiProfile profile =
                NativeAbiProfileStore.Selected;
            if (profile is null)
                return 0;
            try
            {
                return profile.Int(section, key);
            }
            catch
            {
                // Profile validation is completed by BuildProfile. Returning
                // zero keeps static initialization fail-safe so the caller can
                // disable native-write capabilities instead of crashing the
                // MelonLoader assembly at type initialization.
                return 0;
            }
        }

        private static bool _validationAttempted;
        private static bool _validated;
        private static string _validationError;

        internal static bool ValidateCurrentBuild(out string error)
        {
            if (_validationAttempted)
            {
                error = _validationError;
                return _validated;
            }
            _validationAttempted = true;

            var failures = new StringBuilder();
            bool ok = true;

            try
            {
                if (IntPtr.Size != 8)
                {
                    ok = false;
                    AddFailure(
                        "process", $"expected x64 pointers, got {IntPtr.Size * 8}-bit",
                        failures);
                }

                IntPtr noteClass = OpenClass<Il2Cpp_b._fA>();
                int boxed = BoxedValueHeader;
                ok &= Field(noteClass, "_ZD", boxed + Note.Id, "_fA._ZD", failures);
                ok &= Field(noteClass, "_ae", boxed + Note.Group, "_fA._ae", failures);
                ok &= Field(noteClass, "_Ae", boxed + Note.Side, "_fA._Ae", failures);
                ok &= Field(noteClass, "_be", boxed + Note.Type, "_fA._be", failures);
                ok &= Field(noteClass, "_Be", boxed + Note.StartMs, "_fA._Be", failures);
                ok &= Field(noteClass, "_ce", boxed + Note.EndMs, "_fA._ce", failures);
                ok &= Field(noteClass, "_Ce", boxed + Note.PackedStart, "_fA._Ce", failures);
                ok &= Field(noteClass, "_de", boxed + Note.PackedEnd, "_fA._de", failures);
                ok &= Field(noteClass, "_De", boxed + Note.PackedStartWidth, "_fA._De", failures);
                ok &= Field(noteClass, "_ee", boxed + Note.PackedEndWidth, "_fA._ee", failures);
                ok &= Field(noteClass, "_Ee", boxed + Note.RuntimeLead, "_fA._Ee", failures);
                ok &= Field(noteClass, "_Ge", boxed + Note.Consumed, "_fA._Ge", failures);
                ok &= Field(noteClass, "_he", boxed + Note.Flags, "_fA._he", failures);
                ok &= Field(noteClass, "_He", boxed + Note.RecordStart, "_fA._He", failures);
                ok &= Field(noteClass, "_ie", boxed + Note.RecordEnd, "_fA._ie", failures);
                ok &= ValueSize(noteClass, NoteStride, "_fA", failures);

                IntPtr sideClass = OpenClass<Il2Cpp_b._hA>();
                ok &= Field(sideClass, "_ye", CompiledSide.PositionArray, "_hA._ye", failures);
                ok &= Field(sideClass, "_Ye", CompiledSide.MemoryObject, "_hA._Ye", failures);
                ok &= Field(sideClass, "_ze", CompiledSide.MemoryCount, "_hA._ze", failures);

                IntPtr compiledClass = OpenClass<Il2Cpp_b._T>();
                ok &= Field(compiledClass, "_XC", Compiled.RecordDictionary, "_T._XC", failures);
                ok &= Field(compiledClass, "_ZC", Compiled.WarpMemoryObject, "_T._ZC", failures);
                IntPtr warpEntryClass = OpenClass<Il2Cpp_b._IA>();
                ok &= Field(warpEntryClass, "_CE", boxed, "_IA._CE", failures);
                ok &= Field(warpEntryClass, "_dE",
                    boxed + Compiled.WarpEntryEnd, "_IA._dE", failures);
                ok &= ValueSize(
                    warpEntryClass, Compiled.WarpEntryStride, "_IA", failures);

                IntPtr recordClass = OpenClass<Il2Cpp_K._cH>();
                ok &= Field(recordClass, "_Tdb", boxed + Record.GlobalIndex, "_cH._Tdb", failures);
                ok &= Field(recordClass, "_udb", boxed + Record.RelativeTime, "_cH._udb", failures);
                ok &= Field(recordClass, "_wdb", boxed + Record.Judgment, "_cH._wdb", failures);
                ok &= Field(recordClass, "_Wdb", boxed + Record.Final, "_cH._Wdb", failures);
                ok &= ValueSize(recordClass, Record.Stride, "_cH", failures);

                IntPtr judgeClass = OpenClass<Il2Cpp_E._VD>();
                ok &= Field(judgeClass, "_tEA",
                    SkyJudge.CursorX, "_VD._tEA", failures);
                ok &= Field(judgeClass, "_WEA",
                    SkyJudge.ChartTime, "_VD._WEA", failures);
                ok &= Field(judgeClass, "_PEA",
                    SkyJudge.PendingPresentation,
                    "_VD._PEA", failures);
                ok &= Field(judgeClass, "_pEA",
                    SkyJudge.InputStates,
                    "_VD._pEA", failures);
                ok &= Field(judgeClass, "_qEA",
                    SkyJudge.RecordStates,
                    "_VD._qEA", failures);

                IntPtr trackClass =
                    OpenClass<Il2Cppifapp.Game.Scenes.Game.Track>();
                ok &= Field(trackClass, "rightLaneElements", Track.RightLaneElements, "Track.rightLaneElements", failures);
                ok &= Field(trackClass, "leftLane", Track.LeftLaneRenderer, "Track.leftLane", failures);
                ok &= Field(
                    trackClass,
                    "leftLaneJudgementLine",
                    Track.LeftLaneJudgementLine,
                    "Track.leftLaneJudgementLine",
                    failures);
                ok &= Field(trackClass, "masterScene", Track.MasterScene, "Track.masterScene", failures);
                ok &= Field(trackClass, "skyCursor", Track.SkyCursor, "Track.skyCursor", failures);
                ok &= Field(trackClass, "skyCursorMeshRenderer", Track.SkyCursorRenderer, "Track.skyCursorMeshRenderer", failures);
                ok &= Field(trackClass, "skyGhostCursor", Track.SkyGhostCursor, "Track.skyGhostCursor", failures);
                ok &= Field(trackClass, "cursorExtrasContainer", Track.CursorExtrasContainer, "Track.cursorExtrasContainer", failures);
                ok &= Field(trackClass, "skyJudgementLineGlowRenderer", Track.SkyJudgementLineGlowRenderer, "Track.skyJudgementLineGlowRenderer", failures);
                ok &= Field(trackClass, "notesContainer", Track.NotesContainer, "Track.notesContainer", failures);
                ok &= Field(trackClass, "leftWallRenderer", Track.LeftWallRenderer, "Track.leftWallRenderer", failures);
                ok &= Field(trackClass, "rightWallRenderer", Track.RightWallRenderer, "Track.rightWallRenderer", failures);
                ok &= Field(trackClass, "_MMA", Track.RuntimeRenderers, "Track._MMA", failures);
                ok &= Field(trackClass, "_ZMA", Track.HomePosition, "Track._ZMA", failures);
                ok &= Field(trackClass, "_anA", Track.CenterRotation, "Track._anA", failures);
                ok &= Field(trackClass, "_AnA", Track.HoldRotation, "Track._AnA", failures);
                ok &= Field(trackClass, "_enA", Track.RestPosition, "Track._enA", failures);
                ok &= Field(trackClass, "_EnA", Track.RestRotation, "Track._EnA", failures);
                ok &= Field(trackClass, "_FnA", Track.SideLeftPosition, "Track._FnA", failures);
                ok &= Field(trackClass, "_gnA", Track.SideLeftRotation, "Track._gnA", failures);
                ok &= Field(trackClass, "_GnA", Track.SideRightPosition, "Track._GnA", failures);
                ok &= Field(trackClass, "_hnA", Track.SideRightRotation, "Track._hnA", failures);
                ok &= Field(trackClass, "_InA", Track.CenterScaleX, "Track._InA", failures);
                ok &= Field(trackClass, "_jnA", Track.SideScaleVector, "Track._jnA", failures);
                ok &= Field(trackClass, "_MnA", Track.LaneCenters, "Track._MnA", failures);
                ok &= Field(trackClass, "_nnA", Track.TrackWidth, "Track._nnA", failures);
                ok &= Field(trackClass, "_NnA", Track.NoteWidthScale, "Track._NnA", failures);
                ok &= Field(trackClass, "_qnA", Track.BaseSpeed, "Track._qnA", failures);
                ok &= Field(trackClass, "_RnA", Track.DepthFactor, "Track._RnA", failures);
                ok &= Field(trackClass, "_wnA", Track.LaneRenderers, "Track._wnA", failures);

                IntPtr skyClass =
                    OpenClass<Il2Cppifapp.Game.Scenes.Game.SkySafeArea>();
                ok &= Field(skyClass, "meshRenderer", SkySafeArea.MeshRenderer, "SkySafeArea.meshRenderer", failures);
                ok &= Field(skyClass, "colorDetails", SkySafeArea.ColorDetails, "SkySafeArea.colorDetails", failures);
                ok &= Field(skyClass, "_aFA", SkySafeArea.NativeNoteId, "SkySafeArea._aFA", failures);

                IntPtr colorClass =
                    OpenClass<Il2Cppifapp.Game.Scenes.Game.SafeAreaColorDetails>();
                ok &= Field(colorClass, "MainColorDefault", SafeAreaColorDetails.MainDefault, "SafeAreaColorDetails.MainColorDefault", failures);
                ok &= Field(colorClass, "EdgeCenterColorDefault", SafeAreaColorDetails.EdgeCenterDefault, "SafeAreaColorDetails.EdgeCenterColorDefault", failures);
                ok &= Field(colorClass, "EdgeGlowColorDefault", SafeAreaColorDetails.EdgeGlowDefault, "SafeAreaColorDetails.EdgeGlowColorDefault", failures);
                ok &= Field(colorClass, "FrontBackDefault", SafeAreaColorDetails.FrontBackDefault, "SafeAreaColorDetails.FrontBackDefault", failures);
                ok &= Field(colorClass, "MainColorRed", SafeAreaColorDetails.MainRed, "SafeAreaColorDetails.MainColorRed", failures);
                ok &= Field(colorClass, "EdgeCenterColorRed", SafeAreaColorDetails.EdgeCenterRed, "SafeAreaColorDetails.EdgeCenterColorRed", failures);
                ok &= Field(colorClass, "EdgeGlowColorRed", SafeAreaColorDetails.EdgeGlowRed, "SafeAreaColorDetails.EdgeGlowRed", failures);
                ok &= Field(colorClass, "FrontBackRed", SafeAreaColorDetails.FrontBackRed, "SafeAreaColorDetails.FrontBackRed", failures);

                IntPtr backgroundClass = OpenClass<Il2Cpp.GenericGameplayBackground>();
                ok &= Field(backgroundClass, "outputQuad", Background.OutputQuad, "GenericGameplayBackground.outputQuad", failures);

                IntPtr flickNoteClass = OpenClass<Il2Cpp.FlickNoteV3>();
                ok &= Field(flickNoteClass, "backingMeshRenderer", FlickNote.BackingMeshRenderer, "FlickNoteV3.backingMeshRenderer", failures);

                IntPtr tapNoteClass =
                    OpenClass<Il2Cppifapp.Game.Scenes.Game.TapNote>();
                ok &= Field(tapNoteClass, "meshRenderer", TapNote.MeshRenderer, "TapNote.meshRenderer", failures);
                ok &= Field(tapNoteClass, "leftGuideObjects", TapNote.LeftGuideObjects, "TapNote.leftGuideObjects", failures);
                ok &= Field(tapNoteClass, "rightGuideObjects", TapNote.RightGuideObjects, "TapNote.rightGuideObjects", failures);
                ok &= Field(tapNoteClass, "_bMA", TapNote.RuntimeMaterial, "TapNote._bMA", failures);

                IntPtr audioClass = OpenClass<Il2Cpp_J._Cg>();
                ok &= Field(audioClass, "_rUA", Audio.Channel, "_Cg._rUA", failures);

                IntPtr resultClass =
                    OpenClass<Il2Cppifapp.Game.Data.GameResultV4>();
                ok &= Field(resultClass, "CalculatedPaceValue", boxed + Result.Pace, "GameResultV4.CalculatedPaceValue", failures);
                ok &= Field(resultClass, "TapCounts", boxed + Result.TapCounts, "GameResultV4.TapCounts", failures);
                ok &= Field(resultClass, "HoldCounts", boxed + Result.HoldCounts, "GameResultV4.HoldCounts", failures);
                ok &= Field(resultClass, "SkyAreaCounts", boxed + Result.SkyAreaCounts, "GameResultV4.SkyAreaCounts", failures);
                ok &= Field(resultClass, "FlickCounts", boxed + Result.FlickCounts, "GameResultV4.FlickCounts", failures);
                ok &= Field(resultClass, "PlayerScore", boxed + Result.PlayerScore, "GameResultV4.PlayerScore", failures);

                IntPtr songDataClass =
                    OpenClass<Il2Cppifapp.Game.Data.SongData>();
                ok &= Field(songDataClass, "allSongInfo",
                    CustomSong.SongArray, "SongData.allSongInfo", failures);
                ok &= Field(songDataClass, "songIdJacketMaterials",
                    CustomSong.SongIdJacketArray,
                    "SongData.songIdJacketMaterials", failures);
                ok &= Field(songDataClass, "chartIdJacketMaterials",
                    CustomSong.ChartIdJacketArray,
                    "SongData.chartIdJacketMaterials", failures);
                ok &= Field(songDataClass, "_Dfb",
                    CustomSong.SongIdJacketCache,
                    "SongData._Dfb", failures);
                ok &= Field(songDataClass, "_efb",
                    CustomSong.ChartIdJacketCache,
                    "SongData._efb", failures);

                IntPtr customAudioClass = OpenClass<Il2Cpp_J._Cg>();
                ok &= Field(customAudioClass, "_rUA",
                    CustomSong.AudioChannel, "_Cg._rUA", failures);
                ok &= Field(customAudioClass, "_sUA",
                    CustomSong.AudioSound, "_Cg._sUA", failures);
                ok &= Field(customAudioClass, "_TUA",
                    CustomSong.AudioConfigured,
                    "_Cg._TUA", failures);

                // Constrained2D._H is a property whose generated proxy type is
                // intentionally absent. Verify the owning class exists, while the
                // offset remains documented in Background.ConstrainedMaterial.
                if (OpenClassAny(
                        new[] { "Game.UI.Common.dll", "Game.Common.dll", "Game.dll" },
                        "ifapp.Game.UI.Common",
                        "Constrained2D", out string classError) == IntPtr.Zero)
                    AddFailure("Constrained2D", classError, failures);

                if (ok && !InteropUtil.ResolveRecordLayout())
                {
                    ok = false;
                    AddFailure(
                        "record runtime layout",
                        "InteropUtil could not resolve the live _cH record layout",
                        failures);
                }
            }
            catch (Exception e)
            {
                ok = false;
                AddFailure("validation", e.Message, failures);
            }

            _validated = ok;
            _validationError = ok ? null : failures.ToString().Trim();
            error = _validationError;
            if (ok) BuildProfile.MarkCoreAbiValidated();
            return ok;
        }

        private static IntPtr OpenClass<T>()
        {
            IntPtr klass = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (klass != IntPtr.Zero)
                IL2CPP.il2cpp_runtime_class_init(klass);
            return klass;
        }

        private static IntPtr OpenClassAny(
            string[] assemblies,
            string namespaceName,
            string className,
            out string error)
        {
            error = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    IntPtr klass = IL2CPP.GetIl2CppClass(
                        assemblies[i], namespaceName, className);
                    if (klass != IntPtr.Zero) return klass;
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
            }
            if (string.IsNullOrEmpty(error))
                error = $"class {namespaceName}.{className} was not found";
            return IntPtr.Zero;
        }

        private static bool Field(
            IntPtr klass,
            string fieldName,
            int expectedOffset,
            string label,
            StringBuilder failures)
        {
            if (klass == IntPtr.Zero)
            {
                AddFailure(label, "owning class was not found", failures);
                return false;
            }
            IntPtr field = IL2CPP.GetIl2CppField(klass, fieldName);
            if (field == IntPtr.Zero)
            {
                AddFailure(label, "field was not found", failures);
                return false;
            }
            int actual = (int)IL2CPP.il2cpp_field_get_offset(field);
            if (actual == expectedOffset) return true;
            AddFailure(
                label,
                $"expected 0x{expectedOffset:X}, got 0x{actual:X}",
                failures);
            return false;
        }

        private static bool ValueSize(
            IntPtr klass,
            int expectedSize,
            string label,
            StringBuilder failures)
        {
            if (klass == IntPtr.Zero)
            {
                AddFailure(label, "class was not found", failures);
                return false;
            }
            uint alignment = 0;
            int actual = (int)IL2CPP.il2cpp_class_value_size(
                klass, ref alignment);
            if (actual == expectedSize) return true;
            AddFailure(
                label,
                $"expected value size 0x{expectedSize:X}, got 0x{actual:X}",
                failures);
            return false;
        }

        private static void AddFailure(
            string label,
            string reason,
            StringBuilder failures)
        {
            if (failures.Length > 0) failures.AppendLine();
            failures.Append(label).Append(": ").Append(reason);
        }
    }
}
