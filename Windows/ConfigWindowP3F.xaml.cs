using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindowP3F : Window
    {
        private MainWindow main;

        public ConfigWindowP3F(MainWindow _main)
        {
            main = _main;
            InitializeComponent();

            if (main.modPath != null)
                OutputTextbox.Text = main.modPath;
            if (main.gamePath != null)
                ISOTextbox.Text = main.gamePath;
            if (main.launcherPath != null)
                PCSX2Textbox.Text = main.launcherPath;
            if (main.elfPath != null)
                ELFTextbox.Text = main.elfPath;
            AdvancedLaunchOptions.IsChecked = main.config.p3fConfig.advancedLaunchOptions;
            BuildFinishedBox.IsChecked = main.config.p3fConfig.buildFinished;
            BuildWarningBox.IsChecked = main.config.p3fConfig.buildWarning;
            ConfirmUpdateBox.IsChecked = main.config.p3fConfig.updateConfirm;
            ChangelogBox.IsChecked = main.config.p3fConfig.updateChangelog;
            DeleteBox.IsChecked = main.config.p3fConfig.deleteOldVersions;
            UpdateAllBox.IsChecked = main.config.p3fConfig.updateAll;
            UpdateBox.IsChecked = main.config.p3fConfig.updatesEnabled;
            Console.WriteLine("[INFO] Config launched");
        }

        private void modDirectoryClick(object sender, RoutedEventArgs e)
        {
            var directory = openFolder();
            if (directory != null)
            {
                Console.WriteLine($"[INFO] Setting output folder to {directory}");
                main.config.p3fConfig.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsHitTestVisible = true;
                main.MergeButton.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }

        private void BuildWarningChecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = true;
            main.config.p3fConfig.buildWarning = true;
            main.updateConfig();
        }

        private void BuildWarningUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildWarning = false;
            main.config.p3fConfig.buildWarning = false;
            main.updateConfig();
        }
        private void BuildFinishedChecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = true;
            main.config.p3fConfig.buildFinished = true;
            main.updateConfig();
        }
        private void BuildFinishedUnchecked(object sender, RoutedEventArgs e)
        {
            main.buildFinished = false;
            main.config.p3fConfig.buildFinished = false;
            main.updateConfig();
        }
        private void ConfirmUpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updateConfirm = true;
            main.config.p3fConfig.updateConfirm = true;
            main.updateConfig();
        }
        private void ConfirmUpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateConfirm = false;
            main.config.p3fConfig.updateConfirm = false;
            main.updateConfig();
        }
        private void ChangelogChecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = true;
            main.config.p3fConfig.updateChangelog = true;
            main.updateConfig();
        }
        private void ChangelogUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateChangelog = false;
            main.config.p3fConfig.updateChangelog = false;
            main.updateConfig();
        }
        private void UpdateAllChecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = true;
            main.config.p3fConfig.updateAll = true;
            main.updateConfig();
        }

        private void UpdateAllUnchecked(object sender, RoutedEventArgs e)
        {
            main.updateAll = false;
            main.config.p3fConfig.updateAll = false;
            main.updateConfig();
        }
        private void UpdateChecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = true;
            main.config.p3fConfig.updatesEnabled = true;
            main.updateConfig();
            UpdateAllBox.IsEnabled = true;
        }

        private void UpdateUnchecked(object sender, RoutedEventArgs e)
        {
            main.updatesEnabled = false;
            main.config.p3fConfig.updatesEnabled = false;
            main.updateConfig();
            UpdateAllBox.IsChecked = false;
            UpdateAllBox.IsEnabled = false;
        }
        private void DeleteChecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = true;
            main.config.p3fConfig.deleteOldVersions = true;
            main.updateConfig();
        }
        private void DeleteUnchecked(object sender, RoutedEventArgs e)
        {
            main.deleteOldVersions = false;
            main.config.p3fConfig.deleteOldVersions = false;
            main.updateConfig();
        }

        private void AdvancedLaunchOptionsChecked(object sender, RoutedEventArgs e)
        {
            main.p3fConfig.advancedLaunchOptions = true;
            main.updateConfig();
        }
        private void AdvancedLaunchOptionsUnchecked(object sender, RoutedEventArgs e)
        {
            main.p3fConfig.advancedLaunchOptions = false;
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
            string p3fIso = selectExe("Select Persona 3 FES ISO", ".iso");
            if (p3fIso != null && Path.GetExtension(p3fIso).ToLower() == ".iso")
            {
                main.gamePath = p3fIso;
                main.config.p3fConfig.isoPath = p3fIso;
                main.updateConfig();
                ISOTextbox.Text = p3fIso;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid ISO.");
            }
        }

        private void SetupPCSX2Shortcut(object sender, RoutedEventArgs e)
        {
            string pcsx2Exe = selectExe("Select pcsx2.exe", ".exe");
            if (Path.GetFileName(pcsx2Exe) == "pcsx2.exe")
            {
                main.launcherPath = pcsx2Exe;
                main.config.p3fConfig.launcherPath = pcsx2Exe;
                main.updateConfig();
                PCSX2Textbox.Text = pcsx2Exe;
            }
            else
            {
                Console.WriteLine("[ERROR] Invalid EXE.");
            }
        }

        private void SetupELFShortcut(object sender, RoutedEventArgs e)
        {
            string elf = selectExe("Select ELF/SLUS", "");
            if (elf != null)
            {
                try
                {
                    // Read the first four bytes, verify that they end in "ELF"
                    using BinaryReader reader = new BinaryReader(new FileStream(elf, FileMode.Open));
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (!magic.EndsWith("ELF"))
                    {
                        Console.WriteLine("[ERROR] Invalid ELF/SLUS.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] An exception occurred while trying to read the specified ELF/SLUS file: {ex.Message}");
                }

                main.elfPath = elf;
                main.config.p3fConfig.elfPath = elf;
                main.updateConfig();
                ELFTextbox.Text = elf;
            }
            else
            {
                Console.WriteLine("[ERROR] No ELF/SLUS file specified.");
            }
        }

        private string selectExe(string title, string extension)
        {
            string type = "Application";
            if (extension == ".iso")
                type = "PS2 Disc";
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
                string selectedPath = selectExe("Select P3F's iso to unpack", ".iso");
                if (selectedPath != null)
                {
                    main.gamePath = selectedPath;
                    main.config.p3fConfig.isoPath = main.gamePath;
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
