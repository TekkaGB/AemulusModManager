using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindowPQ2 : Window
    {
        private MainWindow main;

        public ConfigWindowPQ2(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                ROMTextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                CitraTextbox.Text = main.launcherPath;
            BuildFinishedBox.IsChecked = main.config.pq2Config.buildFinished;
            BuildWarningBox.IsChecked = main.config.pq2Config.buildWarning;
            ChangelogBox.IsChecked = main.config.pq2Config.updateChangelog;
            DeleteBox.IsChecked = main.config.pq2Config.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.pq2Config.updateAll;
            UpdateBox.IsChecked = main.config.pq2Config.updatesEnabled;
            Utilities.ParallelLogger.Log("[INFO] Config launched");
        }
        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Utilities.ParallelLogger.Log($"[INFO] Setting output folder to {directory}");
                main.config.pq2Config.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0xfb, 0x84, 0x6a));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.pq2Config.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.pq2Config.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.pq2Config.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.pq2Config.buildFinished = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.pq2Config.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.pq2Config.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.pq2Config.updateAll = true;
            main.updateConfig();
        }
        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.pq2Config.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.pq2Config.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.pq2Config.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.pq2Config.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.pq2Config.deleteOldVersions = false;
            main.updateConfig();
        }

        private void onClose(object sender, CancelEventArgs e)
        {
            Utilities.ParallelLogger.Log("[INFO] Config closed");
        }

        // Used for selecting
        private string openFolder()
        {
            var openFolder = new CommonOpenFileDialog();
            openFolder.AllowNonFileSystemItems = true;
            openFolder.IsFolderPicker = true;
            openFolder.EnsurePathExists = true;
            openFolder.EnsureValidNames = true;
            openFolder.Multiselect = false;
            openFolder.Title = "Select Output Folder";
            if (openFolder.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return openFolder.FileName;
            }

            return null;
        }

        private void SetupROMShortcut(object sender, RoutedEventArgs e)
        {
            string pq2Rom = selectExe("Select Persona Q2 ROM", "*.3ds;*.app;*.cxi");
            if (pq2Rom != null)
            {
                main.gamePath = pq2Rom;
                main.config.pq2Config.ROMPath = pq2Rom;
                main.updateConfig();
                ROMTextbox.Text = pq2Rom;
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] No ROM selected.");
            }
        }

        private void SetupCitraShortcut(object sender, RoutedEventArgs e)
        {
            string citraExe = selectExe("Select citra-qt.exe", "*.exe");
            if (Path.GetFileName(citraExe).ToLowerInvariant() == "citra-qt.exe")
            {
                main.launcherPath = citraExe;
                main.config.pq2Config.launcherPath = citraExe;
                main.updateConfig();
                CitraTextbox.Text = citraExe;
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] Invalid exe.");
            }
        }

        private string selectExe(string title, string extension)
        {
            string type = "Application";
            if (extension == "*.3ds;*.app;*.cxi")
                type = "ROM";
            if (extension == "*.cpk")
                type = "File Container";
            var openExe = new CommonOpenFileDialog();
            openExe.Filters.Add(new CommonFileDialogFilter(type, extension));
            openExe.EnsurePathExists = true;
            openExe.EnsureValidNames = true;
            openExe.Title = title;
            if (openExe.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return openExe.FileName;
            }
            return null;
        }

        private async void UnpackPacsClick(object sender, RoutedEventArgs e)
        {
            string selectedPath = selectExe("Select PQ2 data.cpk to unpack", ".cpk");
            if (selectedPath == null)
            {
                Utilities.ParallelLogger.Log("[ERROR] Incorrect file chosen for unpacking.");
                return;
            }
            main.ModGrid.IsHitTestVisible = false;
            UnpackButton.IsHitTestVisible = false;
            foreach (var button in main.buttons)
            {
                button.IsHitTestVisible = false;
                button.Foreground = new SolidColorBrush(Colors.Gray);
            }
            main.GameBox.IsHitTestVisible = false;
            await main.pacUnpack(selectedPath);
            UnpackButton.IsHitTestVisible = true;
        }

        // Stops the user from changing the displayed "Notifications" text to the names of one of the combo boxes
        private void NotifBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            NotifBox.SelectedIndex = 0;
        }
    }
}
