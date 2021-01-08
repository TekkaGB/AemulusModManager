using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AemulusModManager
{
    public static class PacUnpacker
    {
        // P3F
        public static void Unzip(string iso)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = @"Dependencies\7z\7z.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = $"x -y \"{iso}\" -o\"" + @"Original\Persona 3 FES" + "\" BTL.CVM DATA.CVM";
            Console.WriteLine($"[INFO] Extracting BTL.CVM and DATA.CVM from {iso}");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }
            startInfo.Arguments = "x -y \"" + @"Original\Persona 3 FES\BTL.CVM" + "\" -o\"" + @"Original\Persona 3 FES\BTL" + "\" *.BIN *.PAK *.PAC *.TBL -r";
            Console.WriteLine($"[INFO] Extracting base files from BTL.CVM");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }
            File.Delete(@"Original\Persona 3 FES\BTL.CVM");
            startInfo.Arguments = "x -y \"" + @"Original\Persona 3 FES\DATA.CVM" + "\" -o\"" + @"Original\Persona 3 FES\DATA" + "\" *.BIN *.PAK *.PAC -r";
            Console.WriteLine($"[INFO] Extracting base files from DATA.CVM");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }
            File.Delete(@"Original\Persona 3 FES\DATA.CVM");
            Console.WriteLine($"[INFO] Finished extracting base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }

        // P4G
        public static void Unpack(string directory, string cpk)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });
            List<string> pacs = new List<string>();
            List<string> globs = new List<string>{"*[!0-9].bin", "*2[0-1][0-9].bin", "*.arc", "*.pac", "*.pack"};
            switch(cpk)
            {
                case "data_e.cpk":
                    pacs.Add("data00004.pac");
                    pacs.Add("data_e.cpk");
                    break;
                case "data.cpk":
                    pacs.Add("data00000.pac");
                    pacs.Add("data00001.pac");
                    pacs.Add("data00003.pac");
                    pacs.Add("data.cpk");
                    break;
                case "data_k.cpk":
                    pacs.Add("data00005.pac");
                    pacs.Add("data_k.cpk");
                    break;
                case "data_c.cpk":
                    pacs.Add("data00006.pac");
                    pacs.Add("data_c.cpk");
                    break;
            }
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = @"Dependencies\Preappfile\preappfile.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            foreach (var pac in pacs)
            {
                Console.WriteLine($"[INFO] Unpacking files for {pac}...");
                foreach (var glob in globs)
                {
                    startInfo.Arguments = $@"-i ""{directory}\{pac}"" -o ""Original\Persona 4 Golden\{Path.GetFileNameWithoutExtension(pac)}"" --unpack-filter {glob}";
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        Console.WriteLine(process.StandardOutput.ReadToEnd());
                        process.WaitForExit();
                    }
                }
            }

            Console.WriteLine("[INFO] Finished unpacking vanilla files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }


        public static void UnpackCPK(string directory)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            if (File.Exists($@"{directory}\ps3.cpk.66600") && File.Exists($@"{directory}\ps3.cpk.66601") && File.Exists($@"{directory}\ps3.cpk.66602")
                   && !File.Exists($@"{directory}\ps3.cpk"))
            {
                Console.Write("[INFO] Combining ps3.cpk parts");
                ProcessStartInfo cmdInfo = new ProcessStartInfo();
                cmdInfo.CreateNoWindow = true;
                cmdInfo.FileName = @"CMD.exe";
                cmdInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmdInfo.Arguments = $@"/C copy /b ""{directory}\ps3.cpk.66600"" + ""{directory}\ps3.cpk.66601"" + ""{directory}\ps3.cpk.66602"" ""{directory}\ps3.cpk""";

                using (Process process = new Process())
                {
                    process.StartInfo = cmdInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }

            Directory.CreateDirectory(@"Original\Persona 5");

            string[] dataFiles = File.ReadAllLines(@"Dependencies\MakeCpk\filtered_data.csv");
            string[] ps3Files = File.ReadAllLines(@"Dependencies\MakeCpk\filtered_ps3.csv");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = @"Dependencies\MakeCpk\YACpkTool.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            

            Console.WriteLine($"[INFO] Extracting data.cpk");
            foreach (var file in dataFiles)
            {
                startInfo.Arguments = $@"-X {file} -i ""{directory}\data.cpk"" -o ""Original\Persona 5""";

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    Console.WriteLine(process.StandardOutput.ReadToEnd());
                    process.WaitForExit();
                }
            }

            Console.WriteLine($"[INFO] Extracting ps3.cpk");
            foreach (var file in ps3Files)
            {
                startInfo.Arguments = $@"-X {file} -i ""{directory}\ps3.cpk"" -o ""Original\Persona 5""";

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    Console.WriteLine(process.StandardOutput.ReadToEnd());
                    process.WaitForExit();
                }
            }
            Console.WriteLine($"[INFO] Finished extracting base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }
    }
}
