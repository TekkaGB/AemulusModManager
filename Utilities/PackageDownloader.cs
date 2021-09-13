﻿using System;
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
        private string URL_TO_PNG;
        private string MOD_NAME;
        private string AUTHOR; 
        private string DL_ID;
        private string fileName;
        private string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        private bool USE_API = true;
        private bool cancelled;
        private HttpClient client = new HttpClient();
        private GameBananaAPIV4 response = new GameBananaAPIV4();
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private UpdateProgressBox progressBox;
        public async void BrowserDownload(GameBananaRecord record, GameFilter game)
        {
            var gameName = "";
            switch (game)
            {
                case GameFilter.P3:
                    gameName = "Persona 3 FES";
                    break;
                case GameFilter.P4G:
                    gameName = "Persona 4 Golden";
                    break;
                case GameFilter.P5:
                    gameName = "Persona 5";
                    break;
                case GameFilter.P5S:
                    gameName = "Persona 5 Strikers";
                    break;
            }
            DownloadWindow downloadWindow = new DownloadWindow(record);
            downloadWindow.ShowDialog();
            if (downloadWindow.YesNo)
            {
                string downloadUrl = null;
                string fileName = null;
                if (record.Files.Count == 1)
                {
                    downloadUrl = record.Files[0].DownloadUrl;
                    fileName = record.Files[0].FileName;
                }
                else if (record.Files.Count > 1)
                {
                    UpdateFileBox fileBox = new UpdateFileBox(record.Files, record.Title);
                    fileBox.Activate();
                    fileBox.ShowDialog();
                    downloadUrl = fileBox.chosenFileUrl;
                    fileName = fileBox.chosenFileName;
                }
                if (downloadUrl != null && fileName != null)
                {
                    await DownloadFile(downloadUrl, fileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                    if (!cancelled)
                    {
                        await ExtractFile($@"{assemblyLocation}\Downloads\{fileName}", gameName);
                        if (File.Exists($@"{assemblyLocation}\refresh.aem"))
                            FileIOWrapper.Delete($@"{assemblyLocation}\refresh.aem");
                        FileIOWrapper.WriteAllText($@"{assemblyLocation}\refresh.aem", gameName);
                    }
                }
            }
        }
        public async void Download(string line, bool running)
        {
            if (ParseProtocol(line))
            {
                if (!USE_API | await GetData())
                {

                    DownloadWindow downloadWindow = null;
                    if (USE_API)
                    {
                        downloadWindow = new DownloadWindow(response);
                    } else
                    {
                        downloadWindow = new DownloadWindow(MOD_NAME, AUTHOR, new Uri(URL_TO_PNG));
                    }
                    downloadWindow.ShowDialog();
                    downloadWindow.Activate();
                    if (downloadWindow.YesNo)
                    {
                        await DownloadFile(URL_TO_ARCHIVE, fileName, new Progress<DownloadProgress>(ReportUpdateProgress),
                            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token));
                        await ExtractFile($@"{assemblyLocation}\Downloads\{fileName}", response.Game.Name.Replace(" (PC)", ""));
                        if (File.Exists($@"{assemblyLocation}\refresh.aem"))
                            FileIOWrapper.Delete($@"{assemblyLocation}\refresh.aem");
                        FileIOWrapper.WriteAllText($@"{assemblyLocation}\refresh.aem", response.Game.Name.Replace(" (PC)", ""));
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
                response = JsonConvert.DeserializeObject<GameBananaAPIV4>(responseString);
                fileName = response.Files.Where(x => x.ID == DL_ID).ToArray()[0].FileName;
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
                // new! it can now be either aemulus:path_to_archive,mod_type,mod_id or aemulus:path_to_archive,path_to_png,mod_name
                line = line.Replace("aemulus:", "");
                string[] data = line.Split(',');
                URL_TO_ARCHIVE = data[0];
                // Used to grab file info from dictionary
                var match = Regex.Match(URL_TO_ARCHIVE, @"\d*$");
                DL_ID = match.Value;
                string MOD_TYPE = data[1];
                string MOD_ID = data[2];
                var httpMatch = Regex.Match(MOD_TYPE, @"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?$");
                if (httpMatch.Success)
                {
                    USE_API = false;
                    URL_TO_PNG = MOD_TYPE;
                    MOD_NAME = MOD_ID;
                    AUTHOR = data[3];
                    fileName = GetFilenameFromUrl(URL_TO_ARCHIVE);
                } else
                {
                    USE_API = true;
                    URL = $"https://gamebanana.com/apiv4/{MOD_TYPE}/{MOD_ID}";
                }
    
                
                
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while parsing {line}: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        public static string GetFilenameFromUrl(string url)
        {
            return String.IsNullOrEmpty(url.Trim()) || !url.Contains(".") ? string.Empty : Path.GetFileName(new Uri(url).AbsolutePath);
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
                    cancelled = true;
                }
                return;
            }
            catch (Exception e)
            {
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                    cancelled = true;
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
                        MessageBox.Show($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.UseShellExecute = false;
                    startInfo.Arguments = $@"x -y ""{file}"" -o""{assemblyLocation}\temp""";
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();
                    }
                    setAttributesNormal(new DirectoryInfo($@"{assemblyLocation}\temp"));
                    foreach (var folder in Directory.GetDirectories($@"{assemblyLocation}\temp", "*", SearchOption.AllDirectories).Where(x => File.Exists($@"{x}\Package.xml") || File.Exists($@"{x}\Mod.xml")))
                    {
                        string path = $@"{assemblyLocation}\Packages\{game}\{Path.GetFileName(folder)}";
                        int index = 2;
                        while (Directory.Exists(path))
                        {
                            path = $@"{assemblyLocation}\Packages\{game}\{Path.GetFileName(folder)} ({index})";
                            index += 1;
                        }
                        MoveDirectory(folder, path);
                    }
                    var packgeSetup = Directory.GetFiles($@"{assemblyLocation}\temp", "*.xml", SearchOption.TopDirectoryOnly)
                        .Where(xml => !Path.GetFileName(xml).Equals("Package.xml", StringComparison.InvariantCultureIgnoreCase) && !Path.GetFileName(xml).Equals("Mod.xml", StringComparison.InvariantCultureIgnoreCase)).ToList();
                    if (packgeSetup.Count > 0)
                    {
                        Directory.CreateDirectory($@"{assemblyLocation}\Config\temp");
                        foreach (var xml in packgeSetup)
                        {
                            FileIOWrapper.Copy(xml, $@"{assemblyLocation}\Config\temp\{Path.GetFileName(xml)}", true);
                        }
                    }
                    FileIOWrapper.Delete(file);
                }
                else
                {
                    MessageBox.Show($"{file} isn't a .zip, .7z, or .rar, couldn't extract...", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
