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
using System.Net;
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
        private string assemblyLocation;

        public PackageUpdater()
        {
            client = new HttpClient();
            gitHubClient = new GitHubClient(new ProductHeaderValue("Aemulus"));
            assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }
        private string ConvertUrl(string oldUrl)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(oldUrl);
            webRequest.AllowAutoRedirect = false;  // IMPORTANT

            webRequest.Timeout = 10000;           // timeout 10s
            webRequest.Method = "HEAD";
            // Get the response ...
            HttpWebResponse webResponse;
            using (webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                // Now look to see if it's a redirect
                if ((int)webResponse.StatusCode >= 300 && (int)webResponse.StatusCode <= 399)
                {
                    string uriString = webResponse.Headers["Location"];
                    webResponse.Close(); // don't forget to close it - or bad things happen!
                    return uriString;
                }
                else
                    return oldUrl;

            }
        }
        public async Task<bool> CheckForUpdate(DisplayedMetadata[] rows, string game, CancellationTokenSource cancellationToken, bool downloadingMissing = false)
        {
            var updated = false;
            try
            {
                // Check GameBanana Items
                DisplayedMetadata[] gameBananaRows = rows.Where(row => UrlConverter.Convert(row.link) == "GameBanana").ToArray();
                if (gameBananaRows.Length > 0)
                {
                    var requestUrls = new Dictionary<string, List<string>>();
                    var urlCounts = new Dictionary<string, int>();
                    var modList = new Dictionary<string, List<DisplayedMetadata>>();
                    foreach (var row in gameBananaRows)
                    {
                        // Convert the url
                        Uri uri = CreateUri(row.link);
                        string MOD_TYPE = uri.Segments[1];
                        MOD_TYPE = char.ToUpper(MOD_TYPE[0]) + MOD_TYPE.Substring(1, MOD_TYPE.Length - 3);
                        string MOD_ID = uri.Segments[2];
                        switch (MOD_TYPE)
                        {
                            case "Gamefile":
                            case "Skin":
                            case "Gui":
                            case "Texture":
                            case "Effect":
                                var newUrl = ConvertUrl(row.link);
                                uri = CreateUri(newUrl);
                                MOD_TYPE = uri.Segments[1];
                                MOD_TYPE = char.ToUpper(MOD_TYPE[0]) + MOD_TYPE.Substring(1, MOD_TYPE.Length - 3);
                                MOD_ID = uri.Segments[2];
                                break;
                        }
                        if (!urlCounts.ContainsKey(MOD_TYPE))
                            urlCounts.Add(MOD_TYPE, 0);
                        int index = urlCounts[MOD_TYPE];
                        if (!modList.ContainsKey(MOD_TYPE))
                            modList.Add(MOD_TYPE, new List<DisplayedMetadata>());
                        modList[MOD_TYPE].Add(row);
                        if (!requestUrls.ContainsKey(MOD_TYPE))
                            requestUrls.Add(MOD_TYPE, new string[] { $"https://gamebanana.com/apiv6/{MOD_TYPE}/Multi?_csvProperties=_aModManagerIntegrations,_sName,_bHasUpdates,_aLatestUpdates,_aFiles,_aPreviewMedia,_aAlternateFileSources&_csvRowIds=" }.ToList());
                        else if (requestUrls[MOD_TYPE].Count == index)
                            requestUrls[MOD_TYPE].Add($"https://gamebanana.com/apiv6/{MOD_TYPE}/Multi?_csvProperties=_aModManagerIntegrations,_sName,_bHasUpdates,_aLatestUpdates,_aFiles,_aPreviewMedia,_aAlternateFileSources&_csvRowIds=");
                        requestUrls[MOD_TYPE][index] += $"{MOD_ID},";
                        if (requestUrls[MOD_TYPE][index].Length > 1990)
                            urlCounts[MOD_TYPE]++;
                    }
                    // Remove extra comma
                    foreach (var key in requestUrls.Keys)
                    {
                        var counter = 0;
                        foreach (var requestUrl in requestUrls[key].ToList())
                        {
                            if (requestUrl.EndsWith(","))
                                requestUrls[key][counter] = requestUrl.Substring(0, requestUrl.Length - 1);
                            counter++;
                        }

                    }
                    List<GameBananaAPIV4> response = new List<GameBananaAPIV4>();
                    using (var client = new HttpClient())
                    {
                        foreach (var type in requestUrls)
                        {
                            foreach (var requestUrl in type.Value)
                            {
                                var responseString = await client.GetStringAsync(requestUrl);
                                try
                                {
                                    var partialResponse = JsonConvert.DeserializeObject<List<GameBananaAPIV4>>(responseString.Replace("\"_aModManagerIntegrations\": []", "\"_aModManagerIntegrations\": {}"));
                                    response = response.Concat(partialResponse).ToList();
                                }
                                catch (Exception e)
                                {
                                    Utilities.ParallelLogger.Log($"[ERROR] {e.Message}");
                                }
                            }
                        }
                    }
                    if (response == null)
                    {
                        Utilities.ParallelLogger.Log("[ERROR] Error whilst checking for package updates: No response from GameBanana API");
                    }
                    else
                    {
                        var convertedModList = new List<DisplayedMetadata>();
                        var count = 0;
                        foreach (var type in modList)
                        {
                            foreach (var mod in type.Value)
                            {
                                convertedModList.Add(mod);
                                count++;
                            }
                        }
                        for (int i = 0; i < convertedModList.Count; i++)
                        {
                            try
                            {
                                if (await GameBananaUpdate(response[i], convertedModList[i], game, new Progress<DownloadProgress>(ReportUpdateProgress), CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Token), downloadingMissing))
                                    updated = true;
                            }
                            catch (Exception e)
                            {
                                Utilities.ParallelLogger.Log($"[ERROR] Error whilst updating/checking for updates for {convertedModList[i].name}: {e.Message}");
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
                            if (await GitHubUpdate(latestRelease, row, game, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken, downloadingMissing))
                                updated = true;
                        }
                        catch (Exception e)
                        {
                            Utilities.ParallelLogger.Log($"[ERROR] Error whilst updating/checking for updates for {row.name}: {e.Message}");
                        }
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Connection error whilst checking for updates: {e.Message}");
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error whilst checking for updates: {e.Message}");
            }
            return updated;
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
                    Utilities.ParallelLogger.Log($"[INFO] An update is available for Aemulus ({onlineVersion})");
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
                            Utilities.ParallelLogger.Log("[ERROR] Error parsing Aemulus version, cancelling update");
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
                    Utilities.ParallelLogger.Log($"[INFO] No updates available for Aemulus");
                }
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error whilst checking for updates: {e.Message}");
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

        private async Task<bool> GameBananaUpdate(GameBananaAPIV4 item, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing)
        {
            var updated = false;
            if (!downloadingMissing && item.HasUpdates != null && (bool)item.HasUpdates)
            {
                GameBananaItemUpdate[] updates = item.Updates;
                int updateIndex = 0;
                Match onlineVersionMatch;
                string onlineVersion = null;
                // Check Version field first, then Title field
                if (updates[0].Version != null)
                {
                    onlineVersionMatch = Regex.Match(updates[0].Version, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                    if (onlineVersionMatch.Success)
                        onlineVersion = onlineVersionMatch.Groups["version"].Value;
                    else
                    {
                        onlineVersionMatch = Regex.Match(updates[0].Title, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                        if (onlineVersionMatch.Success)
                            onlineVersion = onlineVersionMatch.Groups["version"].Value;
                        // GB Api only returns two latest updates, so if the first doesn't have a version try the second
                        else if (updates.Length > 1)
                        {
                            updateIndex = 1;
                            if (updates[1].Version != null)
                            {
                                onlineVersionMatch = Regex.Match(updates[1].Version, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                                if (onlineVersionMatch.Success)
                                    onlineVersion = onlineVersionMatch.Groups["version"].Value;
                                else
                                {
                                    onlineVersionMatch = Regex.Match(updates[1].Title, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                                    if (onlineVersionMatch.Success)
                                        onlineVersion = onlineVersionMatch.Groups["version"].Value;
                                }
                            }
                            else
                            {
                                onlineVersionMatch = Regex.Match(updates[1].Title, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                                if (onlineVersionMatch.Success)
                                    onlineVersion = onlineVersionMatch.Groups["version"].Value;
                            }
                        }
                    }
                }
                else
                {
                    onlineVersionMatch = Regex.Match(updates[0].Title, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                    if (onlineVersionMatch.Success)
                        onlineVersion = onlineVersionMatch.Groups["version"].Value;
                    // GB Api only returns two latest updates, so if the first doesn't have a version try the second
                    else if (updates.Length > 1)
                    {
                        updateIndex = 1;
                        if (updates[1].Version != null)
                        {
                            onlineVersionMatch = Regex.Match(updates[1].Version, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                            if (onlineVersionMatch.Success)
                                onlineVersion = onlineVersionMatch.Groups["version"].Value;
                            else
                            {
                                onlineVersionMatch = Regex.Match(updates[1].Title, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                                if (onlineVersionMatch.Success)
                                    onlineVersion = onlineVersionMatch.Groups["version"].Value;
                            }
                        }
                        else
                        {
                            onlineVersionMatch = Regex.Match(updates[1].Title, @"(?<version>([0-9]+\.?)+)[^a-zA-Z]*");
                            if (onlineVersionMatch.Success)
                                onlineVersion = onlineVersionMatch.Groups["version"].Value;
                        }
                    }
                }
                // Get local version
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
                        Utilities.ParallelLogger.Log($"[INFO] No updates available for {row.name}");
                        return false;
                    }
                }
                if (UpdateAvailable(onlineVersion, localVersion))
                {
                    Utilities.ParallelLogger.Log($"[INFO] An update is available for {row.name} ({onlineVersion})");
                    // Display the changelog and confirm they want to update
                    ChangelogBox changelogBox = new ChangelogBox(updates[updateIndex], row.name, $"Would you like to update {row.name} to version {onlineVersion}?", row, onlineVersion, $@"{assemblyLocation}\Packages\{game}\{row.path}\Package.xml", false);
                    changelogBox.Activate();
                    changelogBox.ShowDialog();
                    if (!changelogBox.YesNo)
                    {
                        Utilities.ParallelLogger.Log($"[INFO] Cancelled update for {row.name}");
                        return false;
                    }

                    // Download the update
                    await GameBananaDownload(item, row, game, progress, cancellationToken, downloadingMissing, updates, onlineVersion, updateIndex);
                    updated = true;
                }
                else
                {
                    Utilities.ParallelLogger.Log($"[INFO] No updates available for {row.name}");
                }
            }
            else if (downloadingMissing)
            {
                // Ask if the user wants to download the mod
                DownloadWindow downloadWindow = new DownloadWindow(row.name, item.Owner.Name, item.Image);
                downloadWindow.ShowDialog();
                if (downloadWindow.YesNo)
                {
                    await GameBananaDownload(item, row, game, progress, cancellationToken, downloadingMissing, null, null, 0);
                }
            }
            else
            {
                Utilities.ParallelLogger.Log($"[INFO] No updates available for {row.name}");

            }
            return updated;
        }

        private async Task GameBananaDownload(GameBananaAPIV4 item, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing, GameBananaItemUpdate[] updates, string onlineVersion, int updateIndex)
        {
            string downloadUrl = null;
            string fileName = null;
            // Work out which are Aemulus comptaible by examining the file tree
            List<GameBananaItemFile> aemulusCompatibleFiles = item.Files.Where(x => item.ModManagerIntegrations.ContainsKey(x.ID)).ToList();
            if (aemulusCompatibleFiles.Count > 1)
            {
                UpdateFileBox fileBox = new UpdateFileBox(aemulusCompatibleFiles, row.name);
                fileBox.Activate();
                fileBox.ShowDialog();
                downloadUrl = fileBox.chosenFileUrl;
                fileName = fileBox.chosenFileName;
            }
            else if (aemulusCompatibleFiles.Count == 1)
            {
                downloadUrl = aemulusCompatibleFiles.ElementAt(0).DownloadUrl;
                fileName = aemulusCompatibleFiles.ElementAt(0).FileName;
            }
            else if (!downloadingMissing)
            {
                Utilities.ParallelLogger.Log($"[INFO] An update is available for {row.name} ({onlineVersion}) but there are no downloads directly from GameBanana.");
                new AltLinkWindow(item.AlternateFileSources, row.name, game, true).ShowDialog();
                return;
            }
            if (downloadUrl != null && fileName != null)
            {
                await DownloadFile(downloadUrl, fileName, game, row, onlineVersion, progress, cancellationToken, downloadingMissing);
            }
            else
            {
                Utilities.ParallelLogger.Log($"[INFO] Cancelled update for {row.name}");
            }
        }

        private async Task<bool> GitHubUpdate(Release release, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, bool downloadingMissing)
        {
            var updated = false;
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
                        Utilities.ParallelLogger.Log($"[INFO] No updates available for {row.name}");
                        return false;
                    }
                }
                if (UpdateAvailable(onlineVersion, localVersion))
                {
                    Utilities.ParallelLogger.Log($"[INFO] An update is available for {row.name} ({release.TagName})");
                    NotificationBox notification = new NotificationBox($"{row.name} has an update ({release.TagName}):\n{release.Body}\n\nWould you like to update?", false);
                    notification.ShowDialog();
                    notification.Activate();
                    if (!notification.YesNo)
                        return false;
                    await GithubDownload(release, row, game, progress, cancellationToken, downloadingMissing);
                    updated = true;
                }
                else
                {
                    Utilities.ParallelLogger.Log($"[INFO] No updates available for {row.name}");
                }
            }
            return updated;
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
                Utilities.ParallelLogger.Log($"[INFO] An update is available for {row.name} ({release.TagName}) but no downloadable files are available.");
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
                Utilities.ParallelLogger.Log($"[INFO] Cancelled update for {row.name}");
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
                if (!File.Exists($@"{assemblyLocation}\Downloads\{fileName}"))
                {
                    progressBox = new UpdateProgressBox(cancellationToken);
                    progressBox.progressBar.Value = 0;
                    progressBox.progressText.Text = $"Downloading {fileName}";
                    progressBox.finished = false;
                    progressBox.Title = $"{row.name} {(downloadingMissing ? "Download" : "Update")} Progress";
                    progressBox.Show();
                    progressBox.Activate();
                    Utilities.ParallelLogger.Log($"[INFO] Downloading {fileName}");
                    // Write and download the file
                    using (var fs = new FileStream(
                        $@"{assemblyLocation}\Downloads\{fileName}", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                    }
                    Utilities.ParallelLogger.Log($"[INFO] Finished downloading {fileName}");
                    progressBox.Close();
                }
                else
                {
                    Utilities.ParallelLogger.Log($"[INFO] {fileName} already exists in downloads, using this instead");
                }
                ExtractFile(fileName, game);
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete(@$"Downloads\{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error whilst downloading {fileName}: {e.Message}");
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
                Utilities.ParallelLogger.Log($"[INFO] Downloading {fileName}");
                // Write and download the file
                using (var fs = new FileStream(
                    $@"{assemblyLocation}\Downloads\AemulusUpdate\{fileName}", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                Utilities.ParallelLogger.Log($"[INFO] Finished downloading {fileName}");
                // Rename the file
                if (!File.Exists($@"{assemblyLocation}\Downloads\AemulusUpdate\{version}.7z"))
                {
                    File.Move($@"{assemblyLocation}\Downloads\AemulusUpdate\{fileName}", $@"{assemblyLocation}\Downloads\AemulusUpdate\{version}.7z");
                }
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete(@$"{assemblyLocation}\Downloads\AemulusUpdate\{fileName}");
                if (progressBox != null)
                {
                    progressBox.finished = true;
                    progressBox.Close();
                }
                return;
            }
            catch (Exception e)
            {
                Utilities.ParallelLogger.Log($"[ERROR] Error whilst downloading {fileName}: {e.Message}");
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
                Directory.CreateDirectory($@"{assemblyLocation}\temp\{Path.GetFileNameWithoutExtension(file)}");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
                if (!File.Exists(startInfo.FileName))
                {
                    MessageBox.Show($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.Arguments = $@"x -y ""{assemblyLocation}\Downloads\{file}"" -o""{assemblyLocation}\temp\{Path.GetFileNameWithoutExtension(file)}""";
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
                var packageSetup = Directory.GetFiles($@"{assemblyLocation}\temp", "*.xml", SearchOption.AllDirectories)
                        .Where(xml => !Path.GetFileName(xml).Equals("Package.xml", StringComparison.InvariantCultureIgnoreCase) && !Path.GetFileName(xml).Equals("Mod.xml", StringComparison.InvariantCultureIgnoreCase)).ToList();
                if (packageSetup.Count > 0)
                {
                    Directory.CreateDirectory($@"{assemblyLocation}\Config\temp");
                    foreach (var xml in packageSetup)
                    {
                        File.Copy(xml, $@"{assemblyLocation}\Config\temp\{Path.GetFileName(xml)}", true);
                    }
                }
                File.Delete(@$"{assemblyLocation}\Downloads\{file}");
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
                    if (File.Exists(targetFile)) 
                        File.Delete(targetFile);
                    File.Move(file, targetFile);
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
                    Utilities.ParallelLogger.Log($"[ERROR] Couldn't parse {onlineVersion}");
                    return false;
                }
                if (!int.TryParse(localVersionParts[i], out _))
                {
                    Utilities.ParallelLogger.Log($"[ERROR] Couldn't parse {localVersion}");
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
