using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Reflection;

namespace AemulusModManager
{
    public static class PreappfileAppend
    {
        public static string GetChecksumString(string filePath)
        {
            string checksumString = null;

            // get md5 checksum of file
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    // get hash
                    byte[] currentFileSum = md5.ComputeHash(stream);
                    // convert hash to string
                    checksumString = BitConverter.ToString(currentFileSum).Replace("-", "");
                }
            }

            return checksumString;
        }
        public static void Append(string path, string cpkLang)
        {
            // Check if required files are there
            if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\preappfile\preappfile.exe"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find Dependencies\preappfile\preappfile.exe. Please check if it was blocked by your anti-virus.");
                return;
            }
            
            if (!File.Exists($@"{path}\{cpkLang}"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find {path}\{cpkLang} for appending.");
                return;
            }
            // Backup cpk if not backed up already
            if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}"))
            {
                Console.WriteLine($@"[INFO] Backing up {cpkLang}.cpk");
                File.Copy($@"{path}\{cpkLang}", $@"Original\Persona 4 Golden\{cpkLang}");
            }
            // Copy original cpk back if different
            if (GetChecksumString($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}") != GetChecksumString($@"{path}\{cpkLang}"))
            {
                Console.WriteLine($@"[INFO] Reverting {cpkLang} back to original");
                File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}", $@"{path}\{cpkLang}", true);
            }
            if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk"))
            {
                Console.WriteLine($@"[INFO] Backing up movie.cpk");
                File.Copy($@"{path}\movie.cpk", $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk");
            }
            // Copy original cpk back if different
            if (GetChecksumString($@"Original\Persona 4 Golden\movie.cpk") != GetChecksumString($@"{path}\movie.cpk"))
            {
                Console.WriteLine($@"[INFO] Reverting movie.cpk back to original");
                File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk", $@"{path}\movie.cpk", true);
            }
            // Delete modified pacs
            if (File.Exists($@"{path}\data00007.pac"))
            {
                Console.WriteLine($"[INFO] Deleting data00007.pac");
                File.Delete($@"{path}\data00007.pac");
            }
            if (File.Exists($@"{path}\movie00003.pac"))
            {
                Console.WriteLine($"[INFO] Deleting movie00003.pac");
                File.Delete($@"{path}\movie00003.pac");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\Preappfile\preappfile.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            if (Directory.Exists($@"{path}\mods\preappfile\{Path.GetFileNameWithoutExtension(cpkLang)}"))
            {
                Console.WriteLine($@"[INFO] Appending to {cpkLang}");
                startInfo.Arguments = $@"-i  ""{path}\mods\preappfile\{Path.GetFileNameWithoutExtension(cpkLang)}"" -a ""{path}\{cpkLang}"" -o ""{path}\{cpkLang}"" --pac-index 7";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    while (!process.HasExited)
                    {
                        string text = process.StandardOutput.ReadLine();
                        if (text != "" && text != null)
                            Console.WriteLine($"[INFO] {text}");
                    }
                }
            }
            if (Directory.Exists($@"{path}\mods\preappfile\movie"))
            {
                Console.WriteLine($@"[INFO] Appending to movie");
                startInfo.Arguments = $@"-i  ""{path}\mods\preappfile\movie"" -a ""{path}\movie.cpk"" -o ""{path}\movie.cpk"" --pac-index 3";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    while (!process.HasExited)
                    {
                        string text = process.StandardOutput.ReadLine();
                        if (text != "" && text != null)
                            Console.WriteLine($"[INFO] {text}");
                    }
                }
            }
        }
    }
}
