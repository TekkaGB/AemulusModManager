using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private MainWindow main;
        private PacUnpacker pacUnpacker;

        public ConfigWindow(MainWindow _main)
        {
            InitializeComponent();
            main = _main;
            if (main.modPath != null)
            
                OutputTextbox.Text = main.modPath;
            if (main.p4gPath != null)
                P4GTextbox.Text = main.p4gPath;
            if (main.reloadedPath != null)
                ReloadedTextbox.Text = main.reloadedPath;
            KeepSND.IsChecked = main.emptySND;
            TblPatchBox.IsChecked = main.tbl;
            pacUnpacker = new PacUnpacker();
            Console.WriteLine("[INFO] Config launched");
        }


        private void SndChecked(object sender, RoutedEventArgs e)
        {
            main.emptySND = true;
            main.config.emptySND = true;
            main.updateConfig();
        }
        private void SndUnchecked(object sender, RoutedEventArgs e)
        {
            main.emptySND = false;
            main.config.emptySND = false;
            main.updateConfig();
        }

        private void TblChecked(object sender, RoutedEventArgs e)
        {
            main.tbl = true;
            main.config.tbl = true;
            main.updateConfig();
        }
        private void TblUnchecked(object sender, RoutedEventArgs e)
        {
            main.tbl = false;
            main.config.tbl = false;
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
                main.config.modDir = directory;
                main.modPath = directory;
                main.MergeButton.IsEnabled = true;
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
                main.p4gPath = p4gExe;
                main.config.exePath = p4gExe;
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
                main.reloadedPath = reloadedExe;
                main.config.reloadedPath = reloadedExe;
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
                if (File.Exists($@"{directory}\data00004.pac"))
                {
                    UnpackButton.IsEnabled = false;
                    main.ConfigButton.IsEnabled = false;
                    main.RefreshButton.IsEnabled = false;
                    main.MergeButton.IsEnabled = false;
                    main.LaunchButton.IsEnabled = false;
                    await main.pacUnpack(directory);
                    UnpackButton.IsEnabled = true;
                }
                else
                    Console.WriteLine("[ERROR] Invalid folder cannot find data00004.pac");
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
    }
}
