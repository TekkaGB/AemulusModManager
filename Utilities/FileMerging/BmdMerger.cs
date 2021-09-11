using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AemulusModManager.Utilities.FileMerging
{
    public static class BmdMerger
    {
        public static void Merge(List<string> ModList, string game)
        {
            if (!Utils.CompilerExists()) return;

            List<string[]> foundBmds = new List<string[]>();

            foreach (string dir in ModList)
            {
                string[] bmdFiles = Directory.GetFiles(dir, "*.bmd", SearchOption.AllDirectories);
                foreach (string file in bmdFiles)
                {
                    string filePath = Utils.GetRelativePath(file, dir, game);
                    string[] previousFileArr = foundBmds.FindLast(p => p[0] == filePath);
                    string previousFile = previousFileArr == null ? null : previousFileArr[2];
                    // Merge bmds if there are two
                    if (previousFile != null)
                    {
                        // Get the path of the file in original
                        string ogPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{Utils.GetRelativePath(file, dir, game, false)}";
                        if (game == "Persona 3 Portable")
                        {
                            ogPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\extracted\{Utils.GetRelativePath(file, dir, game, false)}";
                        }
                        MergeBmds(new string[] { previousFile, file }, ogPath, game);
                    }
                    string[] foundBmd = { filePath, dir, file };
                    foundBmds.Add(foundBmd);
                }
            }
        }

        // Merge two bmds the second one being the higher priority
        private static void MergeBmds(string[] bmds, string ogPath, string game)
        {
            // Check that the original bmd exists
            if (!File.Exists(ogPath))
            {
                Console.WriteLine($@"[WARNING] Cannot find {ogPath}. Make sure you have unpacked the game's files if merging is needed.");
                return;
            }

            // Get the contents of the bmds
            Dictionary<string, string>[] messages = new Dictionary<string, string>[2];
            Dictionary<string, string> ogMessages = new Dictionary<string, string>();
            messages[0] = GetBmdMessages(bmds[0], game);
            messages[1] = GetBmdMessages(bmds[1], game);
            ogMessages = GetBmdMessages(ogPath, game);

            // Compare the messages to find any that need to be overwritten
            Utils.MergeFiles(game, bmds, messages, ogMessages);
        }

        private static Dictionary<string, string> GetBmdMessages(string file, string game)
        {
            try
            {
                // Decompile the bmd to a msg that can be read easily
                string[] args = Utils.gameArgs[game];
                string msgFile = Path.ChangeExtension(file, "msg");
                string compilerArgs = $"\"{file}\" -Decompile -OutFormat {args[0]} -Library {args[1]} -Encoding {args[2]} -Hook -Out \"{msgFile}\"";
                Utils.ScriptCompilerCommand(compilerArgs);

                return Utils.GetMessages(msgFile);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Couldn't read {file}. Cancelling bmd merging");
            }
            return null;
        }
    }
}
