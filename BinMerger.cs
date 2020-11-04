using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AemulusModManager
{
    class binMerge
    {
        private sprUtils sprUtil;
        private string exePath = @"Dependencies\PAKPack.exe";

        public binMerge()
        {
            sprUtil = new sprUtils();
        }

        private byte[] SliceArray(byte[] source, int start, int end)
        {
            int length = end - start;
            byte[] dest = new byte[length];
            Array.Copy(source, start, dest, 0, length);
            return dest;
        }

        // Use PAKPack command
        private void PAKPackCMD(string args)
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

        private List<string> getFileContents(string path)
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
        private int commonPrefixUtil(String str1, String str2)
        {
            String result = "";
            int n1 = str1.Length,
                n2 = str2.Length;

            // Compare str1 and str2  
            for (int i = 0, j = 0;
                     i <= n1 - 1 && j <= n2 - 1;
                     i++, j++)
            {
                if (str1[i] != str2[j])
                {
                    break;
                }
                result += str1[i];
            }

            return result.Length;
        }

        private List<string> getModList(string dir)
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

        public void Unpack(List<string> ModList, string modDir)
        {
            Console.WriteLine("[INFO] Beginning to unpack...");
            foreach (var mod in ModList)
            {
                if (!Directory.Exists(mod))
                {
                    Console.WriteLine($"[ERROR] Cannot find {mod}");
                    continue;
                }
                List<string> modList = getModList(mod);
                // Copy and overwrite everything thats not a bin
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                {
                    // Copy everything except mods.aem and tblpatch to directory
                    if (Path.GetExtension(file).ToLower() != ".aem" && Path.GetExtension(file).ToLower() != ".tblpatch"
                        && Path.GetExtension(file).ToLower() != ".xml" && Path.GetExtension(file).ToLower() != ".png"
                        && Path.GetExtension(file).ToLower() != ".bat" && Path.GetExtension(file).ToLower() != ".txt")
                    {
                        List<string> folders = new List<string>(file.Split(char.Parse("\\")));
                        int idx = folders.IndexOf(Path.GetFileName(mod));
                        folders = folders.Skip(idx+1).ToList();
                        string binPath = $@"{modDir}\{string.Join("\\", folders.ToArray())}";
                        string ogBinPath = $@"Original\{string.Join("\\", folders.ToArray())}";

                        if (Path.GetExtension(file).ToLower() == ".bin"
                            || Path.GetExtension(file).ToLower() == ".arc"
                            || Path.GetExtension(file).ToLower() == ".pak"
                            || Path.GetExtension(file).ToLower() == ".pack"
                            || Path.GetExtension(file).ToLower() == ".spd")
                        {
                            if ((File.Exists(binPath) && !File.Exists(ogBinPath)) || (File.Exists(ogBinPath) && modList.Count > 0))
                            {
                                if (modList.Count == 0)
                                {
                                    Console.WriteLine($"[WARNING] Using {binPath} as base since nothing was specified in mods.aem");
                                    if (!Directory.Exists(Path.GetDirectoryName(binPath)))
                                        Directory.CreateDirectory(Path.GetDirectoryName(binPath));
                                    File.Copy(file, binPath, true);
                                    continue;
                                }
                                
                                Console.WriteLine($@"[INFO] Unpacking {file}...");
                                // Unpack and transfer modified parts if base already exists
                                PAKPackCMD($"unpack \"{file}\"");
                                // Unpack fully before comparing to mods.aem
                                foreach (var f in Directory.GetFiles(Path.ChangeExtension(file, null), "*", SearchOption.AllDirectories))
                                {
                                    if (Path.GetExtension(f).ToLower() == ".bin"
                                    || Path.GetExtension(f).ToLower() == ".arc"
                                    || Path.GetExtension(f).ToLower() == ".pak"
                                    || Path.GetExtension(f).ToLower() == ".pack"
                                    || Path.GetExtension(f).ToLower() == ".spd")
                                    {
                                        Console.WriteLine($@"[INFO] Unpacking {f}...");
                                        PAKPackCMD($"unpack \"{f}\"");
                                        foreach (var f2 in Directory.GetFiles(Path.ChangeExtension(f, null), "*", SearchOption.AllDirectories))
                                        {
                                            if (Path.GetExtension(f2).ToLower() == ".bin"
                                            || Path.GetExtension(f2).ToLower() == ".arc"
                                            || Path.GetExtension(f2).ToLower() == ".pak"
                                            || Path.GetExtension(f2).ToLower() == ".pack"
                                            || Path.GetExtension(f2).ToLower() == ".spd")
                                            {
                                                Console.WriteLine($@"[INFO] Unpacking {f2}...");
                                                PAKPackCMD($"unpack \"{f2}\"");
                                            }
                                            else if (Path.GetExtension(f2) == ".spr")
                                            {
                                                Console.WriteLine($@"[INFO] Unpacking {f2}...");
                                                string sprFolder2 = Path.ChangeExtension(f2, null);
                                                if (!Directory.Exists(sprFolder2))
                                                    Directory.CreateDirectory(sprFolder2);
                                                Dictionary<string, int> tmxNames = sprUtil.getTmxNames(f2);
                                                foreach (string name in tmxNames.Keys)
                                                {
                                                    byte[] tmx = sprUtil.extractTmx(f2, name);
                                                    File.WriteAllBytes($@"{sprFolder2}\{name}.tmx", tmx);
                                                }
                                            }
                                        }
                                    }
                                    else if (Path.GetExtension(f) == ".spr")
                                    {
                                        Console.WriteLine($@"[INFO] Unpacking {f}...");
                                        string sprFolder = Path.ChangeExtension(f, null);
                                        if (!Directory.Exists(sprFolder))
                                            Directory.CreateDirectory(sprFolder);
                                        Dictionary<string, int> tmxNames = sprUtil.getTmxNames(f);
                                        foreach (string name in tmxNames.Keys)
                                        {
                                            byte[] tmx = sprUtil.extractTmx(f, name);
                                            File.WriteAllBytes($@"{sprFolder}\{name}.tmx", tmx);
                                        }
                                    }

                                }

                                // Copy over loose files specified by mods.aem
                                foreach (var m in modList)
                                {
                                    if (File.Exists($@"{mod}\{m}"))
                                    {
                                        if (!Directory.Exists($@"{modDir}\{Path.GetDirectoryName(m)}"))
                                            Directory.CreateDirectory($@"{modDir}\{Path.GetDirectoryName(m)}");
                                        File.Copy($@"{mod}\{m}", $@"{modDir}\{m}", true);
                                    }
                                }

                            }
                            else
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(binPath)))
                                    Directory.CreateDirectory(Path.GetDirectoryName(binPath));
                                File.Copy(file, binPath, true);
                            }
                        }
                        else
                        {
                            if (!Directory.Exists(Path.GetDirectoryName(binPath)))
                                Directory.CreateDirectory(Path.GetDirectoryName(binPath));
                            File.Copy(file, binPath, true);
                            // TODO: Fix async writing to console
                            // Console.WriteLine($"[INFO] Copying over {file} to {binPath}");
                        }
                    }
                }
                // Go through mod directory again to delete unpacked files after bringing them in
                foreach (var file in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
                {
                    if ((Path.GetExtension(file).ToLower() == ".bin"
                        || Path.GetExtension(file).ToLower() == ".arc"
                        || Path.GetExtension(file).ToLower() == ".pak"
                        || Path.GetExtension(file).ToLower() == ".pack"
                        || Path.GetExtension(file).ToLower() == ".spd") 
                        && Directory.Exists(Path.ChangeExtension(file,null)))
                    {
                        DeleteDirectory(Path.ChangeExtension(file, null));
                    }
                }
            }
            Console.WriteLine("[INFO] Finished unpacking!");
        }

        public void Merge(string modDir)
        {
            Console.WriteLine("[INFO] Beginning to merge...");
            List<string> dirs = new List<string>();
            foreach (var dir in Directory.EnumerateDirectories(modDir))
            {
                var name = Path.GetFileName(dir);
                if (name != "patches" || name != "bins" || name != "SND")
                    dirs.Add($@"{modDir}\{name}");
            }
            // Find bins with loose files
            foreach (var dir in dirs)
            {
                // Check if loose folder matches vanilla bin file
                foreach (var d in Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories))
                {
                    List<string> folders = new List<string>(d.Split(char.Parse("\\")));
                    int idx = folders.IndexOf(Path.GetFileName(dir));
                    folders = folders.Skip(idx).ToList();
                    string ogPath = $@"Original\{string.Join("\\", folders.ToArray())}";

                    if (File.Exists(Path.ChangeExtension(ogPath, ".bin")) && !File.Exists(Path.ChangeExtension(d, ".bin")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".bin");
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        File.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    else if (File.Exists(Path.ChangeExtension(ogPath, ".arc")) && !File.Exists(Path.ChangeExtension(d, ".arc")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".arc");
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        if (!Directory.Exists(Path.GetDirectoryName(d)))
                            Directory.CreateDirectory(Path.GetDirectoryName(d));
                        File.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                    else if (File.Exists(Path.ChangeExtension(ogPath, ".pack")) && !File.Exists(Path.ChangeExtension(d, ".pack")))
                    {
                        ogPath = Path.ChangeExtension(ogPath, ".pack");
                        Console.WriteLine($"[INFO] Copying over {ogPath} to use as base.");
                        if (!Directory.Exists(Path.GetDirectoryName(d)))
                            Directory.CreateDirectory(Path.GetDirectoryName(d));
                        File.Copy(ogPath, $@"{Path.GetDirectoryName(d)}\{Path.GetFileName(ogPath)}");
                    }
                }
                
                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(file).ToLower() == ".bin"
                        || Path.GetExtension(file).ToLower() == ".arc"
                        || Path.GetExtension(file).ToLower() == ".pak"
                        || Path.GetExtension(file).ToLower() == ".pack"
                        || Path.GetExtension(file).ToLower() == ".spd")
                    {
                        if (Directory.Exists(Path.ChangeExtension(file, null)))
                        {
                            Console.WriteLine($@"[INFO] Merging {file}...");
                            string bin = file;
                            string binFolder = Path.ChangeExtension(file, null);

                            // Get contents of init_free
                            List<string> contents = getFileContents(bin);

                            // Unpack init_free for future unpacking
                            string temp = $"{binFolder}_temp";
                            PAKPackCMD($"unpack \"{bin}\" \"{temp}\"");

                            foreach (var f in Directory.GetFiles(binFolder, "*", SearchOption.AllDirectories))
                            {
                                // Get bin path used for PAKPack.exe
                                int numParFolders = Path.ChangeExtension(file, null).Split(char.Parse("\\")).Length;
                                List<string> folders = new List<string>(f.Split(char.Parse("\\")));
                                string binPath = string.Join("/", folders.ToArray().Skip(numParFolders).ToArray());
                                // Check if more unpacking needs to be done to replace
                                if (!contents.Contains(binPath))
                                {
                                    string longestPrefix = "";
                                    int longestPrefixLen = 0;
                                    foreach (var c in contents)
                                    {
                                        int prefixLen = commonPrefixUtil(c, binPath);
                                        if (prefixLen > longestPrefixLen)
                                        {
                                            longestPrefix = c;
                                            longestPrefixLen = prefixLen;
                                        }
                                    }
                                    // Check if we can unpack again
                                    if (Path.GetExtension(longestPrefix).ToLower() == ".bin"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".arc"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".pak"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".pack"
                                    || Path.GetExtension(longestPrefix).ToLower() == ".spd")
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
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".arc"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".pak"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".pack"
                                            || Path.GetExtension(longestPrefix2).ToLower() == ".spd")
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
                                            else if (Path.GetExtension(longestPrefix2) == ".spr" && Path.GetExtension(f) == ".tmx")
                                            {
                                                PAKPackCMD($"unpack \"{file2}\"");
                                                string sprPath = $@"{temp}\{Path.ChangeExtension(longestPrefix.Replace("/", "\\"), null)}\{longestPrefix2.Replace("/", "\\")}";
                                                sprUtil.replaceTmx(sprPath, f);
                                                PAKPackCMD($"replace \"{file2}\" {longestPrefix2} \"{sprPath}\" \"{file2}\"");
                                                PAKPackCMD($"replace \"{bin}\" {longestPrefix} \"{file2}\" \"{bin}\"");
                                            }
                                        }
                                    }
                                    else if (Path.GetExtension(longestPrefix) == ".spr" && Path.GetExtension(f) == ".tmx")
                                    {
                                        string sprPath = $@"{temp}\{longestPrefix.Replace("/", "\\")}";
                                        sprUtil.replaceTmx(sprPath, f);
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
                }
            }
            // Go through mod directory again to delete unpacked files after bringing them in
            foreach (var file in Directory.GetFiles(modDir, "*", SearchOption.AllDirectories))
            {
                if ((Path.GetExtension(file).ToLower() == ".bin"
                    || Path.GetExtension(file).ToLower() == ".arc"
                    || Path.GetExtension(file).ToLower() == ".pak"
                    || Path.GetExtension(file).ToLower() == ".pack"
                    || Path.GetExtension(file).ToLower() == ".spd")
                    && Directory.Exists(Path.ChangeExtension(file, null)))
                {
                    DeleteDirectory(Path.ChangeExtension(file, null));
                }
            }
            Console.WriteLine("[INFO] Finished merging!");
            return;
        }

        public void Restart(string modDir, bool emptySND)
        {
            Console.WriteLine("[INFO] Deleting current mod build...");
            if (!emptySND)
            {
                Console.WriteLine("[INFO] Keeping SND folder.");
                foreach (var dir in Directory.EnumerateDirectories(modDir))
                {
                    if (Path.GetFileName(dir).ToLower() != "snd")
                        DeleteDirectory(dir);
                }
                // Delete top layer files too
                foreach (var file in Directory.EnumerateFiles(modDir))
                {
                    File.Delete(file);
                }
            }
            else
            {
                DeleteDirectory(modDir);
                Directory.CreateDirectory(modDir);
            }
        }
    }
}
