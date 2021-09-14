using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AemulusModManager.Utilities;

namespace AemulusModManager
{
    public static class binMerge
    {
        private static string exePath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\PAKPack\PAKPack.exe";

        // Use PAKPack command
        public static void PAKPackCMD(string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = $"\"{exePath}\"";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = args;
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                // Add this: wait until process does its work
                process.WaitForExit();
            }
        }

        public static List<string> getFileContents(string path)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = $"\"{exePath}\"";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = $"list \"{path}\"";
            List<string> contents = new List<string>();
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    if (!line.Contains(" "))
                    {
                        contents.Add(line);
                    }
                }
                // Add this: wait until process does its work
                process.WaitForExit();
            }
            return contents;
        }
        private static int commonPrefixUtil(String str1, String str2)
        {
            String result = "";
            int n1 = str1.Length,
                n2 = str2.Length;

            // Compare str1 and str2  
            for (int i = 0, j = 0;
                     i <= n1 - 1 && j <= n2 - 1;
                     i++, j++)
            {
                if (!str1[i].ToString().Equals(str2[j].ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
                result += str1[i];
            }

            return result.Length;
        }

        private static List<string> getModList(string dir)
        {
            List<string> mods = new List<string>();
            string line;
            string[] list = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly)
                    .Where(s => (Path.GetExtension(s).ToLower() == ".aem")).ToArray();
            if (list.Length > 0)
            {
                using (StreamReader stream = new StreamReader(list[0]))
                {
                    while ((line = stream.ReadLine()) != null)
                    {
                        mods.Add(line);
                    }
                }
            }
            return mods;
        }
        public static void DeleteDirectory(string path)
        {

            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }
            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        public static void Unpack(List<string> ModList, string modDir, bool useCpk, string cpkLang, string game)
        {
            if (!FileIOWrapper.Exists(exePath))
            {
                Console.WriteLine($"[ERROR] Couldn't find {exePath}. Please check if it was blocked by your anti-virus.");
                return;
            }
            Console.WriteLine("[INFO] Beginning to unpack...");
            foreach (var mod in ModList)
            {
                if (!Directory.Exists(mod))
                {
                    Console.WriteLine($"[ERROR] Cannot find {mod}");
                    continue;
                }

                // Run prebuild.bat
                if (FileIOWrapper.Exists($@"{mod}\prebuild.bat") && new FileInfo($@"{mod}\prebuild.bat").Length > 0)
                {
                    Console.WriteLine($@"[INFO] Running {mod}\prebuild.bat...");

                    ProcessStartInfo ProcessInfo;

                    ProcessInfo = new ProcessStartInfo();
                    ProcessInfo.FileName = Path.GetFullPath($@"{mod}\prebuild.bat");
                    ProcessInfo.CreateNoWindow = true;
                    ProcessInfo.UseShellExecute = false;
                    ProcessInfo.WorkingDirectory = Path.GetFullPath(mod);

                    using (Process process = new Process())
                    {
                        process.StartInfo = ProcessInfo;

                        process.Start();

                        process.WaitForExit();
                    }

                    Console.WriteLine($@"[INFO] Finished running {mod}\prebuild.bat!");
                }

                List<string> modList = getModList(mod);

                // Copy and overwrite everything thats not a bin
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                {
                    // Copy everything except mods.aem and tblpatch to directory
                    if (Path.GetExtension(file).ToLower() != ".aem" && Path.GetExtension(file).ToLower() != ".tblpatch"
                        && Path.GetExtension(file).ToLower() != ".xml" && Path.GetExtension(file).ToLower() != ".png"
                        && Path.GetExtension(file).ToLower() != ".jpg" && Path.GetExtension(file).ToLower() != ".7z"
                        && Path.GetExtension(file).ToLower() != ".bat" && Path.GetExtension(file).ToLower() != ".txt"
                        && Path.GetExtension(file).ToLower() != ".zip" && Path.GetExtension(file).ToLower() != ".json"
                        && Path.GetExtension(file).ToLower() != ".tbp" && Path.GetExtension(file).ToLower() != ".rar"
                        && Path.GetExtension(file).ToLower() != ".exe" && Path.GetExtension(file).ToLower() != ".dll"
                        && Path.GetExtension(file).ToLower() != ".flow" && Path.GetExtension(file).ToLower() != ".msg"
                        && Path.GetExtension(file).ToLower() != ".back"
                        && Path.GetFileNameWithoutExtension(file).ToLower() != "preview")
                    {
                        List<string> folders = new List<string>(file.Split(char.Parse("\\")));
                        int idx = folders.IndexOf(Path.GetFileName(mod));
                        folders = folders.Skip(idx + 1).ToList();
                        string binPath = $@"{modDir}\{string.Join("\\", folders.ToArray())}";
                        string ogBinPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{string.Join("\\", folders.ToArray())}";

                        if (Path.GetExtension(file).ToLower() == ".bin"
                            || Path.GetExtension(file).ToLower() == ".arc"
                            || Path.GetExtension(file).ToLower() == ".abin"
                            || Path.GetExtension(file).ToLower() == ".pak"
                            || Path.GetExtension(file).ToLower() == ".pac"
                            || Path.GetExtension(file).ToLower() == ".pack")
                        {
                            if (FileIOWrapper.Exists(ogBinPath) && modList.Count > 0)
                            {
                                // Check if mods.aem contains the modified parts of a bin
                                if (!modList.Exists(x => x.Contains($@"{Path.GetDirectoryName(string.Join("\\", folders.ToArray()))}\{Path.GetFileNameWithoutExtension(binPath)}\")))
                                {
                                    Console.WriteLine($"[WARNING] Using {binPath} as base since nothing was specified in mods.aem");
                                    if (useCpk)
                                        binPath = Regex.Replace(binPath, "data0000[0-6]", Path.GetFileNameWithoutExtension(cpkLang));
                                    Directory.CreateDirectory(Path.GetDirectoryName(binPath));
                                    FileIOWrapper.Copy(file, binPath, true);
                                    continue;
                                }

                                Console.WriteLine($@"[INFO] Unpacking {file}...");
                                // Unpack and transfer modified parts if base already exists
                                PAKPackCMD($"unpack \"{file}\"");
                                // Unpack fully before comparing to mods.aem
                                foreach (var f in Directory.GetFiles(Path.ChangeExtension(file, null), "*", SearchOption.AllDirectories))
                                {
                                    if (Path.GetExtension(f).ToLower() == ".bin"
                                    || Path.GetExtension(f).ToLower() == ".abin"
                                    || Path.GetExtension(f).ToLower() == ".arc"
                                    || Path.GetExtension(f).ToLower() == ".pak"
                                    || Path.GetExtension(f).ToLower() == ".pac"
                                    || Path.GetExtension(f).ToLower() == ".pack")
                                    {
                                        Console.WriteLine($@"[INFO] Unpacking {f}...");
                                        PAKPackCMD($"unpack \"{f}\"");
                                        foreach (var f2 in Directory.GetFiles(Path.ChangeExtension(f, null), "*", SearchOption.AllDirectories))
                                        {
                                            if (Path.GetExtension(f2).ToLower() == ".bin"
                                            || Path.GetExtension(f2).ToLower() == ".arc"
                                            || Path.GetExtension(f2).ToLower() == ".pak"
                                            || Path.GetExtension(f2).ToLower() == ".abin"
                                            || Path.GetExtension(f2).ToLower() == ".pac"
                                            || Path.GetExtension(f2).ToLower() == ".pack")
                                            {
                                                Console.WriteLine($@"[INFO] Unpacking {f2}...");
                                                PAKPackCMD($"unpack \"{f2}\"");
                                            }
                                            else if (Path.GetExtension(f2).ToLower() == ".spd")
                                            {
                                                Console.WriteLine($@"[INFO] Unpacking {f2}...");
                                                Directory.CreateDirectory(Path.ChangeExtension(f2, null));
                                                List<DDS> ddsFiles = spdUtils.getDDSFiles(f2);
                                                foreach (var ddsFile in ddsFiles)
                                                {
                                                    string spdFolder = Path.ChangeExtension(f2, null);
                                                    FileIOWrapper.WriteAllBytes($@"{spdFolder}\{ddsFile.name}.dds", ddsFile.file);
                                                }
                                                List<SPDKey> spdKeys = spdUtils.getSPDKeys(f2);
                                                foreach (var spdKey in spdKeys)
                                                {
                                                    string spdFolder = Path.ChangeExtension(f2, null);
                                                    FileIOWrapper.WriteAllBytes($@"{spdFolder}\{spdKey.id}.spdspr", spdKey.file);
                                                }
                                            }
                                            else if (Path.GetExtension(f2) == ".spr")
                                            {
                                                Console.WriteLine($@"[INFO] Unpacking {f2}...");
                                                string sprFolder2 = Path.ChangeExtension(f2, null);
                                                Directory.CreateDirectory(sprFolder2);
                                                Dictionary<string, int> tmxNames = sprUtils.getTmxNames(f2);
                                                foreach (string name in tmxNames.Keys)
                                                {
                                                    byte[] tmx = sprUtils.extractTmx(f2, name);
                                                    FileIOWrapper.WriteAllBytes($@"{sprFolder2}\{name}.tmx", tmx);
                                                }
                                            }
                                        }
                                    }
                                    else if (Path.GetExtension(f).ToLower() == ".spd")
                                    {
                                        Directory.CreateDirectory(Path.ChangeExtension(f, null));
                                        List<DDS> ddsFiles = spdUtils.getDDSFiles(f);
                                        foreach (var ddsFile in ddsFiles)
                                        {
                                            string spdFolder = Path.ChangeExtension(f, null);
                                            FileIOWrapper.WriteAllBytes($@"{spdFolder}\{ddsFile.name}.dds", ddsFile.file);
                                        }
                                        List<SPDKey> spdKeys = spdUtils.getSPDKeys(f);
                                        foreach (var spdKey in spdKeys)
                                        {
                                            string spdFolder = Path.ChangeExtension(f, null);
                                            FileIOWrapper.WriteAllBytes($@"{spdFolder}\{spdKey.id}.spdspr", spdKey.file);
                                        }
                                    }
                                    else if (Path.GetExtension(f) == ".spr")
                                    {
                                        Console.WriteLine($@"[INFO] Unpacking {f}...");
                                        string sprFolder = Path.ChangeExtension(f, null);
                                        Directory.CreateDirectory(sprFolder);
                                        Dictionary<string, int> tmxNames = sprUtils.getTmxNames(f);
                                        foreach (string name in tmxNames.Keys)
                                        {
                                            byte[] tmx = sprUtils.extractTmx(f, name);
                                            FileIOWrapper.WriteAllBytes($@"{sprFolder}\{name}.tmx", tmx);
                                        }
                                    }

                                }
                            }
                            else
                            {
                                if (useCpk)
                                {
                                    binPath = Regex.Replace(binPath, "data0000[0-6]", Path.GetFileNameWithoutExtension(cpkLang));
                                    binPath = Regex.Replace(binPath, "movie0000[0-2]", "movie");
                                }
                                Directory.CreateDirectory(Path.GetDirectoryName(binPath));
                                FileIOWrapper.Copy(file, binPath, true);
                                Console.WriteLine($"[INFO] Copying over {file} to {binPath}");
                            }
                        }
                        else if (Path.GetExtension(file).ToLower() == ".spd")
                        {
                            if (FileIOWrapper.Exists(ogBinPath) && modList.Count > 0)
                            {
                                Console.WriteLine($@"[INFO] Unpacking {file}...");
                                Directory.CreateDirectory(Path.ChangeExtension(file, null));
                                List<DDS> ddsFiles = spdUtils.getDDSFiles(file);
                                foreach (var ddsFile in ddsFiles)
                                {
                                    string spdFolder = Path.ChangeExtension(file, null);
                                    FileIOWrapper.WriteAllBytes($@"{spdFolder}\{ddsFile.name}.dds", ddsFile.file);
                                }
                                List<SPDKey> spdKeys = spdUtils.getSPDKeys(file);
                                foreach (var spdKey in spdKeys)
                                {
                                    string spdFolder = Path.ChangeExtension(file, null);
                                    FileIOWrapper.WriteAllBytes($@"{spdFolder}\{spdKey.id}.spdspr", spdKey.file);
                                }
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(binPath));
                                FileIOWrapper.Copy(file, binPath, true);
                                Console.WriteLine($"[INFO] Copying over {file} to {binPath}");
                            }
                        }
                        else
                        {
                            if (useCpk)
                            {
                                binPath = Regex.Replace(binPath, "data0000[0-6]", Path.GetFileNameWithoutExtension(cpkLang));
                                binPath = Regex.Replace(binPath, "movie0000[0-2]", "movie");
                            }
                            Directory.CreateDirectory(Path.GetDirectoryName(binPath));
                            FileIOWrapper.Copy(file, binPath, true);
                            Console.WriteLine($"[INFO] Copying over {file} to {binPath}");
                        }
                    }
                }

                // Copy over loose files specified by mods.aem
                foreach (var m in modList)
                {
                    if (FileIOWrapper.Exists($@"{mod}\{m}"))
                    {
                        string dir = $@"{modDir}\{m}";
                        if (useCpk)
                        {
                            dir = Regex.Replace(dir, "data0000[0-6]", Path.GetFileNameWithoutExtension(cpkLang));
                            dir = Regex.Replace(dir, "movie0000[0-2]", "movie");
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(dir));
                        FileIOWrapper.Copy($@"{mod}\{m}", dir, true);
                        Console.WriteLine($@"[INFO] Copying over {mod}\{m} as specified by mods.aem");
                    }
                }

                // Go through mod directory again to delete unpacked files after bringing them in
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                {
                    if ((Path.GetExtension(file).ToLower() == ".bin"
                        || Path.GetExtension(file).ToLower() == ".abin"
                        || Path.GetExtension(file).ToLower() == ".arc"
                        || Path.GetExtension(file).ToLower() == ".pak"
                        || Path.GetExtension(file).ToLower() == ".pac"
                        || Path.GetExtension(file).ToLower() == ".pack"
                        || Path.GetExtension(file).ToLower() == ".spd")
                        && Directory.Exists(Path.ChangeExtension(file, null))
                        && Path.GetFileName(Path.ChangeExtension(file, null)) != "result"
                        && Path.GetFileName(Path.ChangeExtension(file, null)) != "panel"
                        && Path.GetFileName(Path.ChangeExtension(file, null)) != "crossword")
                    {
                        DeleteDirectory(Path.ChangeExtension(file, null));
                    }
                }

                if (FileIOWrapper.Exists($@"{mod}\battle\result.pac") && Directory.Exists($@"{mod}\battle\result\result"))
                {
                    foreach (var f in Directory.GetFiles($@"{mod}\battle\result\result"))
                    {
                        if (Path.GetExtension(f).ToLower() == ".gfs" || Path.GetExtension(f).ToLower() == ".gmd")
                            FileIOWrapper.Delete(f);
                    }
                }
                if (FileIOWrapper.Exists($@"{mod}\battle\result\result.spd") && Directory.Exists($@"{mod}\battle\result\result"))
                {
                    foreach (var f in Directory.GetFiles($@"{mod}\battle\result\result"))
                    {
                        if (Path.GetExtension(f).ToLower() == ".dds" || Path.GetExtension(f).ToLower() == ".spdspr")
                            FileIOWrapper.Delete(f);
                    }
                }
                if (FileIOWrapper.Exists($@"{mod}\field\panel.bin") && Directory.Exists($@"{mod}\field\panel\panel"))
                    DeleteDirectory($@"{mod}\field\panel\panel");
                if (Directory.Exists($@"{mod}\battle\result\result") && !Directory.GetFiles($@"{mod}\battle\result\result", "*", SearchOption.AllDirectories).Any())
                    DeleteDirectory($@"{mod}\battle\result\result");
                if (Directory.Exists($@"{mod}\battle\result") && !Directory.GetFiles($@"{mod}\battle\result", "*", SearchOption.AllDirectories).Any())
                    DeleteDirectory($@"{mod}\battle\result");
                if (Directory.Exists($@"{mod}\field\panel") && !Directory.EnumerateFileSystemEntries($@"{mod}\field\panel").Any())
                    DeleteDirectory($@"{mod}\field\panel");
                if ((FileIOWrapper.Exists($@"{mod}\minigame\crossword.pak") || FileIOWrapper.Exists($@"{mod}\minigame\crossword.spd")) && Directory.Exists($@"{mod}\minigame\crossword"))
                {
                    foreach (var f in Directory.GetFiles($@"{mod}\minigame\crossword"))
                    {
                        if (Path.GetExtension(f).ToLower() != ".pak")
                            FileIOWrapper.Delete(f);
                    }
                }
                if (Directory.Exists($@"{mod}\minigame\crossword") && !Directory.GetFiles($@"{mod}\minigame\crossword", "*", SearchOption.AllDirectories).Any())
                    DeleteDirectory($@"{mod}\minigame\crossword");

            }
            Console.WriteLine("[INFO] Finished unpacking!");
        }

        public static void Merge(string modDir, string game)
        {
            Console.WriteLine("[INFO] Beginning to merge...");
            List<string> dirs = new List<string>();
            foreach (var dir in Directory.GetDirectories(modDir))
            {
                var name = Path.GetFileName(dir);
                if (name != "patches" || name != "bins" || name != "SND")
                    dirs.Add($@"{modDir}\{name}");
            }
            // Find bins with loose files
            foreach (var dir in dirs)
            {
                // Check if loose folder matches vanilla bin file
                foreach (var d in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
                {
                    List<string> folders = new List<string>(d.Split(char.Parse("\\")));
                    int idx = folders.IndexOf(Path.GetFileName(dir));
                    folders = folders.Skip(idx).ToList();
                    string ogPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{string.Join("\\", folders.ToArray())}";

                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".bin")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".bin")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".bin");
                        if (Path.GetFileName(ogPath) == "panel.bin")
                        {
                            if (!Directory.Exists($@"{d}\panel"))
                                continue;
                        }
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        if (!Directory.Exists(Path.GetDirectoryName(d)))
                            Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".arc")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".arc")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".arc");
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".pac")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".pac")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".pac");
                        if (Path.GetFileName(ogPath) == "result.pac")
                        {
                            if (!Directory.Exists($@"{d}\result"))
                                continue;
                            if (!Directory.GetFiles($@"{d}\result", "*.GFS", SearchOption.TopDirectoryOnly).Any()
                                && !Directory.GetFiles($@"{d}\result", "*.GMD", SearchOption.TopDirectoryOnly).Any())
                                continue;
                        }
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".pak")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".pak")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".pak");
                        if (Path.GetFileName(ogPath) == "crossword.pak")
                        {
                            if (!Directory.GetFiles(d, "*.dds", SearchOption.AllDirectories).Any()
                                && !Directory.GetFiles(d, "*.spdspr", SearchOption.AllDirectories).Any()
                                && !Directory.GetFiles(d, "*.bmd", SearchOption.AllDirectories).Any()
                                && !Directory.GetFiles(d, "*.plg", SearchOption.AllDirectories).Any())
                                continue;
                        }
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".pack")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".pack")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".pack");
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".abin")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".abin")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".abin");
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".spr")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".spr")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".spr");
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    if (FileIOWrapper.Exists(Path.ChangeExtension(ogPath, ".spd")) && !FileIOWrapper.Exists(Path.ChangeExtension(d, ".spd")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".spd");
                        if (Path.GetFileName(ogPath) == "result.spd")
                        {
                            if (!Directory.GetFiles(d, "*.dds", SearchOption.TopDirectoryOnly).Any()
                                && !Directory.GetFiles(d, "*.spdspr", SearchOption.TopDirectoryOnly).Any())
                                continue;
                        }
                        if (Path.GetFileName(ogPath) == "crossword.spd")
                        {
                            if (!Directory.GetFiles(d, "*.dds", SearchOption.TopDirectoryOnly).Any()
                                && !Directory.GetFiles(d, "*.spdspr", SearchOption.TopDirectoryOnly).Any())
                                continue;
                        }
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        Directory.CreateDirectory(Path.GetDirectoryName(d));
                        FileIOWrapper.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                }

                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(file).ToLower() == ".bin"
                        || Path.GetExtension(file).ToLower() == ".abin"
                        || Path.GetExtension(file).ToLower() == ".arc"
                        || Path.GetExtension(file).ToLower() == ".pak"
                        || Path.GetExtension(file).ToLower() == ".pac"
                        || Path.GetExtension(file).ToLower() == ".pack")
                    {
                        if (Directory.Exists(Path.ChangeExtension(file, null)))
                        {
                            Console.WriteLine($@"[INFO] Merging {file}...");
                            string bin = file;
                            string binFolder = Path.ChangeExtension(file, null);

                            // Get contents of init_free
                            List<string> contents = getFileContents(bin);

                            // Unpack archive for future unpacking
                            string temp = $"{binFolder}_temp";
                            PAKPackCMD($"unpack \"{bin}\" \"{temp}\"");

                            foreach (var f in Directory.GetFiles(binFolder, "*", SearchOption.AllDirectories))
                            {
                                // Get bin path used for PAKPack.exe
                                int numParFolders = Path.ChangeExtension(file, null).Split(char.Parse("\\")).Length;
                                List<string> folders = new List<string>(f.Split(char.Parse("\\")));
                                string binPath = string.Join("/", folders.ToArray().Skip(numParFolders).ToArray());

                                // Case for paths in Persona 5 event paks
                                if (contents.Contains($"../../../{binPath}"))
                                {
                                    string args = $"replace \"{bin}\" ../../../{binPath} \"{f}\" \"{bin}\"";
                                    PAKPackCMD(args);
                                }
                                else if (contents.Contains($"../../{binPath}"))
                                {
                                    string args = $"replace \"{bin}\" ../../{binPath} \"{f}\" \"{bin}\"";
                                    PAKPackCMD(args);
                                }
                                else if (contents.Contains($"../{binPath}"))
                                {
                                    string args = $"replace \"{bin}\" ../{binPath} \"{f}\" \"{bin}\"";
                                    PAKPackCMD(args);
                                }
                                // Check if more unpacking needs to be done to replace
                                else if (!contents.Contains(binPath))
                                {
                                    string longestPrefix = "";
                                    int longestPrefixLen = 0;
                                    foreach (var c in contents)
                                    {
                                        int prefixLen = commonPrefixUtil(c, binPath);
                                        int otherPrefixLen = commonPrefixUtil(c, $"../../{binPath}");
                                        int otherOtherPrefixLen = commonPrefixUtil(c, $"../{binPath}");
                                        if (Math.Max(Math.Max(prefixLen, otherPrefixLen), otherOtherPrefixLen) > longestPrefixLen)
                                        {
                                            longestPrefix = c;
                                            longestPrefixLen = Math.Max(Math.Max(prefixLen, otherPrefixLen), otherOtherPrefixLen);
                                        }
                                    }
                                    // Check if we can unpack again
                                    if (Path.GetExtension(longestPrefix).ToLower() == ".bin"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".abin"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".arc"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".pak"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".pac"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".pack")
                                    {
                                        string file2 = $@"{temp}\{longestPrefix.Replace("/", "\\")}";
                                        List<string> contents2 = getFileContents(file2);

                                        List<string> split = new List<string>(binPath.Split(char.Parse("/")));
                                        int numPrefixFolders = longestPrefix.Split(char.Parse("/")).Length;
                                        string binPath2 = string.Join("/", split.ToArray().Skip(numPrefixFolders).ToArray());

                                        if (contents2.Contains(binPath2))
                                        {
                                            PAKPackCMD($"replace \"{file2}\" {binPath2} \"{f}\" \"{file2}\"");
                                            PAKPackCMD($"replace \"{bin}\" {longestPrefix} \"{file2}\" \"{bin}\"");
                                        }
                                        else
                                        {
                                            string longestPrefix2 = "";
                                            int longestPrefixLen2 = 0;
                                            foreach (var c in contents2)
                                            {
                                                int prefixLen = commonPrefixUtil(c, binPath2);
                                                if (prefixLen > longestPrefixLen2)
                                                {
                                                    longestPrefix2 = c;
                                                    longestPrefixLen2 = prefixLen;
                                                }
                                            }
                                            // Check if we can unpack again
                                            if (Path.GetExtension(longestPrefix2).ToLower() == ".bin"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".abin"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".arc"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".pak"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".pac"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".pack")
                                            {
                                                string file3 = $@"{temp}\{Path.ChangeExtension(longestPrefix.Replace("/", "\\"), null)}\{longestPrefix2.Replace("/", "\\")}";
                                                PAKPackCMD($"unpack \"{file2}\"");
                                                List<string> contents3 = getFileContents(file3);

                                                List<string> split2 = new List<string>(binPath2.Split(char.Parse("/")));
                                                int numPrefixFolders2 = longestPrefix2.Split(char.Parse("/")).Length;
                                                string binPath3 = string.Join("/", split2.ToArray().Skip(numPrefixFolders2).ToArray());

                                                if (contents3.Contains(binPath3))
                                                {
                                                    PAKPackCMD($"replace \"{file3}\" {binPath3} \"{f}\" \"{file3}\"");
                                                    PAKPackCMD($"replace \"{file2}\" {longestPrefix2} \"{file3}\" \"{file2}\"");
                                                    PAKPackCMD($"replace \"{bin}\" {longestPrefix} \"{file2}\" \"{bin}\"");
                                                }
                                            }
                                            else if (Path.GetExtension(longestPrefix2).ToLower() == ".spd" && (Path.GetExtension(f).ToLower() == ".dds" || Path.GetExtension(f).ToLower() == ".spdspr"))
                                            {
                                                PAKPackCMD($"unpack \"{file2}\"");
                                                string spdPath = $@"{temp}\{Path.ChangeExtension(longestPrefix.Replace("/", "\\"), null)}\{longestPrefix2.Replace("/", "\\")}";
                                                if (Path.GetExtension(f).ToLower() == ".dds")
                                                    spdUtils.replaceDDS(spdPath, f);
                                                else
                                                    spdUtils.replaceSPDKey(spdPath, f);
                                                Console.WriteLine($"[INFO] Replacing {spdPath} in {f}");
                                                PAKPackCMD($"replace \"{file2}\" {longestPrefix2} \"{spdPath}\" \"{file2}\"");
                                                PAKPackCMD($"replace \"{bin}\" {longestPrefix} \"{file2}\" \"{bin}\"");
                                            }
                                            else if (Path.GetExtension(longestPrefix2).ToLower() == ".spr" && Path.GetExtension(f).ToLower() == ".tmx")
                                            {
                                                PAKPackCMD($"unpack \"{file2}\"");
                                                string sprPath = $@"{temp}\{Path.ChangeExtension(longestPrefix.Replace("/", "\\"), null)}\{longestPrefix2.Replace("/", "\\")}";
                                                sprUtils.replaceTmx(sprPath, f);
                                                PAKPackCMD($"replace \"{file2}\" {longestPrefix2} \"{sprPath}\" \"{file2}\"");
                                                PAKPackCMD($"replace \"{bin}\" {longestPrefix} \"{file2}\" \"{bin}\"");
                                            }
                                        }
                                    }
                                    else if (Path.GetExtension(longestPrefix).ToLower() == ".spd" && (Path.GetExtension(f).ToLower() == ".dds" || Path.GetExtension(f).ToLower() == ".spdspr"))
                                    {
                                        string spdPath = $@"{temp}\{longestPrefix.Replace("/", "\\")}";
                                        if (Path.GetExtension(f).ToLower() == ".dds")
                                            spdUtils.replaceDDS(spdPath, f);
                                        else
                                            spdUtils.replaceSPDKey(spdPath, f);
                                        Console.WriteLine($"[INFO] Replacing {spdPath} in {f}");
                                        PAKPackCMD($"replace \"{bin}\" {longestPrefix} \"{spdPath}\" \"{bin}\"");
                                    }
                                    else if (Path.GetExtension(longestPrefix).ToLower() == ".spr" && Path.GetExtension(f).ToLower() == ".tmx")
                                    {
                                        string path = longestPrefix.Replace("../", "");
                                        string sprPath = $@"{temp}\{path.Replace("/", "\\")}";
                                        sprUtils.replaceTmx(sprPath, f);
                                        PAKPackCMD($"replace \"{bin}\" {longestPrefix} \"{sprPath}\" \"{bin}\"");
                                    }
                                }
                                else
                                {
                                    string args = $"replace \"{bin}\" {binPath} \"{f}\" \"{bin}\"";
                                    PAKPackCMD(args);
                                }
                            }
                            DeleteDirectory(temp);
                        }
                    }
                    else if (Path.GetExtension(file).ToLower() == ".spd")
                    {
                        Console.WriteLine($@"[INFO] Merging {file}...");
                        string spdFolder = Path.ChangeExtension(file, null);
                        if (Directory.Exists(spdFolder))
                        {
                            foreach (var spdFile in Directory.GetFiles(spdFolder, "*", SearchOption.AllDirectories))
                            {
                                if (Path.GetExtension(spdFile).ToLower() == ".dds")
                                {
                                    Console.WriteLine($"[INFO] Replacing {spdFile} in {file}");
                                    spdUtils.replaceDDS(file, spdFile);
                                }
                                else if (Path.GetExtension(spdFile).ToLower() == ".spdspr")
                                {
                                    spdUtils.replaceSPDKey(file, spdFile);
                                    Console.WriteLine($"[INFO] Replacing {spdFile} in {file}");
                                }
                            }
                        }
                    }
                    else if (Path.GetExtension(file).ToLower() == ".spr")
                    {
                        Console.WriteLine($@"[INFO] Merging {file}...");
                        string sprFolder = Path.ChangeExtension(file, null);
                        if (Directory.Exists(sprFolder))
                        {
                            foreach (var sprFile in Directory.GetFiles(sprFolder, "*", SearchOption.AllDirectories))
                            {
                                Console.WriteLine($"[INFO] Replacing {sprFile} in {file}");
                                sprUtils.replaceTmx(file, sprFile);
                            }
                        }
                    }
                }
            }
            // Go through mod directory again to delete unpacked files after bringing them in
            foreach (var file in Directory.GetFiles(modDir, "*", SearchOption.AllDirectories))
            {
                if ((Path.GetExtension(file).ToLower() == ".bin"
                    || Path.GetExtension(file).ToLower() == ".abin"
                    || Path.GetExtension(file).ToLower() == ".arc"
                    || Path.GetExtension(file).ToLower() == ".pak"
                    || Path.GetExtension(file).ToLower() == ".pac"
                    || Path.GetExtension(file).ToLower() == ".pack"
                    || Path.GetExtension(file).ToLower() == ".spd"
                    || Path.GetExtension(file).ToLower() == ".spr")
                    && Directory.Exists(Path.ChangeExtension(file, null))
                    && Path.GetFileName(Path.ChangeExtension(file, null)) != "result"
                    && Path.GetFileName(Path.ChangeExtension(file, null)) != "panel"
                    && Path.GetFileName(Path.ChangeExtension(file, null)) != "crossword")
                {
                    DeleteDirectory(Path.ChangeExtension(file, null));
                }
            }

            // Hardcoded cases TODO: reimplement extracted folders to have file extensions as part of the name, although would need to refactor every aemulus mod

            if (FileIOWrapper.Exists($@"{modDir}\battle\result.pac") && !FileIOWrapper.Exists($@"{modDir}\battle\result\result.spd") && Directory.Exists($@"{modDir}\battle\result"))
                DeleteDirectory($@"{modDir}\battle\result");
            if (Directory.Exists($@"{modDir}\battle\result\result"))
                DeleteDirectory($@"{modDir}\battle\result\result");
            if (Directory.Exists($@"{modDir}\minigame\crossword\crossword"))
                DeleteDirectory($@"{modDir}\minigame\crossword\crossword");
            if (Directory.Exists($@"{modDir}\field\panel\panel"))
                DeleteDirectory($@"{modDir}\field\panel\panel");
            if (Directory.Exists($@"{modDir}\field\panel") && !Directory.EnumerateFileSystemEntries($@"{modDir}\field\panel").Any())
                DeleteDirectory($@"{modDir}\field\panel");

            if (Directory.Exists($@"{modDir}\minigame\crossword\crossword"))
                DeleteDirectory($@"{modDir}\minigame\crossword\crossword");
            if (Directory.Exists($@"{modDir}\minigame\crossword"))
            {
                foreach (var file in Directory.GetFiles($@"{modDir}\minigame\crossword", "*", SearchOption.AllDirectories))
                    if (Path.GetExtension(file).ToLower() != ".pak")
                        FileIOWrapper.Delete(file);
            }
            if (Directory.Exists($@"{modDir}\minigame\crossword") && !Directory.EnumerateFileSystemEntries($@"{modDir}\minigame\crossword").Any())
                DeleteDirectory($@"{modDir}\minigame\crossword");

            Console.WriteLine("[INFO] Finished merging!");
            return;
        }

        public static void Restart(string modDir, bool emptySND, string game, string cpkLang)
        {
            Console.WriteLine("[INFO] Deleting current mod build...");
            // Revert appended cpks
            if (game == "Persona 4 Golden")
            {
                string path = Path.GetDirectoryName(modDir);
                // Copy original cpk back if different
                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}") && FileIOWrapper.Exists($@"{path}\{cpkLang}") 
                    && GetChecksumString($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}") != GetChecksumString($@"{path}\{cpkLang}"))
                {
                    Console.WriteLine($@"[INFO] Reverting {cpkLang} back to original");
                    FileIOWrapper.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpkLang}", $@"{path}\{cpkLang}", true);
                }
                // Copy original cpk back if different
                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk") && FileIOWrapper.Exists($@"{path}\{cpkLang}")
                    && GetChecksumString($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk") != GetChecksumString($@"{path}\movie.cpk"))
                {
                    Console.WriteLine($@"[INFO] Reverting movie.cpk back to original");
                    FileIOWrapper.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk", $@"{path}\movie.cpk", true);
                }
                // Delete modified pacs
                if (FileIOWrapper.Exists($@"{path}\data00007.pac"))
                {
                    Console.WriteLine($"[INFO] Deleting data00007.pac");
                    FileIOWrapper.Delete($@"{path}\data00007.pac");
                }
                if (FileIOWrapper.Exists($@"{path}\movie00003.pac"))
                {
                    Console.WriteLine($"[INFO] Deleting movie00003.pac");
                    FileIOWrapper.Delete($@"{path}\movie00003.pac");
                }
            }

            if (!emptySND || game == "Persona 3 FES")
            {
                //Console.WriteLine("[INFO] Keeping SND folder.");
                foreach (var dir in Directory.GetDirectories(modDir))
                {
                    if (Path.GetFileName(dir).ToLower() != "snd")
                        DeleteDirectory(dir);
                }
                // Delete top layer files too
                foreach (var file in Directory.GetFiles(modDir))
                {
                    if (Path.GetExtension(file).ToLower() != ".elf" && Path.GetExtension(file).ToLower() != ".iso")
                        FileIOWrapper.Delete(file);
                }
            }
            else
            {
                if (Directory.Exists(modDir))
                    DeleteDirectory(modDir);
                Directory.CreateDirectory(modDir);
            }
        }

        public static string GetChecksumString(string filePath)
        {
            string checksumString = null;

            // get md5 checksum of file
            using (var md5 = MD5.Create())
            {
                using (var stream = FileIOWrapper.OpenRead(filePath))
                {
                    // get hash
                    byte[] currentFileSum = md5.ComputeHash(stream);
                    // convert hash to string
                    checksumString = BitConverter.ToString(currentFileSum).Replace("-", "");
                }
            }

            return checksumString;
        }

        public static void MakeCpk(string modDir, bool UseCrc = true)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\CpkMakeC\cpkmakec.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
            {
                Console.WriteLine($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            if (UseCrc)
                startInfo.Arguments = $"\"{modDir}\" \"{modDir}\".cpk -mode=FILENAME -crc";
            else
                startInfo.Arguments = $"\"{modDir}\" \"{modDir}\".cpk -mode=FILENAME";
            Console.WriteLine($"[INFO] Building {Path.GetFileName(modDir)}.cpk...");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
        }

    }
}
