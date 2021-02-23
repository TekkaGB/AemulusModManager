using Onova.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AemulusModManager.Utilities.PackageUpdating.DownloadUtils
{
    /// <summary>
    /// Extracts files from 7z-archived packages.
    /// </summary>
    public class Zip7Extractor : IPackageExtractor
    {
        public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath,
            IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.FileName = @"Dependencies\7z\7z.exe";
            if (!File.Exists(startInfo.FileName))
            {
                Console.WriteLine($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }
            // Extract the file
            startInfo.Arguments = $"x -y \"{sourceFilePath}\" -o\"{destDirPath}\"";
            Console.WriteLine($"[INFO] Extracting {sourceFilePath}");

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }
            // TODO Check if it actually succeeded (by reading the command output I guess)
            Console.WriteLine($"[INFO] Done Extracting {sourceFilePath}");
            File.Delete(@$"{sourceFilePath}");
            Console.WriteLine(@$"[INFO] Deleted {sourceFilePath}");
            // Move the folders to the right place
            string parentPath = Directory.GetParent(destDirPath).FullName;
            Directory.Move(Directory.GetDirectories(destDirPath)[0], $@"{parentPath}\Aemulus");
            Directory.Delete(destDirPath);
            Directory.Move($@"{parentPath}\Aemulus", destDirPath);

            /*    
            // Rename the folder to version as this is what onva looks for
                if (Directory.Exists(@$"{oldPath}\{game.Split(',')[1]}"))
                {
                    Directory.Delete(@$"{oldPath}\{game.Split(',')[1]}", true);
                }
                Directory.Move(Directory.GetDirectories(oldPath)[0], @$"{oldPath}\{game.Split(',')[1]}");
            */
        }

    }
}
