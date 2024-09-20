using AemulusModManager.Utilities.TblPatching;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

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

        private static void unpackTbls(string archive, string game)
        {
            if (game == "Persona 3 FES" || game == "Persona 5 Royal (Switch)")
                return;
            PAKPackCMD($@"unpack ""{archive}"" ""{tblDir}""");
        }

        private static string exePath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\PAKPack\PAKPack.exe";

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
            if (game == "Persona 4 Golden" || game == "Persona 4 Golden (Vita)" || game == "Persona 3 Portable")
            {
                parent = "battle";
                if (Path.GetFileName(tbl).Equals("ITEMTBL.TBL"))
                {
                    parent = "init";
                    tbl = tbl.Replace(@"battle\ITEMTBL.TBL", @"init\itemtbl.bin");
                }
            }
            else if (game == "Persona 5" || game == "Persona 5 Royal (PS4)")
                parent = "table";
            else if (game == "Persona 3 FES")
                return;
            PAKPackCMD($@"replace ""{archive}"" {parent}/{Path.GetFileName(tbl)} ""{tbl}""");
        }

        private static string[] p4gTables = { "SKILL", "UNIT", "MSG", "PERSONA", "ENCOUNT", "EFFECT", "MODEL", "AICALC", "ITEMTBL" };
        private static string[] p3pTables = { "SKILL", "UNIT", "MSG", "PERSONA", "ENCOUNT", "EFFECT", "MODEL", "AICALC" };
        private static string[] p3fTables = { "SKILL", "SKILL_F", "UNIT", "UNIT_F", "MSG", "PERSONA", "PERSONA_F", "ENCOUNT", "ENCOUNT_F", "EFFECT", "MODEL", "AICALC", "AICALC_F" };
        private static string[] p5Tables = { "AICALC", "ELSAI", "ENCOUNT", "EXIST", "ITEM", "NAME", "PERSONA", "PLAYER", "SKILL", "TALKINFO", "UNIT", "VISUAL" };
        private static string[] pqNameTbls = { "battle/table/personanametable.tbl", "battle/table/enemynametable.tbl", "battle/table/skillnametable.tbl" };

        public static void Patch(List<string> ModList, string modDir, bool useCpk, string cpkLang, string game)
        {
            if (!File.Exists(exePath))
            {
                Utilities.ParallelLogger.Log($"[ERROR] Couldn't find {exePath}. Please check if it was blocked by your anti-virus.");
                return;
            }
            Utilities.ParallelLogger.Log("[INFO] Patching TBLs...");
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
            else if (game == "Persona 4 Golden (Vita)")
                archive = $@"init_free.bin";
            else if (game == "Persona 3 Portable")
                archive = $@"data\init_free.bin";
            else if (game == "Persona 5" || game == "Persona 5 Royal (PS4)")
                archive = @"battle\table.pac";
            if (game != "Persona 3 FES" && game != "Persona 5 Royal (Switch)" && game != "Persona Q" && game != "Persona Q2")
            {
                if (!File.Exists($@"{modDir}\{archive}"))
                {
                    if (File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{archive}"))
                    {
                        Directory.CreateDirectory($@"{modDir}\{Path.GetDirectoryName(archive)}");
                        File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{archive}", $@"{modDir}\{archive}", true);
                        Utilities.ParallelLogger.Log($"[INFO] Copied over {archive} from Original directory.");
                    }
                    else
                    {
                        Utilities.ParallelLogger.Log($"[WARNING] {archive} not found in output directory or Original directory.");
                        return;
                    }
                }

                tblDir = $@"{modDir}\{Path.ChangeExtension(archive, null)}_tbls";
                // Unpack archive
                Utilities.ParallelLogger.Log($"[INFO] Unpacking TBLs from {archive}...");
                unpackTbls($@"{modDir}\{archive}", game);
            }
            // Keep track of which tables are edited
            List<string> editedTables = new List<string>();
            List<NameSection> sections = null;
            // Load EnabledPatches in order
            foreach (string dir in ModList)
            {
                Utilities.ParallelLogger.Log($"[INFO] Searching for/applying tblpatches in {dir}...");
                if (!Directory.Exists($@"{dir}\tblpatches"))
                {
                    Utilities.ParallelLogger.Log($"[INFO] No tblpatches folder found in {dir}");
                    continue;
                }
                // Apply original tblpatch files
                foreach (var t in Directory.GetFiles($@"{dir}\tblpatches", "*.tblpatch", SearchOption.AllDirectories))
                {
                    byte[] file = File.ReadAllBytes(t);
                    string fileName = Path.GetFileName(t);
                    Utilities.ParallelLogger.Log($"[INFO] Loading {fileName}");
                    if (file.Length < 3)
                    {
                        Utilities.ParallelLogger.Log("[ERROR] Improper .tblpatch format.");
                        continue;
                    }

                    // Name of tbl file
                    string tblName = Encoding.ASCII.GetString(SliceArray(file, 0, 3));

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
                            if (game == "Persona 5" || game == "Persona 5 Royal (PS4)" || game == "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
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
                            if (game == "Persona 5" || game == "Persona 5 Royal (PS4)" || game == "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "MDL":
                            tblName = "MODEL.TBL";
                            if (game == "Persona 5" || game == "Persona 5 Royal (PS4)" || game == "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
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
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "ENF":
                            tblName = "ENCOUNT_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "PSF":
                            tblName = "PERSONA_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "SKF":
                            tblName = "SKILL_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "UNF":
                            tblName = "UNIT_F.TBL";
                            if (game != "Persona 3 FES")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "EAI":
                            tblName = "ELSAI.TBL";
                            if (game != "Persona 5" && game != "Persona 5 Royal (PS4)" && game != "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "EXT":
                            tblName = "EXIST.TBL";
                            if (game != "Persona 5" && game != "Persona 5 Royal (PS4)" && game != "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "ITM":
                            tblName = "ITEM.TBL";
                            if (game != "Persona 5" && game != "Persona 5 Royal (PS4)" && game != "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "NME":
                            tblName = "NAME.TBL";
                            if (game != "Persona 5" && game != "Persona 5 Royal (PS4)" && game != "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "PLY":
                            tblName = "PLAYER.TBL";
                            if (game != "Persona 5" && game != "Persona 5 Royal (PS4)" && game != "Persona 5 Royal(Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "TKI":
                            tblName = "TALKINFO.TBL";
                            if (game != "Persona 5" && game != "Persona 5 Royal (PS4)" && game != "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        case "VSL":
                            tblName = "VISUAL.TBL";
                            if (game != "Persona 5" && game != "Persona 5 Royal (PS4)" && game != "Persona 5 Royal (Switch)")
                            {
                                Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in {game}, skipping");
                                continue;
                            }
                            break;
                        default:
                            Utilities.ParallelLogger.Log($"[ERROR] Unknown tbl name for {t}.");
                            continue;
                    }



                    // Keep track of which TBL's were edited
                    if (!editedTables.Contains(tblName))
                    {
                        editedTables.Add(tblName);
                    }

                    if (tblName != "NAME.TBL")
                    {
                        if (file.Length < 12)
                        {
                            Utilities.ParallelLogger.Log("[ERROR] Improper .tblpatch format.");
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
                        if (game == "Persona 3 FES")
                        {
                            if (!File.Exists($@"{modDir}\BTL\BATTLE\{tblName}"))
                            {
                                if (File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL\BATTLE\{tblName}"))
                                {
                                    Directory.CreateDirectory($@"{modDir}\BTL\BATTLE");
                                    File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL\BATTLE\{tblName}", $@"{modDir}\BTL\BATTLE\{tblName}", true);
                                    Utilities.ParallelLogger.Log($"[INFO] Copied over {tblName} from Original directory.");
                                }
                                else if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL\BATTLE\{tblName}"))
                                {
                                    Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in output directory or Original directory.");
                                    continue;
                                }
                            }
                            string tblPath = $@"{modDir}\BTL\BATTLE\{tblName}";
                            byte[] tblBytes = File.ReadAllBytes(tblPath);
                            fileContents.CopyTo(tblBytes, offset);
                            File.WriteAllBytes(tblPath, tblBytes);
                        }
                        else if (game == "Persona 5 Royal (Switch)")
                        {
                            if (!File.Exists($@"{modDir}\BASE\BATTLE\TABLE\{tblName}"))
                            {
                                if (File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BASE\BATTLE\TABLE\{tblName}"))
                                {
                                    Directory.CreateDirectory($@"{modDir}\BASE\BATTLE\TABLE");
                                    File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BASE\BATTLE\TABLE\{tblName}", $@"{modDir}\BASE\BATTLE\TABLE\{tblName}", true);
                                    Utilities.ParallelLogger.Log($"[INFO] Copied over {tblName} from Original directory.");
                                }
                                else if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BASE\BATTLE\TABLE\{tblName}"))
                                {
                                    Utilities.ParallelLogger.Log($"[WARNING] {tblName} not found in output directory or Original directory.");
                                    continue;
                                }
                            }
                            string tblPath = $@"{modDir}\BASE\BATTLE\TABLE\{tblName}";
                            byte[] tblBytes = File.ReadAllBytes(tblPath);
                            fileContents.CopyTo(tblBytes, offset);
                            File.WriteAllBytes(tblPath, tblBytes);
                        }
                        else
                        {
                            string unpackedTblPath = null;
                            if (game == "Persona 4 Golden" || game == "Persona 4 Golden (Vita)")
                                unpackedTblPath = $@"{tblDir}\battle\{tblName}";
                            else
                                unpackedTblPath = $@"{tblDir}\table\{tblName}";
                            byte[] tblBytes = File.ReadAllBytes(unpackedTblPath);
                            fileContents.CopyTo(tblBytes, offset);
                            File.WriteAllBytes(unpackedTblPath, tblBytes);
                        }
                    }
                    else
                    {
                        sections = GetNameSections($@"{tblDir}\table\{tblName}");
                        if (file.Length < 6)
                        {
                            Utilities.ParallelLogger.Log("[ERROR] Improper .tblpatch format.");
                            continue;
                        }
                        var temp = ReplaceName(sections, file, null, game);
                        if (temp != null)
                        {
                            sections = temp;
                            WriteNameTbl(sections, $@"{tblDir}\table\NAME.TBL");
                        }
                    }
                }

                List<Table> tables = new List<Table>();
                // Apply new tbp json patching
                foreach (var t in Directory.GetFiles($@"{dir}\tblpatches", "*.tbp", SearchOption.AllDirectories))
                {
                    TablePatches tablePatches = null;
                    try
                    {
                        tablePatches = JsonConvert.DeserializeObject<TablePatches>(File.ReadAllText(t));
                    }
                    catch (Exception ex)
                    {
                        Utilities.ParallelLogger.Log($"[ERROR] Couldn't deserialize {t} ({ex.Message}), skipping...");
                        continue;
                    }
                    if (tablePatches.Version != 1)
                    {
                        Utilities.ParallelLogger.Log($"[ERROR] Invalid version for {t}, skipping...");
                        continue;
                    }
                    if (tablePatches.Patches != null)
                    {
                        foreach (var patch in tablePatches.Patches)
                        {
                            Utilities.ParallelLogger.Log($"[INFO] Current patch: tbl={patch.tbl}, section={patch.section}, offset={patch.offset}, index={patch.index}"); //debug message
                            // Keep track of which TBL's were edited and get sections
                            if (!tables.Exists(x => x.tableName == patch.tbl))
                            {
                                if ((game == "Persona 4 Golden" && !p4gTables.Contains(patch.tbl))
                                    || (game == "Persona 4 Golden (Vita)" && !p4gTables.Contains(patch.tbl))
                                    || (game == "Persona 3 FES" && !p3fTables.Contains(patch.tbl))
                                    || (game == "Persona 5" && !p5Tables.Contains(patch.tbl))
                                    || (game == "Persona 5 Royal (PS4)" && !p5Tables.Contains(patch.tbl))
                                    || (game == "Persona 5 Royal (Switch)" && !p5Tables.Contains(patch.tbl))
                                    || (game == "Persona 3 Portable" && !p3pTables.Contains(patch.tbl))
                                    || ((game == "Persona Q" || game == "Persona Q2") && !QTblExists(game, patch.tbl)))
                                {
                                    Utilities.ParallelLogger.Log($"[ERROR] {patch.tbl} doesn't exist in {game}, skipping...");
                                    continue;
                                }
                                Table table = new Table();
                                string tablePath = null;
                                if (game == "Persona 3 FES")
                                {
                                    tablePath = $@"{modDir}\BTL\BATTLE\{patch.tbl}.TBL";
                                    if (!File.Exists(tablePath))
                                    {
                                        if (File.Exists($@"Original\{game}\BTL\BATTLE\{patch.tbl}.TBL"))
                                        {
                                            Directory.CreateDirectory($@"{modDir}\BTL\BATTLE");
                                            File.Copy($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL\BATTLE\{patch.tbl}.TBL", tablePath, true);
                                            Utilities.ParallelLogger.Log($"[INFO] Copied over {patch.tbl}.TBL from Original directory.");
                                        }
                                        else if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL\BATTLE\{patch.tbl}.TBL"))
                                        {
                                            Utilities.ParallelLogger.Log($"[WARNING] {patch.tbl}.TBL not found in output directory or Original directory.");
                                            continue;
                                        }
                                    }
                                }
                                else if (game == "Persona 5 Royal (Switch)")
                                {
                                    tablePath = patch.tbl.Equals("NAME", StringComparison.InvariantCultureIgnoreCase) ? $@"{modDir}\{cpkLang}\BATTLE\TABLE\{patch.tbl}.TBL" : $@"{modDir}\BASE\BATTLE\TABLE\{patch.tbl}.TBL";
                                    var originalPath = patch.tbl.Equals("NAME", StringComparison.InvariantCultureIgnoreCase) ?
                                        $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{cpkLang}\BATTLE\TABLE\{patch.tbl}.TBL" : $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BASE\BATTLE\TABLE\{patch.tbl}.TBL";
                                    if (!File.Exists(tablePath))
                                    {
                                        if (File.Exists(originalPath))
                                        {
                                            Directory.CreateDirectory(Path.GetDirectoryName(tablePath));
                                            File.Copy(originalPath, tablePath, true);
                                            Utilities.ParallelLogger.Log($"[INFO] Copied over {patch.tbl}.TBL from Original directory.");
                                        }
                                        else if (!File.Exists(originalPath))
                                        {
                                            Utilities.ParallelLogger.Log($"[WARNING] {patch.tbl}.TBL not found in output directory or Original directory.");
                                            continue;
                                        }
                                    }
                                }
                                else if (game == "Persona Q" || game == "Persona Q2")
                                {
                                    tablePath = $@"{modDir}/{patch.tbl}";
                                    var originalPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/Original/{game}/{patch.tbl}";
                                    if (!File.Exists(tablePath))
                                    {
                                        if (File.Exists(originalPath))
                                        {
                                            Directory.CreateDirectory(Path.GetDirectoryName(tablePath));
                                            File.Copy(originalPath, tablePath, true);
                                            Utilities.ParallelLogger.Log($"[INFO] Copied over {patch.tbl} from Original directory.");
                                        }
                                        else
                                        {
                                            Utilities.ParallelLogger.Log($"[WARNING] {patch.tbl} not found in output directory or Original directory.");
                                            continue;
                                        }
                                    }
                                }
                                else if (game == "Persona 4 Golden" || game == "Persona 4 Golden (Vita)" || game == "Persona 3 Portable")
                                    tablePath = patch.tbl.Equals("ITEMTBL") ? $@"{tblDir}\init\itemtbl.bin" : $@"{tblDir}\battle\{patch.tbl}.TBL";
                                else if (game == "Persona 5" || game == "Persona 5 Royal (PS4)")
                                    tablePath = $@"{tblDir}\table\{patch.tbl}.TBL";

                                if (patch.tbl == "NAME")
                                    table.nameSections = GetNameSections(tablePath);
                                else if (pqNameTbls.Contains(patch.tbl))
                                    table.nameSections = GetNameSectionQ(tablePath);
                                else
                                    table.sections = GetSections(tablePath, game);
                                table.tableName = patch.tbl;
                                tables.Add(table);
                            }
                            if (patch.tbl == "NAME" || pqNameTbls.Contains(patch.tbl))
                                tables.Find(x => x.tableName == patch.tbl).nameSections = ReplaceName(tables.Find(x => x.tableName == patch.tbl).nameSections, null, patch, game);
                            else
                                tables.Find(x => x.tableName == patch.tbl).sections = ReplaceSection(tables.Find(x => x.tableName == patch.tbl).sections, patch);
                        }
                    }
                }
                foreach (var table in tables)
                {
                    // Keep track of which TBL's were edited
                    if (!editedTables.Contains($"{table.tableName}.TBL"))
                        editedTables.Add($"{table.tableName}.TBL");
                    else if((game == "Persona Q" || game == "Persona Q2") && !editedTables.Contains($@"{table.tableName}"))
                        editedTables.Add($"{table.tableName}");

                    string path = null;
                    if (game == "Persona 3 FES")
                        path = $@"{modDir}\BTL\BATTLE\{table.tableName}.TBL";
                    else if (game == "Persona 5 Royal (Switch)")
                        path = table.tableName.Equals("NAME", StringComparison.InvariantCultureIgnoreCase) ? $@"{modDir}\{cpkLang}\BATTLE\TABLE\{table.tableName}.TBL" : $@"{modDir}\BASE\BATTLE\TABLE\{table.tableName}.TBL";
                    else if (game == "Persona 4 Golden" || game == "Persona 4 Golden (Vita)" || game == "Persona 3 Portable")
                        path = table.tableName.Equals("ITEMTBL") ? $@"{tblDir}\init\itemtbl.bin" : $@"{tblDir}\battle\{table.tableName}.TBL";
                    else if (game == "Persona 5" || game == "Persona 5 Royal (PS4)")
                        path = $@"{tblDir}\table\{table.tableName}.TBL";
                    else if (game == "Persona Q" ||  game == "Persona Q2")
                        path = $@"{modDir}/{table.tableName}";

                    if (table.tableName == "NAME")
                        WriteNameTbl(table.nameSections, path);
                    else if (pqNameTbls.Contains(table.tableName))
                        WriteNameTblQ(table.nameSections, path);
                    else
                        WriteTbl(table.sections, path, game);
                }

                Utilities.ParallelLogger.Log($"[INFO] Applied patches from {dir}");

            }

            if (game != "Persona 3 FES" && game != "Persona 5 Royal (Switch)" && game != "Persona Q" && game != "Persona Q2")
            {
                // Replace each edited TBL's
                foreach (string u in editedTables)
                {
                    if (u == "ITEMTBL.TBL")
                        Utilities.ParallelLogger.Log($"[INFO] Replacing itemtbl.bin in {archive}");
                    else
                        Utilities.ParallelLogger.Log($"[INFO] Replacing {u} in {archive}");
                    if (game == "Persona 5" || game == "Persona 5 Royal (PS4)")
                        repackTbls($@"{tblDir}\table\{u}", $@"{modDir}\{archive}", game);
                    else
                        repackTbls($@"{tblDir}\battle\{u}", $@"{modDir}\{archive}", game);
                }

                Utilities.ParallelLogger.Log($"[INFO] Deleting temp tbl folder...");
                // Delete all unpacked files
                Directory.Delete(tblDir, true);
            }
            Utilities.ParallelLogger.Log("[INFO] Finished patching TBLs!");
        }

        private static List<Section> GetSections(string tbl, string game)
        {
            List<Section> sections = new List<Section>();
            if (Path.GetFileName(tbl) == "itemtbl.bin")
            {
                var file = File.ReadAllBytes(tbl);
                sections.Add(new Section()
                {
                    size = file.Length,
                    data = file
                });
                return sections;
            }
            bool bigEndian = false;
            if (game == "Persona 5" || game == "Persona 5 Royal (PS4)" || game == "Persona 5 Royal (Switch)")
                bigEndian = true;
            using (FileStream
            fileStream = new FileStream(tbl, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(fileStream))
                {
                    while (br.BaseStream.Position < fileStream.Length)
                    {
                        Section section = new Section();
                        if (bigEndian)
                        {
                            var data = br.ReadBytes(4);
                            Array.Reverse(data);
                            section.size = BitConverter.ToInt32(data, 0);
                        }
                        else if (game == "Persona Q" || game == "Persona Q2") { section.size = (int)fileStream.Length; }
                        else
                            section.size = br.ReadInt32();
                        section.data = br.ReadBytes(section.size);
                        if ((br.BaseStream.Position % 16) != 0)
                        {
                            br.BaseStream.Position += 16 - (br.BaseStream.Position % 16);
                        }
                        sections.Add(section);
                    }
                }
            }
            return sections;
        }

        // P5's NAME.TBL Expandable support
        private static List<NameSection> GetNameSections(string tbl)
        {
            List<NameSection> sections = new List<NameSection>();
            byte[] tblBytes = File.ReadAllBytes(tbl);
            int pos = 0;
            NameSection section;
            while (pos < tblBytes.Length)
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

        private static List<NameSection> GetNameSectionQ(string tbl)
        {
            List<NameSection> sections = new List<NameSection>();
            byte[] tblBytes = File.ReadAllBytes(tbl);
            int pos = 0;
            NameSection section = new NameSection();

            section.pointersSize = BitConverter.ToInt16(SliceArray(tblBytes, pos, pos + 2), 0); //actually number of pointers in q2 nametbls
            byte[] segment = SliceArray(tblBytes, pos + 2, pos + 2 + section.pointersSize * 2);
            section.pointers = new List<ushort>();
            for (int i = 0; i < segment.Length; i += 2)
            {
                section.pointers.Add(BitConverter.ToUInt16(SliceArray(segment, i, i+2), 0));
            }

            pos += section.pointersSize * 2 + 2;
            section.namesSize = tblBytes.Length - pos;
            segment = SliceArray(tblBytes, pos, pos + section.namesSize);
            section.names = new List<byte[]>();
            List<byte> name = new List<byte>();
            foreach(var segmentByte in segment)
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
            section.names.Add(Encoding.ASCII.GetBytes("owo what\'s this")); // last ptr goes past eof, add dummy name for convenience later

            sections.Add(section);
            return sections;
        }

        private static List<NameSection> ReplaceName(List<NameSection> sections, byte[] patch, TablePatch namePatch, string game)
        {
            int section = 0;
            int index = 0;
            byte[] fileContents = null;
            if (patch != null)
            {
                section = Convert.ToInt32(patch[3]);
                index = BitConverter.ToInt16(SliceArray(patch, 4, 6).Reverse().ToArray(), 0);
                // Contents is what to replace
                fileContents = SliceArray(patch, 6, patch.Length);
            }
            else if (namePatch != null)
            {
                if (namePatch.section == null || namePatch.index == null || namePatch.name == null)
                {
                    Utilities.ParallelLogger.Log($"[ERROR] Incomplete patch, skipping...");
                    return sections;
                }
                section = (int)namePatch.section;
                index = (int)namePatch.index;
                fileContents = ConvertName(namePatch.name);
                if (fileContents == null)
                    return sections;
            }
            else
            {
                Utilities.ParallelLogger.Log($"[ERROR] No patch passed to replace function, skipping...");
                return sections;
            }

            if (section >= sections.Count)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Section chosen is out of bounds, skipping...");
                return sections;
            }

            if (index < 0)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Index cannot be negative, skipping...");
                return sections;
            }

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
                for (int i = (game == "Persona Q" || game == "Persona Q2" ? index : index + 1); i < sections[section].pointers.Count; i++)
                {
                    sections[section].pointers[i] += (UInt16)delta;
                }
            }
            return sections;
        }

        private static byte[] ConvertName(string name)
        {
            string[] stringData = Regex.Split(name, @"(\[.*?\])");
            List<byte> byteName = new List<byte>();
            foreach (var part in stringData)
            {
                if (!part.Contains('['))
                {
                    foreach (byte b in Encoding.ASCII.GetBytes(part))
                        byteName.Add(b);
                }
                else
                {
                    foreach (var hex in part.Substring(1, part.Length - 2).Split(' '))
                    {
                        if (hex.Length != 2)
                        {
                            Utilities.ParallelLogger.Log($"[ERROR] Couldn't parse hex string, skipping...");
                            return null;
                        }
                        try
                        {
                            byteName.Add(Convert.ToByte(hex, 16));
                        }
                        catch (Exception ex)
                        {
                            Utilities.ParallelLogger.Log($"[ERROR] Couldn't parse hex string ({ex.Message}), skipping...");
                            return null;
                        }
                    }
                }
            }
            return byteName.ToArray();
        }

        private static List<Section> ReplaceSection(List<Section> sections, TablePatch patch)
        {
            if (patch.offset == null || patch.section == null || patch.data == null)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Incomplete patch, skipping...");
                return sections;
            }
            // Get info from json patch
            int section = (int)patch.section;
            int offset = (int)patch.offset;
            string[] stringData = patch.data.Split(' ');
            byte[] data = new byte[stringData.Length];
            for (int i = 0; i < data.Length; i++)
            {
                try
                {
                    data[i] = Convert.ToByte(stringData[i], 16);
                }
                catch (Exception ex)
                {
                    Utilities.ParallelLogger.Log($"[ERROR] Couldn't parse hex string {stringData[i]} ({ex.Message}), skipping...");
                    return sections;
                }
            }
            if (offset < 0)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Offset cannot be negative, skipping...");
                return sections;
            }
            if (section >= sections.Count)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Section chosen is out of bounds, skipping...");
                return sections;
            }
            if (offset + data.Length >= sections[section].data.Length)
            {
                using (MemoryStream
                memoryStream = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(memoryStream))
                    {
                        bw.Write(sections[section].data);
                        while (offset >= memoryStream.Length)
                            bw.Write((byte)0);
                        bw.BaseStream.Position = offset;
                        bw.Write(data);
                        sections[section].data = memoryStream.ToArray();
                        sections[section].size = sections[section].data.Length;
                    }
                }
            }
            else
            {
                using (MemoryStream
                memoryStream = new MemoryStream(sections[section].data))
                {
                    using (BinaryWriter bw = new BinaryWriter(memoryStream))
                    {
                        if (offset >= memoryStream.Length)
                        {
                            bw.BaseStream.Position = memoryStream.Length - 1;
                            while (offset >= memoryStream.Length)
                                bw.Write((byte)0);
                        }
                        bw.BaseStream.Position = offset;
                        bw.Write(data);
                    }
                }
            }

            return sections;
        }

        private static void WriteNameTbl(List<NameSection> sections, string path)
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
                        foreach (var name in section.names)
                        {
                            bw.Write(name);
                            bw.Write((byte)0);
                        }
                        while (bw.BaseStream.Position % 16 != 0)
                            bw.Write((byte)0);
                    }
                }
            }
        }

        private static void WriteNameTblQ(List<NameSection> sections, string path)
        {
            using FileStream fStream = new FileStream(path, FileMode.Create);
            using BinaryWriter bw = new BinaryWriter(fStream);
            bw.Write(BitConverter.GetBytes((short)sections[0].pointersSize));
            foreach (var pointer in sections[0].pointers) { bw.Write(BitConverter.GetBytes(pointer)); }
            foreach (var name in sections[0].names)
            {
                bw.Write(name);
                bw.Write((byte)0);
            }
        }

        private static void WriteTbl(List<Section> sections, string path, string game)
        {
            bool bigEndian = false;
            if (game == "Persona 5" || game == "Persona 5 Royal (PS4)" || game == "Persona 5 Royal (Switch)")
                bigEndian = true;
            using (FileStream
            fileStream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fileStream))
                {
                    if (((game == "Persona 4 Golden" || game == "Persona 4 Golden (Vita)") && Path.GetFileName(path).Equals("itemtbl.bin", StringComparison.InvariantCultureIgnoreCase))
                        || (game == "Persona Q" || game == "Persona Q2"))
                        bw.Write(sections[0].data);
                    else
                    {
                        foreach (var section in sections)
                        {
                            // Write section size
                            if (bigEndian)
                                bw.Write(BitConverter.GetBytes(section.size).Reverse().ToArray());
                            else
                                bw.Write(BitConverter.GetBytes(section.size));
                            bw.Write(section.data);
                            while (bw.BaseStream.Position % 16 != 0)
                                bw.Write((byte)0);
                        }
                    }
                }
            }
        }

        private static bool QTblExists(string game, string tblPath)
        {
            if (game != "Persona Q" && game != "Persona Q2") { return false; }

            string csv = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/Dependencies/FilteredCpkCsv/filtered_data_pq";
            if (game == "Persona Q2") { csv += "2"; }
            csv += ".csv";

            if (!File.Exists(csv))
            {
                Utilities.ParallelLogger.Log($@"[ERROR] Couldn't find CSV file in Dependencies\FilteredCpkCsv");
                return false;
            }

            List<string> qTbls = File.ReadAllLines(csv).Where(t => Path.GetExtension(t) == ".tbl").ToList();

            return qTbls.Contains(tblPath);
        }

    }
}
