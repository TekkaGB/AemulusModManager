using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using AemulusModManager.Utilities;
using System.Collections.Generic;
using System.Linq;

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
        public static void Validate(string path, string cpkLang)
        {
            var validated = true;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\Preappfile\preappfile.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            if (File.Exists($@"{path}\data00007.pac"))
            {
                startInfo.Arguments = $@"""{path}\data00007.pac""";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
                foreach (var file in Directory.GetFiles($@"{path}\mods\preappfile\{Path.GetFileNameWithoutExtension(cpkLang)}", "*", SearchOption.AllDirectories))
                {
                    var folders = new List<string>(file.Split(char.Parse("\\")));
                    int idx = folders.IndexOf(Path.GetFileNameWithoutExtension(cpkLang));
                    if (File.Exists($@"{path}\data00007\{string.Join("\\", folders.Skip(idx + 1).ToArray())}"))
                        Utilities.ParallelLogger.Log($"[INFO] Validated that {file} was appended");
                    else
                    {
                        Utilities.ParallelLogger.Log($"[WARNING] {file} not appended");
                        validated = false;
                    }

                }
                if (Directory.Exists($@"{path}\data00007"))
                    Directory.Delete($@"{path}\data00007", true);
            }
            if (File.Exists($@"{path}\movie00003.pac"))
            {
                startInfo.Arguments = $@"""{path}\movie00003.pac""";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
                foreach (var file in Directory.GetFiles($@"{path}\mods\preappfile\movie", "*", SearchOption.AllDirectories))
                {
                    var folders = new List<string>(file.Split(char.Parse("\\")));
                    int idx = folders.IndexOf("movie");
                    if (File.Exists($@"{path}\movie00003\{string.Join("\\", folders.Skip(idx + 1).ToArray())}"))
                        Utilities.ParallelLogger.Log($@"[INFO] Validated appended {file}");
                    else
                    {
                        Utilities.ParallelLogger.Log($@"[WARNING] {file} not appended");
                        validated = false;
                    }

                }
                if (Directory.Exists($@"{path}\movie00003"))
                    Directory.Delete($@"{path}\movie00003", true);
            }
            if (!validated)
            {
                Utilities.ParallelLogger.Log($"[WARNING] Not all appended files were validated, trying again");
                Append(path, cpkLang);
                Validate(path, cpkLang);
            }
        }
        public static void Append(string path, string cpkLang)
        {
            // Check if required files are there
            if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\preappfile\preappfile.exe"))
            {
                Utilities.ParallelLogger.Log($@"[ERROR] Couldn't find Dependencies\preappfile\preappfile.exe. Please check if it was blocked by your anti-virus.");
                return;
            }

            if (!File.Exists($@"{path}\{cpkLang}"))
            {
                Utilities.ParallelLogger.Log($@"[ERROR] Couldn't find {path}\{cpkLang} for appending.");
                return;
            }
            // Backup cpk if not backed up already
            if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}"))
            {
                Utilities.ParallelLogger.Log($@"[INFO] Backing up {cpkLang}.cpk");
                File.Copy($@"{path}\{cpkLang}", $@"Original\Persona 4 Golden\{cpkLang}");
            }
            // Copy original cpk back if different
            if (GetChecksumString($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}") != GetChecksumString($@"{path}\{cpkLang}"))
            {
                Utilities.ParallelLogger.Log($@"[INFO] Reverting {cpkLang} back to original");
                File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}", $@"{path}\{cpkLang}", true);
            }
            if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk"))
            {
                Utilities.ParallelLogger.Log($@"[INFO] Backing up movie.cpk");
                File.Copy($@"{path}\movie.cpk", $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk");
            }
            // Copy original cpk back if different
            if (GetChecksumString($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk") != GetChecksumString($@"{path}\movie.cpk"))
            {
                Utilities.ParallelLogger.Log($@"[INFO] Reverting movie.cpk back to original");
                File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk", $@"{path}\movie.cpk", true);
            }
            // Delete modified pacs
            if (File.Exists($@"{path}\data00007.pac"))
            {
                Utilities.ParallelLogger.Log($"[INFO] Deleting data00007.pac");
                File.Delete($@"{path}\data00007.pac");
            }
            if (File.Exists($@"{path}\movie00003.pac"))
            {
                Utilities.ParallelLogger.Log($"[INFO] Deleting movie00003.pac");
                File.Delete($@"{path}\movie00003.pac");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\Preappfile\preappfile.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            if (Directory.Exists($@"{path}\mods\preappfile\{Path.GetFileNameWithoutExtension(cpkLang)}"))
            {
                Utilities.ParallelLogger.Log($@"[INFO] Appending to {cpkLang}");
                startInfo.Arguments = $@"-i  ""{path}\mods\preappfile\{Path.GetFileNameWithoutExtension(cpkLang)}"" -a ""{path}\{cpkLang}"" -o ""{path}\{cpkLang}"" --pac-index 7";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
            if (Directory.Exists($@"{path}\mods\preappfile\movie"))
            {
                Utilities.ParallelLogger.Log($@"[INFO] Appending to movie");
                startInfo.Arguments = $@"-i  ""{path}\mods\preappfile\movie"" -a ""{path}\movie.cpk"" -o ""{path}\movie.cpk"" --pac-index 3";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
        }
    }
}
