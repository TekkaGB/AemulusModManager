using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using AemulusModManager.Utilities.SpdPatching;
using AemulusModManager.Utilities;


namespace AemulusModManager
{
    public static class SpdPatcher
    {
        public static void Patch(List<string> ModList, string modDir, bool useCpk, string cpkLang, string game)
        {
            Utilities.ParallelLogger.Log("[INFO] Patching files...");

            // Load EnabledPatches in order
            foreach (string dir in ModList)
            {
                Utilities.ParallelLogger.Log($"[INFO] Searching for/applying spd patches in {dir}...");
                if (!Directory.Exists($@"{dir}\spdpatches"))
                {
                    Utilities.ParallelLogger.Log($"[INFO] No spdpatches folder found in {dir}");
                    continue;
                }

                var patchList = new List<SpdPatches>();
                // Apply spd json patching
                foreach (var t in Directory.GetFiles($@"{dir}\spdpatches", "*.spdp", SearchOption.AllDirectories))
                {
                    SpdPatches patches = null;
                    try
                    {
                        patches = JsonConvert.DeserializeObject<SpdPatches>(File.ReadAllText(t));
                    }
                    catch (Exception ex)
                    {
                        Utilities.ParallelLogger.Log($"[ERROR] Couldn't deserialize {t} ({ex.Message}), skipping...");
                        continue;
                    }
                    if (patches.Version != 1)
                    {
                        Utilities.ParallelLogger.Log($"[ERROR] Invalid version for {t}, skipping...");
                        continue;
                    }
                    if (patches.Patches != null)
                    {
                        foreach (var patch in patches.Patches)
                        {
                            var outputFile = $@"{modDir}\{patch.SpdPath}";
                            // Copy over original file
                            if (!File.Exists(outputFile))
                            {
                                var originalFile = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{patch.SpdPath}";
                                if (File.Exists(originalFile))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                                    File.Copy(originalFile, outputFile, true);
                                }
                                else
                                {
                                    Utilities.ParallelLogger.Log($"[WARNING] {patch.SpdPath} not found in output directory or Original directory.");
                                    continue;
                                }
                            }

                            Process process = new Process();
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\SpdPatcher\SPD Patcher.exe";
                            startInfo.Arguments = $"\"{t}\" " + $"\"{outputFile}\" " + $"\"{outputFile}\"";
                            process.StartInfo = startInfo;
                            process.Start();
                            Utilities.ParallelLogger.Log($"[INFO] Patched {patch.SpdPath} with {Path.GetFileName(t)}");
                            process.WaitForExit();
                            break;
                        }
                    }
                }

            }

        }
    }


}