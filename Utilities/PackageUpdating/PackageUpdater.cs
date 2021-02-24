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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AemulusModManager
{
    public class PackageUpdater
    {
        private readonly HttpClient client;
        private GitHubClient gitHubClient;
        private UpdateProgressBox progressBox;
        private MainWindow main;

        public PackageUpdater(MainWindow mainWindow)
        {
            client = new HttpClient();
            gitHubClient = new GitHubClient(new ProductHeaderValue("Aemulus"));
            main = mainWindow;
        }

        public async Task CheckForUpdate(DisplayedMetadata[] rows, string game, CancellationTokenSource cancellationToken)
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
                        gameBananaRequestUrl += $"itemtype[]={itemType}&itemid[]={itemId}&fields[]=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates(),Files().aFiles()&";
                    }
                    gameBananaRequestUrl += "return_keys=1";
                    // Parse the response
                    string responseString = await client.GetStringAsync(gameBananaRequestUrl);
                    GameBananaItem[] response = JsonConvert.DeserializeObject<GameBananaItem[]>(responseString);
                    if (response == null)
                    {
                        Console.WriteLine("[ERROR] Error whilst checking for package updates: No response from GameBanana API");
                    }
                    else
                    {
                        for (int i = 0; i < gameBananaRows.Length; i++)
                        {
                            await GameBananaUpdate(response[i], gameBananaRows[i], game, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken);
                        }
                    }
                }
                // Check GitHub Items
                DisplayedMetadata[] gitHubRows = rows.Where(row => UrlConverter.Convert(row.link) == "GitHub").ToArray();
                if (gitHubRows.Length > 0)
                {
                    foreach (DisplayedMetadata row in gitHubRows)
                    {
                        Uri uri = CreateUri(row.link);
                        Release latestRelease = await gitHubClient.Repository.Release.GetLatest(uri.Segments[1].Replace("/", ""), uri.Segments[2].Replace("/", ""));
                        await GitHubUpdate(latestRelease, row, game, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken);
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

                string requestUrl = $"https://api.gamebanana.com/Core/Item/Data?itemtype=Tool&itemid=6878&fields=Updates().bSubmissionHasUpdates(),Updates().aGetLatestUpdates(),Files().aFiles()&return_keys=1";
                GameBananaItem response = JsonConvert.DeserializeObject<GameBananaItem>(await client.GetStringAsync(requestUrl));
                if (response == null)
                {
                    Console.WriteLine("[ERROR] Error whilst checking for Aemulus update: No response from GameBanana API");
                    return false;
                }
                if (response.HasUpdates)
                {
                    GameBananaItemUpdate[] updates = response.Updates;
                    string updateTitle = updates[0].Title;
                    int updateIndex = 0;
                    Match onlineVersionMatch = Regex.Match(updateTitle, @"(?<version>([1-9]+\.?)+)[^a-zA-Z]");
                    string onlineVersion = null;
                    if (onlineVersionMatch.Success)
                    {
                        onlineVersion = onlineVersionMatch.Value;
                    }
                    // GB Api only returns two latest updates, so if the first doesn't have a version try the second
                    else if (updates.Length > 1)
                    {
                        updateTitle = updates[1].Title;
                        onlineVersionMatch = Regex.Match(updateTitle, @"(?<version>([1-9]+\.?)+)[^a-zA-Z]");
                        updateIndex = 1;
                        if (onlineVersionMatch.Success)
                        {
                            onlineVersion = onlineVersionMatch.Value;
                        }
                    }
                    if (UpdateAvailable(onlineVersion, aemulusVersion))
                    {
                        Console.WriteLine($"[INFO] An update is available for Aemulus ({onlineVersion})");
                        ChangelogBox notification = new ChangelogBox(updates[updateIndex], "Aemulus", $"A new version of Aemulus is available (v{onlineVersion}), would you like to update now?", false);
                        notification.ShowDialog();
                        notification.Activate();
                        if (notification.YesNo)
                        {
                            Console.WriteLine($"[INFO] Updating Aemulus to v{onlineVersion}");
                            Dictionary<String, GameBananaItemFile> files = response.Files;
                            string downloadUrl = files.ElementAt(updateIndex).Value.DownloadUrl;
                            string fileName = files.ElementAt(updateIndex).Value.FileName;
                            // Download the update
                            await DownloadAemulus(downloadUrl, fileName, onlineVersion, new Progress<DownloadProgress>(ReportUpdateProgress), cancellationToken);
                            // Notify that the update is about to happen
                            NotificationBox finishedNotification = new NotificationBox($"Finished downloading {fileName}!\nAemulus will now restart.", true);
                            finishedNotification.ShowDialog();
                            finishedNotification.Activate();
                            // Update Aemulus
                            UpdateManager updateManager = new UpdateManager(new LocalPackageResolver(@"Downloads\AemulusUpdate"), new Zip7Extractor());
                            if (!Version.TryParse(onlineVersion, out Version version))
                            {
                                Console.WriteLine("[ERROR] Error parsing Aemulus version, cancelling update");
                                // TODO Delete the downloaded stuff
                                return false;
                            }
                            // Updates and restarts Aemulus
                            await updateManager.PrepareUpdateAsync(version);
                            updateManager.LaunchUpdater(version);
                            return true;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[INFO] No updates available for Aemulus");
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
            progressBox.progressText.Text = $"Downloading {progress.FileName}\n{Math.Round(progress.Percentage * 100, 2)}% ({StringConverters.FormatSize(progress.DownloadedBytes)} of {StringConverters.FormatSize(progress.TotalBytes)})";
        }

        private async Task GameBananaUpdate(GameBananaItem item, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            if (item.HasUpdates)
            {
                GameBananaItemUpdate[] updates = item.Updates;
                string updateTitle = updates[0].Title;
                int updateIndex = 0;
                Match onlineVersionMatch = Regex.Match(updateTitle, @"(?<version>([1-9]+\.?)+)[^a-zA-Z]");
                string onlineVersion = null;
                if (onlineVersionMatch.Success)
                {
                    onlineVersion = onlineVersionMatch.Value;
                }
                // GB Api only returns two latest updates, so if the first doesn't have a version try the second
                else if (updates.Length > 1)
                {
                    updateTitle = updates[1].Title;
                    onlineVersionMatch = Regex.Match(updateTitle, @"(?<version>([1-9]+\.?)+)[^a-zA-Z]");
                    updateIndex = 1;
                    if (onlineVersionMatch.Success)
                    {
                        onlineVersion = onlineVersionMatch.Value;
                    }
                }
                if (UpdateAvailable(onlineVersion, row.version))
                {
                    Console.WriteLine($"[INFO] An update is available for {row.name} ({onlineVersion})");
                    // Display the changelog and confirm they want to update
                    if (main.updateConfirm)
                    {
                        ChangelogBox changelogBox = new ChangelogBox(updates[updateIndex], row.name, $"Would you like to update {row.name} to version {onlineVersion}?", false);
                        changelogBox.Activate();
                        changelogBox.ShowDialog();
                        if (!changelogBox.YesNo)
                        {
                            Console.WriteLine($"[INFO] Cancelled update for {row.name}");
                            return;
                        }
                    }

                    // Download the update
                    Dictionary<String, GameBananaItemFile> files = item.Files;
                    string downloadUrl, fileName;
                    if (files.Count > 1)
                    {
                        UpdateFileBox fileBox = new UpdateFileBox(files, row.name);
                        fileBox.Activate();
                        fileBox.ShowDialog();
                        downloadUrl = fileBox.chosenFileUrl;
                        fileName = fileBox.chosenFileName;
                    }
                    else if (files.Count == 1)
                    {
                        downloadUrl = files.ElementAt(0).Value.DownloadUrl;
                        fileName = files.ElementAt(0).Value.FileName;
                    }
                    else
                    {
                        Console.WriteLine($"[INFO] An update is available for {row.name} ({onlineVersion}) but no downloadable files are available.");
                        NotificationBox notification = new NotificationBox($"{row.name} has an update ({onlineVersion}) but no downloadable files.\nWould you like to go to the page to manually download the update?", false);
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
                        await DownloadFile(downloadUrl, fileName, game, row.path, row.name, progress, cancellationToken, updates[updateIndex]);
                    }
                    else
                    {
                        Console.WriteLine($"[INFO] Cancelled update for {row.name}");
                    }

                }
                else
                {
                    Console.WriteLine($"[INFO] No updates available for {row.name}");
                }
                // TODO Check if there was no version number
            }
            else
            {
                Console.WriteLine($"[INFO] No updates available for {row.name}");

            }
        }

        private async Task GitHubUpdate(Release release, DisplayedMetadata row, string game, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken)
        {
            if (UpdateAvailable(release.TagName, row.version))
            {
                Console.WriteLine($"[INFO] An update is available for {row.name} ({release.TagName})");
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
                    await DownloadFile(downloadUrl, fileName, game, row.path, row.name, progress, cancellationToken);
                }
                else
                {
                    Console.WriteLine($"[INFO] Cancelled update for {row.name}");
                }
            }
            else
            {
                Console.WriteLine($"[INFO] No updates available for {row.name}");
            }
        }

        private async Task DownloadFile(string uri, string fileName, string game, string oldPath, string packageName, Progress<DownloadProgress> progress, CancellationTokenSource cancellationToken, GameBananaItemUpdate update = null)
        {
            try
            {
                // Create the downloads folder if necessary
                if (!Directory.Exists("Downloads"))
                {
                    Directory.CreateDirectory("Downloads");
                }
                // Download the file if it doesn't already exist
                if (!File.Exists($"Downloads/{fileName}"))
                {
                    progressBox = new UpdateProgressBox(cancellationToken);
                    progressBox.progressBar.Value = 0;
                    progressBox.progressText.Text = $"Downloading {fileName}";
                    progressBox.finished = false;
                    progressBox.Title = $"{packageName} Update Progress";
                    progressBox.Show();
                    progressBox.Activate();
                    Console.WriteLine($"[INFO] Downloading {fileName}");
                    // Write and download the file
                    using (var fs = new FileStream(
                        $"Downloads/{fileName}", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
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
                ExtractFile(fileName, game, oldPath, packageName, update);
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
                if (!Directory.Exists("Downloads"))
                {
                    Directory.CreateDirectory("Downloads");
                }
                // Create the downloads folder if necessary
                if (!Directory.Exists("Downloads/AemulusUpdate"))
                {
                    Directory.CreateDirectory("Downloads/AemulusUpdate");
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
                    $"Downloads/AemulusUpdate/{fileName}", System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadAsync(uri, fs, fileName, progress, cancellationToken.Token);
                }
                Console.WriteLine($"[INFO] Finished downloading {fileName}");
                // Rename the file
                if (!File.Exists($@"Downloads/AemulusUpdate/{version}.7z"))
                {
                    File.Move($@"Downloads/AemulusUpdate/{fileName}", $@"Downloads/AemulusUpdate/{version}.7z");
                }
                progressBox.Close();
            }
            catch (OperationCanceledException)
            {
                // Remove the file is it will be a partially downloaded one and close up
                File.Delete(@$"Downloads\AemulusUpdate\{fileName}");
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

        private void ExtractFile(string fileName, string game, string oldPath, string packageName, GameBananaItemUpdate update = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.FileName = @"Dependencies\7z\7z.exe";
            if (!File.Exists(startInfo.FileName))
            {
                Console.WriteLine($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                return;
            }
            // Extract the file
            startInfo.Arguments = $"x -y \"Downloads\\{fileName}\" -o\"Downloads\\{packageName}\"";
            Console.WriteLine($"[INFO] Extracting {fileName}");
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                if (output.Contains("Everything is Ok"))
                {
                    Console.WriteLine($"[INFO] Done Extracting {fileName}");
                }
                else
                {
                    Console.WriteLine($"[ERROR] There was an error extracting {fileName}:\n{output}");
                    // Remove the download as it is likely corrupted
                    File.Delete(@$"Downloads\{fileName}");
                    if (Directory.Exists($@"Downloads\{packageName}"))
                    {
                        Directory.Delete($@"Downloads\{packageName}", true);
                    }
                    Console.WriteLine(@$"[INFO] Cleaned up {packageName} download files");
                    return;
                }
                process.WaitForExit();
            }
            // Find the root and move the extracted file to the correct package folder
            string[] packageRoots = Array.ConvertAll(Directory.GetFiles(@$"Downloads\{packageName}", "Package.xml", SearchOption.AllDirectories), path => Path.GetDirectoryName(path));
            if (packageRoots.Length == 1)
            {
                // Remove the old package directory
                Directory.Delete($@"Packages\{game}\{oldPath}", true);
                Console.WriteLine($@"[INFO] Deleted old installation (Packages\{game}\{oldPath})");
                Directory.Move(packageRoots[0], $@"Packages\{game}\{oldPath}");
                Console.WriteLine($"[INFO] Successfully updated {packageName}");
                // Display the changelog if it hasn't been displayed already and is wanted
                if (main.updateChangelog && !main.updateConfirm && update != null)
                {
                    ChangelogBox changelogBox = new ChangelogBox(update, packageName, $"Successfully updated {packageName}!", true);
                    changelogBox.Activate();
                    changelogBox.ShowDialog();
                }
            }
            else if (packageRoots.Length > 1)
            {
                // Open a dialog asking which folder to use
                PackageFolderBox folderBox = new PackageFolderBox(packageRoots, packageName);
                folderBox.Activate();
                folderBox.ShowDialog();
                if (folderBox.chosenFolder == null)
                {
                    Console.WriteLine($"[INFO] Cancelled update for {packageName}");
                    return;
                }
                // Remove the old package directory
                Directory.Delete($@"Packages\{game}\{oldPath}", true);
                Console.WriteLine($@"[INFO] Deleted old installation (Packages\{game}\{oldPath})");
                Directory.Move(folderBox.chosenFolder, $@"Packages\{game}\{oldPath}");
                Console.WriteLine($"[INFO] Successfully updated {packageName}");
                // Display the changelog if it hasn't been displayed already and is wanted
                if (main.updateChangelog && !main.updateConfirm && update != null)
                {
                    ChangelogBox changelogBox = new ChangelogBox(update, packageName, $"Successfully updated {packageName}!", true);
                    changelogBox.Activate();
                    changelogBox.ShowDialog();
                }
            }
            else
            {
                Console.WriteLine($"[ERROR] {fileName} does not contain a valid package (no Package.xml is present), ignoring it");
            }
            File.Delete(@$"Downloads\{fileName}");
            if (Directory.Exists($@"Downloads\{packageName}"))
            {
                Directory.Delete($@"Downloads\{packageName}", true);
            }
            Console.WriteLine(@$"[INFO] Cleaned up {packageName} download files");
        }

        private bool UpdateAvailable(string onlineVersion, string localVersion)
        {
            if (onlineVersion is null)
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
                if (int.Parse(onlineVersionParts[i]) > int.Parse(localVersionParts[i]))
                {
                    return true;
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
