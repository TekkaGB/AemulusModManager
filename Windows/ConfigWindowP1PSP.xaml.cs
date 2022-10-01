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
    public partial class ConfigWindowP1PSP : Window
    {
        private MainWindow main;

        public ConfigWindowP1PSP(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                ISOTextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                PPSSPPTextbox.Text = main.launcherPath;
            if (main.config.p1pspConfig.texturesPath != null)
                TexturesTextbox.Text = main.config.p1pspConfig.texturesPath;
            if (main.config.p1pspConfig.cheatsPath != null)
                CheatsTextbox.Text = main.config.p1pspConfig.cheatsPath;
            BuildFinishedBox.IsChecked = main.config.p1pspConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p1pspConfig.buildWarning;
            ChangelogBox.IsChecked = main.config.p1pspConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p1pspConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p1pspConfig.updateAll;
            UpdateBox.IsChecked = main.config.p1pspConfig.updatesEnabled;
            Console.WriteLine("[INFO] Config launched");
        }
        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting output folder to {directory}");
                main.config.p1pspConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0xb6, 0x83, 0xfc));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void textureDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting textures folder to {directory}");
                main.config.p1pspConfig.texturesPath = directory;
                main.updateConfig();
                TexturesTextbox.Text = directory;
            }
        }
        private void cheatDirectoryClick(object sender, RoutedEventArgs e)
        {
            var file = selectExe("Select the P1PSP cheats ini (ULUS10432.ini)", "*.ini");
            if (file != null)
            {
                Console.WriteLine($"[INFO] Setting cheats ini to {file}");
                main.config.p1pspConfig.cheatsPath = file;
                main.updateConfig();
                CheatsTextbox.Text = file;
            }
            else
            {
                Console.WriteLine("[ERROR] No ini selected");
            }
        }
        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.p1pspConfig.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.p1pspConfig.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.p1pspConfig.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.p1pspConfig.buildFinished = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.p1pspConfig.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.p1pspConfig.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.p1pspConfig.updateAll = true;
            main.updateConfig();
        }
        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.p1pspConfig.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.p1pspConfig.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.p1pspConfig.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.p1pspConfig.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.p1pspConfig.deleteOldVersions = false;
            main.updateConfig();
        }

        private void onClose(object sender, CancelEventArgs e)
        {
            Console.WriteLine("[INFO] Config closed");
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

        private void SetupISOShortcut(object sender, RoutedEventArgs e)
        {
            string p1pspISO = selectExe("Select Persona 1 (PSP) ISO", ".iso");
            if (p1pspISO != null)
            {
                main.gamePath = p1pspISO;
                main.config.p1pspConfig.isoPath = p1pspISO;
                main.updateConfig();
                ISOTextbox.Text = p1pspISO;
            }
            else
            {
                Console.WriteLine("[ERROR] No ISO selected.");
            }
        }

        private void SetupPPSSPPShortcut(object sender, RoutedEventArgs e)
        {
            string ppssppExe = selectExe("Select PPSSPPWindows.exe/PPSSPPWindows64.exe", ".exe");
            if (Path.GetFileName(ppssppExe).ToLowerInvariant() == "ppssppwindows.exe" ||
                Path.GetFileName(ppssppExe).ToLowerInvariant() == "ppssppwindows64.exe")
            {
                main.launcherPath = ppssppExe;
                main.config.p1pspConfig.launcherPath = ppssppExe;
                main.updateConfig();
                PPSSPPTextbox.Text = ppssppExe;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid exe.");
            }
        }

        private string selectExe(string title, string extension)
        {
            string type = "Application";
            if (extension == ".iso")
                type = "Disk";
            var openExe = new CommonOpenFileDialog();
            openExe.Filters.Add(new CommonFileDialogFilter(type, $"*{extension}"));
            openExe.EnsurePathExists = true;
            openExe.EnsureValidNames = true;
            openExe.Title = title;
            if (openExe.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return openExe.FileName;
            }
            return null;
        }

        // Use 7zip on iso
        private async void UnpackPacsClick(object sender, RoutedEventArgs e)
        {
            if (main.gamePath == null || main.gamePath == "")
            {
                string selectedPath = selectExe("Select P1PSP ISO to unpack", ".iso");
                if (selectedPath != null)
                {
                    main.gamePath = selectedPath;
                    main.config.p1pspConfig.isoPath = main.gamePath;
                    main.updateConfig();
                }
                else
                {
                    Console.WriteLine("[ERROR] Incorrect file chosen for unpacking.");
                    return;
                }
            }
            main.ModGrid.IsHitTestVisible = false;
            UnpackButton.IsHitTestVisible = false;
            foreach (var button in main.buttons)
            {
                button.IsHitTestVisible = false;
                button.Foreground = new SolidColorBrush(Colors.Gray);
            }
            main.GameBox.IsHitTestVisible = false;
            await main.pacUnpack(main.gamePath);
            UnpackButton.IsHitTestVisible = true;
        }

        // Stops the user from changing the displayed "Notifications" text to the names of one of the combo boxes
        private void NotifBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            NotifBox.SelectedIndex = 0;
        }

    }
}
