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
                main.updateConfig();
                OutputTextbox.Text = directory;
            }
        }
        private void NotifChecked(object sender, RoutedEventArgs e)
        {
            main.messageBox = true;
            main.config.p3fConfig.disableMessageBox = true;
            main.updateConfig();
        }
        private void NotifUnchecked(object sender, RoutedEventArgs e)
        {
            main.messageBox = false;
            main.config.p3fConfig.disableMessageBox = false;
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
                Console.WriteLine("[ERROR] Invalid iso.");
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
                Console.WriteLine("[ERROR] Invalid exe.");
            }
        }

        private void SetupELFShortcut(object sender, RoutedEventArgs e)
        {
            string elf = selectExe("Select ELF", ".elf");
            if (elf != null && Path.GetExtension(elf).ToLower() == ".elf")
            {
                main.elfPath = elf;
                main.config.p3fConfig.elfPath = elf;
                main.updateConfig();
                ELFTextbox.Text = elf;
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
            if (main.gamePath != null)
            {
                main.ModGrid.IsHitTestVisible = false;
                UnpackButton.IsHitTestVisible = false;
                main.ConfigButton.IsHitTestVisible = false;
                main.MergeButton.IsHitTestVisible = false;
                main.LaunchButton.IsHitTestVisible = false;
                main.GameBox.IsHitTestVisible = false;
                main.RefreshButton.IsHitTestVisible = false;
                main.NewButton.IsHitTestVisible = false;
                await main.pacUnpack(main.gamePath);
                UnpackButton.IsHitTestVisible = true;
            }
        }

    }
}
