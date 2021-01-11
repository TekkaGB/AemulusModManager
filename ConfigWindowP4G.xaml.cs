using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindowP4G : Window
    {
        private MainWindow main;

        public ConfigWindowP4G(MainWindow _main)
        {
            main = _main;
            InitializeComponent();
            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                P4GTextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                ReloadedTextbox.Text = main.launcherPath;
            KeepSND.IsChecked = main.emptySND;
            CpkBox.IsChecked = main.useCpk;
            switch (main.cpkLang)
            {
                case "data_e.cpk":
                    LanguageBox.SelectedIndex = 0;
                    break;
                case "data.cpk":
                    LanguageBox.SelectedIndex = 1;
                    break;
                case "data_c.cpk":
                    LanguageBox.SelectedIndex = 2;
                    break;
                case "data_k.cpk":
                    LanguageBox.SelectedIndex = 3;
                    break;
                default:
                    LanguageBox.SelectedIndex = 0;
                    main.cpkLang = "data_e.cpk";
                    main.config.p4gConfig.cpkLang = "data_e.cpk";
                    main.updateConfig();
                    break;
            }
            Console.WriteLine("[INFO] Config launched");
        }


        private void SndChecked(object sender, RoutedEventArgs e)
        {
            main.emptySND = true;
            main.config.p4gConfig.emptySND = true;
            main.updateConfig();
        }
        private void SndUnchecked(object sender, RoutedEventArgs e)
        {
            main.emptySND = false;
            main.config.p4gConfig.emptySND = false;
            main.updateConfig();
        }

        private void CpkChecked(object sender, RoutedEventArgs e)
        {
            main.useCpk = true;
            main.config.p4gConfig.useCpk = true;
            main.updateConfig();
        }
        private void CpkUnchecked(object sender, RoutedEventArgs e)
        {
            main.useCpk = false;
            main.config.p4gConfig.useCpk = false;
            main.updateConfig();
        }

        private void NotifChecked(object sender, RoutedEventArgs e)
        {
            main.messageBox = true;
            main.config.p4gConfig.disableMessageBox = true;
            main.updateConfig();
        }
        private void NotifUnchecked(object sender, RoutedEventArgs e)
        {
            main.messageBox = false;
            main.config.p4gConfig.disableMessageBox = false;
            main.updateConfig();
        }

        private void onClose(object sender, CancelEventArgs e)
        {
            Console.WriteLine("[INFO] Config closed");
        }

        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting output folder to {directory}");
                main.config.p4gConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
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

        private void SetupP4GShortcut(object sender, RoutedEventArgs e)
        {
            string p4gExe = selectExe("Select P4G.exe");
            if (Path.GetFileName(p4gExe) == "P4G.exe")
            {
                main.gamePath = p4gExe;
                main.config.p4gConfig.exePath = p4gExe;
                main.updateConfig();
                P4GTextbox.Text = p4gExe;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid exe.");
            }
        }

        private void SetupReloadedShortcut(object sender, RoutedEventArgs e)
        {
            string reloadedExe = selectExe("Select Reloaded-II.exe");
            if (Path.GetFileName(reloadedExe) == "Reloaded-II.exe" ||
                Path.GetFileName(reloadedExe) == "Reloaded-II32.exe")
            {
                main.launcherPath = reloadedExe;
                main.config.p4gConfig.reloadedPath = reloadedExe;
                main.updateConfig();
                ReloadedTextbox.Text = reloadedExe;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid exe.");
            }
        }
        private string selectExe(string title)
        {
            var openExe = new CommonOpenFileDialog();
            openExe.Filters.Add(new CommonFileDialogFilter("Application", "*.exe"));
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
            string directory;
            if (main.modPath != null && File.Exists($@"{Directory.GetParent(main.modPath)}\data00004.pac"))
                directory = Directory.GetParent(main.modPath).ToString();
            else
                directory = openPacsFolder();
            if (directory != null)
            {
                if (File.Exists($@"{directory}\{main.cpkLang}"))
                {
                    UnpackButton.IsHitTestVisible = false;
                    main.GameBox.IsHitTestVisible = false;
                    main.ConfigButton.IsHitTestVisible = false;
                    main.MergeButton.IsHitTestVisible = false;
                    main.LaunchButton.IsHitTestVisible = false;
                    main.RefreshButton.IsHitTestVisible = false;
                    main.ModGrid.IsHitTestVisible = false;
                    main.NewButton.IsHitTestVisible = false;
                    main.SwapButton.IsHitTestVisible = false;
                    main.FolderButton.IsHitTestVisible = false;
                    await main.pacUnpack(directory);
                    UnpackButton.IsHitTestVisible = true;
                }
                else
                    Console.WriteLine($"[ERROR] Invalid folder cannot find {main.cpkLang}");
            }
        }

        private string openPacsFolder()
        {
            var openFolder = new CommonOpenFileDialog();
            openFolder.AllowNonFileSystemItems = true;
            openFolder.Multiselect = false;
            openFolder.IsFolderPicker = true;
            openFolder.EnsurePathExists = true;
            openFolder.EnsureValidNames = true;
            openFolder.Title = "Select P4G Game Directory";
            if (openFolder.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return openFolder.FileName;
            }

            return null;
        }


        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LanguageBox.SelectedIndex != -1 && IsLoaded)
            {
                int index = LanguageBox.SelectedIndex;
                string selectedLanguage = null;
                switch (index)
                {
                    case 0:
                        selectedLanguage = "data_e.cpk";
                        break;
                    case 1:
                        selectedLanguage = "data.cpk";
                        break;
                    case 2:
                        selectedLanguage = "data_c.cpk";
                        break;
                    case 3:
                        selectedLanguage = "data_k.cpk";
                        break;
                }
                main.config.p4gConfig.cpkLang = selectedLanguage;
                main.cpkLang = selectedLanguage;
                main.updateConfig();
            }
        }
    }
}
