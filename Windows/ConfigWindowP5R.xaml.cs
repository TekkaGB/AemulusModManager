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
    public partial class ConfigWindowP5R : Window
    {
        private MainWindow main;
        private bool handled;
        private bool language_handled;
        private bool version_handled;

        public ConfigWindowP5R(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            BuildFinishedBox.IsChecked = main.config.p5rConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p5rConfig.buildWarning;
            ChangelogBox.IsChecked = main.config.p5rConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p5rConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p5rConfig.updateAll;
            UpdateBox.IsChecked = main.config.p5rConfig.updatesEnabled;
            switch (main.config.p5rConfig.cpkName)
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
            switch (main.config.p5rConfig.language)
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
            switch (main.config.p5rConfig.version)
            {
                case ">= 1.02":
                    VersionBox.SelectedIndex = 0;
                    break;
                case "< 1.02":
                    VersionBox.SelectedIndex = 1;
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
                main.config.p5rConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0xf7, 0x64, 0x84));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.p5rConfig.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.p5rConfig.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.p5rConfig.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.p5rConfig.buildFinished = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.p5rConfig.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.p5rConfig.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.p5rConfig.updateAll = true;
            main.updateConfig();
        }
        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.p5rConfig.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.p5rConfig.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.p5rConfig.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.p5rConfig.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.p5rConfig.deleteOldVersions = false;
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
                var extraCpk = String.Empty;
                cpksNeeded.Add("dataR.cpk");
                cpksNeeded.Add("ps4R.cpk");
                switch (main.config.p5rConfig.language)
                {
                    case "English":
                        break;
                    case "French":
                        cpksNeeded.Add("dataR_F.cpk");
                        extraCpk = ", dataR_F.cpk";
                        break;
                    case "Italian":
                        cpksNeeded.Add("dataR_I.cpk");
                        extraCpk = ", dataR_I.cpk";
                        break;
                    case "German":
                        cpksNeeded.Add("dataR_G.cpk");
                        extraCpk = ", dataR_G.cpk";
                        break;
                    case "Spanish":
                        cpksNeeded.Add("dataR_S.cpk");
                        extraCpk = ", dataR_S.cpk";
                        break;
                }

                if (main.config.p5rConfig.version == ">= 1.02")
                {
                    cpksNeeded.Add("patch2R.cpk");
                    extraCpk += ", patch2R.cpk";
                    switch (main.config.p5rConfig.language)
                    {
                        case "English":
                            break;
                        case "French":
                            cpksNeeded.Add("patch2R_F.cpk");
                            extraCpk += ", patch2R_F.cpk";
                            break;
                        case "Italian":
                            cpksNeeded.Add("patch2R_I.cpk");
                            extraCpk += ", patch2R_I.cpk";
                            break;
                        case "German":
                            cpksNeeded.Add("patch2R_G.cpk");
                            extraCpk += ", patch2R_G.cpk";
                            break;
                        case "Spanish":
                            cpksNeeded.Add("patch2R_S.cpk");
                            extraCpk += ", patch2R_S.cpk";
                            break;
                    }
                }

                var cpks = Directory.GetFiles(selectedPath, "*.cpk", SearchOption.TopDirectoryOnly);
                if (cpksNeeded.Except(cpks.Select(x => Path.GetFileName(x))).Any())
                {
                    Console.WriteLine($"[ERROR] Not all cpks needed (dataR.cpk, ps4R.cpk{extraCpk}) are found in top directory of {selectedPath}");
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
                if (main.config.p5rConfig.cpkName != cpkName)
                {
                    Console.WriteLine($"[INFO] Output changed to {cpkName}");
                    main.config.p5rConfig.cpkName = cpkName;
                    main.updateConfig();
                }
                handled = false;
            }
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
                if (main.config.p5rConfig.language != language)
                {
                    Console.WriteLine($"[INFO] Language changed to {language}");
                    main.config.p5rConfig.language = language;
                    main.updateConfig();
                }
                language_handled = false;
            }
        }
        private void VersionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            version_handled = true;
        }

        private void VersionBox_DropDownClosed(object sender, EventArgs e)
        {
            if (version_handled)
            {
                var version = VersionBox.SelectedIndex == 0 ? ">= 1.02" : "< 1.02";
                if (main.config.p5rConfig.version != version)
                {
                    Console.WriteLine($"[INFO] Version changed to {version}");
                    main.config.p5rConfig.version = version;
                    main.updateConfig();
                }
                version_handled = false;
            }
        }
    }
}
