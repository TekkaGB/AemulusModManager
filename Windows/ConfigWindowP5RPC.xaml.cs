using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using AemulusModManager.Utilities;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindowP5RPC : Window
    {
        private MainWindow main;
        private bool handled;
        private bool language_handled;
        private bool version_handled;

        public ConfigWindowP5RPC(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                EXETextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                ReloadedTextbox.Text = main.launcherPath;
            BuildFinishedBox.IsChecked = main.config.p5rPCConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p5rPCConfig.buildWarning;
            ChangelogBox.IsChecked = main.config.p5rPCConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p5rPCConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p5rPCConfig.updateAll;
            UpdateBox.IsChecked = main.config.p5rPCConfig.updatesEnabled;
            switch (main.config.p5rPCConfig.language)
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
            switch (main.config.p5rPCConfig.cpkName)
            {
                case "BIND":
                    CPKBox.SelectedIndex = 0;
                    break;
                case "1":
                    CPKBox.SelectedIndex = 1;
                    break;
                case "2":
                    CPKBox.SelectedIndex = 2;
                    break;
                case "3":
                    CPKBox.SelectedIndex = 3;
                    break;
                case "MOD.CPK":
                    CPKBox.SelectedIndex = 4;
                    break;
                case "1.CPK":
                    CPKBox.SelectedIndex = 5;
                    break;
                case "2.CPK":
                    CPKBox.SelectedIndex = 6;
                    break;
                case "3.CPK":
                    CPKBox.SelectedIndex = 7;
                    break;
            }
            Console.WriteLine("[INFO] Config launched");
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
                    main.config.p5rPCConfig.cpkName = cpkName;
                    main.updateConfig();
                }
                handled = false;
            }
        }
        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder("Select output folder");
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting output folder to {directory}");
                main.config.p5rPCConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0xf7, 0x64, 0x84));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void SetupEXEShortcut(object sender, RoutedEventArgs e)
        {
            string p5rexe = selectExe("Select P5R.exe", "*.exe");
            if (p5rexe != null)
            {
                main.gamePath = p5rexe;
                main.config.p5rPCConfig.exePath = p5rexe;
                main.updateConfig();
                EXETextbox.Text = p5rexe;
            }
            else
            {
                Console.WriteLine("[ERROR] No EXE selected.");
            }
        }

        private string selectExe(string title, string extension)
        {
            string type = "Application";
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

        private void SetupReloadedShortcut(object sender, RoutedEventArgs e)
        {
            string reloadedExe = selectExe("Select Reloaded-II.exe", "*.exe");
            if (Path.GetFileName(reloadedExe) == "Reloaded-II.exe" ||
                Path.GetFileName(reloadedExe) == "Reloaded-II32.exe")
            {
                main.launcherPath = reloadedExe;
                main.config.p5rPCConfig.reloadedPath = reloadedExe;
                main.updateConfig();
                ReloadedTextbox.Text = reloadedExe;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid exe.");
            }
        }
        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.p5rPCConfig.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.p5rPCConfig.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.p5rPCConfig.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.p5rPCConfig.buildFinished = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.p5rPCConfig.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.p5rPCConfig.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.p5rPCConfig.updateAll = true;
            main.updateConfig();
        }
        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.p5rPCConfig.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.p5rPCConfig.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.p5rPCConfig.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.p5rPCConfig.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.p5rPCConfig.deleteOldVersions = false;
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
            string directory;
            if (main.gamePath != null && FileIOWrapper.Exists($@"{Directory.GetParent(main.gamePath)}\CPK\BASE.CPK"))
                directory = Directory.GetParent(main.modPath).ToString();
            else
            {
                string p5rexe = selectExe("Select P5R.exe", "*.exe");
                if (p5rexe != null)
                {
                    main.gamePath = p5rexe;
                    main.config.p5rPCConfig.exePath = p5rexe;
                    main.updateConfig();
                    EXETextbox.Text = p5rexe;
                    directory = $@"{Directory.GetParent(main.gamePath)}\CPK";
                }
                else
                {
                    Console.WriteLine("[ERROR] No EXE selected.");
                    return;
                }
            }
            if (directory != null)
            {
                if (FileIOWrapper.Exists($@"{Directory.GetParent(main.gamePath)}\CPK\BASE.CPK"))
                {
                    UnpackButton.IsHitTestVisible = false;
                    main.ModGrid.IsHitTestVisible = false;
                    main.GameBox.IsHitTestVisible = false;
                    foreach (var button in main.buttons)
                    {
                        button.Foreground = new SolidColorBrush(Colors.Gray);
                        button.IsHitTestVisible = false;
                    }
                    await main.pacUnpack(directory);
                    UnpackButton.IsHitTestVisible = true;
                }
                else
                    Console.WriteLine($"[ERROR] Invalid folder cannot find {main.cpkLang}");
            }
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
                if (main.config.p5rPCConfig.language != language)
                {
                    Console.WriteLine($"[INFO] Language changed to {language}");
                    main.config.p5rPCConfig.language = language;
                    main.updateConfig();
                }
                language_handled = false;
            }
        }
    }
}
