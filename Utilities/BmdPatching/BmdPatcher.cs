using AemulusModManager.Utilities.FlowMerging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AemulusModManager.Utilities.BmdPatching
{
    public static class BmdPatcher
    {
        static string messagePattern = @"// index [0-9]+\s+(\[.*?\])(.*?)(?=(?:// index)|\Z)";
        private static string compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\AtlusScriptCompiler\AtlusScriptCompiler.exe";

        public static void Merge(List<string> ModList, string game)
        {
            if (!File.Exists(compilerPath))
            {
                Console.WriteLine($"[ERROR] Couldn't find {compilerPath}. Please check if it was blocked by your anti-virus.");
                return;
            }

            List<string[]> foundBmds = new List<string[]>();

            foreach (string dir in ModList)
            {
                string[] bmdFiles = Directory.GetFiles(dir, "*.bmd", SearchOption.AllDirectories);
                foreach (string file in bmdFiles)
                {
                    string filePath = GetRelativePath(file, dir, game);
                    string[] previousFileArr = foundBmds.FindLast(p => p[0] == filePath);
                    // TODO make thing use a bmd.bak instead of bmd if it exists
                    string previousFile = previousFileArr == null ? null : previousFileArr[2];
                    // Copy a previously compiled bf so it can be merged
                    if (previousFile != null)
                    {
                        // Get the path of the file in original
                        string ogPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{GetRelativePath(file, dir, game, false)}";
                        MergeBmds(new string[] { previousFile, file }, ogPath, game);
                    }
                    else
                    {
                        string[] foundBmd = { filePath, dir, file };
                        foundBmds.Add(foundBmd);
                    }
                }
            }
        }

        private static string GetRelativePath(string file, string dir, string game, bool removeData = true)
        {
            List<string> folders = new List<string>(file.Split(char.Parse("\\")));
            int idx = folders.IndexOf(Path.GetFileName(dir)) + 1;
            if (game == "Persona 4 Golden" && removeData) idx++; // Account for varying data folder names
            folders = folders.Skip(idx).ToList();
            return string.Join("\\", folders.ToArray());
        }

        // Merge two bmds the second one being the higher priority
        private static void MergeBmds(string[] bmds, string ogPath, string game)
        {
            // Check that the original bmd exists
            if (!File.Exists(ogPath))
            {
                Console.WriteLine($@"[ERROR] Cannot find {ogPath}. Make sure you have unpacked the game's files");
                return;
            }

            // Get the contents of the bmds
            Dictionary<string, string>[] messages = new Dictionary<string, string>[2];
            Dictionary<string, string> ogMessages = new Dictionary<string, string>();
            messages[0] = GetBmdMessages(bmds[0], game);
            messages[1] = GetBmdMessages(bmds[1], game);
            ogMessages = GetBmdMessages(ogPath, game);

            // Compare the messages to find any that need to be overwritten
            Dictionary<string, string> changedMessages = new Dictionary<string, string>();
            foreach (var ogMessage in ogMessages)
            {
                // Check both bmds to be merged for the current original message
                foreach (var messageArr in messages)
                {
                    if (messageArr.TryGetValue(ogMessage.Key, out string messageContent))
                    {
                        // If the message in the new bmd is different from the old it needs to be changed
                        if (messageContent != ogMessage.Value)
                        {
                            changedMessages.Add(ogMessage.Key, messageContent);
                        }
                    }
                    else
                    {
                        // The message wasn't in the original, therefore it should be changed (as it's new)
                        changedMessages.Add(ogMessage.Key, messageContent);
                    }
                }
            }

            if (changedMessages.Count <= 0)
                return;
            try
            {
                // Modify the current bmd with the changes
                string msgFile = Path.ChangeExtension(bmds[1], "msg");
                string bmdContent = File.ReadAllText(msgFile);
                foreach (var message in changedMessages)
                {
                    if (!ogMessages.TryGetValue(message.Key, out string ogMessage)) return;
                    bmdContent = bmdContent.Replace($"{message.Key}\n{ogMessage}", $"{message.Key}\n{message.Value}");
                }
                // Make a copy of the unmerged bmd (.bmd.bak)
                FileIOWrapper.Copy(bmds[1], bmds[1] + ".bak", true);

                // Write the changes to the msg and compile it
                File.WriteAllText(msgFile, bmdContent);
                FlowMerger.Compile(msgFile, bmds[1], game);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error reading {bmds[1]}. Cancelling bmd merging");
            }
        }

        private static Dictionary<string, string> GetBmdMessages(string file, string game)
        {
            try
            {
                // Decompile the bmd to a msg that can be read easily
                string[] args = FlowMerger.gameArgs[game];
                string msgFile = Path.ChangeExtension(file, "msg");
                string compilerArgs = $"\"{file}\" -Decompile -OutFormat {args[0]} -Library {args[1]} -Encoding {args[2]} -Hook -Out \"{msgFile}\"";
                FlowMerger.ScriptCompilerCommand(compilerArgs);

                // Read the text of the msg
                string text = File.ReadAllText(msgFile);
                Regex rg = new Regex(messagePattern, RegexOptions.Singleline);
                MatchCollection matches = rg.Matches(text);
                // Add all of the found messages into the list to be returned
                Dictionary<string, string> messages = new Dictionary<string, string>();
                foreach (Match match in matches)
                {
                    // Group[1] is the message name e.g [msg name]
                    // Group[2] is the actual message content
                    messages.Add(match.Groups[1].Value, match.Groups[2].Value);

                }
                return messages;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error reading {file}. Cancelling bmd merging");
            }
            return null;
        }

        // Restore all of the .bmd.bak files to .bmd for the next time they will be merged
        public static void RestoreBackups(List<string> modList)
        {
            foreach(string modDir in modList)
            {
                // Find all bak files
                string[] bakFiles = Directory.GetFiles(modDir, "*.bmd.bak", SearchOption.AllDirectories);
                foreach(string file in bakFiles)
                {
                    // Replace the existing bmd with the backup
                    string bmd = file.Substring(0, file.Length - 4);
                    if (File.Exists(bmd))
                        File.Delete(bmd);
                    File.Move(file, bmd);
                }
            }
        }
    }
}
