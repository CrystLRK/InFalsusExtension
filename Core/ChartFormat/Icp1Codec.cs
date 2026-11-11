// Code organization: ICP1 codec. Runtime behavior is unchanged.
using System;
using System.Collections.Generic;

namespace InFalsusChartExt
{
    /// <summary>
    /// Decoder/encoder for the ICP1 chart container, ported from chart_packer.py.
    /// Decodes every note/event, lets the caller transform them, and re-encodes the
    /// result with the same rolling key so the game's own parser reads it normally.
    /// </summary>
    internal static class Icp1Codec
    {
        internal const int HeaderSize = 28;
        internal const int NoteSize = 80;
        internal const int EventSize = 32;
        private const ulong M64 = ulong.MaxValue;
        private const uint M32 = 0xFFFFFFFFu;

        internal sealed class Note
        {
            public long Index;
            public long Group;
            public uint StartMs;
            public uint EndMs;
            public uint Side;
            public uint Type;
            public uint Flags;
            /// <summary>Eight raw int32 values of the four fraction pairs.</summary>
            public readonly int[] Pairs = new int[8];
            /// <summary>四个分数对的原始密文值 (S32)。重编码必须原样写回（官方谱大量使用
            /// cipher ≠ gcd(raw) 的表示，重新推导密文会让游戏侧解码状态发散）。</summary>
            public readonly int[] Cipher = new int[4];

            /// <summary>
            /// Returns the same restored fraction pair the native game reads.
            /// Pairs alone contain the raw numerator/denominator; Cipher scales
            /// them per note, so consumers must use this method rather than
            /// interpreting Pairs directly.
            /// </summary>
            internal bool TryGetScaledPair(
                int pairIndex, out int numerator, out int denominator)
            {
                if ((uint)pairIndex >= 4)
                {
                    numerator = 0;
                    denominator = 0;
                    return false;
                }

                ScalePair(
                    Cipher[pairIndex],
                    Pairs[pairIndex * 2],
                    Pairs[pairIndex * 2 + 1],
                    out numerator,
                    out denominator);
                return denominator != 0;
            }
        }

        internal sealed class Event
        {
            public ulong Id;
            public uint TimeMs;
            public uint Type;
            public ulong Value1;
            public ulong Value2;
        }

        internal sealed class Chart
        {
            public ushort Version;
            public uint U20;
            public uint U24;
            public readonly List<Note> Notes = new List<Note>();
            public readonly List<Event> Events = new List<Event>();
        }

        private struct State
        {
            public ulong S0, S1, S2, S3;
            public uint Counter;
        }

        private static ulong Rol(ulong value, int amount) => (value << amount) | (value >> (64 - amount));

        private static uint U32(byte[] data, int offset) =>
            (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));

        private static ulong U64(byte[] data, int offset) =>
            U32(data, offset) | ((ulong)U32(data, offset + 4) << 32);

        private static int S32(uint value) => unchecked((int)value);

        private static int PositiveGcd(int a, int b)
        {
            long x = Math.Abs((long)a);
            long y = Math.Abs((long)b);
            if (x == 0 && y == 0) return 1;
            while (y != 0)
            {
                long t = x % y;
                x = y;
                y = t;
            }
            return (int)(x == 0 ? 1 : x);
        }

        private static ulong Gen(State s, uint idx, uint k)
        {
            ulong r9 = ((ulong)(idx & M32) << 32) | (k & M32);
            switch (k & 3)
            {
                case 3:
                    return Rol((s.S2 + s.S1 + r9) & M64, 37) ^ s.S3;
                case 2:
                    r9 ^= s.S0;
                    return ((Rol(r9, 29) + s.S2) & M64) ^ s.S1;
                case 1:
                    return ((s.S1 - Rol((s.S3 + r9) & M64, 17)) & M64) ^ s.S0;
                default:
                    r9 ^= s.S2;
                    return ((Rol(r9, 11) + s.S0) & M64) ^ s.S3;
            }
        }

