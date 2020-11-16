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
            switch(cpk)
            {
                case "data_e.cpk":
                    pacs.Add("data00004.pac");
                    break;
                case "data.cpk":
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
            foreach (var pac in pacs)
                paclist += $" \"{directory}" + @"\" + $"{pac}\" *.bin;*.arc;*.pac;*.pack";
            startInfo.Arguments = $"{paclist}";
            Console.WriteLine($@"[INFO] Unpacking files for {cpk}... (This part takes a bit)");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            
            Console.WriteLine("[INFO] Transferring mergeable files to Original directory... (This part might take a bit too)");

            foreach (var pac in pacs)
            {
                FileSystem.CopyDirectory($@"{directory}\{Path.GetFileNameWithoutExtension(pac)}", $@"Original\{Path.GetFileNameWithoutExtension(pac)}", true);
                FileSystem.MoveDirectory($@"{directory}\{Path.GetFileNameWithoutExtension(pac)}", $@"Original\{Path.GetFileNameWithoutExtension(cpk)}", true);
            }
            Console.WriteLine("[INFO] Finished unpacking vanilla files!");
        }
    }
}
