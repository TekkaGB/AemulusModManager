using AtlusScriptLibrary.Common.Collections;
using AtlusScriptLibrary.Common.Libraries;
using AtlusScriptLibrary.Common.Logging;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.FlowScriptLanguage.Compiler;
using AtlusScriptLibrary.FlowScriptLanguage.Syntax;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FlowFormatVersion = AtlusScriptLibrary.FlowScriptLanguage.FormatVersion;
using MsgFormatVersion = AtlusScriptLibrary.MessageScriptLanguage.FormatVersion;

namespace AemulusModManager.Utilities.FileMerging
{
    class Utils
    {
        static AtlusLogListener logListener = new AtlusLogListener(LogLevel.Error);
        public static bool CompilerExists(bool pm1 = false)
        {
            string compilerPath;
            // Decide which compiler it is
            if (!pm1)
            {
                compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\AtlusScriptLibrary.dll";
            }
            else
            {
                compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\PM1MessageScriptEditor\PM1MessageScriptEditor.exe";
            }
            // Check if it exists
            if (!File.Exists(compilerPath))
            {
                Utilities.ParallelLogger.Log($"[ERROR] Couldn't find {compilerPath}. Please check if it was blocked by your anti-virus.");
                return false;
            }
            return true;
        }

        struct GameCompilerInfo
        {
            internal Library Library { get; }
            internal Encoding Encoding { get; }
            internal FlowFormatVersion FlowFormatVersion { get; }
            internal MsgFormatVersion MsgFormatVersion { get; }

            internal GameCompilerInfo(Library library, Encoding encoding, FlowFormatVersion flowFormatVersion, MsgFormatVersion msgFormatVersion)
            {
                Library = library;
                Encoding = encoding;
                FlowFormatVersion = flowFormatVersion;
                MsgFormatVersion = msgFormatVersion;
            }
        }

        static Dictionary<string, GameCompilerInfo> compilerInfos = new Dictionary<string, GameCompilerInfo>()
        {
            {"Persona 4 Golden", new GameCompilerInfo(LibraryLookup.GetLibrary("P4G"), AtlusEncoding.GetByName("P4"), FlowFormatVersion.Version1, MsgFormatVersion.Version1) },
            {"Persona 4 Golden (Vita)", new GameCompilerInfo(LibraryLookup.GetLibrary("P4G"), AtlusEncoding.GetByName("P4"), FlowFormatVersion.Version1, MsgFormatVersion.Version1) },
            {"Persona 3 FES", new GameCompilerInfo(LibraryLookup.GetLibrary("P3F"), AtlusEncoding.GetByName("P3"), FlowFormatVersion.Version1, MsgFormatVersion.Version1) },
            {"Persona 5", new GameCompilerInfo(LibraryLookup.GetLibrary("P5"), AtlusEncoding.GetByName("P5"), FlowFormatVersion.Version3BigEndian, MsgFormatVersion.Version1BigEndian) },
            {"Persona 3 Portable", new GameCompilerInfo(LibraryLookup.GetLibrary("P3P"), AtlusEncoding.GetByName("P3"), FlowFormatVersion.Version1, MsgFormatVersion.Version1) },
            {"Persona 5 Royal (PS4)", new GameCompilerInfo(LibraryLookup.GetLibrary("P5R"), AtlusEncoding.GetByName("P5"), FlowFormatVersion.Version3BigEndian, MsgFormatVersion.Version1BigEndian) },
            {"Persona 5 Royal (Switch)", new GameCompilerInfo(LibraryLookup.GetLibrary("P5R"), AtlusEncoding.GetByName("P5"), FlowFormatVersion.Version3BigEndian, MsgFormatVersion.Version1BigEndian) },
            {"Persona Q2", new GameCompilerInfo(LibraryLookup.GetLibrary("PQ2"), ShiftJISEncoding.Instance, FlowFormatVersion.Version2, MsgFormatVersion.Version1) },
        };

