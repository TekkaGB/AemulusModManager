using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AemulusModManager.Utilities.FileMerging
{
    public static class BmdMerger
    {
        public static void Merge(List<string> ModList, string game, string language)
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
                        MergeBmds(new string[] { previousFile, file }, ogPath, game, language);
                    }
                    string[] foundBmd = { filePath, dir, file };
                    foundBmds.Add(foundBmd);
                }
            }
        }

        // Merge two bmds the second one being the higher priority
        private static void MergeBmds(string[] bmds, string ogPath, string game, string language)
        {
            // Check that the original bmd exists
            if (!File.Exists(ogPath))
            {
                Utilities.ParallelLogger.Log($@"[WARNING] Cannot find {ogPath}. Make sure you have unpacked the game's files if merging is needed.");
                return;
            }

            // Get the contents of the bmds
            MessageScript[] messages = new MessageScript[2];
            var originalMessages = Utils.MessageScriptFromBmd(ogPath, game);
            messages[0] = Utils.MessageScriptFromBmd(bmds[0], game);
            messages[1] = Utils.MessageScriptFromBmd(bmds[1], game);

            if (messages[0] == null || messages[1] == null || originalMessages == null)
                return;

            // Compare the messages to find any that need to be overwritten (changes go into originalMessages)
            int numChanges = Utils.MergeMessageScripts(originalMessages, messages, game);
            Utilities.ParallelLogger.Log($"[INFO] Merged {bmds[0]} with {bmds[1]} with {numChanges} changes");

            // Make a copy of the unmerged bmd (.bmd.back)
            try
            {
                File.Copy(bmds[1], bmds[1] + ".back", true);
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error backing up {bmds[1]}: {e.Message}. Cancelling bmd merging");
                return;
            }

            // Compile the new bmd
            try
            {
                originalMessages.ToFile(bmds[1]);
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error compiling merged bmd to {bmds[1]}: {e.Message}");
                return;
            }

        }
    }
}