        private static State Finalize(State s, ulong rdx, ulong r8, ulong r9)
        {
            switch (s.Counter & 3)
            {
                case 0:
                    s.S0 = (s.S0 + (Rol(s.S3, 7) + rdx)) & M64;
                    {
                        ulong rax = Rol((s.S0 + r8) & M64, 13);
                        s.S1 ^= rax;
                        r9 ^= s.S1;
                        s.S2 = (s.S2 - r9) & M64;
                        s.S3 = Rol((s.S2 + s.S3) & M64, 27) ^ s.S0;
                    }
                    break;
                case 1:
                    s.S1 = (s.S1 + (Rol(s.S0, 9) + r9)) & M64;
                    {
                        ulong rax = Rol((s.S1 + rdx) & M64, 19);
                        s.S2 ^= rax;
                        r8 ^= s.S2;
                        s.S3 = (s.S3 - r8) & M64;
                        s.S0 = Rol((s.S3 + s.S0) & M64, 31) ^ s.S1;
                    }
                    break;
                case 2:
                    s.S2 = (s.S2 + (Rol(s.S1, 15) + r8)) & M64;
                    {
                        ulong rax = Rol((s.S2 + r9) & M64, 23);
                        s.S3 ^= rax;
                        rdx ^= s.S3;
                        s.S0 = (s.S0 - rdx) & M64;
                        s.S1 = Rol((s.S1 + s.S0) & M64, 39) ^ s.S2;
                    }
                    break;
                default:
                    {
                        ulong keep = s.S2;
                        s.S3 = (s.S3 + (Rol(s.S2, 21) + rdx)) & M64;
                        ulong rax = Rol((s.S3 + r8) & M64, 29);
                        s.S0 ^= rax;
                        rax = s.S0 ^ r9;
                        s.S1 = (s.S1 - rax) & M64;
                        s.S2 = Rol((keep + s.S1) & M64, 43) ^ s.S3;
                    }
                    break;
            }
            s.Counter = (s.Counter + 1) & M32;
            return s;
        }

        private static State DeriveKey(string seed, int count, ushort u16) =>
            // seed state + per-character mix + trailing mix; mirrors derive_key() in chart_packer.py
            DeriveKeyCore(seed, count, u16);

        private static State DeriveKeyCore(string seed, int count, ushort u16)
        {
            State st = new State
            {
                S0 = (0xC4CEB9FE1A85EC53UL + (uint)count) & M64,
                S1 = (((ulong)count & M32) << 32) ^ 0x9E3779B185EBCA87UL,
                S2 = (0xD1B54A32D192ED03UL + (ulong)(u16 & 0xFFFF)) & M64,
                S3 = ((ulong)(u16 & 0xFFFF) ^ 0x94D049BB133111EBUL) & M64,
                Counter = 0,
            };
            for (int i = 0; i < seed.Length; i++)
            {
                ulong rdx = ((ulong)(i & M32) << 32) | ((uint)seed[i] & M32);
                ulong rdi = (ulong)(u16 & 0xFFFF) ^ rdx;
                ulong r8 = (rdx + (uint)count) & M64;
                switch (st.Counter & 3)
                {
                    case 0:
                        st.S0 = (st.S0 + (Rol(st.S3, 7) + rdx)) & M64;
                        {
                            ulong rax = Rol((st.S0 + r8) & M64, 13) ^ st.S1;
                            st.S1 = rax;
                            rdi ^= rax;
                            st.S2 = (st.S2 - rdi) & M64;
                            st.S3 = Rol((st.S3 + st.S2) & M64, 27) ^ st.S0;
                        }
                        break;
                    case 1:
                        st.S1 = (Rol(st.S0, 9) + st.S1 + rdi) & M64;
                        {
                            ulong rcx = Rol((st.S1 + rdx) & M64, 19) ^ st.S2;
                            st.S2 = rcx;
                            r8 ^= rcx;
                            st.S3 = (st.S3 - r8) & M64;
                            st.S0 = Rol((st.S0 + st.S3) & M64, 31) ^ st.S1;
                        }
                        break;
                    case 2:
                        st.S2 = (Rol(st.S1, 15) + r8 + st.S2) & M64;
                        {
                            ulong rcx = Rol((st.S2 + rdi) & M64, 23) ^ st.S3;
                            rdx ^= rcx;
                            st.S3 = rcx;
                            st.S0 = (st.S0 - rdx) & M64;
                            st.S1 = Rol((st.S1 + st.S0) & M64, 39) ^ st.S2;
                        }
                        break;
                    default:
                        {
                            ulong keep = st.S2;
                            st.S3 = (Rol(st.S2, 21) + st.S3 + rdx) & M64;
                            ulong rcx = Rol((st.S3 + r8) & M64, 29);
                            st.S0 ^= rcx;
                            rdi ^= st.S0;
                            st.S1 = (st.S1 - rdi) & M64;
                            st.S2 = Rol((keep + st.S1) & M64, 43) ^ st.S3;
                        }
                        break;
                }
                st.Counter = (st.Counter + 1) & M32;
            }
            return Finalize(st, (uint)count, (uint)u16, (uint)seed.Length);
        }

