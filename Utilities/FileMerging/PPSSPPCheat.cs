using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AemulusModManager.Utilities.FileMerging
{
    internal class PPSSPPCheat
    {
        internal List<string> Contents { get; set; }
        internal bool Enabled { get; set; }
        internal string Name { get; set; }

        internal PPSSPPCheat(string name, List<string> contents, bool enabled)
        {
            Name = name;
            Contents = contents;
            Enabled = enabled;
        }
    }

    internal class PPSSPPCheatFile
    {
        internal List<PPSSPPCheat> Cheats { get; set; }
        internal string GameID { get; set; }
        internal string GameName { get; set; }
        internal PPSSPPCheatFile(List<PPSSPPCheat> cheats, string gameID, string gameName)
        {
            Cheats = cheats;
            GameID = gameID;
            GameName = gameName;
        }

        /// <summary>
        /// Gets a list of all of the cheats in a PPSSPP cheat ini
        /// </summary>
        /// <param name="cheatFilePath">A full path to a PPSSPP cheat ini file</param>
        /// <returns>A List of <see cref="PPSSPPCheat"/>s representing all of those in the file</returns>
        internal static PPSSPPCheatFile ParseCheats(string cheatFilePath)
        {
            PPSSPPCheatFile cheatFile = new PPSSPPCheatFile(new List<PPSSPPCheat>(), "", "");
            
            try
            {
                PPSSPPCheat currentCheat = null;
                foreach (var line in File.ReadLines(cheatFilePath))
                {
                    // Game id
                    if(Regex.IsMatch(line, @"^_S .*")) {
                        cheatFile.GameID = line.Substring(3);
                        continue;
                    }

                    // Game name
                    if (Regex.IsMatch(line, @"^_G .*"))
                    {
                        cheatFile.GameName = line.Substring(3);
                        continue;
                    }

                    // Start of a new cheat
                    if (Regex.IsMatch(line, @"^_C[01] \S"))
                    {
                        if (currentCheat != null)
                            cheatFile.Cheats.Add(currentCheat);
                        var match = Regex.Match(line, @"^_C([01]) (.*)");
                        currentCheat = new PPSSPPCheat(match.Groups[2].Value, new List<string>(), match.Groups[1].Value == "1");
                        continue;
                    }

                    if (currentCheat != null)
                        currentCheat.Contents.Add(line);
                }
                cheatFile.Cheats.Add(currentCheat);
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Unable to parse cheats of {cheatFilePath}: {e.Message}");
            }
            return cheatFile;
        }
    }
}
