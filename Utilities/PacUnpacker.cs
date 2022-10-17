using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using AemulusModManager.Utilities;

namespace AemulusModManager
{
    public static class PacUnpacker
    {
        //P1PSP
        public static async Task UnzipAndUnBin(string iso)
        {
            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 1 (PSP)");
            if (!FileIOWrapper.Exists(iso))
            {
                Console.Write($"[ERROR] Couldn't find {iso}. Please correct the file path in config.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
            {
                Console.Write($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });


            var tasks = new List<Task>();

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = $"x -y \"{iso}\" -o\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 1 (PSP)";
            Console.WriteLine($"[INFO] Extracting files from {iso}");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            FileIOWrapper.Move($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 1 (PSP)\PSP_GAME\SYSDIR\EBOOT.BIN", $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 1 (PSP)\PSP_GAME\SYSDIR\EBOOT_ENC.BIN");
            ProcessStartInfo ebootDecoder = new ProcessStartInfo();
            ebootDecoder.CreateNoWindow = true;
            ebootDecoder.UseShellExecute = false;
            ebootDecoder.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\\DecEboot\deceboot.exe";
            ebootDecoder.WindowStyle = ProcessWindowStyle.Hidden;
            ebootDecoder.Arguments = "\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 1 (PSP)\PSP_GAME\SYSDIR\EBOOT_ENC.BIN" + "\" \"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 1 (PSP)\PSP_GAME\SYSDIR\EBOOT.BIN" + "\"";
            Console.WriteLine($"[INFO] Decrypting EBOOT.BIN");
            using (Process process = new Process())
            {
                process.StartInfo = ebootDecoder;
                process.Start();

                // Add this: wait until process does its work
                process.WaitForExit();
            }
            FileIOWrapper.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 1 (PSP)\PSP_GAME\SYSDIR\EBOOT_ENC.BIN");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }

        // P3F
        public static async Task Unzip(string iso)
        {
            if (!FileIOWrapper.Exists(iso))
            {
                Console.Write($"[ERROR] Couldn't find {iso}. Please correct the file path in config.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
            {
                Console.Write($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            var tasks = new List<Task>();

            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = $"x -y \"{iso}\" -o\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES" + "\" BTL.CVM DATA.CVM";
            Console.WriteLine($"[INFO] Extracting BTL.CVM and DATA.CVM from {iso}");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            startInfo.Arguments = "x -y \"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\BTL.CVM" + "\" -o\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\BTL" + "\" *.BIN *.PAK *.PAC *.TBL *.SPR *.BF *.BMD *.PM1 *.bf *.bmd *.pm1 *.FPC -r";
            Console.WriteLine($"[INFO] Extracting base files from BTL.CVM");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            startInfo.Arguments = "x -y \"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\DATA.CVM" + "\" -o\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\DATA" + "\" *.BIN *.PAK *.PAC *.TBL *.SPR *.BF *.BMD *.PM1 *.bf *.bmd *.pm1 *.FPC -r";
            Console.WriteLine($"[INFO] Extracting base files from DATA.CVM");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }
            ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES");
            FileIOWrapper.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\BTL.CVM");
            FileIOWrapper.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 FES\DATA.CVM");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }

        // P3P
        public static async Task UnzipAndUnpackCPK(string iso)
        {
            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable");
            if (!FileIOWrapper.Exists(iso))
            {
                Console.Write($"[ERROR] Couldn't find {iso}. Please correct the file path in config.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
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
            startInfo.Arguments = $"x -y \"{iso}\" -o\"" + $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable" + "\" PSP_GAME\\USRDIR\\umd0.cpk";
            Console.WriteLine($"[INFO] Extracting umd0.cpk from {iso}");
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }

            string[] umd0Files = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_umd0.csv");
            var umd0FileChunks = umd0Files.Split(umd0Files.Length / 2);

            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\YACpkTool.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
            {
                Console.WriteLine($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }

            var umd0Path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable\PSP_GAME\USRDIR\umd0.cpk";
            var tasks = new List<Task>();

            Console.WriteLine($"[INFO] Extracting files from umd0.cpk");
            foreach (var chunk in umd0FileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        if (FileIOWrapper.Exists(umd0Path))
                        {
                            startInfo.RedirectStandardOutput = true;
                            foreach (var file in chunk)
                            {
                                startInfo.Arguments = $@"-X {file} -i ""{umd0Path}"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable""";

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
                            Console.WriteLine($@"[ERROR] Couldn't find {Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable\PSP_GAME\USRDIR\umd0.cpk.");
                    }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            Console.WriteLine("[INFO] Unpacking extracted files");
                ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable\data");
            if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable\PSP_GAME"))
                Directory.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 3 Portable\PSP_GAME", true);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }

        // P4G
        public static void Unpack(string directory, string cpk)
        {
            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden");
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"[ERROR] Couldn't find {directory}. Please correct the file path in config.");
                return;
            }
            List<string> pacs = new List<string>();
            List<string> globs = new List<string> { "*[!0-9].bin", "*2[0-1][0-9].bin", "*.arc", "*.pac", "*.pack", "*.bf", "*.bmd", "*.pm1" };
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
            if (!FileIOWrapper.Exists(startInfo.FileName))
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
                ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{Path.GetFileNameWithoutExtension(pac)}");
            }
            if (FileIOWrapper.Exists($@"{directory}\{cpk}") && !FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpk}"))
            {
                Console.WriteLine($@"[INFO] Backing up {cpk}");
                FileIOWrapper.Copy($@"{directory}\{cpk}", $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\{cpk}", true);
            }
            if (FileIOWrapper.Exists($@"{directory}\movie.cpk") && !FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk"))
            {
                Console.WriteLine($@"[INFO] Backing up movie.cpk");
                FileIOWrapper.Copy($@"{directory}\movie.cpk", $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden\movie.cpk", true);
            }

            Console.WriteLine("[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }


        public static async Task UnpackP5CPK(string directory)
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

            if (FileIOWrapper.Exists($@"{directory}\ps3.cpk.66600") && FileIOWrapper.Exists($@"{directory}\ps3.cpk.66601") && FileIOWrapper.Exists($@"{directory}\ps3.cpk.66602")
                   && !FileIOWrapper.Exists($@"{directory}\ps3.cpk"))
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

            if (!FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_data.csv") 
                || !FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ps3.csv"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find CSV files used for unpacking in Dependencies\MakeCpk");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }

            string[] dataFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_data.csv");
            var dataFileChunks = dataFiles.Split(dataFiles.Length / 2);
            string[] ps3Files = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ps3.csv");
            var ps3FileChunks = ps3Files.Split(ps3Files.Length / 2);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\YACpkTool.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
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

            var tasks = new List<Task>();

            Console.WriteLine($"[INFO] Extracting data.cpk");
            foreach (var chunk in dataFileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        if (FileIOWrapper.Exists($@"{directory}\data.cpk"))
                        {
                            foreach (var file in chunk)
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
                    }));
            }

            Console.WriteLine($"[INFO] Extracting ps3.cpk");
            foreach (var chunk in ps3FileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        if (FileIOWrapper.Exists($@"{directory}\ps3.cpk"))
                        {
                            foreach (var file in chunk)
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
                    }));
            }
            await Task.WhenAll(tasks);
            tasks.Clear();
            ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }
        public static async Task UnpackP5RCPKs(string directory, string language, string version)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"[ERROR] Couldn't find {directory}. Please correct the file path.");
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal");

            if (!FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_dataR.csv")
                || !FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ps4R.csv"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find CSV files used for unpacking in Dependencies\MakeCpk");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }

            string[] dataRFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_dataR.csv");
            var dataRFileChunks = dataRFiles.Split(dataRFiles.Length / 2);
            string[] ps4RFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ps4R.csv");
            var ps4RFileChunks = ps4RFiles.Split(ps4RFiles.Length / 2);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\YACpkTool.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
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

            var tasks = new List<Task>();

            Console.WriteLine($"[INFO] Extracting dataR.cpk");
            foreach (var chunk in dataRFileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        if (FileIOWrapper.Exists($@"{directory}\dataR.cpk"))
                        {
                            foreach (var file in chunk)
                            {
                                startInfo.Arguments = $@"-X {file} -i ""{directory}\dataR.cpk"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal""";

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
                            Console.WriteLine($"[ERROR] Couldn't find dataR.cpk in {directory}.");
                    }));
            }

            Console.WriteLine($"[INFO] Extracting ps4R.cpk");
            foreach (var chunk in ps4RFileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        if (FileIOWrapper.Exists($@"{directory}\ps4R.cpk"))
                        {
                            foreach (var file in chunk)
                            {
                                startInfo.Arguments = $@"-X {file} -i ""{directory}\ps4R.cpk"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal""";

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
                            Console.WriteLine($"[ERROR] Couldn't find ps4R.cpk in {directory}.");
                    }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            if (language != "English")
            {
                string[] dataRLocalizedFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_dataR_Localized.csv");
                var dataRLocalizedFileChunks = dataRLocalizedFiles.Split(dataRLocalizedFiles.Length / 2);
                var localizedCpk = String.Empty;
                switch (language)
                {
                    case "French":
                        localizedCpk = "dataR_F.cpk";
                        break;
                    case "Italian":
                        localizedCpk = "dataR_I.cpk";
                        break;
                    case "German":
                        localizedCpk = "dataR_G.cpk";
                        break;
                    case "Spanish":
                        localizedCpk = "dataR_S.cpk";
                        break;
                }
                Console.WriteLine($"[INFO] Extracting {localizedCpk}");
                foreach (var chunk in dataRLocalizedFileChunks)
                {
                    tasks.Add(
                        Task.Run(() =>
                        {
                            if (FileIOWrapper.Exists($@"{directory}\{localizedCpk}"))
                            {
                                foreach (var file in chunk)
                                {
                                    startInfo.Arguments = $@"-X {file} -i ""{directory}\{localizedCpk}"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal""";

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
                                Console.WriteLine($"[ERROR] Couldn't find {localizedCpk} in {directory}.");
                        }));
                }
                await Task.WhenAll(tasks);
                tasks.Clear();
            }

            // Extract patch2R.cpk files
            if (version == ">= 1.02")
            {
                string[] patch2RFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_patch2R.csv");
                var patch2RFileChunks = patch2RFiles.Split(patch2RFiles.Length / 2);
                Console.WriteLine($"[INFO] Extracting patch2R.cpk");
                foreach (var chunk in patch2RFileChunks)
                {
                    tasks.Add(
                        Task.Run(() =>
                        {
                            if (FileIOWrapper.Exists($@"{directory}\patch2R.cpk"))
                            {
                                foreach (var file in chunk)
                                {
                                    startInfo.Arguments = $@"-X {file} -i ""{directory}\patch2R.cpk"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal""";

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
                                Console.WriteLine($"[ERROR] Couldn't find patch2R.cpk in {directory}.");
                        }));
                }
                await Task.WhenAll(tasks);
                tasks.Clear();
                if (language != "English")
                {
                    var patchSuffix = String.Empty;
                    switch (language)
                    {
                        case "French":
                            patchSuffix = "_F";
                            break;
                        case "Italian":
                            patchSuffix = "_I";
                            break;
                        case "German":
                            patchSuffix = "_G";
                            break;
                        case "Spanish":
                            patchSuffix = "_S";
                            break;
                    }
                    string[] patch2RLocalizedFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_patch2R{patchSuffix}.csv");
                    var patch2RLocalizedFileChunks = patch2RFiles.Split(patch2RLocalizedFiles.Length / 2);
                    Console.WriteLine($"[INFO] Extracting patch2R{patchSuffix}.cpk");
                    foreach (var chunk in patch2RLocalizedFileChunks)
                    {
                        tasks.Add(
                            Task.Run(() =>
                            {
                                if (FileIOWrapper.Exists($@"{directory}\patch2R{patchSuffix}.cpk"))
                                {
                                    foreach (var file in chunk)
                                    {
                                        startInfo.Arguments = $@"-X {file} -i ""{directory}\patch2R{patchSuffix}.cpk"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal""";

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
                                    Console.WriteLine($"[ERROR] Couldn't find patch2R{patchSuffix}.cpk in {directory}.");
                            }));
                    }
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }
        public static async Task UnpackP5RSwitchCPKs(string directory, string language)
        {
            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"[ERROR] Couldn't find {directory}. Please correct the file path.");
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal (Switch)");

            if (!FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ALL_USEU_BASE.csv")
                || !FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_PATCH1.csv"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find CSV files used for unpacking in Dependencies\MakeCpk");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }

            string[] baseFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ALL_USEU_BASE.csv");
            var baseFileChunks = dataRFiles.Split(dataRFiles.Length / 2);
            string[] patch1Files = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_PATCH1.csv");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\YACpkTool.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
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

            var tasks = new List<Task>();

            Console.WriteLine($"[INFO] Extracting PATCH1.CPK");
            if (FileIOWrapper.Exists($@"{directory}\PATCH1.CPK"))
            {
                foreach (var file in chunk)
                {
                    startInfo.Arguments = $@"-X {file} -i ""{directory}\PATCH1.cpk"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal (Switch)""";

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
                Console.WriteLine($"[ERROR] Couldn't find PATCH1.CPK in {directory}.");
            Console.WriteLine($"[INFO] Extracting base files from ALL_USEU.CPK");
            foreach (var chunk in baseFileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        if (FileIOWrapper.Exists($@"{directory}\ALL_USEU.CPK"))
                        {
                            foreach (var file in chunk)
                            {
                                startInfo.Arguments = $@"-X {file} -i ""{directory}\ALL_USEU.CPK"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal (Switch)""";

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
                            Console.WriteLine($"[ERROR] Couldn't find ALL_USEU.CPK in {directory}.");
                    }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            var languageSuffix = String.Empty;
            switch (language)
            {
                case "English":
                    languageSuffix = "_E";
                    break;
                case "French":
                    languageSuffix = "_F";
                    break;
                case "Italian":
                    languageSuffix = "_I";
                    break;
                case "German":
                    languageSuffix = "_G";
                    break;
                case "Spanish":
                    languageSuffix = "_S";
                    break;
            }
            string[] localizedFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_ALL_USEU{languageSuffix}.csv");
            var localizedFileChunks = dataRLocalizedFiles.Split(dataRLocalizedFiles.Length / 2);
            Console.WriteLine($"[INFO] Extracting {language} files from ALL_USEU.CPK");
            foreach (var chunk in localizedFileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        if (FileIOWrapper.Exists($@"{directory}\ALL_USEU.CPK"))
                        {
                            foreach (var file in chunk)
                            {
                                startInfo.Arguments = $@"-X {file} -i ""{directory}\ALL_USEU.CPK"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal (Switch)""";

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
                            Console.WriteLine($"[ERROR] Couldn't find ALL_USEU.CPK in {directory}.");
                    }));
            }
            await Task.WhenAll(tasks);
            tasks.Clear();

            ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 5 Royal (Switch)");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }
        public static async Task UnpackP4GCPK(string cpk)
        {
            if (!FileIOWrapper.Exists(cpk))
            {
                Console.WriteLine($"[ERROR] Couldn't find {cpk}. Please correct the file path.");
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden (Vita)");

            if (!FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_p4gdata.csv"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find CSV file used for unpacking in Dependencies\MakeCpk");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }

            string[] dataFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_p4gdata.csv");
            var dataFileChunks = dataFiles.Split(dataFiles.Length / 2);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\YACpkTool.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
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

            var tasks = new List<Task>();

            Console.WriteLine($"[INFO] Extracting data.cpk");
            foreach (var chunk in dataFileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        foreach (var file in chunk)
                        {
                            startInfo.Arguments = $@"-X {file} -i ""{cpk}"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden (Vita)""";

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
                    }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
            Console.WriteLine("[INFO] Unpacking extracted files");
            ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona 4 Golden (Vita)");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }
        public static async Task UnpackPQ2CPK(string cpk)
        {
            if (!FileIOWrapper.Exists(cpk))
            {
                Console.WriteLine($"[ERROR] Couldn't find {cpk}. Please correct the file path.");
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona Q2");

            if (!FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_data_pq2.csv"))
            {
                Console.WriteLine($@"[ERROR] Couldn't find CSV file used for unpacking in Dependencies\MakeCpk");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                return;
            }

            string[] dataFiles = FileIOWrapper.ReadAllLines($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\filtered_data_pq2.csv");
            var dataFileChunks = dataFiles.Split(dataFiles.Length / 4);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\MakeCpk\YACpkTool.exe";
            if (!FileIOWrapper.Exists(startInfo.FileName))
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

            var tasks = new List<Task>();

            Console.WriteLine($"[INFO] Extracting data.cpk");
            foreach (var chunk in dataFileChunks)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        foreach (var file in chunk)
                        {
                            startInfo.Arguments = $@"-X {file} -i ""{cpk}"" -o ""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona Q2""";

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
                    }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
            Console.WriteLine("[INFO] Unpacking extracted files");
            ExtractWantedFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\Persona Q2");
            Console.WriteLine($"[INFO] Finished unpacking base files!");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }
        private static void ExtractWantedFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            var files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories).
                Where(s => s.ToLower().EndsWith(".arc") || s.ToLower().EndsWith(".bin") || s.ToLower().EndsWith(".pac") || s.ToLower().EndsWith(".pak") || s.ToLower().EndsWith(".abin")
                || s.ToLower().EndsWith(".gsd") || s.ToLower().EndsWith(".tpc"));
            foreach(string file in files)
            {
                List<string> contents = binMerge.getFileContents(file).Select(x => x.ToLower()).ToList();
                // Check if there are any files we want (or files that could have files we want) and unpack them if so
                bool containersFound = contents.Exists(x => x.ToLower().EndsWith(".bin") || x.ToLower().EndsWith(".pac") || x.ToLower().EndsWith(".pak") || x.ToLower().EndsWith(".abin") || x.ToLower().EndsWith(".arc"));
                if(contents.Exists(x => x.ToLower().EndsWith(".bf") || x.ToLower().EndsWith(".bmd") || x.ToLower().EndsWith(".pm1") || x.ToLower().EndsWith(".dat") || x.ToLower().EndsWith(".ctd") || x.ToLower().EndsWith(".ftd") || x.ToLower().EndsWith(".spd") || containersFound))
                {
                    Console.WriteLine($"[INFO] Unpacking {file}");
                    binMerge.PAKPackCMD($"unpack \"{file}\"");

                    // Search the location of the unpacked container for wanted files
                    if (containersFound)
                        ExtractWantedFiles(Path.Combine(Path.GetDirectoryName(file),Path.GetFileNameWithoutExtension(file)));
                }
            }

        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size)
        {
            for (var i = 0; i < (float)array.Length / size; i++)
            {
                yield return array.Skip(i * size).Take(size);
            }
        }

    }
}
