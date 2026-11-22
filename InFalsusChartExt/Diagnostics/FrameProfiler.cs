using System;
using System.Diagnostics;

namespace InFalsusChartExt
{
    /// <summary>
    /// Lightweight per-frame timing for the main Mod entry points. Samples
    /// are kept in fixed buffers and reduced once every five seconds, so the
    /// profiler itself does not allocate per frame.
    /// </summary>
    internal static class FrameProfiler
    {
        internal const int Track = 0;
        internal const int Post = 1;
        internal const int SkyVisual = 2;
        internal const int SkyGlow = 3;
        internal const int Tick = 4;
        internal const int PostA = 5;
        internal const int PostB = 6;
        internal const int PostC = 7;
        internal const int PostD = 8;
        internal const int PostA1 = 9;
        internal const int PostA2 = 10;
        internal const int PostA3 = 11;
        internal const int PostA4 = 12;
        private const int SectionCount = 13;
        private const int Capacity = 900;
        private const double WindowSeconds = 5.0;

        private static readonly string[] Names =
        {
            "track", "post", "sky-vis", "sky-glow", "tick",
            "post-a", "post-b", "post-c", "post-d",
            "a1", "a2", "a3", "a4",
        };
        private static readonly double[][] Samples =
            new double[SectionCount][];
        private static readonly int[] Counts =
            new int[SectionCount];
        private static readonly long[] AllocBytes =
            new long[SectionCount];

        private static long _windowStart;
        private static long _gcStart;
        private static long _trackStart;
        private static int _frames;
        private static bool _enabled = true;

        internal static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        internal static void TrackBegin()
        {
            if (!_enabled) return;
            _trackStart = Stopwatch.GetTimestamp();
        }

        internal static void TrackEnd()
        {
            if (!_enabled) return;
            long now = Stopwatch.GetTimestamp();
            if (_trackStart != 0)
                Add(Track, now - _trackStart);
            _frames++;
            if (_windowStart == 0)
            {
                _windowStart = now;
                _gcStart = GC.GetAllocatedBytesForCurrentThread();
                return;
            }
            double seconds =
                (now - _windowStart) / (double)Stopwatch.Frequency;
            if (seconds >= WindowSeconds)
                Flush(now);
        }

        internal static long Begin(out long allocated)
        {
            allocated = _enabled
                ? GC.GetAllocatedBytesForCurrentThread()
                : 0;
            return _enabled ? Stopwatch.GetTimestamp() : 0;
        }

        internal static void End(
            int section, long start, long allocated)
        {
            if (!_enabled || start == 0) return;
            Add(section, Stopwatch.GetTimestamp() - start);
            AllocBytes[section] +=
                GC.GetAllocatedBytesForCurrentThread() - allocated;
        }

        internal static void ResetWindow()
        {
            _windowStart = 0;
            _frames = 0;
            for (int i = 0; i < SectionCount; i++)
            {
                Counts[i] = 0;
                AllocBytes[i] = 0;
            }
        }

        private static void Add(int section, long ticks)
        {
            int count = Counts[section];
            if (count >= Capacity) return;
            double[] buffer = Samples[section];
            if (buffer == null)
            {
                buffer = new double[Capacity];
                Samples[section] = buffer;
            }
            buffer[count] =
                ticks * 1000.0 / Stopwatch.Frequency;
            Counts[section] = count + 1;
        }

        private static void Flush(long now)
        {
            long allocated =
                GC.GetAllocatedBytesForCurrentThread() - _gcStart;
            double seconds =
                (now - _windowStart) / (double)Stopwatch.Frequency;
            int frames = _frames;
            var builder = new System.Text.StringBuilder(256);
            builder.Append("[ChartExt][frame] ");
            builder.Append("fps=");
            builder.Append((frames / seconds).ToString("F1"));
            builder.Append(" gcKB/s=");
            builder.Append((allocated / 1024.0 / seconds)
                .ToString("F1"));
            for (int section = 0; section < SectionCount; section++)
            {
                int count = Counts[section];
                if (count == 0) continue;
                double[] buffer = Samples[section];
                Array.Sort(buffer, 0, count);
                double p50 = buffer[count / 2];
                double p95 = buffer[Math.Min(
                    count - 1, (int)(count * 0.95))];
                double max = buffer[count - 1];
                builder.Append(' ');
                builder.Append(Names[section]);
                builder.Append('=');
                builder.Append(p50.ToString("F2"));
                builder.Append('/');
                builder.Append(p95.ToString("F2"));
                builder.Append('/');
                builder.Append(max.ToString("F2"));
                builder.Append(" allocKB=");
                builder.Append((AllocBytes[section] / 1024.0)
                    .ToString("F0"));
                Counts[section] = 0;
                AllocBytes[section] = 0;
            }
            builder.Append(" ms(p50/p95/max)");
            ChartExtMod.Log.Msg(builder.ToString());
            _windowStart = now;
            _gcStart = GC.GetAllocatedBytesForCurrentThread();
            _frames = 0;
        }
    }
}
