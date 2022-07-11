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
            Console.WriteLine("[INFO] Patching files...");

            // Load EnabledPatches in order
            foreach (string dir in ModList)
            {
                Console.WriteLine($"[INFO] Searching for/applying spd patches in {dir}...");
                if (!Directory.Exists($@"{dir}\spdpatches"))
                {
                    Console.WriteLine($"[INFO] No spdpatches folder found in {dir}");
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
                        Console.WriteLine($"[ERROR] Couldn't deserialize {t} ({ex.Message}), skipping...");
                        continue;
                    }
                    if (patches.Version != 1)
                    {
                        Console.WriteLine($"[ERROR] Invalid version for {t}, skipping...");
                        continue;
                    }
                    if (patches.Patches != null)
                    {
                        foreach (var patch in patches.Patches)
                        {
                            var outputFile = $@"{modDir}\{patch.SpdPath}";
                            // Copy over original file
                            if (!FileIOWrapper.Exists(outputFile))
                            {
                                var originalFile = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{patch.SpdPath}";
                                if (FileIOWrapper.Exists(originalFile))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                                    FileIOWrapper.Copy(originalFile, outputFile, true);
                                }
                                else
                                {
                                    Console.WriteLine($"[WARNING] {patch.SpdPath} not found in output directory or Original directory.");
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
                            Console.WriteLine($"[INFO] Patched {patch.SpdPath} with {Path.GetFileName(t)}");
                            process.WaitForExit();
                            break;
                        }
                    }
                }

            }

        }
    }


}