        // Compile a file with script compiler, returning true if it compiled successfully otherwise false
        public static bool Compile(string inFilePath, string outFile, string game, string language, string modName = "")
        {
            if (!File.Exists(inFilePath))
                return false;
            // Get the last modified date of the current bf to see if it compiles successfully
            DateTime lastModified;
            try
            {
                lastModified = File.GetLastWriteTime(outFile);
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error getting last write time for {outFile}: {e.Message}. Cancelling {Path.GetExtension(outFile)} merging");
                return false;
            }

            // Compile the file
            Utilities.ParallelLogger.Log($"[INFO] Compiling {inFilePath}");
            string extension = Path.GetExtension(outFile).ToLowerInvariant();
            if (extension == ".pm1")
            {
                string compilerPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\PM1MessageScriptEditor\PM1MessageScriptEditor.exe";
                RunCommand(compilerPath, $"\"{inFilePath}\"");
            }
            else if (extension == ".bf")
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                FlowFormatVersion format = compilerInfos[game].FlowFormatVersion;

                // Persona 5 bmds have a different outformat than their bfs
                if ((game == "Persona 5" || game == "Persona 5 Royal (PS4)" || game == "Persona 5 Royal (Switch)") && Path.GetExtension(inFilePath).ToLowerInvariant() == ".msg")
                    format = FlowFormatVersion.Version1BigEndian;

                if (game == "Persona Q2" && Path.GetExtension(inFilePath).ToLowerInvariant() == ".msg")
                    format = FlowFormatVersion.Version1;

                var compiler = new FlowScriptCompiler(format);

                compiler.Library = compilerInfos[game].Library;
                compiler.Encoding = compilerInfos[game].Encoding;
                compiler.ProcedureHookMode = ProcedureHookMode.ImportedOnly;

                if ((game == "Persona 5 Royal (PS4)" || game == "Persona 5 Royal (Switch)") && language != null && language != "English")
                    compiler.Encoding = AtlusEncoding.GetByName("P5R_EFIGS");

                var inFile = System.IO.File.Open(inFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                compiler.AddListener(logListener);

                if (!compiler.TryCompile(inFile, out FlowScript flowScript))
                {
                    Utilities.ParallelLogger.Log($"[ERROR] Error compiling {inFilePath}");
                    watch.Stop();
                    return false;
                }

                try
                {
                    var outFileInfo = new FileInfo(outFile);
                    while (IsFileLocked(outFileInfo)) ;
                    flowScript.ToFile(outFile);
                }
                catch (Exception e)
                {
                    Utilities.ParallelLogger.Log($"[ERROR] Error writing flowscript to {outFile}: {e.Message}");
                }
                watch.Stop();
                Utilities.ParallelLogger.Log($"[INFO] {outFile} compiled successfully in {watch.ElapsedMilliseconds}ms");
                return true;
            }
            else
            {
                Utilities.ParallelLogger.Log($"[ERROR] {extension} is not a supported file type, not compiling");
                return false;
            }

            // Check if the file was written to (successfully compiled)
            if (File.GetLastWriteTime(outFile) > lastModified)
            {
                Utilities.ParallelLogger.Log($"[INFO] Finished compiling {inFilePath}");
                return true;
            }
            else
            {
                // Copy over script compiler mod since there was an error
                string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string newLog = $@"{assemblyPath}\Logs\{modName}{(modName == "" ? "" : " - ")}{Path.GetFileName(inFilePath)}.log";
                if (File.Exists($@"{assemblyPath}\AtlusScriptCompiler.log"))
                {
                    if (File.Exists(newLog))
                        File.Delete(newLog);
                    if (!Directory.Exists($@"{assemblyPath}\Logs"))
                        Directory.CreateDirectory($@"{assemblyPath}\Logs");
                    File.Move($@"{assemblyPath}\AtlusScriptCompiler.log", newLog);
                }
                Utilities.ParallelLogger.Log(@$"[ERROR] Error compiling {inFilePath}. Check {newLog} for details.");
                return false;
            }
        }
        public static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }

            //file is not locked
            return false;
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

        public static void RunCommand(string file, string args)
        {
            if (!File.Exists(file))
            {
                Utilities.ParallelLogger.Log($"[ERROR] Couldn't find {file}. Please check if it was blocked by your anti-virus.");
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
                Utilities.ParallelLogger.Log($"[ERROR] Error reading {file}: {e.Message}. Cancelling {fileType} merging");
            }
            return null;
        }

