using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AemulusModManager.Utilities;

namespace AemulusModManager
{
    public static class sprUtils
    {
        private static int Search(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        private static byte[] SliceArray(byte[] source, int start, int end)
        {
            int length = end - start;
            byte[] dest = new byte[length];
            Array.Copy(source, start, dest, 0, length);
            return dest;
        }

        private static string getTmxName(byte[] tmx)
        {
            int end = Search(tmx, new byte[] { 0x00 });
            byte[] name = tmx.Take(end).ToArray();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(932).GetString(name);
        }

        public static Dictionary<string, int> getTmxNames(string spr)
        {
            Dictionary<string, int> tmxNames = new Dictionary<string, int>();
            byte[] sprBytes = File.ReadAllBytes(spr);
            byte[] pattern = Encoding.ASCII.GetBytes("TMX0");
            int offset = 0;
            int found = 0;
            while (found != -1)
            {
                // Start search after "TMX0"
                found = Search(SliceArray(sprBytes, offset, sprBytes.Length), pattern);
                offset = found + offset + 4;
                if (found != -1)
                {
                    string ogTmxName = getTmxName(SliceArray(sprBytes, (offset + 24), sprBytes.Length));
                    string tmxName = ogTmxName;
                    int index = 2;
                    while (tmxNames.ContainsKey(tmxName))
                    {
                        tmxName = $"{ogTmxName}({index})";
                        index += 1;
                    }
                    tmxNames.Add(tmxName, offset - 12);
                }

            }
            return tmxNames;
        }

        private static List<int> getTmxOffsets(string spr)
        {
            List<int> tmxOffsets = new List<int>();
            byte[] sprBytes = File.ReadAllBytes(spr);
            byte[] pattern = Encoding.ASCII.GetBytes("TMX0");
            int offset = 0;
            int found = 0;
            while (found != -1)
            {
                // Start search after "TMX0"
                found = Search(SliceArray(sprBytes, offset, sprBytes.Length), pattern);
                offset = found + offset + 4;
                if (found != -1)
                {
                    tmxOffsets.Add(offset - 12);
                }
            }
            return tmxOffsets;
        }

        private static int findTmx(string spr, string tmxName)
        {
            // Get all tmx names instead to prevent replacing similar names
            if (File.Exists(spr))
            {
                Dictionary<string, int> tmxNames = getTmxNames(spr);
                if (tmxNames.ContainsKey(tmxName))
                    return tmxNames[tmxName];
            }
            return -1;
        }

        public static void replaceTmx(string spr, string tmx)
        {
            string tmxPattern = Path.GetFileNameWithoutExtension(tmx);
            int offset = findTmx(spr, tmxPattern);
            if (offset > -1)
            {
                Utilities.ParallelLogger.Log($"[INFO] Merging {tmx} onto {spr}");
                byte[] tmxBytes = File.ReadAllBytes(tmx);
                int repTmxLen = tmxBytes.Length;
                int ogTmxLen = BitConverter.ToInt32(File.ReadAllBytes(spr), (offset + 4));

                if (repTmxLen == ogTmxLen)
                {
                    using (Stream stream = File.Open(spr, FileMode.Open))
                    {
                        stream.Position = offset;
                        stream.Write(tmxBytes, 0, repTmxLen);
                    }
                }
                else // Insert and update offsets
                {
                    byte[] sprBytes = File.ReadAllBytes(spr);
                    byte[] newSpr = new byte[sprBytes.Length + (repTmxLen - ogTmxLen)];
                    SliceArray(sprBytes, 0, offset).CopyTo(newSpr, 0);
                    SliceArray(sprBytes, offset + ogTmxLen, sprBytes.Length).CopyTo(newSpr, offset + repTmxLen);
                    tmxBytes.CopyTo(newSpr, offset);
                    File.WriteAllBytes(spr, newSpr);
                    updateOffsets(spr, getTmxOffsets(spr));
                }
            }
            else
                Utilities.ParallelLogger.Log($"[WARNING] Couldn't find {tmx} in {spr}");
        }

        private static void updateOffsets(string spr, List<int> offsets)
        {
            // Start of tmx offsets
            int pos = 36;
            using (Stream stream = File.Open(spr, FileMode.Open))
            {
                foreach (int offset in offsets)
                {
                    byte[] offsetBytes = BitConverter.GetBytes(offset);
                    stream.Position = pos;
                    stream.Write(offsetBytes, 0, 4);
                    pos += 8;
                }
            }
        }

        public static byte[] extractTmx(string spr, string tmx)
        {
            string tmxPattern = Path.GetFileNameWithoutExtension(tmx);
            int offset = findTmx(spr, tmxPattern);
            if (offset > -1)
            {
                byte[] sprBytes = File.ReadAllBytes(spr);
                int tmxLen = BitConverter.ToInt32(sprBytes, (offset + 4));
                return SliceArray(sprBytes, offset, offset + tmxLen);
            }
            return null;
        }
    }
}
