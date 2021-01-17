using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace AemulusModManager
{
    public static class tblPatch
    {
        private static string tblDir;

        private static byte[] SliceArray(byte[] source, int start, int end)
        {
            int length = end - start;
            byte[] dest = new byte[length];
            Array.Copy(source, start, dest, 0, length);
            return dest;
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

        
        private static void unpackTbls(string archive, string game)
        {
            if (game == "Persona 3 FES")
                return;
            PAKPackCMD($@"unpack ""{archive}"" ""{tblDir}""");
        }

        private static string exePath = @"Dependencies\PAKPack\PAKPack.exe";

        // Use PAKPack command
        private static void PAKPackCMD(string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = $"\"{exePath}\"";
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

        private static void repackTbls(string tbl, string archive, string game)
        {
            string parent = null;
            if (game == "Persona 4 Golden")
                parent = "battle";
            else if (game == "Persona 5")
                parent = "table";
            else if (game == "Persona 3 FES")
                return;
            PAKPackCMD($@"replace ""{archive}"" {parent}/{Path.GetFileName(tbl)} ""{tbl}""");
        }

        public static void Patch(List<string> ModList, string modDir, bool useCpk, string cpkLang, string game)
        {
            Console.WriteLine("[INFO] Patching .tbl's...");
            // Check if init_free exists and return if not
            string archive = null;
            if (game == "Persona 4 Golden")
            {
                if (useCpk)
                    archive = $@"{Path.GetFileNameWithoutExtension(cpkLang)}\init_free.bin";
                else
                {
                    switch (cpkLang)
                    {
                        case "data_e.cpk":
                            archive = $@"data00004\init_free.bin";
                            break;
                        case "data.cpk":
                            archive = $@"data00001\init_free.bin";
                            break;
                        case "data_c.cpk":
                            archive = $@"data00006\init_free.bin";
                            break;
                        case "data_k.cpk":
                            archive = $@"data00005\init_free.bin";
                            break;
                        default:
                            archive = $@"data00004\init_free.bin";
                            break;
                    }
                }
            }
            else if (game == "Persona 5")
                archive = @"battle\table.pac";
            if (game != "Persona 3 FES")
            {
                if (!File.Exists($@"{modDir}\{archive}"))
                {
                    if (File.Exists($@"Original\{game}\{archive}"))
                    {
                        Directory.CreateDirectory($@"{modDir}\{Path.GetDirectoryName(archive)}");
                        File.Copy($@"Original\{game}\{archive}", $@"{modDir}\{archive}", true);
                        Console.WriteLine($"[INFO] Copied over {archive} from Original directory.");
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] {archive} not found in output directory or Original directory.");
                        return;
                    }
                }
            
                tblDir = $@"{modDir}\{Path.ChangeExtension(archive, null)}_tbls";
                // Unpack archive
                Console.WriteLine($"[INFO] Unpacking tbl's from {archive}...");
                unpackTbls($@"{modDir}\{archive}", game);
            }
            // Keep track of which tables are edited
            List<string> editedTables = new List<string>();
            List<NameSection> sections = null;
            // Load EnabledPatches in order
            foreach (string dir in ModList)
            {
                Console.WriteLine($"[INFO] Searching for/applying tblpatches in {dir}...");
                if (!Directory.Exists($@"{dir}\tblpatches"))
                {
                    Console.WriteLine($"[INFO] No tblpatches folder found in {dir}");
                    continue;
                }
                foreach (var t in Directory.EnumerateFiles($@"{dir}\tblpatches", "*.tblpatch", SearchOption.TopDirectoryOnly).Union
                       (Directory.EnumerateFiles(dir, "*.tblpatch", SearchOption.TopDirectoryOnly)))
                {
                    byte[] file = File.ReadAllBytes(t);
                    string fileName = Path.GetFileName(t);
                    Console.WriteLine($"[INFO] Loading {fileName}");
                    if (file.Length < 3)
                    {
                        Console.WriteLine("[ERROR] Improper .tblpatch format.");
                        continue;
                    }

                    // Name of tbl file
                    string tblName = Encoding.ASCII.GetString(SliceArray(file, 0, 3));

                    /*
                    * P4G TBLS:
                    * SKILL - SKL
                    * UNIT - UNT
                    * MSG - MSG
                    * PERSONA - PSA
                    * ENCOUNT - ENC
                    * EFFECT - EFF
                    * MODEL - MDL
                    * AICALC - AIC
                    *
                    * P3F TBLS:
                    * AICALC - AIC
                    * AICALC_F - AIF
                    * EFFECT - EFF
                    * ENCOUNT - ENC
                    * ENCOUNT_F - ENF
                    * MODEL - MDL
                    * MSG - MSG
                    * PERSONA - PSA
                    * PERSONA_F - PSF
                    * SKILL - SKL
                    * SKILL_F - SKF
                    * UNIT - UNT
                    * UNIT_F - UNF
                    * 
                    * P5 TBLS:
                    * AICALC - AIC
                    * ELSAI - EAI
                    * ENCOUNT - ENC
                    * EXIST - EXT
                    * ITEM - ITM
                    * NAME - NME
                    * PERSONA - PSA
                    * PLAYER - PLY
                    * SKILL - SKL
                    * TALKINFO - TKI
                    * UNIT - UNT
                    * VISUAL - VSL
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
                            if (game == "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "PSA":
                            tblName = "PERSONA.TBL";
                            break;
                        case "ENC":
                            tblName = "ENCOUNT.TBL";
                            break;
                        case "EFF":
                            tblName = "EFFECT.TBL";
                            if (game == "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "MDL":
                            tblName = "MODEL.TBL";
                            if (game == "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "AIC":
                            tblName = "AICALC.TBL";
                            break;
                        case "AIF":
                            tblName = "AICALC_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "ENF":
                            tblName = "ENCOUNT_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "PSF":
                            tblName = "PERSONA_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "SKF":
                            tblName = "SKILL_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "UNF":
                            tblName = "UNIT_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "EAI":
                            tblName = "ELSAI.TBL";
                            if (game != "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "EXT":
                            tblName = "EXIST.TBL";
                            if (game != "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "ITM":
                            tblName = "ITEM.TBL";
                            if (game != "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "NME":
                            tblName = "NAME.TBL";
                            if (game != "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "PLY":
                            tblName = "PLAYER.TBL";
                            if (game != "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "TKI":
                            tblName = "TALKINFO.TBL";
                            if (game != "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "VSL":
                            tblName = "VISUAL.TBL";
                            if (game != "Persona 5")
                            {
                                Console.WriteLine($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        default:
                            Console.WriteLine($"[ERROR] Unknown tbl name for {t}.");
                            continue;
                    }

                    

                    // Keep track of which TBL's were edited
                    if (!editedTables.Contains(tblName))
                    {
                        editedTables.Add(tblName);
                        if (tblName == "NAME.TBL")
                            sections = getSections($@"{tblDir}\table\{tblName}");
                    }

                    if (tblName != "NAME.TBL")
                    {
                        if (file.Length < 12)
                        {
                            Console.WriteLine("[ERROR] Improper .tblpatch format.");
                            continue;
                        }
                        // Offset to start overwriting at
                        byte[] byteOffset = SliceArray(file, 3, 11);
                        // Reverse endianess
                        Array.Reverse(byteOffset, 0, 8);
                        long offset = BitConverter.ToInt64(byteOffset, 0);
                        // Contents is what to replace
                        byte[] fileContents = SliceArray(file, 11, file.Length);

                        // TBL file to edit
                        if (game != "Persona 3 FES")
                        {
                            string unpackedTblPath = null;
                            if (game == "Persona 4 Golden")
                                unpackedTblPath = $@"{tblDir}\battle\{tblName}";
                            else
                                unpackedTblPath = $@"{tblDir}\table\{tblName}";
                            byte[] tblBytes = File.ReadAllBytes(unpackedTblPath);
                            fileContents.CopyTo(tblBytes, offset);
                            File.WriteAllBytes(unpackedTblPath, tblBytes);
                        }
                        else
                        {
                            if (!File.Exists($@"{modDir}\BTL\BATTLE\{tblName}"))
                            {
                                if (File.Exists($@"Original\{game}\BTL\BATTLE\{tblName}") && !File.Exists($@"{modDir}\BTL\BATTLE\{tblName}"))
                                {
                                    Directory.CreateDirectory($@"{modDir}\BTL\BATTLE");
                                    File.Copy($@"Original\{game}\BTL\BATTLE\{tblName}", $@"{modDir}\BTL\BATTLE\{tblName}", true);
                                    Console.WriteLine($"[INFO] Copied over {tblName} from Original directory.");
                                }
                                else if (!File.Exists($@"Original\{game}\BTL\BATTLE\{tblName}") && !File.Exists($@"{modDir}\BTL\BATTLE\{tblName}"))
                                {
                                    Console.WriteLine($"[WARNING] {tblName} not found in output directory or Original directory.");
                                    continue;
                                }
                                string tblPath = $@"{modDir}\BTL\BATTLE\{tblName}";
                                byte[] tblBytes = File.ReadAllBytes(tblPath);
                                fileContents.CopyTo(tblBytes, offset);
                                File.WriteAllBytes(tblPath, tblBytes);
                            }
                        }
                    }
                    else
                    {
                        if (file.Length < 6)
                        {
                            Console.WriteLine("[ERROR] Improper .tblpatch format.");
                            continue;
                        }
                        var temp = replaceName(sections, file);
                        if (temp != null)
                            sections = temp;
                    }
                }

                Console.WriteLine($"[INFO] Applied patches from {dir}");
                
            }
            if (game != "Persona 3 FES")
            {
                // Replace each edited TBL's
                foreach (string u in editedTables)
                {
                    if (u == "NAME.TBL")
                        writeTbl(sections, $@"{tblDir}\table\{u}");
                    Console.WriteLine($"[INFO] Replacing {u} in {archive}");
                    if (game == "Persona 5")
                        repackTbls($@"{tblDir}\table\{u}", $@"{modDir}\{archive}", game);
                    else
                        repackTbls($@"{tblDir}\battle\{u}", $@"{modDir}\{archive}", game);
                }

                Console.WriteLine($"[INFO] Deleting temp tbl folder...");
                // Delete all unpacked files
                Directory.Delete(tblDir, true);
            }
            Console.WriteLine("[INFO] Finished patching tbl's!");
        }

        

        // P5's NAME.TBL Expandable support
        private static List<NameSection> getSections(string tbl)
        {
            List<NameSection> sections = new List<NameSection>();
            byte[] tblBytes = File.ReadAllBytes(tbl);
            int pos = 0;
            NameSection section;
            // 33 sections
            for (int i = 0; i <= 16; i++)
            {
                section = new NameSection();
                // Get big endian section size
                section.pointersSize = BitConverter.ToInt32(SliceArray(tblBytes, pos, pos + 4).Reverse().ToArray(), 0);

                // Get pointers
                byte[] segment = SliceArray(tblBytes, pos + 4, pos + 4 + section.pointersSize);
                section.pointers = new List<UInt16>();
                for (int j = 0; j < segment.Length; j += 2)
                {
                    section.pointers.Add(BitConverter.ToUInt16(SliceArray(segment, j, j + 2).Reverse().ToArray(), 0));
                }

                // Get to name section
                pos += section.pointersSize + 4;
                if ((pos % 16) != 0)
                {
                    pos += 16 - (pos % 16);
                }

                // Get big endian section size
                section.namesSize = BitConverter.ToInt32(SliceArray(tblBytes, pos, pos + 4).Reverse().ToArray(), 0);

                // Get names
                segment = SliceArray(tblBytes, pos + 4, pos + 4 + section.namesSize);
                section.names = new List<byte[]>();
                List<byte> name = new List<byte>();
                foreach (var segmentByte in segment)
                {
                    if (segmentByte == (byte)0)
                    {
                        section.names.Add(name.ToArray());
                        name = new List<byte>();
                    }
                    else
                    {
                        name.Add(segmentByte);
                    }

                }

                // Get to next section
                pos += section.namesSize + 4;
                if ((pos % 16) != 0)
                {
                    pos += 16 - (pos % 16);
                }
                sections.Add(section);
            }
            return sections;
        }

        private static List<NameSection> replaceName(List<NameSection> sections, byte[] patch)
        {
            int section = Convert.ToInt32(patch[3]);
            if (section >= sections.Count)
            {
                Console.WriteLine($"[ERROR] Section chosen is out of range.");
                return null;
            }
            int index = BitConverter.ToInt16(SliceArray(patch, 4, 6).Reverse().ToArray(), 0);
            // Contents is what to replace
            byte[] fileContents = SliceArray(patch, 6, patch.Length);

            if (index >= sections[section].names.Count)
            {
                byte[] dummy = Encoding.ASCII.GetBytes("RESERVE");
                // Add RESERVE names if index is further down
                while (sections[section].names.Count < index)
                {
                    sections[section].pointers.Add((ushort)(sections[section].pointers.Last() + sections[section].names.Last().Length + 1));
                    sections[section].names.Add(dummy);
                    sections[section].pointersSize += 2;
                    sections[section].namesSize += dummy.Length + 1;
                }
                // Add expanded name
                sections[section].pointers.Add((ushort)(sections[section].pointers.Last() + sections[section].names.Last().Length + 1));
                sections[section].names.Add(fileContents);
                sections[section].pointersSize += 2;
                sections[section].namesSize += fileContents.Length + 1;
            }
            else
            {
                int delta = fileContents.Length - sections[section].names[index].Length;
                sections[section].names[index] = fileContents;
                sections[section].namesSize += delta;
                for (int i = index + 1; i < sections[section].pointers.Count; i++)
                {
                    sections[section].pointers[i] += (UInt16)delta;
                }
            }
            return sections;
        }

        private static void writeTbl(List<NameSection> sections, string path)
        {
            using (FileStream
            fileStream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fileStream))
                {
                    foreach (var section in sections)
                    {
                        // Write Pointer size
                        bw.Write(BitConverter.GetBytes(section.pointersSize).Reverse().ToArray());
                        // Write pointer section
                        foreach (var pointer in section.pointers)
                            bw.Write(BitConverter.GetBytes(pointer).Reverse().ToArray());
                        while (bw.BaseStream.Position % 16 != 0)
                            bw.Write((byte)0);
                        // Write names size
                        bw.Write(BitConverter.GetBytes(section.namesSize).Reverse().ToArray());
                        // Write names
                        byte[] last = section.names.Last();
                        foreach (var name in section.names)
                        {
                            bw.Write(name);
                            if (name != last)
                                bw.Write((byte)0);
                        }
                        while (bw.BaseStream.Position % 16 != 0)
                            bw.Write((byte)0);
                    }
                }
            }
        }


        /* 
         * ArcanaNames 0
         * SkillNames 1
         * UnitNames 2
         * PersonaNames 3
         * AccessoryNames 4
         * ArmorNames 5
         * ConsumableItemNames 6
         * KeyItemNames 7
         * MaterialNames 8
         * MeleeWeaponNames 9
         * BattleActionNames 10 A
         * OutfitNames 11 B
         * SkillCardNames 12 C
         * ConfidantNames 13 D
         * PartyMemberLastNames 14 E
         * PartyMemberFirstNames 15 F
         * RangedWeaponNames 16 10
         */
    }

    public class NameSection
    {
        public int namesSize;
        public int pointersSize;
        public List<byte[]> names;
        public List<UInt16> pointers;
    }

}