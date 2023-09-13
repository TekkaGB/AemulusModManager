using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using AemulusModManager.Utilities;
using Pri.LongPath;
using Directory = Pri.LongPath.Directory;
using File = Pri.LongPath.File;
using Path = Pri.LongPath.Path;

namespace AemulusModManager.Utilities.AwbMerging
{
    internal class AwbMerger
    {
        private static string exePath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\SonicAudioTools\AcbEditor.exe";

        private static void RunAcbEditor(string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = exePath;
            if (!FileIOWrapper.Exists(startInfo.FileName))
            {
                Utilities.ParallelLogger.Log($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = args;
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
        }
        public static bool AcbExists(string path)
        {
            return FileIOWrapper.Exists(Path.ChangeExtension(path, ".acb"));
        }
        private static void CopyAndUnpackArchive(string acbPath, string ogAcbPath)
        {
            ogAcbPath = Path.ChangeExtension(ogAcbPath, ".acb");
            acbPath = Path.ChangeExtension(acbPath, ".acb");
            //NOTE: this code assumes that paired acbs and awbs all have the same name, if this is not the case file an issue or do a pr
            string ogAwbPath = Path.ChangeExtension(ogAcbPath, ".awb");
            string awbPath = Path.ChangeExtension(acbPath, ".awb");

            Utilities.ParallelLogger.Log($"[INFO] Copying over {ogAcbPath} to use as base.");
            Directory.CreateDirectory(Path.GetDirectoryName(acbPath));
            FileIOWrapper.Copy(ogAcbPath, acbPath, true);

            if (FileIOWrapper.Exists(ogAwbPath))
            {
                Utilities.ParallelLogger.Log($"[INFO] Copying over {ogAwbPath} to use as base.");
                FileIOWrapper.Copy(ogAwbPath, awbPath, true);
            }

            Utilities.ParallelLogger.Log($"[INFO] Unpacking {acbPath}");
            RunAcbEditor(acbPath);
        }
        public static void Merge(List<string> ModList, string game, string modDir)
        {
            List<string> acbs = new List<string>();
            foreach(string mod in ModList)
            {
                List<string> directories = new List<string>(Directory.EnumerateDirectories(mod, "*", SearchOption.AllDirectories));
                string[] AemIgnore = FileIOWrapper.Exists($@"{mod}\Ignore.aem") ? FileIOWrapper.ReadAllLines($@"{mod}\Ignore.aem") : null;

                foreach (string dir in directories)
                {
                    List<string> folders = new List<string>(dir.Split(char.Parse("\\")));
                    int idx = folders.IndexOf(Path.GetFileName(mod));
                    folders = folders.Skip(idx + 1).ToList();
                    string acbPath = $@"{modDir}\{string.Join("\\", folders.ToArray())}";
                    string ogAcbPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{string.Join("\\", folders.ToArray())}";

                    if (AcbExists(ogAcbPath))
                    {
                        List<string> files = new List<string>(Directory.GetFiles(dir));

                        foreach (string file in files)
                        {
                            if (AemIgnore != null && AemIgnore.Any(file.Contains))
                                continue;
                            if (!Directory.Exists(acbPath))
                            {
                                CopyAndUnpackArchive(acbPath, ogAcbPath);
                                acbs.Add(acbPath);
                            }
                            string fileName = Path.GetFileName(file);
                            FileIOWrapper.Copy(file, $@"{acbPath}\{fileName}", true);
                            Utilities.ParallelLogger.Log($"[INFO] Copying over {file} to {acbPath}");
                        }
                    }
                }
            }
            foreach(string acb in acbs)
            {
                Utilities.ParallelLogger.Log($"[INFO] Repacking {Path.ChangeExtension(acb, ".acb")}");
                RunAcbEditor(acb);
                Directory.Delete(acb, true);
            }
        }
    }
}
