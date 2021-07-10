using AemulusModManager.Utilities;
using AemulusModManager.Utilities.PackageUpdating;
using AemulusModManager.Utilities.PackageUpdating.DownloadUtils;
using AemulusModManager.Windows;
using Newtonsoft.Json;
using Octokit;
using Onova;
using Onova.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace AemulusModManager
{
    public class PackageUpdater
    {
        private readonly HttpClient client;
        private GitHubClient gitHubClient;
        private UpdateProgressBox progressBox;
        private MainWindow main;
        private string assemblyLocation;

        public PackageUpdater(MainWindow mainWindow)
        {
            client = new HttpClient();
            gitHubClient = new GitHubClient(new ProductHeaderValue("Aemulus"));
            main = mainWindow;
            assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public async Task CheckForUpdate(DisplayedMetadata[] rows, string game, CancellationTokenSource cancellationToken, bool downloadingMissing = false)
        {
            try
            {
                // Check GameBanana Items
                DisplayedMetadata[] gameBananaRows = rows.Where(row => UrlConverter.Convert(row.link) == "GameBanana").ToArray();
                if (gameBananaRows.Length > 0)
                {

                    string gameBananaRequestUrl = "https://api.gamebanana.com/Core/Item/Data?";
                    // GameBanana
                    foreach (DisplayedMetadata row in gameBananaRows)
                    {
                        // Convert the url
                        Uri uri = CreateUri(row.link);
                        string itemType = uri.Segments[1];
                        itemType = char.ToUpper(itemType[0]) + itemType.Substring(1, itemType.Length - 3);
                        string itemId = uri.Segments[2];
                        gameBananaRequestUrl += $"itemtype[]={itemType}&itemid[]={itemId}&fields[]=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates(),Files().aFiles(),Owner().name,Preview().sStructuredDataFullsizeUrl(),name&";
                    }
                    gameBananaRequestUrl += "return_keys=1";
                    // Parse the response
                    string responseString = await client.GetStringAsync(gameBananaRequestUrl);
                    responseString = responseString.Replace("\"Files().aFiles()\": []", "\"Files().aFiles()\": {}");
                    GameBananaItem[] response = JsonConvert.DeserializeObject<GameBananaItem[]>(responseString);
                    if (response == null)
                    {
                        Console.WriteLine("[ERROR] Error whilst checking for package updates: No response from GameBanana API");
                    }
                    else
                    {
                        for (int i = 0; i < gameBananaRows.Length; i++)
                        {
                            try
                            {
                                await GameBananaUpdate(response[i], gameBananaRows[i], game, new Progress<DownloadProgress>(ReportUpdateProgress), CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token), downloadingMissing);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"[ERROR] Error whilst updating/checking for updates for {gameBananaRows[i].name}: {e.Message}");
                            }
                        }
                    }
                }
                // Check GitHub Items
                DisplayedMetadata[] gitHubRows = rows.Where(row => UrlConverter.Convert(row.link) == "GitHub").ToArray();
                if (gitHubRows.Length > 0)
                {
                    foreach (DisplayedMetadata row in gitHubRows)
                    {
                        try
                        {
                            Uri uri = CreateUri(row.link);
                            Release latestRelease = await gitHubClient.Repository.Release.GetLatest(uri.Segments[1].Replace("/", ""), uri.Segments[2].Replace("/", ""));
                            await GitHubUpdate(latestRelease, row, game, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken, downloadingMissing);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"[ERROR] Error whilst updating/checking for updates for {row.name}: {e.Message}");
                        }
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"[ERROR] Connection error whilst checking for updates: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error whilst checking for updates: {e.Message}");
            }
        }

        public async Task<bool> CheckForAemulusUpdate(string aemulusVersion, CancellationTokenSource cancellationToken)
        {
            try
            {
                Uri uri = CreateUri($"https://github.com/TekkaGB/AemulusModManager");
                Release release = await gitHubClient.Repository.Release.GetLatest(uri.Segments[1].Replace("/", ""), uri.Segments[2].Replace("/", ""));
                Match onlineVersionMatch = Regex.Match(release.TagName, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                string onlineVersion = null;
                if (onlineVersionMatch.Success)
                {
                    onlineVersion = onlineVersionMatch.Groups["version"].Value;
                }
                if (UpdateAvailable(onlineVersion, aemulusVersion))
                {
                    Console.WriteLine($"[INFO] An update is available for Aemulus ({onlineVersion})");
                    NotificationBox notification = new NotificationBox($"Aemulus has a new update ({release.TagName}):\n{release.Body}\n\nWould you like to update?", false);
                    notification.ShowDialog();
                    notification.Activate();
                    if (!notification.YesNo)
                        return false;
                    string downloadUrl, fileName;
                    downloadUrl = release.Assets.First().BrowserDownloadUrl;
                    fileName = release.Assets.First().Name;
                    if (downloadUrl != null && fileName != null)
                    {
                        await DownloadAemulus(downloadUrl, fileName, onlineVersion, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken);
                        // Notify that the update is about to happen
                        NotificationBox finishedNotification = new NotificationBox($"Finished downloading {fileName}!\nAemulus will now restart.", true);
                        finishedNotification.ShowDialog();
                        finishedNotification.Activate();
                        // Update Aemulus
                        UpdateManager updateManager = new UpdateManager(new LocalPackageResolver(@$"{assemblyLocation}\Downloads\AemulusUpdate"), new Zip7Extractor());
                        if (!Version.TryParse(onlineVersion, out Version version))
                        {
                            Console.WriteLine("[ERROR] Error parsing Aemulus version, cancelling update");
                            // TODO Delete the downloaded stuff
                            return false;
                        }
                        // Updates and restarts Aemulus
                        await updateManager.PrepareUpdateAsync(version);
                        // Clean up the downloaded files
                        if (Directory.Exists($@"{assemblyLocation}\Downloads\"))
                        {
                            Directory.Delete($@"{assemblyLocation}\Downloads\", true);
                        }
                        updateManager.LaunchUpdater(version);
                        return true;
                    }
                }
                else
                {
                    Console.WriteLine($"[INFO] No updates available for Aemulus");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error whilst checking for updates: {e.Message}");
            }
            return false;
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

        private async Task GameBananaUpdate(GameBananaItem item, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing)
        {
            if (!downloadingMissing && item.HasUpdates)
            {
                GameBananaItemUpdate[] updates = item.Updates;
                string updateTitle = updates[0].Title;
                int updateIndex = 0;
                Match onlineVersionMatch = Regex.Match(updateTitle, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                string onlineVersion = null;
                if (onlineVersionMatch.Success)
                {
                    onlineVersion = onlineVersionMatch.Groups["version"].Value;
                }
                // GB Api only returns two latest updates, so if the first doesn't have a version try the second
                else if (updates.Length > 1)
                {
                    updateTitle = updates[1].Title;
                    onlineVersionMatch = Regex.Match(updateTitle, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                    updateIndex = 1;
                    if (onlineVersionMatch.Success)
                    {
                        onlineVersion = onlineVersionMatch.Groups["version"].Value;
                    }
                }
                Match localVersionMatch = Regex.Match(row.version, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                string localVersion = null;
                if (localVersionMatch.Success)
                {
                    localVersion = localVersionMatch.Groups["version"].Value;
                }
                if (row.skippedVersion != null)
                {
                    if (row.skippedVersion == "all" || !UpdateAvailable(onlineVersion, row.skippedVersion))
                    {
                        Console.WriteLine($"[INFO] No updates available for {row.name}");
                        return;
                    }
                }
                if (UpdateAvailable(onlineVersion, localVersion))
                {
                    Console.WriteLine($"[INFO] An update is available for {row.name} ({onlineVersion})");
                    // Display the changelog and confirm they want to update
                    ChangelogBox changelogBox = new ChangelogBox(updates[updateIndex], row.name, $"Would you like to update {row.name} to version {onlineVersion}?", row, onlineVersion, $@"{assemblyLocation}\Packages\{game}\{row.path}\Package.xml", false);
                    changelogBox.Activate();
                    changelogBox.ShowDialog();
                    if (!changelogBox.YesNo)
                    {
                        Console.WriteLine($"[INFO] Cancelled update for {row.name}");
                        return;
                    }

                    // Download the update
                    await GameBananaDownload(item, row, game, progress, cancellationToken, downloadingMissing, updates, onlineVersion, updateIndex);

                }
                else
                {
                    Console.WriteLine($"[INFO] No updates available for {row.name}");
                }
            }
            else if (downloadingMissing)
            {
                // Ask if the user wants to download the mod
                DownloadWindow downloadWindow = new DownloadWindow(row.name, item.Owner, item.EmbedImage);
                downloadWindow.ShowDialog();
                if (downloadWindow.YesNo)
                {
                    await GameBananaDownload(item, row, game, progress, cancellationToken, downloadingMissing, null, null, 0);
                }
            }
            else
            {
                Console.WriteLine($"[INFO] No updates available for {row.name}");

            }
        }

        private async Task GameBananaDownload(GameBananaItem item, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing, GameBananaItemUpdate[] updates, string onlineVersion, int updateIndex)
        {
            Dictionary<String, GameBananaItemFile> files = item.Files;
            string downloadUrl = null;
            string fileName = null;
            // Work out which are Aemulus comptaible by examining the file tree
            Dictionary<String, GameBananaItemFile> aemulusCompatibleFiles = new Dictionary<string, GameBananaItemFile>();
            foreach (KeyValuePair<string, GameBananaItemFile> file in files)
            {
                if (file.Value.FileMetadata.Values.Count > 0)
                {
                    string fileTree = file.Value.FileMetadata.Values.ElementAt(1).ToString();
                    if (!fileTree.ToLower().Contains(".disable_gb1click") && (fileTree.ToLower().Contains("package.xml") || fileTree.ToLower().Contains("mod.xml") || fileTree == "[]"))
                    {
                        aemulusCompatibleFiles.Add(file.Key, file.Value);
                    }
                }
            }
            if (aemulusCompatibleFiles.Count > 1)
            {
                UpdateFileBox fileBox = new UpdateFileBox(aemulusCompatibleFiles.Values.ToList(), row.name);
                fileBox.Activate();
                fileBox.ShowDialog();
                downloadUrl = fileBox.chosenFileUrl;
                fileName = fileBox.chosenFileName;
            }
            else if (aemulusCompatibleFiles.Count == 1)
            {
                downloadUrl = aemulusCompatibleFiles.ElementAt(0).Value.DownloadUrl;
                fileName = aemulusCompatibleFiles.ElementAt(0).Value.FileName;
            }
            else if (!downloadingMissing)
            {
                Console.WriteLine($"[INFO] An update is available for {row.name} ({onlineVersion}) but there are no downloads directly from GameBanana.");
                // Convert the url
                Uri uri = CreateUri(row.link);
                string itemType = uri.Segments[1];
                itemType = char.ToUpper(itemType[0]) + itemType.Substring(1, itemType.Length - 3);
                string itemId = uri.Segments[2];
                // Parse the response
                string responseString = await client.GetStringAsync($"https://gamebanana.com/apiv4/{itemType}/{itemId}");
                var response = JsonConvert.DeserializeObject<GameBananaAPIV4>(responseString);
                new AltLinkWindow(response.AlternateFileSources, row.name, game, true).ShowDialog();
                return;
            }
            if (downloadUrl != null && fileName != null)
            {
                await DownloadFile(downloadUrl, fileName, game, row, onlineVersion, progress, cancellationToken, downloadingMissing);
            }
            else
            {
                Console.WriteLine($"[INFO] Cancelled update for {row.name}");
            }
        }

        private async Task GitHubUpdate(Release release, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing)
        {
            if (downloadingMissing)
            {
                // Ask if the user wants to download the mod
                DownloadWindow downloadWindow = new DownloadWindow(row.name, row.author);
                downloadWindow.ShowDialog();
                if (downloadWindow.YesNo)
                {
                    await GithubDownload(release, row, game, progress, cancellationToken, downloadingMissing);
                }
            }
            else
            {
                Match onlineVersionMatch = Regex.Match(release.TagName, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                string onlineVersion = null;
                if (onlineVersionMatch.Success)
                {
                    onlineVersion = onlineVersionMatch.Groups["version"].Value;
                }
                Match localVersionMatch = Regex.Match(row.version, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                string localVersion = null;
                if (localVersionMatch.Success)
                {
                    localVersion = localVersionMatch.Groups["version"].Value;
                }
                if (row.skippedVersion != null)
                {
                    if (row.skippedVersion == "all" || !UpdateAvailable(onlineVersion, row.skippedVersion))
                    {
                        Console.WriteLine($"[INFO] No updates available for {row.name}");
                        return;
                    }
                }
                if (UpdateAvailable(onlineVersion, localVersion))
                {
                    Console.WriteLine($"[INFO] An update is available for {row.name} ({release.TagName})");
                    NotificationBox notification = new NotificationBox($"{row.name} has an update ({release.TagName}):\n{release.Body}\n\nWould you like to update?", false);
                    notification.ShowDialog();
                    notification.Activate();
                    if (!notification.YesNo)
                        return;
                    await GithubDownload(release, row, game, progress, cancellationToken, downloadingMissing);
                }
                else
                {
                    Console.WriteLine($"[INFO] No updates available for {row.name}");
                }
            }

        }

        private async Task GithubDownload(Release release, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing)
        {
            string downloadUrl, fileName;
            if (release.Assets.Count > 1)
            {
                UpdateFileBox fileBox = new UpdateFileBox(release.Assets, row.name);
                fileBox.Activate();
                fileBox.ShowDialog();
                downloadUrl = fileBox.chosenFileUrl;
                fileName = fileBox.chosenFileName;
            }
            else if (release.Assets.Count == 1)
            {
                downloadUrl = release.Assets.First().BrowserDownloadUrl;
                fileName = release.Assets.First().Name;
            }
            else
            {
                Console.WriteLine($"[INFO] An update is available for {row.name} ({release.TagName}) but no downloadable files are available.");
                NotificationBox notification = new NotificationBox($"{row.name} has an update ({release.TagName}) but no downloadable files.\nWould you like to go to the page to manually download the update?", false);
                notification.ShowDialog();
                notification.Activate();
                if (notification.YesNo)
                {
                    Process.Start(row.link);
                }
                return;
            }
            if (downloadUrl != null && fileName != null)
            {
                await DownloadFile(downloadUrl, fileName, game, row, release.TagName, progress, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token), downloadingMissing);
            }
            else
            {
                Console.WriteLine($"[INFO] Cancelled update for {row.name}");
            }

        }

        private async Task DownloadFile(string uri, string fileName, string game, DisplayedMetadata row, string version, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing)
        {
            try
            {
                // Create the downloads folder if necessary
                if (!Directory.Exists($@"{assemblyLocation}\Downloads"))
                {
                    Directory.CreateDirectory($@"{assemblyLocation}\Downloads");
                }
                // Download the file if it doesn't already exist
                if (!FileIOWrapper.Exists($@"{assemblyLocation}\Downloads\{fileName}"))
                {
                    progressBox = new UpdateProgressBox(cancellationToken);
                    progressBox.progressBar.Value = 0;
                    progressBox.progressText.Text = $"Downloading {fileName}";
                    progressBox.finished = false;
                    progressBox.Title = $"{row.name} {(downloadingMissing ? "Download" : "Update")} Progress";
                    progressBox.Show();
                    progressBox.Activate();
                    Console.WriteLine($"[INFO] Downloading {fileName}");
                    // Write and download the file
                    using (var fs = new FileStream(
                        $@"{assemblyLocation}\Downloads\{fileName}", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                    }
                    Console.WriteLine($"[INFO] Finished downloading {fileName}");
                    progressBox.Close();
                }
                else
                {
                    Console.WriteLine($"[INFO] {fileName} already exists in downloads, using this instead");
                }
                ExtractFile(fileName, game);
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                FileIOWrapper.Delete(@$"Downloads\{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error whilst downloading {fileName}: {e.Message}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
            }
        }

        private async Task DownloadAemulus(string uri, string fileName, string version, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            try
            {
                // Create the downloads folder if necessary
                if (!Directory.Exists(@$"{assemblyLocation}\Downloads"))
                {
                    Directory.CreateDirectory(@$"{assemblyLocation}\Downloads");
                }
                // Create the downloads folder if necessary
                if (!Directory.Exists(@$"{assemblyLocation}\Downloads\AemulusUpdate"))
                {
                    Directory.CreateDirectory(@$"{assemblyLocation}\Downloads\AemulusUpdate");
                }
                progressBox = new UpdateProgressBox(cancellationToken);
                progressBox.progressBar.Value = 0;
                progressBox.progressText.Text = $"Downloading {fileName}";
                progressBox.Title = "Aemulus Update Progress";
                progressBox.finished = false;
                progressBox.Show();
                progressBox.Activate();
                Console.WriteLine($"[INFO] Downloading {fileName}");
                // Write and download the file
                using (var fs = new FileStream(
                    $@"{assemblyLocation}\Downloads\AemulusUpdate\{fileName}", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                Console.WriteLine($"[INFO] Finished downloading {fileName}");
                // Rename the file
                if (!FileIOWrapper.Exists($@"{assemblyLocation}\Downloads\AemulusUpdate\{version}.7z"))
                {
                    FileIOWrapper.Move($@"{assemblyLocation}\Downloads\AemulusUpdate\{fileName}", $@"{assemblyLocation}\Downloads\AemulusUpdate\{version}.7z");
                }
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                FileIOWrapper.Delete(@$"{assemblyLocation}\Downloads\AemulusUpdate\{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Error whilst downloading {fileName}: {e.Message}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
            }
        }

        private void ExtractFile(string file, string game)
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
                startInfo.Arguments = $@"x -y ""{assemblyLocation}\Downloads\{file}"" -o""{assemblyLocation}\temp""";
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
                var packgeSetup = Directory.GetFiles($@"{assemblyLocation}\temp", "*Packages.xml", SearchOption.AllDirectories);
                if (packgeSetup.Length > 0)
                {
                    Directory.CreateDirectory($@"{assemblyLocation}\Config\temp");
                    foreach (var xml in packgeSetup)
                    {
                        File.Copy(xml, $@"{assemblyLocation}\Config\temp\{Path.GetFileName(xml)}", true);
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

        private bool UpdateAvailable(string onlineVersion, string localVersion)
        {
            if (onlineVersion is null || localVersion is null)
            {
                return false;
            }
            string[] onlineVersionParts = onlineVersion.Split('.');
            string[] localVersionParts = localVersion.Split('.');
            // Pad the version if one has more parts than another (e.g. 1.2.1 and 1.2)
            if (onlineVersionParts.Length > localVersionParts.Length)
            {
                for (int i = localVersionParts.Length; i < onlineVersionParts.Length; i++)
                {
                    localVersionParts = localVersionParts.Append("0").ToArray();
                }
            }
            else if (localVersionParts.Length > onlineVersionParts.Length)
            {
                for (int i = onlineVersionParts.Length; i < localVersionParts.Length; i++)
                {
                    onlineVersionParts = onlineVersionParts.Append("0").ToArray();
                }
            }
            // Decide whether the online version is new than local
            for (int i = 0; i < onlineVersionParts.Length; i++)
            {
                if (!int.TryParse(onlineVersionParts[i], out _))
                {
                    Console.WriteLine($"[ERROR] Couldn't parse {onlineVersion}");
                    return false;
                }
                if (!int.TryParse(localVersionParts[i], out _))
                {
                    Console.WriteLine($"[ERROR] Couldn't parse {localVersion}");
                    return false;
                }
                if (int.Parse(onlineVersionParts[i]) > int.Parse(localVersionParts[i]))
                {
                    return true;
                }
                else if (int.Parse(onlineVersionParts[i]) != int.Parse(localVersionParts[i]))
                {
                    return false;
                }
            }
            return false;
        }

        private Uri CreateUri(string url)
        {
            Uri uri;
            if ((Uri.TryCreate(url, UriKind.Absolute, out uri) || Uri.TryCreate("http://" + url, UriKind.Absolute, out uri)) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri;
            }
            return null;
        }
    }
}
