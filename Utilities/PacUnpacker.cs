using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
            if (!File.Exists(iso))
            {
                Console.Write($"[ERROR] Couldn't find {iso}. Please correct the file path in config.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
            if (!File.Exists(startInfo.FileName))
            {
                Console.Write($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = $"x -y \"{iso}\" -o\"" + @"Original\Persona 3 FES" + "\" BTL.CVM DATA.CVM";
            Console.WriteLine($"[INFO] Extracting BTL.CVM and DATA.CVM from {iso}");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            startInfo.Arguments = "x -y \"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\BTL.CVM" + "\" -o\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\BTL" + "\" *.BIN *.PAK *.PAC *.TBL -r";
            Console.WriteLine($"[INFO] Extracting base files from BTL.CVM");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            File.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\BTL.CVM");
            startInfo.Arguments = "x -y \"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\DATA.CVM" + "\" -o\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\DATA" + "\" *.BIN *.PAK *.PAC -r";
            Console.WriteLine($"[INFO] Extracting base files from DATA.CVM");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            File.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\DATA.CVM");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }

        // P4G
        public static void Unpack(string directory, string cpk)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"[ERROR] Couldn't find {directory}. Please correct the file path in config.");
                return;
            }
            List<string> pacs = new List<string>();
            List<string> globs = new List<string> { "*[!0-9].bin", "*2[0-1][0-9].bin", "*.arc", "*.pac", "*.pack" };
            switch (cpk)
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
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\Preappfile\preappfile.exe";
            if (!File.Exists(startInfo.FileName))
            {
                Console.WriteLine($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            foreach (var pac in pacs)
            {
                Console.WriteLine($"[INFO] Unpacking files for {pac}...");
                foreach (var glob in globs)
                {
                    startInfo.Arguments = $@"-i ""{directory}\{pac}"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{Path.GetFileNameWithoutExtension(pac)}"" --unpack-filter {glob}";
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        while (!process.HasExited)
                        {
                            string text = process.StandardOutput.ReadLine();
                            if (text != "" && text != null)
                                Console.WriteLine($"[INFO] {text}");
                        }
                    }
                }
            }

            if (File.Exists($@"{directory}\{cpk}") && !File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpk}"))
            {
                Console.WriteLine($@"[INFO] Backing up {cpk}");
                File.Copy($@"{directory}\{cpk}", $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpk}", true);
            }
            if (File.Exists($@"{directory}\movie.cpk") && !File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk"))
            {
                Console.WriteLine($@"[INFO] Backing up movie.cpk");
                File.Copy($@"{directory}\movie.cpk", $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk", true);
            }

            Console.WriteLine("[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }


        public static void UnpackCPK(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"[ERROR] Couldn't find {directory}. Please correct the file path in config.");
                return;
            }
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

            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5");

            if (!File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_data.csv") 
                || !File.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ps3.csv"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find CSV files used for unpacking in Dependencies\MakeCpk");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }

            string[] dataFiles = File.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_data.csv");
            string[] ps3Files = File.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ps3.csv");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\YACpkTool.exe";
            if (!File.Exists(startInfo.FileName))
            {
                Console.WriteLine($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;


            if (File.Exists($@"{directory}\data.cpk"))
            {
                Console.WriteLine($"[INFO] Extracting data.cpk");
                foreach (var file in dataFiles)
                {
                    startInfo.Arguments = $@"-X {file} -i ""{directory}\data.cpk"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5""";

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        while (!process.HasExited)
                        {
                            string text = process.StandardOutput.ReadLine();
                            if (text != "" && text != null)
                                Console.WriteLine($"[INFO] {text}");
                        }
                    }
                }
            }
            else
                Console.WriteLine($"[ERROR] Couldn't find data.cpk in {directory}.");

            if (File.Exists($@"{directory}\data.cpk"))
            {
                Console.WriteLine($"[INFO] Extracting ps3.cpk");
                foreach (var file in ps3Files)
                {
                    startInfo.Arguments = $@"-X {file} -i ""{directory}\ps3.cpk"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5""";

                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        while (!process.HasExited)
                        {
                            string text = process.StandardOutput.ReadLine();
                            if (text != "" && text != null)
                                Console.WriteLine($"[INFO] {text}");
                        }
                    }
                }
            }
            else
                Console.WriteLine($"[ERROR] Couldn't find ps3.cpk in {directory}.");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }
    }
}
