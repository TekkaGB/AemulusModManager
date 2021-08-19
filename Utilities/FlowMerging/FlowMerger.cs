using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AemulusModManager.Utilities.FlowMerging
{
    public static class FlowMerger
    {
        private static string compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\AtlusScriptCompiler\AtlusScriptCompiler.exe";

        // List of script compiler args for each game
        public static Dictionary<string, string[]> gameArgs = new Dictionary<string, string[]>()
        {
            {"Persona 4 Golden", new string[]{ "V1", "P4G", "P4" } },
            {"Persona 3 FES" , new string[]{ "V1", "P3F", "P3" } },
            {"Persona 5", new string[]{"V3BE", "P5", "P5"} }
        };

        public static void Merge(List<string> ModList, string game)
        {
            if (!File.Exists(compilerPath))
            {
                Console.WriteLine($"[ERROR] Couldn't find {compilerPath}. Please check if it was blocked by your anti-virus.");
                return;
            }

            List<string[]> compiledFiles = new List<string[]>();

            foreach (string dir in ModList)
            {
                string[] flowFiles = Directory.GetFiles(dir, "*.flow", SearchOption.AllDirectories);
                foreach (string file in flowFiles)
                {
                    string filePath = GetRelativePath(file, dir, game);
                    string[] previousFileArr = compiledFiles.FindLast(p => p[0]== filePath);
                    string previousFile = previousFileArr == null ? null : previousFileArr[2];
                    // Copy a previously compiled bf so it can be merged
                    if (previousFile != null)
                    {
                        File.Copy(previousFile, Path.ChangeExtension(file, "bf"), true);
                    }
                    else
                    {
                        // Get the path of the file in original
                        string ogPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{GetRelativePath(file, dir, game, false)}";
                        // Copy the original file to be used as a base
                        if (FileIOWrapper.Exists(ogPath))
                        {
                            File.Copy(ogPath, Path.ChangeExtension(file, "bf"), true);
                        }
                        else
                        {
                            Console.WriteLine($@"[ERROR] Cannot find {ogPath}. Make sure you have unpacked the game's files");
                            continue;
                        }
                    }
                    if (!Compile(file, Path.ChangeExtension(file, "bf"), game))
                        continue;
                    string[] compiledFile = { filePath, dir, Path.ChangeExtension(file, "bf") };
                    compiledFiles.Add(compiledFile);
                }
            }
        }

        // Compile a flow file, returning true if it compiled successfully otherwise false
        public static bool Compile(string inFile, string outFile, string game)
        {
            if (!File.Exists(inFile))
                return false;
            // Get the last modified date of the current bf to see if it compiles successfully
            var lastModified = File.GetLastWriteTime(outFile);

            // Compile the file
            string[] args = gameArgs[game];
            string compilerArgs = $"\"{inFile}\" -Compile -OutFormat {args[0]} -Library {args[1]} -Encoding {args[2]} -Hook -Out \"{outFile}\"";
            Console.WriteLine($"[INFO] Compiling {inFile}");
            ScriptCompilerCommand(compilerArgs);

            // Check if the file was written to (successfully compiled)
            if (File.GetLastWriteTime(outFile) > lastModified)
            {
                Console.WriteLine($"[INFO] Finished compiling {inFile}");
                return true;
            } else
            {
                Console.WriteLine(@$"[ERROR] Error compiling {inFile}. Check {Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\AtlusScriptCompiler.log for details.");
                return false;
            }
        }

        // Gets the path for a file relative to the game's file system
        // e.g. field/script/...
        private static string GetRelativePath(string file, string dir, string game, bool removeData = true)
        {
            List<string> folders = new List<string>(Path.ChangeExtension(file, "bf").Split(char.Parse("\\")));
            int idx = folders.IndexOf(Path.GetFileName(dir)) + 1;
            if (game == "Persona 4 Golden" && removeData) idx++; // Account for varying data folder names
            folders = folders.Skip(idx).ToList();
            return string.Join("\\", folders.ToArray());
        }

        public static void ScriptCompilerCommand(string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = $"\"{compilerPath}\"";
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
    }
}