        /// <summary>
        /// Returns the <see cref="MessageScript"/> for a bmd file
        /// </summary>
        /// <param name="filePath">The full path to the bmd file</param>
        /// <param name="game">The game this is for</param>
        /// <returns>A <see cref="MessageScript"/> for the file or null if the file couldn't be parsed</returns>
        public static MessageScript MessageScriptFromBmd(string filePath, string game)
        {
            try
            {
                return MessageScript.FromFile(filePath, compilerInfos[game].MsgFormatVersion, compilerInfos[game].Encoding);
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Unable to parse bmd {filePath}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Merges multiple <see cref="MessageScript"/>s into one
        /// </summary>
        /// <param name="baseMessages">The <see cref="MessageScript"/> that <paramref name="newMessages"/> are based off of, the merges will be applied to this</param>
        /// <param name="newMessages">An array of <see cref="MessageScript"/>s to merge together, the priority of messages is based on the order in the array (the last item will have highest priority)</param>
        /// <returns>The number of changes that were made</returns>
        public static int MergeMessageScripts(MessageScript baseMessages, MessageScript[] newMessages, string game)
        {
            int numChanges = 0;
            // Replace and add all of the messages to ogMessages
            List<IDialog> originalMsgs = new List<IDialog>();
            var library = compilerInfos[game].Library;
            foreach (var messageFile in newMessages)
            {
                foreach (var newMessage in messageFile.Dialogs)
                {
                    var existingMsg = baseMessages.Dialogs.FirstOrDefault(m => m.Name == newMessage.Name);
                    if (existingMsg == null)
                    {
                        numChanges++;
                        baseMessages.Dialogs.Add(newMessage);
                    }
                    else
                    {
                        if (!DialogsEqual(existingMsg, newMessage, library))
                        {
                            // Only replace the message if it is different from the original one
                            if (!originalMsgs.Any(m => DialogsEqual(m, newMessage, library)))
                            {
                                numChanges++;
                                originalMsgs.Add(existingMsg);
                                baseMessages.Dialogs.Insert(baseMessages.Dialogs.IndexOf(existingMsg), newMessage);
                                baseMessages.Dialogs.Remove(existingMsg);
                            }
                        }
                    }
                }
            }
            
            return numChanges;
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
                Utilities.ParallelLogger.Log($"[ERROR] Error reading {msgFile}: {e.Message}. Cancelling {Path.GetExtension(files[0])} merging");
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
                File.Copy(files[1], files[1] + ".back", true);
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error backing up {files[1]}: {e.Message}. Cancelling {Path.GetExtension(files[0])} merging");
                return;
            }

            // Write the changes to the msg and compile it
            try
            {
                File.WriteAllText(msgFile, fileContent);
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error writing changes to {msgFile}: {e.Message}. Cancelling {Path.GetExtension(files[0])} merging");
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
                Utilities.ParallelLogger.Log($@"[INFO] Compared file hashes in {timer.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// Checks if two dialogs are equal
        /// </summary>
        /// <param name="dialogA">The first dialog</param>
        /// <param name="dialogB">The second dialog</param>
        /// <param name="library">The library that these were decompiled/compiled with</param>
        /// <returns>True if the two dialogs are exactly the same (excluding "unused" functions), false otherwise</returns>
        public static bool DialogsEqual(IDialog dialogA, IDialog dialogB, Library library)
        {
            if (dialogA.Name != dialogB.Name || dialogA.Lines.Count != dialogB.Lines.Count)
                return false;
            for (int i = 0; i < dialogA.Lines.Count; i++)
            {
                var lineA = dialogA.Lines[i];
                var lineB = dialogB.Lines[i];
                // Remove unused functions
                lineA.Tokens.RemoveAll(t => t.Kind == TokenKind.Function && IsUnusedMsgFunction((FunctionToken)t, library));
                lineB.Tokens.RemoveAll(t => t.Kind == TokenKind.Function && IsUnusedMsgFunction((FunctionToken)t, library));
                if (lineA.Tokens.Count != lineB.Tokens.Count)
                {
                    return false;
                }
                else
                {
                    for (int j = 0; j < lineA.Tokens.Count; j++)
                    {
                        var tokenA = lineA.Tokens[j];
                        var tokenB = lineB.Tokens[j];
                        if (tokenA.Kind != tokenB.Kind)
                            return false;
                        switch (tokenA.Kind)
                        {
                            case TokenKind.String:
                                if (((StringToken)tokenA).Value != ((StringToken)tokenB).Value)
                                    return false;
                                break;
                            case TokenKind.Function:
                                if (!FunctionTokenEquals((FunctionToken)tokenA, (FunctionToken)tokenB))
                                    return false;
                                break;
                            case TokenKind.CodePoint:
                                if (((CodePointToken)tokenA).ToString() != ((CodePointToken)tokenB).ToString())
                                    return false;
                                break;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if two function tokens are exactly the same (same function with same arguments)
        /// </summary>
        /// <param name="a">The first function token</param>
        /// <param name="b">The second function token</param>
        /// <returns>True if the two tokens are exactly the saem, false otherwise</returns>
        public static bool FunctionTokenEquals(FunctionToken a, FunctionToken b)
        {
            if (a.FunctionTableIndex != b.FunctionTableIndex
                || a.FunctionIndex != b.FunctionIndex
                || a.Arguments.Count != b.Arguments.Count)
                return false;
            for (int i = 0; i < a.Arguments.Count; i++)
            {
                if (a.Arguments[i] != b.Arguments[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Check if a message function is an "Unused" one (excluded from decompilation outputs and unneccesary to have in msgs)
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <param name="library">The library the token is from</param>
        /// <returns>True if the token is marked as Unused, false if the function cannot be found in the library or it is not marked as Unused</returns>
        public static bool IsUnusedMsgFunction(FunctionToken token, Library library)
        {
            var functionTable = library.MessageScriptLibraries.FirstOrDefault(x => x.Index == token.FunctionTableIndex);
            if (functionTable != null)
            {
                var function = functionTable.Functions.FirstOrDefault(x => x.Index == token.FunctionIndex);
                if (function != null)
                {
                    return function.Semantic == MessageScriptLibraryFunctionSemantic.Unused;
                }
            }
            return false;
        }
    }
}
