using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AemulusModManager.Utilities.FileMerging
{
    public static class FlowMerger
    {
        public static void Merge(List<string> ModList, string game)
        {
            if (!Utils.CompilerExists()) return;

            List<string[]> compiledFiles = new List<string[]>();

            foreach (string dir in ModList)
            {
                string[] flowFiles = Directory.GetFiles(dir, "*.flow", SearchOption.AllDirectories);
                foreach (string file in flowFiles)
                {
                    string bf = Path.ChangeExtension(file, "bf");
                    string filePath = Utils.GetRelativePath(bf, dir, game);
                    string[] previousFileArr = compiledFiles.FindLast(p => p[0]== filePath);
                    string previousFile = previousFileArr == null ? null : previousFileArr[2];
                    // Copy a previously compiled bf so it can be merged
                    if (previousFile != null)
                    {
                        File.Copy(previousFile, bf, true);
                    }
                    else
                    {
                        // Get the path of the file in original
                        string ogPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{Utils.GetRelativePath(bf, dir, game, false)}";
                        // Copy the original file to be used as a base
                        if (FileIOWrapper.Exists(ogPath))
                        {
                            File.Copy(ogPath, bf, true);
                        }
                        else
                        {
                            Console.WriteLine($@"[ERROR] Cannot find {ogPath}. Make sure you have unpacked the game's files");
                            continue;
                        }
                    }
                    if (!Utils.Compile(file, bf, game))
                        continue;
                    string[] compiledFile = { filePath, dir, bf };
                    compiledFiles.Add(compiledFile);
                }
            }
        }
    }
}
