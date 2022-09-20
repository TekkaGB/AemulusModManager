using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AemulusModManager.Utilities.FileMerging
{
    class Utils
    {
        public static bool CompilerExists(bool pm1 = false)
        {
            string compilerPath;
            // Decide which compiler it is
            if (!pm1)
            {
                compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\AtlusScriptCompiler\AtlusScriptCompiler.exe";
            }
            else
            {
                compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\PM1MessageScriptEditor\PM1MessageScriptEditor.exe";
            }
            // Check if it exists
            if (!File.Exists(compilerPath))
            {
                Console.WriteLine($"[ERROR] Couldn't find {compilerPath}. Please check if it was blocked by your anti-virus.");
                return false;
            }
            return true;
        }

        // List of script compiler args for each game
        public static Dictionary<string, string[]> gameArgs = new Dictionary<string, string[]>()
        {
            {"Persona 4 Golden", new string[]{ "V1", "P4G", "P4" } },
            {"Persona 4 Golden (Vita)", new string[]{ "V1", "P4G", "P4" } },
            {"Persona 3 FES" , new string[]{ "V1", "P3F", "P3" } },
            {"Persona 5", new string[]{"V3BE", "P5", "P5"} },
            {"Persona 3 Portable", new string[]{"V1", "P3P", "P3"} },
            {"Persona 5 Royal", new string[]{"V3BE", "P5R", "P5"} },
            {"Persona Q2", new string[]{"V2", "PQ2", "SJ"} }
        };

        // Compile a file with script compiler, returning true if it compiled successfully otherwise false
        public static bool Compile(string inFile, string outFile, string game, string language, string modName = "")
        {
            if (!File.Exists(inFile))
                return false;
            // Get the last modified date of the current bf to see if it compiles successfully
            DateTime lastModified;
            try
            {
                lastModified = File.GetLastWriteTime(outFile);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error getting last write time for {outFile}: {e.Message}. Cancelling {Path.GetExtension(outFile)} merging");
                return false;
            }

            // Compile the file
            Console.WriteLine($"[INFO] Compiling {inFile}");
            if (Path.GetExtension(outFile).ToLowerInvariant() == ".pm1")
            {
                string compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\PM1MessageScriptEditor\PM1MessageScriptEditor.exe";
                RunCommand(compilerPath, $"\"{inFile}\"");
            }
            else
            {
                string[] args = gameArgs[game];

                // Persona 5 bmds have a different outformat than their bfs
                if ((game == "Persona 5" || game == "Persona 5 Royal") && Path.GetExtension(inFile).ToLowerInvariant() == ".msg")
                    args[0] = "V1BE";

                if (game == "Persona Q2" && Path.GetExtension(inFile).ToLowerInvariant() == ".msg")
                    args[0] = "V1";

                if (game == "Persona 5 Royal" && language != null && language != "English")
                    args[2] = "P5R_EFIGS";

                string compilerArgs = $"\"{inFile}\" -Compile -OutFormat {args[0]} -Library {args[1]} -Encoding {args[2]} -Hook -Out \"{outFile}\"";
                ScriptCompilerCommand(compilerArgs);
            }

            // Check if the file was written to (successfully compiled)
            if (File.GetLastWriteTime(outFile) > lastModified)
            {
                Console.WriteLine($"[INFO] Finished compiling {inFile}");
                return true;
            }
            else
            {
                // Copy over script compiler mod since there was an error
                string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string newLog = $@"{assemblyPath}\Logs\{modName}{(modName == "" ? "" : " - ")}{Path.GetFileName(inFile)}.log";
                if (File.Exists($@"{assemblyPath}\AtlusScriptCompiler.log"))
                {
                    if (File.Exists(newLog))
                        File.Delete(newLog);
                    if (!Directory.Exists($@"{assemblyPath}\Logs"))
                        Directory.CreateDirectory($@"{assemblyPath}\Logs");
                    File.Move($@"{assemblyPath}\AtlusScriptCompiler.log", newLog);
                }
                Console.WriteLine(@$"[ERROR] Error compiling {inFile}. Check {newLog} for details.");
                return false;
            }
        }

        // Gets the path for a file relative to the game's file system
        // e.g. field/script/...
        public static string GetRelativePath(string file, string dir, string game, bool removeData = true)
        {
            List<string> folders = new List<string>(file.Split(char.Parse("\\")));
            int idx = folders.IndexOf(Path.GetFileName(dir)) + 1;
            if (game == "Persona 4 Golden" && removeData) idx++; // Account for varying data folder names
            folders = folders.Skip(idx).ToList();
            return string.Join("\\", folders.ToArray());
        }

        public static void ScriptCompilerCommand(string args)
        {
            string compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\AtlusScriptCompiler\AtlusScriptCompiler.exe";
            RunCommand(compilerPath, args);
        }

        public static void RunCommand(string file, string args)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"[ERROR] Couldn't find {file}. Please check if it was blocked by your anti-virus.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = $"\"{file}\"";
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

        public static Dictionary<string, string> GetMessages(string file, string fileType)
        {
            try
            {
                // Read the text of the msg
                string messagePattern = @"(\[.+ .+\])\s+((?:\[.*\s+?)+)";
                string text = File.ReadAllText(file).Replace("[x 0x80 0x80]", " ");
                Regex rg = new Regex(messagePattern);
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
                Console.WriteLine($"[ERROR] Error reading {file}: {e.Message}. Cancelling {fileType} merging");
            }
            return null;
        }

        public static void MergeFiles(string game, string[] files, Dictionary<string, string>[] messages, Dictionary<string, string> ogMessages, string language)
        {
            // Compare the messages to find any that need to be overwritten
            Dictionary<string, string> changedMessages = new Dictionary<string, string>();
            foreach (var ogMessage in ogMessages)
            {
                // Check both files to be merged for the current original message
                foreach (var messageArr in messages)
                {
                    if (messageArr.TryGetValue(ogMessage.Key, out string messageContent))
                    {
                        // If the message in the new file is different from the old it needs to be changed
                        if (messageContent != ogMessage.Value)
                        {
                            if (changedMessages.ContainsKey(ogMessage.Key))
                                changedMessages.Remove(ogMessage.Key);
                            changedMessages.Add(ogMessage.Key, messageContent);
                        }
                    }
                }
            }

            // Get any completely new messages
            // (only checks the lower priority msg as the higher one is the one where text is replaced so any additions in there will persist)
            var newMessages = messages[0].Where(m => !ogMessages.ContainsKey(m.Key) && !messages[1].ContainsKey(m.Key));
            // Add all of the new messages to the changed messages
            foreach (var newMessage in newMessages)
                changedMessages.Add(newMessage.Key, newMessage.Value);


            if (changedMessages.Count <= 0)
                return;

            // Modify the current file with the changes
            string msgFile = Path.ChangeExtension(files[1], "msg");
            string fileContent;
            try
            {
                fileContent = File.ReadAllText(msgFile);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error reading {msgFile}: {e.Message}. Cancelling {Path.GetExtension(files[0])} merging");
                return;
            }
            foreach (var message in changedMessages)
            {
                if (!ogMessages.TryGetValue(message.Key, out string ogMessage))
                {
                    fileContent += $"{message.Key}\r\n{message.Value}\r\n";
                }
                else
                {
                    fileContent = fileContent.Replace($"{message.Key}\r\n{ogMessage}", $"{message.Key}\r\n{message.Value}");
                }
            }

            // Make a copy of the unmerged file (.file.back)
            try
            {
                FileIOWrapper.Copy(files[1], files[1] + ".back", true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error backing up {files[1]}: {e.Message}. Cancelling {Path.GetExtension(files[0])} merging");
                return;
            }

            // Write the changes to the msg and compile it
            try
            {
                File.WriteAllText(msgFile, fileContent);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error writing changes to {msgFile}: {e.Message}. Cancelling {Path.GetExtension(files[0])} merging");
                return;
            }
            Compile(msgFile, files[1], game, language);
        }

        // Restore all of the .*.back files to .* for the next time they will be merged
        public static void RestoreBackups(List<string> modList)
        {
            foreach (string modDir in modList)
            {
                // Find all bak files
                string[] bakFiles = Directory.GetFiles(modDir, "*.*.back", SearchOption.AllDirectories);
                foreach (string file in bakFiles)
                {
                    // Replace the existing file with the backup
                    string newFile = file.Substring(0, file.Length - 4);
                    if (File.Exists(newFile))
                        File.Delete(newFile);
                    File.Move(file, newFile);
                }
            }
        }

        /// <summary>
        /// Checks if two files are the same by comparing their last write time
        /// This is not perfect as file contents are not actually compared however, it is significantly faster than comparing file contents/hashes
        /// and will almost always be right (you'd have to actually try to create two different files with the exact same last write time)
        /// </summary>
        /// <param name="file1">The full path to the first file to check</param>
        /// <param name="file2">The full path to the second file to check</param>
        /// <returns>True if the two files have the same last write time, 
        /// false if they do not or if an error occurs checking the files (such as one not existing)</returns>
        public static bool SameFiles(string file1, string file2)
        {
            try
            {
                FileInfo file1Info = new FileInfo(file1);
                FileInfo file2Info = new FileInfo(file2);
                return file1Info.LastWriteTimeUtc.Equals(file2Info.LastWriteTimeUtc);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if two files are the same by comparing their SHA512 hashes
        /// <param name="=file1">The full path to the first file to check</param>
        /// <param name="=file2">The full path to the second file to check</param>
        /// </summary>
        /// <returns>True if the two files have the same hash (they're the same file), 
        /// false if they are different or if an error occurs checking the two files (such as one not existing)</returns>
        public static bool SameFilesByHash(string file1, string file2)
        {
            var timer = new Stopwatch();
            timer.Start();
            try
            {
                byte[] file1Bytes = File.ReadAllBytes(file1);
                byte[] file2Bytes = File.ReadAllBytes(file2);
                var sha512 = new SHA512CryptoServiceProvider();
                var hash1 = sha512.ComputeHash(file1Bytes);
                var hash2 = sha512.ComputeHash(file2Bytes);
                for (int i = 0; i < hash1.Length; i++)
                {
                    if (hash1[i] == hash2[i])
                        return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                timer.Stop();
                Console.WriteLine($@"[INFO] Compared file hashes in {timer.ElapsedMilliseconds}ms");
            }
        }

    }
}
