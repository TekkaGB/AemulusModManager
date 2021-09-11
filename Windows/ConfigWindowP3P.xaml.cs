using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Reflection;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindowP3P : Window
    {
        private MainWindow main;

        public ConfigWindowP3P(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.launcherPath != null)
                PCSX2Textbox.Text = main.launcherPath;
            if (main.gamePath != null)
                P3PTextBox.Text = main.gamePath;
            AdvancedLaunchOptions.IsChecked = main.config.p3pConfig.leaveUmd;
            BuildFinishedBox.IsChecked = main.config.p3pConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p3pConfig.buildWarning;
            ChangelogBox.IsChecked = main.config.p3pConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p3pConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p3pConfig.updateAll;
            UpdateBox.IsChecked = main.config.p3pConfig.updatesEnabled;
            Console.WriteLine("[INFO] Config launched");
        }

        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting output folder to {directory}");
                main.config.p3pConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(255, 79, 193));
                main.updateConfig();
                OutputTextbox.Text = directory;
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

        private void AdvancedLaunchOptionsChecked(object sender, RoutedEventArgs e)
        {
            main.p3pConfig.leaveUmd = true;
            main.updateConfig();
        }
        private void AdvancedLaunchOptionsUnchecked(object sender, RoutedEventArgs e)
        {
            main.p3pConfig.leaveUmd = false;
            main.updateConfig();
        }

        private void onClose(object sender, CancelEventArgs e)
        {
            Console.WriteLine("[INFO] Config closed");
        }

        // Used for selecting
        private string openFolder(string title = "Select Output Folder")
        {
            var openFolder = new CommonOpenFileDialog();
            openFolder.AllowNonFileSystemItems = true;
            openFolder.IsFolderPicker = true;
            openFolder.EnsurePathExists = true;
            openFolder.EnsureValidNames = true;
            openFolder.Multiselect = false;
            openFolder.Title = title;
            if (openFolder.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return openFolder.FileName;
            }

            return null;
        }

        private void SetupPPSSPPShortcut(object sender, RoutedEventArgs e)
        {
            string ppssppExe = selectExe("Select ppsspp.exe", ".exe");
            if (Path.GetFileName(ppssppExe) == "PPSSPP.exe"
                || Path.GetFileName(ppssppExe) == "ppsspp.exe"
                || Path.GetFileName(ppssppExe) == "PPSSPPWindows.exe"
                || Path.GetFileName(ppssppExe) == "PPSSPPWindows64.exe")
            {
                main.launcherPath = ppssppExe;
                main.config.p3pConfig.launcherPath = ppssppExe;
                main.updateConfig();
                PCSX2Textbox.Text = ppssppExe;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid EXE.");
            }
        }

        private void SetupGameDirectory(object sender, RoutedEventArgs e)
        {
            var directory = openFolder("Select Game Folder");
            
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting game folder to {directory}");
                
                main.config.p3pConfig.p3pDir = directory;
                main.gamePath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(255, 79, 193));
                main.updateConfig();
                P3PTextBox.Text = directory;
            }
        }

        private string selectExe(string title, string extension, string type = "Application")
        {
            if (extension == ".exe")
                type = "PPSSPP Executable";
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
            string selectedPath = selectExe("Select UMD0.cpk to unpack it", ".cpk", "umd0.cpk");
            if (selectedPath == null)
            {
                Console.WriteLine("[ERROR] Incorrect file chosen for unpacking.");
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

        private void ELFTextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void OutputTextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