        private static State FinalizeRecordState(State s, int noteIndex, long field0, int[] pairGcds)
        {
            ulong repeatedIndex = ((ulong)(uint)noteIndex) | ((ulong)(uint)noteIndex << 32);
            ulong foldedState = (repeatedIndex + ((s.S0 + Rol(s.S1, 9)) ^ (s.S3 + Rol(s.S2, 23)))) & M64;
            uint fold = (uint)(foldedState ^ (foldedState >> 32));

            ulong fourth = (((ulong)(uint)(pairGcds[3] - 1)) ^ (fold >> 24)) & M64;
            ulong third = (Rol(((ulong)(uint)(pairGcds[2] - 1)) ^ ((fold >> 16) & 0xFF), 29) ^ Rol(fourth, 47)) & M64;
            ulong combined = (((ulong)(uint)(pairGcds[0] - 1)) ^ (fold & 0xFF)
                              ^ Rol(((ulong)(uint)(pairGcds[1] - 1)) ^ ((fold >> 8) & 0xFF), 13)
                              ^ third) & M64;

            uint mix = (uint)field0 ^ (uint)noteIndex ^ (uint)combined ^ (uint)(combined >> 32) ^ (uint)((ulong)field0 >> 32);
            ulong repeated = mix | ((ulong)mix << 32);

            s.S0 ^= repeated;
            s.S1 = (s.S1 + Rol(repeated, 17)) & M64;
            s.S2 = (s.S2 - Rol(repeated, 31)) & M64;
            s.S3 ^= Rol(repeated, 47);
            return s;
        }

        /// <summary>Masks a signed fraction value the same way chart_packer.py does.</summary>
        private static ulong PairLow(int value) => value > 0 ? (ulong)value : (uint)value;

        private static ulong PairHigh(int value) => (ulong)(uint)value;

        /// <summary>The rolling state advance after one note; mirrors the reference decode
        /// (In Libertas): the fraction packing uses the RESTORED (scaled) pairs, while the
        /// record-state gcds use the raw plains. 官方谱 7100/7128 对分数的恢复值 ≠ 原始对；
        /// 用错这一处会让解码从第一个音符起发散（曾导致官谱被误判并重编码成"一个音符"）。</summary>
        private static State AdvanceNote(State state, long field0, long group, uint startMs, uint endMs,
            uint sideAndType, uint flags, int[] scaled, int[] raw, int noteIndex, out int[] gcds)
        {
            state = Finalize(state, (ulong)field0, (ulong)group, ((ulong)endMs << 32) | startMs);
            state = Finalize(state, sideAndType, flags, (PairHigh(scaled[1]) << 32) | PairLow(scaled[0]));
            state = Finalize(state, (PairHigh(scaled[3]) << 32) | PairLow(scaled[2]),
                (PairHigh(scaled[5]) << 32) | PairLow(scaled[4]),
                (PairHigh(scaled[7]) << 32) | PairLow(scaled[6]));

            gcds = new int[4];
            for (int i = 0; i < 4; i++)
                gcds[i] = PositiveGcd(S32((uint)raw[i * 2]), S32((uint)raw[i * 2 + 1]));
            return FinalizeRecordState(state, noteIndex, field0, gcds);
        }

