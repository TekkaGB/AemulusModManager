using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using System.Reflection.Metadata;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindowP3PSwitch : Window
    {
        private MainWindow main;
        private bool language_handled;
        private bool handled;

        public ConfigWindowP3PSwitch(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                ROMTextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                EmulatorTextbox.Text = main.launcherPath;
            BuildFinishedBox.IsChecked = main.config.p3pSwitchConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p3pSwitchConfig.buildWarning;
            ChangelogBox.IsChecked = main.config.p3pSwitchConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p3pSwitchConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p3pSwitchConfig.updateAll;
            UpdateBox.IsChecked = main.config.p3pSwitchConfig.updatesEnabled;
            switch (main.config.p3pSwitchConfig.language)
            {
                case "English":
                    LanguageBox.SelectedIndex = 0;
                    break;
                case "French":
                    LanguageBox.SelectedIndex = 1;
                    break;
                case "Italian":
                    LanguageBox.SelectedIndex = 2;
                    break;
                case "German":
                    LanguageBox.SelectedIndex = 3;
                    break;
                case "Spanish":
                    LanguageBox.SelectedIndex = 4;
                    break;
            }
            switch (main.config.p3pSwitchConfig.cpkName)
            {
                case "mod.cpk":
                    CPKBox.SelectedIndex = 0;
                    break;
                case "mod_extra.cpk":
                    CPKBox.SelectedIndex = 1;
                    break;
                case "mod_bgm.cpk":
                    CPKBox.SelectedIndex = 2;
                    break;
            }
            Utilities.ParallelLogger.Log("[INFO] Config launched");
        }
        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder("Select output folder");
            if (directory != null)
            {
                Utilities.ParallelLogger.Log($"[INFO] Setting output folder to {directory}");
                main.config.p3pSwitchConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0xf7, 0x64, 0x84));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void SetupROMShortcut(object sender, RoutedEventArgs e)
        {
            string p3pSwitchRom = selectExe("Select Persona 3 Portable (Switch) ROM", "*.xci;*.nsp");
            if (p3pSwitchRom != null)
            {
                main.gamePath = p3pSwitchRom;
                main.config.p3pSwitchConfig.gamePath = p3pSwitchRom;
                main.updateConfig();
                ROMTextbox.Text = p3pSwitchRom;
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] No ROM selected.");
            }
        }

        private string selectExe(string title, string extension)
        {
            string type = "Application";
            if (extension == "*.xci;*.nsp")
                type = "ROM";
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

        private void SetupEmulatorShortcut(object sender, RoutedEventArgs e)
        {
            string emulatorExe = selectExe("Select Exectuable for Emulator (yuzu.exe or Ryujinx.exe)", "*.exe");
            if (Path.GetFileName(emulatorExe).ToLowerInvariant() == "yuzu.exe" || Path.GetFileName(emulatorExe).ToLowerInvariant() == "ryujinx.exe")
            {
                main.launcherPath = emulatorExe;
                main.config.p3pSwitchConfig.launcherPath = emulatorExe;
                main.updateConfig();
                EmulatorTextbox.Text = emulatorExe;
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] Invalid exe.");
            }
        }
        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.p3pSwitchConfig.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.p3pSwitchConfig.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.p3pSwitchConfig.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.p3pSwitchConfig.buildFinished = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.p3pSwitchConfig.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.p3pSwitchConfig.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.p3pSwitchConfig.updateAll = true;
            main.updateConfig();
        }
        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.p3pSwitchConfig.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.p3pSwitchConfig.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.p3pSwitchConfig.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.p3pSwitchConfig.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.p3pSwitchConfig.deleteOldVersions = false;
            main.updateConfig();
        }

        private void onClose(object sender, CancelEventArgs e)
        {
            Utilities.ParallelLogger.Log("[INFO] Config closed");
        }

        // Used for selecting
        private string openFolder(string title)
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
        private async void UnpackPacsClick(object sender, RoutedEventArgs e)
        {
            string selectedPath = openFolder("Select your P3P romfs dump folder (Either 'romfs' or 'NCA FS'");
            if (selectedPath != null)
            {
                var cpksNeeded = new List<string>();
                var extraCpk = String.Empty;
                cpksNeeded.Add("data/umd0.cpk");
                cpksNeeded.Add("data/umd1.cpk");
                cpksNeeded.Add("sysdat.cpk");
                switch (main.config.p3pSwitchConfig.language)
                {
                    case "English":
                        cpksNeeded.Add("data_EN/umd0.cpk");
                        extraCpk = ", umd0.cpk";
                        break;
                    case "French":
                        cpksNeeded.Add("data_FR/umd0.cpk");
                        extraCpk = ", umd0.cpk";
                        break;
                    case "Italian":
                        cpksNeeded.Add("data_IT/umd0.cpk");
                        extraCpk = ", umd0.cpk";
                        break;
                    case "German":
                        cpksNeeded.Add("data_DE/umd0.cpk");
                        extraCpk = ", umd0.cpk";
                        break;
                    case "Spanish":
                        cpksNeeded.Add("data_ES/umd0.cpk");
                        extraCpk = ", umd0.cpk";
                        break;
                }
            }
            else
            {
                Utilities.ParallelLogger.Log("[ERROR] No folder chosen");
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

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            language_handled = true;
        }

        private void LanguageBox_DropDownClosed(object sender, EventArgs e)
        {
            if (language_handled)
            {
                var language = (LanguageBox.SelectedValue as ComboBoxItem).Content as String;
                if (main.config.p3pSwitchConfig.language != language)
                {
                    Utilities.ParallelLogger.Log($"[INFO] Language changed to {language}");
                    main.config.p3pSwitchConfig.language = language;
                    main.updateConfig();
                }
                language_handled = false;
            }
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
                if (main.config.p3pSwitchConfig.cpkName != cpkName)
                {
                    Utilities.ParallelLogger.Log($"[INFO] Output changed to {cpkName}");
                    main.config.p3pSwitchConfig.cpkName = cpkName;
                    main.updateConfig();
                }
                handled = false;
            }
        }
    }
}
