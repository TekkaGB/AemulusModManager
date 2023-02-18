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
    public partial class ConfigWindowP3P : Window
    {
        private MainWindow main;
        private bool handled;

        public ConfigWindowP3P(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                ISOTextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                PPSSPPTextbox.Text = main.launcherPath;
            if (main.config.p3pConfig.texturesPath != null)
                TexturesTextbox.Text = main.config.p3pConfig.texturesPath;
            if (main.config.p3pConfig.cheatsPath != null)
                CheatsTextbox.Text = main.config.p3pConfig.cheatsPath;
            BuildFinishedBox.IsChecked = main.config.p3pConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p3pConfig.buildWarning;
            ChangelogBox.IsChecked = main.config.p3pConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p3pConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p3pConfig.updateAll;
            UpdateBox.IsChecked = main.config.p3pConfig.updatesEnabled;
            switch (main.config.p3pConfig.cpkName)
            {
                case "bind":
                    CPKBox.SelectedIndex = 0;
                    break;
                case "mod.cpk":
                    CPKBox.SelectedIndex = 1;
                    break;
                case "mod1.cpk":
                    CPKBox.SelectedIndex = 2;
                    break;
                case "mod2.cpk":
                    CPKBox.SelectedIndex = 3;
                    break;
                case "mod3.cpk":
                    CPKBox.SelectedIndex = 4;
                    break;
            }
            Utilities.ParallelLogger.Log("[INFO] Config launched");
        }
        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Utilities.ParallelLogger.Log($"[INFO] Setting output folder to {directory}");
                main.config.p3pConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0xfc, 0x83, 0xe3));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void textureDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Utilities.ParallelLogger.Log($"[INFO] Setting textures folder to {directory}");
                main.config.p3pConfig.texturesPath = directory;
                main.updateConfig();
                TexturesTextbox.Text = directory;
            }
        }
        private void cheatDirectoryClick(object sender, RoutedEventArgs e)
        {
            var file = selectExe("Select the P3P cheats ini (ULUS10512.ini)", "*.ini");
            if (file != null)
            {
                Utilities.ParallelLogger.Log($"[INFO] Setting cheats ini to {file}");
                main.config.p3pConfig.cheatsPath = file;
                main.updateConfig();
                CheatsTextbox.Text = file;
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] No ini selected");
            }
        }
        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.p3pConfig.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.p3pConfig.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.p3pConfig.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.p3pConfig.buildFinished = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.p3pConfig.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.p3pConfig.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.p3pConfig.updateAll = true;
            main.updateConfig();
        }
        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.p3pConfig.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.p3pConfig.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.p3pConfig.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.p3pConfig.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.p3pConfig.deleteOldVersions = false;
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

        private void SetupISOShortcut(object sender, RoutedEventArgs e)
        {
            string p3pISO = selectExe("Select Persona 3 Portable ISO", ".iso");
            if (p3pISO != null)
            {
                main.gamePath = p3pISO;
                main.config.p3pConfig.isoPath = p3pISO;
                main.updateConfig();
                ISOTextbox.Text = p3pISO;
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] No ISO selected.");
            }
        }

        private void SetupPPSSPPShortcut(object sender, RoutedEventArgs e)
        {
            string ppssppExe = selectExe("Select PPSSPPWindows.exe/PPSSPPWindows64.exe", ".exe");
            if (Path.GetFileName(ppssppExe).ToLowerInvariant() == "ppssppwindows.exe" ||
                Path.GetFileName(ppssppExe).ToLowerInvariant() == "ppssppwindows64.exe")
            {
                main.launcherPath = ppssppExe;
                main.config.p3pConfig.launcherPath = ppssppExe;
                main.updateConfig();
                PPSSPPTextbox.Text = ppssppExe;
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] Invalid exe.");
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
                string selectedPath = selectExe("Select P3P ISO to unpack", ".iso");
                if (selectedPath != null)
                {
                    main.gamePath = selectedPath;
                    main.config.p3pConfig.isoPath = main.gamePath;
                    main.updateConfig();
                }
                else
                {
                    Utilities.ParallelLogger.Log("[ERROR] Incorrect file chosen for unpacking.");
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

        private void CPKBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            handled = true;
        }

        private void CPKBox_DropDownClosed(object sender, EventArgs e)
        {
            if (handled)
            {
                var cpkName = (CPKBox.SelectedValue as ComboBoxItem).Content as String;
                if (main.config.p3pConfig.cpkName != cpkName)
                {
                    Utilities.ParallelLogger.Log($"[INFO] Output changed to {cpkName}");
                    main.config.p3pConfig.cpkName = cpkName;
                    main.updateConfig();
                }
                handled = false;
            }
        }

    }
}
