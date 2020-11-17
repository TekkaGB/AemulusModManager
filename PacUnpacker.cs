using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AemulusModManager
{
    class PacUnpacker
    {
        public void Unpack(string directory, string cpk)
        {
            List<string> pacs = new List<string>();
            List<string> globs = new List<string>{"*[!0-9].bin", "*2[0-1][0-9].bin", "*.arc", "*.pac", "*.pack"};
            switch(cpk)
            {
                case "data_e.cpk":
                    pacs.Add("data00004.pac");
                    break;
                case "data.cpk":
                    pacs.Add("data00000.pac");
                    pacs.Add("data00001.pac");
                    pacs.Add("data00003.pac");
                    break;
                case "data_k.cpk":
                    pacs.Add("data00005.pac");
                    break;
                case "data_c.cpk":
                    pacs.Add("data00006.pac");
                    break;
            }
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = @"Dependencies\nr2_unpacker.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            string paclist = "";
            Console.WriteLine($"[INFO] Unpacking pac files for {cpk}");
            foreach (var pac in pacs)
                paclist += $" \"{directory}" + @"\" + $"{pac}\" *.bin;*.arc;*.pac;*.pack";
            startInfo.Arguments = $"{paclist}";
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            Console.WriteLine("[INFO] Transferring mergeable pac files to Original directory... (This part might take a bit for data.cpk)");
            if (Directory.Exists($@"{directory}\data00000\bustup"))
                FileSystem.DeleteDirectory($@"{directory}\data00000\bustup", DeleteDirectoryOption.DeleteAllContents);
            if (Directory.Exists($@"{directory}\data00001\bustup"))
                FileSystem.DeleteDirectory($@"{directory}\data00001\bustup", DeleteDirectoryOption.DeleteAllContents);
            foreach (var pac in pacs)
                FileSystem.MoveDirectory($@"{directory}\{Path.GetFileNameWithoutExtension(pac)}", $@"Original\{Path.GetFileNameWithoutExtension(pac)}", true);
            Console.WriteLine($@"[INFO] Unpacking files for {cpk}...");
            startInfo.FileName = @"Dependencies\Preappfile\preappfile.exe";
            foreach (var glob in globs)
            {
                startInfo.Arguments = $@"-i ""{directory}\{cpk}"" -o Original\{Path.GetFileNameWithoutExtension(cpk)} --unpack-filter {glob}";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
            Console.WriteLine("[INFO] Finished unpacking vanilla files!");
            
        }
    }
}
