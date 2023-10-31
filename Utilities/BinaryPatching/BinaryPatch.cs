using AemulusModManager.Utilities.TblPatching;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using AemulusModManager.Utilities.BinaryPatching;
using AemulusModManager.Utilities;

namespace AemulusModManager
{
    public static class BinaryPatcher
    {
        public static void Patch(List<string> ModList, string modDir, bool useCpk, string cpkLang, string game)
        {
            Utilities.ParallelLogger.Log("[INFO] Patching files...");
            
            // Load EnabledPatches in order
            foreach (string dir in ModList)
            {
                Utilities.ParallelLogger.Log($"[INFO] Searching for/applying binary patches in {dir}...");
                if (!Directory.Exists($@"{dir}\binarypatches"))
                {
                    Utilities.ParallelLogger.Log($"[INFO] No binarypatches folder found in {dir}");
                    continue;
                }

                var patchList = new List<BinaryPatches>();
                // Apply bp json patching
                foreach (var t in Directory.GetFiles($@"{dir}\binarypatches", "*.bp", SearchOption.AllDirectories))
                {
                    BinaryPatches patches = null;
                    try
                    {
                        patches = JsonConvert.DeserializeObject<BinaryPatches>(File.ReadAllText(t));
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
                            var p4gArchive = String.Empty;
                            if (game == "Persona 4 Golden")
                            {
                                if (useCpk)
                                    p4gArchive = $@"{Path.GetFileNameWithoutExtension(cpkLang)}\";
                                else
                                {
                                    switch (cpkLang)
                                    {
                                        case "data_e.cpk":
                                            p4gArchive = $@"data00004\";
                                            break;
                                        case "data.cpk":
                                            p4gArchive = $@"data00001\";
                                            break;
                                        case "data_c.cpk":
                                            p4gArchive = $@"data00006\";
                                            break;
                                        case "data_k.cpk":
                                            p4gArchive = $@"data00005\";
                                            break;
                                        default:
                                            p4gArchive = $@"data00004\";
                                            break;
                                    }
                                }
                            }

                            var outputFile = $@"{modDir}\{p4gArchive}{patch.file}";
                            // Copy over original file
                            if (!File.Exists(outputFile))
                            {
                                var originalFile = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{p4gArchive}{patch.file}";
                                if (File.Exists(originalFile))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                                    File.Copy(originalFile, outputFile, true);
                                }
                                else
                                {
                                    Utilities.ParallelLogger.Log($"[WARNING] {patch.file} not found in output directory or Original directory.");
                                    continue;
                                }
                            }
                            var handled = false;
                            string[] stringData = patch.data.Split(' ');
                            byte[] data = new byte[stringData.Length];
                            for (int i = 0; i < data.Length; i++)
                            {
                                try
                                {
                                    data[i] = Convert.ToByte(stringData[i], 16);
                                }
                                catch (Exception ex)
                                {
                                    Utilities.ParallelLogger.Log($"[ERROR] Couldn't parse hex string {stringData[i]} ({ex.Message}), skipping...");
                                    handled = true;
                                    break;
                                }
                            }
                            if (handled)
                            {
                                handled = false;
                                continue;
                            }
                            var fileBytes = File.ReadAllBytes(outputFile).ToList();
                            // Add null bytes if offset is greater than count
                            if ((int)patch.offset > fileBytes.Count)
                            {
                                int count = (int)patch.offset - fileBytes.Count;
                                while (count > 0)
                                {
                                    fileBytes.Add((byte)0);
                                    count--;
                                }
                            }
                            // Remove only bytes at the end of length exceeds range
                            else if ((int)patch.offset + data.Length > fileBytes.Count)
                                fileBytes.RemoveRange((int)patch.offset, fileBytes.Count - (int)patch.offset);
                            else
                                fileBytes.RemoveRange((int)patch.offset, data.Length);
                            fileBytes.InsertRange((int)patch.offset, data);
                            File.WriteAllBytes(outputFile, fileBytes.ToArray());
                            Utilities.ParallelLogger.Log($"[INFO] Patched {patch.file} with {Path.GetFileName(t)}");
                        }
                    }
                }

            }

        }
    }


}