// Code organization: visual-note descriptors. Runtime behavior is unchanged.
using System;

namespace InFalsusChartExt
{
    internal enum VisualNoteKind
    {
        Tap = 1,
        Hold = 2,
        Flick = 4,
        Bomb = 6,
        SkyArea = 5,
    }

    internal enum VisualNoteLifetime
    {
        NoInput,
        NoClip,
    }

    /// <summary>
    /// Immutable description of a note that is rendered by the extension layer.
    /// It never stores a native object pointer or array position.
    /// </summary>
    internal sealed class VisualNoteDescriptor
    {
        internal long VisualId;
        internal long SourceId;
        internal VisualNoteKind Kind;
        internal VisualNoteLifetime Lifetime;
        internal int TimingGroup;
        internal long Group;
        internal uint Side;
        internal uint StartMs;
        internal uint EndMs;
        internal uint Flags;
        internal int[] Cipher = new int[4];
        internal readonly int[] Pairs = new int[8];
        internal double StartCenter;
        internal double StartWidth;
        internal double EndCenter;
        internal double EndWidth;

        internal static VisualNoteDescriptor FromNative(
            Icp1Codec.Note note,
            VisualNoteKind kind,
            VisualNoteLifetime lifetime,
            int timingGroup)
        {
            var descriptor = new VisualNoteDescriptor
            {
                VisualId = note.Index,
                SourceId = note.Index,
                Kind = kind,
                Lifetime = lifetime,
                TimingGroup = timingGroup,
                Group = note.Group,
                Side = note.Side,
                StartMs = note.StartMs,
                EndMs = note.EndMs,
                Flags = note.Flags,
            };
            Array.Copy(note.Cipher, descriptor.Cipher,
                descriptor.Cipher.Length);
            Array.Copy(note.Pairs, descriptor.Pairs, descriptor.Pairs.Length);
            if (note.TryGetScaledPair(
                    0, out int startCenterNumerator,
                    out int startCenterDenominator))
                descriptor.StartCenter =
                    startCenterNumerator /
                    (double)startCenterDenominator;
            if (note.TryGetScaledPair(
                    1, out int endCenterNumerator,
                    out int endCenterDenominator))
                descriptor.EndCenter =
                    endCenterNumerator /
                    (double)endCenterDenominator;
            if (note.TryGetScaledPair(
                    2, out int startWidthNumerator,
                    out int startWidthDenominator))
                descriptor.StartWidth = Math.Abs(
                    startWidthNumerator /
                    (double)startWidthDenominator);
            if (note.TryGetScaledPair(
                    3, out int endWidthNumerator,
                    out int endWidthDenominator))
                descriptor.EndWidth = Math.Abs(
                    endWidthNumerator /
                    (double)endWidthDenominator);
            return descriptor;
        }
    }
}
