using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AemulusModManager.Utilities;


namespace AemulusModManager
{
    public class DDS
    {
        public string name;
        public long pos;
        public int size;
        public byte[] file;
    }
    public class SPDKey
    {
        public int id;
        public int pos;
        public byte[] file;
    }
    public class SpdHeaderHelper
    {
        public long offset;
        public int size;
    }
    public static class spdUtils
    {
        public static List<DDS> getDDSFiles(string spd)
        {
            List<DDS> ddsNames = new List<DDS>();
            byte[] spdBytes = File.ReadAllBytes(spd);
            int numTextures = BitConverter.ToUInt16(spdBytes, 20);
            int pos = 32;
            DDS dds;
            for (int i = 0; i < numTextures; i++)
            {
                dds = new DDS();
                int tag = BitConverter.ToInt32(spdBytes, pos);
                dds.pos = BitConverter.ToUInt32(spdBytes, pos + 8);
                dds.size = BitConverter.ToInt32(spdBytes, pos + 12);
                dds.name = $"{Encoding.ASCII.GetString(SliceArray(spdBytes, pos + 32, pos + 47)).TrimEnd('\0')}[{tag}]";
                dds.file = SliceArray(spdBytes, dds.pos, dds.pos + dds.size - 1);
                ddsNames.Add(dds);
                pos += 48;
            }
            return ddsNames;
        }

        public static List<SPDKey> getSPDKeys(string spd)
        {
            List<SPDKey> spdKeys = new List<SPDKey>();
            byte[] spdBytes = File.ReadAllBytes(spd);
            int numKeys = BitConverter.ToUInt16(spdBytes, 22);
            int pos = BitConverter.ToInt32(spdBytes, 28);
            SPDKey spdKey;
            for (int i = 0; i < numKeys; i++)
            {
                spdKey = new SPDKey();
                spdKey.pos = pos;
                spdKey.id = BitConverter.ToInt32(spdBytes, pos);
                spdKey.file = SliceArray(spdBytes, spdKey.pos, spdKey.pos + 160);
                spdKeys.Add(spdKey);
                pos += 160;
            }
            return spdKeys;
        }

        public static void replaceSPDKey(string spd, string spdspr)
        {
            List<SPDKey> spdKeys = getSPDKeys(spd);
            byte[] spdBytes = File.ReadAllBytes(spdspr);
            foreach (var spdKey in spdKeys)
            {
                if (spdKey.id.ToString() == Path.GetFileNameWithoutExtension(spdspr))
                {
                    using (Stream stream = File.Open(spd, FileMode.Open))
                    {
                        stream.Position = spdKey.pos;
                        stream.Write(spdBytes, 0, 160);
                    }
                    return;
                }
            }
        }

        private static byte[] SliceArray(byte[] source, long start, long end)
        {
            long length = end - start;
            byte[] dest = new byte[length];
            Array.Copy(source, start, dest, 0, length);
            return dest;
        }

        public static void replaceDDS(string spd, string dds)
        {
            List<DDS> ddsFiles = getDDSFiles(spd);
            byte[] ddsBytes = File.ReadAllBytes(dds);
            foreach (var ddsFile in ddsFiles)
            {
                if (ddsFile.name == Path.GetFileNameWithoutExtension(dds))
                {
                    if (ddsBytes.Length == ddsFile.size)
                    {
                        using (Stream stream = File.Open(spd, FileMode.Open))
                        {
                            stream.Position = ddsFile.pos;
                            stream.Write(ddsBytes, 0, ddsFile.size);
                        }
                    }
                    else
                    {
                        byte[] spdBytes = File.ReadAllBytes(spd);
                        byte[] newSpd = new byte[spdBytes.Length + (ddsBytes.Length - ddsFile.size)];
                        SliceArray(spdBytes, 0, ddsFile.pos).CopyTo(newSpd, 0);
                        SliceArray(spdBytes, ddsFile.pos + ddsFile.size, spdBytes.Length).CopyTo(newSpd, ddsFile.pos + ddsBytes.Length);
                        ddsBytes.CopyTo(newSpd, ddsFile.pos);
                        File.WriteAllBytes(spd, newSpd);
                        updateOffsets(spd, getDDSOffsets(spd));
                    }
                }
            }
        }

        private static List<SpdHeaderHelper> getDDSOffsets(string spd)
        {
            List<SpdHeaderHelper> ddsHelpers = new List<SpdHeaderHelper>();
            byte[] spdBytes = File.ReadAllBytes(spd);
            byte[] pattern = Encoding.ASCII.GetBytes("DDS |");
            int offset = 0;
            int found = 0;
            SpdHeaderHelper helper;
            while (found != -1)
            {
                helper = new SpdHeaderHelper();
                // Start search after "DDS"
                found = Search(SliceArray(spdBytes, offset, spdBytes.Length), pattern);
                offset = found + offset;
                if (found != -1)
                {
                    helper.offset = offset;
                    ddsHelpers.Add(helper);
                }
                offset += 4;
            }
            for (int i = 0; i < ddsHelpers.Count; i++)
            {
                if (i != ddsHelpers.Count - 1)
                    ddsHelpers[i].size = (int)(ddsHelpers[i + 1].offset - ddsHelpers[i].offset);
                else
                    ddsHelpers[i].size = (int)(spdBytes.Length - ddsHelpers[i].offset);

            }

            return ddsHelpers;
        }
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

        private static void updateOffsets(string spd, List<SpdHeaderHelper> helpers)
        {
            // Start of dds offsets
            int pos = 40;
            using (Stream stream = File.Open(spd, FileMode.Open))
            {
                foreach (var helper in helpers)
                {
                    stream.Position = pos;
                    stream.Write(BitConverter.GetBytes(helper.offset), 0, 4);
                    pos += 4;
                    stream.Position = pos;
                    stream.Write(BitConverter.GetBytes(helper.size), 0, 4);
                    pos += 44;
                }
            }
        }
    }
}