        /// <summary>还原一对分数：scale=密文(S32)，(num,den)=原始对；结果 = scale × (num/g, den/g)。
        /// 与参考实现 (scale_pair) 完全一致。</summary>
        private static void ScalePair(int scale, int num, int den, out int outNum, out int outDen)
        {
            int div = PositiveGcd(num, den);
            if (div == 0) div = 1;
            unchecked
            {
                outNum = scale * (num / div);
                outDen = scale * (den / div);
            }
        }

        internal static bool TryDecode(byte[] data, string chartKey, out Chart chart)
        {
            chart = null;
            if (data == null || data.Length < HeaderSize) return false;
            if (data[0] != (byte)'I' || data[1] != (byte)'C' || data[2] != (byte)'P' || data[3] != (byte)'1') return false;

            ushort version = (ushort)(data[4] | (data[5] << 8));
            ushort headerLen = (ushort)(data[6] | (data[7] << 8));
            ushort noteLen = (ushort)(data[8] | (data[9] << 8));
            ushort eventLen = (ushort)(data[10] | (data[11] << 8));
            uint noteCount = U32(data, 12);
            uint eventCount = U32(data, 16);
            if (headerLen != HeaderSize || noteLen != NoteSize || eventLen != EventSize) return false;
            long expected = HeaderSize + (long)noteCount * NoteSize + (long)eventCount * EventSize;
            if (expected != data.Length) return false;

            chart = new Chart { Version = version, U20 = U32(data, 20), U24 = U32(data, 24) };
            State state = DeriveKey(chartKey, (int)noteCount, 1);

            for (uint i = 0; i < noteCount; i++)
            {
                int off = HeaderSize + (int)i * NoteSize;
                Note note = new Note
                {
                    Index = (long)(U64(data, off) - Gen(state, i, 0)),
                    Group = (long)(U64(data, off + 8) - Gen(state, i, 1)),
                };
                uint f2 = U32(data, off + 0x10) - (uint)Gen(state, i, 2);
                uint f3 = U32(data, off + 0x14) - (uint)Gen(state, i, 3);
                uint f4 = U32(data, off + 0x18) - (uint)Gen(state, i, 4);
                uint f5 = U32(data, off + 0x1C) - (uint)Gen(state, i, 5);
                uint f6 = U32(data, off + 0x20) - (uint)Gen(state, i, 6);
                uint f7 = U32(data, off + 0x24) - (uint)Gen(state, i, 7);
                uint f8 = U32(data, off + 0x28) - (uint)Gen(state, i, 8);
                uint f9 = U32(data, off + 0x2C) - (uint)Gen(state, i, 9);
                note.StartMs = f2;
                note.EndMs = f3;
                note.Side = f4 & 0xFF;
                note.Type = (f4 >> 8) & 0xFF;
                note.Flags = f5;
                for (int p = 0; p < 8; p++)
                    note.Pairs[p] = S32(U32(data, off + 0x30 + 4 * p));
                note.Cipher[0] = S32(f6);
                note.Cipher[1] = S32(f7);
                note.Cipher[2] = S32(f8);
                note.Cipher[3] = S32(f9);
                int[] scaled = new int[8];
                for (int p = 0; p < 4; p++)
                {
                    ScalePair(note.Cipher[p], note.Pairs[2 * p], note.Pairs[2 * p + 1],
                              out scaled[2 * p], out scaled[2 * p + 1]);
                }

                state = AdvanceNote(state, note.Index, note.Group, f2, f3, f4, f5, scaled, note.Pairs, (int)i, out _);

                chart.Notes.Add(note);
            }

            int eventBase = HeaderSize + (int)noteCount * NoteSize;
            for (uint i = 0; i < eventCount; i++)
            {
                int off = eventBase + (int)i * EventSize;
                chart.Events.Add(new Event
                {
                    Id = U64(data, off),
                    TimeMs = U32(data, off + 8),
                    Type = U32(data, off + 12),
                    Value1 = U64(data, off + 16),
                    Value2 = U64(data, off + 24),
                });
            }
            return true;
        }

