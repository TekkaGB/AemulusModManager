using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace AemulusModManager
{
    public class tblPatch
    {
        private string tblDir;

        private byte[] SliceArray(byte[] source, int start, int end)
        {
            int length = end - start;
            byte[] dest = new byte[length];
            Array.Copy(source, start, dest, 0, length);
            return dest;
        }

        private int Search(byte[] src, byte[] pattern)
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

        
        private void unpackTbls(string init_free)
        {
            string[] tbls = new string[] { "SKILL.TBL", "UNIT.TBL", "MSG.TBL", "PERSONA.TBL", "ENCOUNT.TBL", "EFFECT.TBL", "MODEL.TBL", "AICALC.TBL" };
            foreach (var tbl in tbls)
            {
                byte[] ifBytes = File.ReadAllBytes(init_free);
                byte[] pattern = Encoding.ASCII.GetBytes($"battle/{tbl}");
                int nameOffset = Search(ifBytes, pattern);
                
                int tblLength = BitConverter.ToInt32(ifBytes, nameOffset+252);
                byte[] tblBytes = SliceArray(ifBytes, nameOffset + 256, nameOffset + 256 + tblLength);
                File.WriteAllBytes($@"{tblDir}\{tbl}", tblBytes);
                Console.WriteLine($"Unpacked {tbl}");
            }
        }
        
        private void repackTbls(string tbl, string init_free)
        {
            byte[] ifBytes = File.ReadAllBytes(init_free);
            byte[] pattern = Encoding.ASCII.GetBytes($"battle/{Path.GetFileName(tbl)}");
            int offset = Search(ifBytes, pattern) + 256;
            byte[] tblBytes = File.ReadAllBytes(tbl);
            tblBytes.CopyTo(ifBytes, offset);
            File.WriteAllBytes(init_free, ifBytes);
        }

        public void Patch(List<string> ModList, string modDir, bool useCpk, string cpkLang)
        {
            Console.WriteLine("Patching .tbl's...");
            // Check if init_free exists and return if not
            string init_free;
            if (useCpk)
                init_free = $@"{Path.GetFileNameWithoutExtension(cpkLang)}\init_free.bin";
            else
            {
                switch (cpkLang)
                {
                    case "data_e.cpk":
                        init_free = $@"data00004\init_free.bin";
                        break;
                    case "data.cpk":
                        init_free = $@"data00001\init_free.bin";
                        break;
                    case "data_c.cpk":
                        init_free = $@"data00006\init_free.bin";
                        break;
                    case "data_k.cpk":
                        init_free = $@"data00005\init_free.bin";
                        break;
                    default:
                        init_free = $@"data00004\init_free.bin";
                        break;
                }
            }
            if (!File.Exists($@"{modDir}\{init_free}"))
            {
                if (File.Exists($@"Original\{init_free}"))
                {
                    if (!Directory.Exists($@"{modDir}\{Path.GetDirectoryName(init_free)}"))
                        Directory.CreateDirectory($@"{modDir}\{Path.GetDirectoryName(init_free)}");
                    File.Copy($@"Original\{init_free}", $@"{modDir}\{init_free}", true);
                    Console.WriteLine($"[INFO] Copied over init_free.bin from Original directory.");
                }
                else
                {
                    Console.WriteLine($"[WARNING] {init_free} not found in output directory or Original directory.");
                    return;
                }
            }
            tblDir = $@"{modDir}\{Path.ChangeExtension(init_free, null)}_tbls";
            Directory.CreateDirectory(tblDir);
            // Unpack init_free
            Console.WriteLine($"[INFO] Unpacking tbl's from init_free.bin...");
            unpackTbls($@"{modDir}\{init_free}");
            // Keep track of which tables are edited
            List<string> editedTables = new List<string>();

            // Load EnabledPatches in order
            foreach (string dir in ModList)
            {
                Console.WriteLine($"[INFO] Searching for/applying tblpatches in {dir}...");
                IEnumerable<string> files;
                if (Directory.Exists($@"{dir}\tblpatches"))
                {
                    files = Directory.EnumerateFiles($@"{dir}\tblpatches", "*.tblpatch", SearchOption.TopDirectoryOnly).Union
                       (Directory.EnumerateFiles(dir, "*.tblpatch", SearchOption.TopDirectoryOnly));
                }
                else
                    files = Directory.EnumerateFiles(dir, "*.tblpatch", SearchOption.TopDirectoryOnly);
                foreach (var t in files)
                {
                    byte[] file = File.ReadAllBytes(t);
                    string fileName = Path.GetFileName(t);
                    //Console.WriteLine($"[INFO] Loading {fileName}");
                    if (file.Length < 12)
                    {
                        Console.WriteLine("[ERROR] Improper .tblpatch format.");
                        continue;
                    }

                    // Name of tbl file
                    string tblName = Encoding.ASCII.GetString(SliceArray(file, 0, 3));
                    // Offset to start overwriting at
                    byte[] byteOffset = SliceArray(file, 3, 11);
                    // Reverse endianess
                    Array.Reverse(byteOffset, 0, 8);
                    long offset = BitConverter.ToInt64(byteOffset, 0);
                    // Contents is what to replace
                    byte[] fileContents = SliceArray(file, 11, file.Length);

                    /*
                        * TBLS:
                        * SKILL - SKL
                        * UNIT - UNT
                        * MSG - MSG
                        * PERSONA - PSA
                        * ENCOUNT - ENC
                        * EFFECT - EFF
                        * MODEL - MDL
                        * AICALC - AIC
                        */

                    switch (tblName)
                    {
                        case "SKL":
                            tblName = "SKILL.TBL";
                            break;
                        case "UNT":
                            tblName = "UNIT.TBL";
                            break;
                        case "MSG":
                            tblName = "MSG.TBL";
                            break;
                        case "PSA":
                            tblName = "PERSONA.TBL";
                            break;
                        case "ENC":
                            tblName = "ENCOUNT.TBL";
                            break;
                        case "EFF":
                            tblName = "EFFECT.TBL";
                            break;
                        case "MDL":
                            tblName = "MODEL.TBL";
                            break;
                        case "AIC":
                            tblName = "AICALC.TBL";
                            break;
                        default:
                            Console.WriteLine($"[ERROR] Unknown tbl name for {t}.");
                            continue;
                    }

                    // Keep track of which TBL's were edited
                    if (!editedTables.Contains(tblName))
                        editedTables.Add(tblName);

                    // TBL file to edit
                    string unpackedTblPath = $@"{tblDir}\{tblName}";
                    byte[] tblBytes = File.ReadAllBytes(unpackedTblPath);
                    fileContents.CopyTo(tblBytes, offset);
                    File.WriteAllBytes(unpackedTblPath, tblBytes);
                }

                if (files.ToList().Count > 0)
                {
                    Console.WriteLine($"[INFO] Applied patches from {dir}");
                }
                
            }
            // Replace each edited TBL's
            foreach (string u in editedTables)
            {
                Console.WriteLine($"[INFO] Replacing {u} in init_free.bin");
                repackTbls($@"{tblDir}\{u}", $@"{modDir}\{init_free}");
            }

            Console.WriteLine($"[INFO] Deleting temp tbl folder...");
            // Delete all unpacked files
            Directory.Delete(tblDir, true);

            Console.WriteLine("[INFO] Finished patching tbl's!");
        }
    }

}