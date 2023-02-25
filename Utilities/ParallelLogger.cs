using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace AemulusModManager.Utilities
{
    public static class ParallelLogger
    {
        private const int FlushInterval = 100;
        private static readonly BlockingCollection<string> _Queue = new BlockingCollection<string>();
        private static readonly object _QueueLock = new object();
        private static bool _IsFlushing = false;
        private static readonly Timer _FlushTimer = new Timer(FlushLogMessages, null, FlushInterval, FlushInterval);

        private static void FlushLogMessages(object state)
        {
            if (_IsFlushing) return;
            lock (_QueueLock)
            {
                if (_IsFlushing || _Queue.Count == 0) return;
                _IsFlushing = true;
            }

            StringBuilder builder = new StringBuilder();
            int count = _Queue.Count;
            for (int i = 0; i < count; i++)
            {
                builder.AppendLine(_Queue.Take());
            }
            Console.WriteLine(builder.ToString());
            builder.Clear();

            lock (_QueueLock)
            {
                _IsFlushing = false;
            }
        }

        [Conditional("DEBUG")]
        public static void AddDebugInfo(string caller, int line, ref string message)
        {
            if (line != -1) message = $"({caller}-{line}){message}
        }

        public static void Log(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = -1)
        {
            AddDebugInfo(caller, line, ref message);
            lock (_QueueLock)
            {
                _Queue.Add(message);
            }
        }
    }
}