        internal static byte[] Encode(Chart chart, string chartKey)
        {
            int noteCount = chart.Notes.Count;
            int eventCount = chart.Events.Count;
            byte[] outData = new byte[HeaderSize + noteCount * NoteSize + eventCount * EventSize];

            outData[0] = (byte)'I'; outData[1] = (byte)'C'; outData[2] = (byte)'P'; outData[3] = (byte)'1';
            outData[4] = (byte)(chart.Version & 0xFF); outData[5] = (byte)(chart.Version >> 8);
            outData[6] = HeaderSize; outData[7] = 0;
            outData[8] = NoteSize; outData[9] = 0;
            outData[10] = EventSize; outData[11] = 0;
            WriteU32(outData, 12, (uint)noteCount);
            WriteU32(outData, 16, (uint)eventCount);
            WriteU32(outData, 20, chart.U20);
            WriteU32(outData, 24, chart.U24);

            State state = DeriveKey(chartKey, noteCount, 1);
            for (int i = 0; i < noteCount; i++)
            {
                Note note = chart.Notes[i];
                int off = HeaderSize + i * NoteSize;
                uint f4 = note.Side | (note.Type << 8);
                int[] scaled = new int[8];
                for (int p = 0; p < 4; p++)
                {
                    ScalePair(note.Cipher[p], note.Pairs[2 * p], note.Pairs[2 * p + 1],
                              out scaled[2 * p], out scaled[2 * p + 1]);
                }

                WriteU64(outData, off, (ulong)note.Index + Gen(state, (uint)i, 0));
                WriteU64(outData, off + 8, (ulong)note.Group + Gen(state, (uint)i, 1));
                WriteU32(outData, off + 0x10, note.StartMs + (uint)Gen(state, (uint)i, 2));
                WriteU32(outData, off + 0x14, note.EndMs + (uint)Gen(state, (uint)i, 3));
                WriteU32(outData, off + 0x18, f4 + (uint)Gen(state, (uint)i, 4));
                WriteU32(outData, off + 0x1C, note.Flags + (uint)Gen(state, (uint)i, 5));
                for (int p = 0; p < 4; p++)
                    WriteU32(outData, off + 0x20 + 4 * p, (uint)note.Cipher[p] + (uint)Gen(state, (uint)i, (uint)(6 + p)));
                for (int p = 0; p < 8; p++)
                    WriteU32(outData, off + 0x30 + 4 * p, (uint)note.Pairs[p]);

                state = AdvanceNote(state, note.Index, note.Group, note.StartMs, note.EndMs, f4, note.Flags, scaled, note.Pairs, i, out _);
            }

            int eventBase = HeaderSize + noteCount * NoteSize;
            for (int i = 0; i < eventCount; i++)
            {
                Event ev = chart.Events[i];
                int off = eventBase + i * EventSize;
                WriteU64(outData, off, ev.Id);
                WriteU32(outData, off + 8, ev.TimeMs);
                WriteU32(outData, off + 12, ev.Type);
                WriteU64(outData, off + 16, ev.Value1);
                WriteU64(outData, off + 24, ev.Value2);
            }
            return outData;
        }

        private static void WriteU32(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteU64(byte[] data, int offset, ulong value)
        {
            WriteU32(data, offset, (uint)value);
            WriteU32(data, offset + 4, (uint)(value >> 32));
        }
    }
}
