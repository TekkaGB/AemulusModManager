using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindowP5RSwitch : Window
    {
        private MainWindow main;
        private bool language_handled;

        public ConfigWindowP5RSwitch(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                ROMTextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                EmulatorTextbox.Text = main.launcherPath;
            BuildFinishedBox.IsChecked = main.config.p5rSwitchConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p5rSwitchConfig.buildWarning;
            ChangelogBox.IsChecked = main.config.p5rSwitchConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p5rSwitchConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p5rSwitchConfig.updateAll;
            UpdateBox.IsChecked = main.config.p5rSwitchConfig.updatesEnabled;
            switch (main.config.p5rSwitchConfig.language)
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
            Console.WriteLine("[INFO] Config launched");
        }
        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder("Select output folder");
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting output folder to {directory}");
                main.config.p5rSwitchConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0xf7, 0x64, 0x84));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void SetupROMShortcut(object sender, RoutedEventArgs e)
        {
            string p5rRom = selectExe("Select Persona 5 Royal (Switch) ROM", "*.xci;*.nsp");
            if (p5rRom != null)
            {
                main.gamePath = p5rRom;
                main.config.p5rSwitchConfig.gamePath = p5rRom;
                main.updateConfig();
                ROMTextbox.Text = p5rRom;
            }
            else
            {
                Console.WriteLine("[ERROR] No ROM selected.");
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
                main.config.p5rSwitchConfig.launcherPath = emulatorExe;
                main.updateConfig();
                EmulatorTextbox.Text = emulatorExe;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid exe.");
            }
        }
        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.p5rSwitchConfig.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.p5rSwitchConfig.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.p5rSwitchConfig.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.p5rSwitchConfig.buildFinished = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.p5rSwitchConfig.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.p5rSwitchConfig.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.p5rSwitchConfig.updateAll = true;
            main.updateConfig();
        }
        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.p5rSwitchConfig.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.p5rSwitchConfig.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.p5rSwitchConfig.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.p5rSwitchConfig.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.p5rSwitchConfig.deleteOldVersions = false;
            main.updateConfig();
        }

        private void onClose(object sender, CancelEventArgs e)
        {
            Console.WriteLine("[INFO] Config closed");
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
            string selectedPath = openFolder("Select folder with P5R cpks");
            if (selectedPath != null)
            {
                var cpksNeeded = new List<string>();
                cpksNeeded.Add("ALL_USEU.CPK");
                cpksNeeded.Add("PATCH1.CPK");

                var cpks = Directory.GetFiles(selectedPath, "*.cpk", SearchOption.TopDirectoryOnly);
                if (cpksNeeded.Except(cpks.Select(x => Path.GetFileName(x))).Any())
                {
                    Console.WriteLine($"[ERROR] Not all cpks needed (ALL_USEU.CPK and PATCH1.CPK) are found in top directory of {selectedPath}");
                    return;
                }
            }
            else
            {
                Console.WriteLine("[ERROR] No folder chosen");
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
                if (main.config.p5rSwitchConfig.language != language)
                {
                    Console.WriteLine($"[INFO] Language changed to {language}");
                    main.config.p5rSwitchConfig.language = language;
                    main.updateConfig();
                }
                language_handled = false;
            }
        }
    }
}
