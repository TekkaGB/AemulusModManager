using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AemulusModManager
{
    class PacUnpacker
    {
        public void Unpack(string directory)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = @"Dependencies\MultiExtractor.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = $"\"{directory}" + @"\" + "data00004.pac\"";
            startInfo.RedirectStandardInput = true;
            Console.WriteLine($@"[INFO] Unpacking files from data00004.pac... (This part takes a bit)");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                StreamWriter myStreamWriter = process.StandardInput;

                // The following will put an 'Enter' on the first dh_make request for user input
                myStreamWriter.WriteLine(" ");
                myStreamWriter.Close();
                // Add this: wait until process does its work
                process.WaitForExit();
            }

            Console.WriteLine("[INFO] Filtering mergeable files...");
            foreach (var dir in Directory.EnumerateFiles($@"{directory}\data00004", "*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".bin") || s.EndsWith(".arc") || s.EndsWith(".pack")))
            {
                var simpleDir = dir.Replace($@"{directory}\", "");
                Thread.Sleep(150);
                if (!Directory.Exists($@"Original\{Path.GetDirectoryName(simpleDir)}"))
                    Directory.CreateDirectory($@"Original\{Path.GetDirectoryName(simpleDir)}");
                Console.WriteLine($@"[INFO] Copying {dir} to Original");
                try
                {
                    File.Copy(dir, $@"Original\{simpleDir}", true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                }

            }

            Console.WriteLine("[INFO] Deleting unpacked folder...");
            Directory.Delete($@"{directory}\data00004", true);

            Console.WriteLine("[INFO] Finished unpacking vanilla files!");
        }
    }
}
