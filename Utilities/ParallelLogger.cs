using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AemulusModManager.Utilities
{
    public static class ParallelLogger
    {
        private static readonly BlockingCollection<string> _Queue = new BlockingCollection<string>();

        static ParallelLogger()
        {
            var thread = new Thread(
              () =>
              {
                  StringBuilder builder = new StringBuilder();
                  int count = 0;
                  Stopwatch sw = Stopwatch.StartNew();
                  sw.Start();
                  while (true)
                  {
                      count = _Queue.Count;
                      if (count > 0)
                      {
                          if (sw.ElapsedMilliseconds > 100)
                          {
                              sw.Restart();
                              for (int i = 0; i < count; i++)
                              {
                                  builder.AppendLine(_Queue.Take());
                              }
                              Console.WriteLine(builder.ToString());
                              builder.Clear();
                          }
                      }


                  }
              });
            thread.IsBackground = true;
            thread.Start();
        }

        public static void Log(string value)
        {
            _Queue.Add(value);
        }
    }

}
