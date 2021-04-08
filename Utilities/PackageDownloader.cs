using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Windows;
using System.Text.RegularExpressions;
using System.Reflection;
using AemulusModManager.Utilities.PackageUpdating.DownloadUtils;
using AemulusModManager.Windows;
using System.Threading;
using AemulusModManager.Utilities.PackageUpdating;
using System.Diagnostics;

namespace AemulusModManager.Utilities
{
    public class PackageDownloader
    {
        private string URL_TO_ARCHIVE;
        private string URL;
        private string DL_ID;
        private string fileName;
        private string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private HttpClient client = new HttpClient();
        private GameBananaItem response = new GameBananaItem();
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private UpdateProgressBox progressBox;
        public async void Download(string line, bool running)
        {
            if (ParseProtocol(line))
            {
                if (await GetData())
                {
                    DownloadWindow downloadWindow = new DownloadWindow(response);
                    downloadWindow.ShowDialog();
                    if (downloadWindow.YesNo)
                    {
                        await DownloadFile(URL_TO_ARCHIVE, fileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                        await ExtractFile($@"{assemblyLocation}\Downloads\{fileName}", response.Game.Replace(" (PC)", ""));
                        var notification = new NotificationBox($"Finished installing {response.Name} for {response.Game.Replace("(PC)", "")}!\nHit refresh!");
                        notification.ShowDialog();
                    }
                }
            }
            if (running)
                Environment.Exit(0);
        }

        private async Task<bool> GetData()
        {
            try
            {
                string responseString = await client.GetStringAsync(URL);
                response = JsonConvert.DeserializeObject<GameBananaItem>(responseString);
                fileName = response.Files[DL_ID].FileName;
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while fetching data: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        
        private void ReportUpdateProgress(DownloadProgress progress)
        {
            if (progress.Percentage == 1)
            {
                progressBox.finished = true;
            }
            progressBox.progressBar.Value = progress.Percentage * 100;
            progressBox.taskBarItem.ProgressValue = progress.Percentage;
            progressBox.progressTitle.Text = $"Downloading {progress.FileName}...";
            progressBox.progressText.Text = $"{Math.Round(progress.Percentage * 100, 2)}% " +
                $"({StringConverters.FormatSize(progress.DownloadedBytes)} of {StringConverters.FormatSize(progress.TotalBytes)})";
        }
        
        private bool ParseProtocol(string line)
        {
            try
            {
                line = line.Replace("aemulus:", "");
                string[] data = line.Split(',');
                URL_TO_ARCHIVE = data[0];
                // Used to grab file info from dictionary
                var match = Regex.Match(URL_TO_ARCHIVE, @"\d*$");
                DL_ID = match.Value;
                string MOD_TYPE = data[1];
                string MOD_ID = data[2];
                URL = $"https://api.gamebanana.com/Core/Item/Data?itemtype={MOD_TYPE}&itemid={MOD_ID}&fields=name,Game().name," +
                    $"Files().aFiles(),Preview().sStructuredDataFullsizeUrl(),Preview().sSubFeedImageUrl()&return_keys=1";
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while parsing {line}: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        private async Task DownloadFile(string uri, string fileName, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            try
            {
                // Create the downloads folder if necessary
                Directory.CreateDirectory($@"{assemblyLocation}/Downloads");
                // Download the file if it doesn't already exist
                if (File.Exists($@"{assemblyLocation}/Downloads/{fileName}"))
                {
                    try
                    {
                        File.Delete($@"{assemblyLocation}/Downloads/{fileName}");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Couldn't delete the already existing {assemblyLocation}/Downloads/{fileName} ({e.Message})",
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                progressBox = new UpdateProgressBox(cancellationToken);
                progressBox.progressBar.Value = 0;
                progressBox.finished = false;
                progressBox.Title = $"Update Progress";
                progressBox.Show();
                progressBox.Activate();
                // Write and download the file
                using (var fs = new FileStream(
                    $@"{assemblyLocation}/Downloads/{fileName}", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete($@"{assemblyLocation}/Downloads/{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                MessageBox.Show($"Error whilst downloading {fileName}: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private async Task ExtractFile(string file, string game)
        {
            await Task.Run(() =>
            {
                    if (Path.GetExtension(file).ToLower() == ".7z" || Path.GetExtension(file).ToLower() == ".rar" || Path.GetExtension(file).ToLower() == ".zip")
                    {
                        Directory.CreateDirectory($@"{assemblyLocation}\temp");
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.CreateNoWindow = true;
                        startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
                        if (!FileIOWrapper.Exists(startInfo.FileName))
                        {
                            Console.Write($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                            return;
                        }

                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = false;
                        startInfo.Arguments = $@"x -y ""{file}"" -o""{assemblyLocation}\temp""";
                        Console.WriteLine($@"[INFO] Extracting {file} into Packages\{game}");
                        using (Process process = new Process())
                        {
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();
                        }
                        // Put in folder if extraction comes in multiple files/folders
                        if (Directory.GetFileSystemEntries($@"{assemblyLocation}\temp").Length > 1)
                        {
                            setAttributesNormal(new DirectoryInfo($@"{assemblyLocation}\temp"));
                            string path = $@"{assemblyLocation}\Packages\{game}\{Path.GetFileNameWithoutExtension(file)}";
                            int index = 2;
                            while (Directory.Exists(path))
                            {
                                path = $@"{assemblyLocation}\Packages\{game}\{Path.GetFileNameWithoutExtension(file)} ({index})";
                                index += 1;
                            }
                            MoveDirectory($@"{assemblyLocation}\temp", path);
                        }
                        // Move folder if extraction is just a folder
                        else if (Directory.GetFileSystemEntries($@"{assemblyLocation}\temp").Length == 1 && Directory.Exists(Directory.GetFileSystemEntries($@"{assemblyLocation}\temp")[0]))
                        {
                            setAttributesNormal(new DirectoryInfo($@"{assemblyLocation}\temp"));
                            string path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileName(Directory.GetFileSystemEntries($@"{assemblyLocation}\temp")[0])}";
                            int index = 2;
                            while (Directory.Exists(path))
                            {
                                path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileName(Directory.GetFileSystemEntries($@"{assemblyLocation}\temp")[0])} ({index})";
                                index += 1;
                            }
                            MoveDirectory(Directory.GetFileSystemEntries($@"{assemblyLocation}\temp")[0], path);
                        }
                        FileIOWrapper.Delete(file);
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] {file} isn't a folder, .zip, .7z, or .rar, skipping...");
                    }
                if (Directory.Exists($@"{assemblyLocation}\temp"))
                    Directory.Delete($@"{assemblyLocation}\temp", true);
            });
        }
        private void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (FileIOWrapper.Exists(targetFile)) FileIOWrapper.Delete(targetFile);
                    FileIOWrapper.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }
        public void setAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                setAttributesNormal(subDir);
                subDir.Attributes = FileAttributes.Normal;
            }
            foreach (var file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
        }
    }
}
