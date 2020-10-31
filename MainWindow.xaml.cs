using GongSolutions.Wpf.DragDrop.Utilities;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace AemulusModManager
{
    public partial class MainWindow : Window
    {
        private TextBoxOutputter outputter;
        public Config config;
        private XmlSerializer xs;
        private XmlSerializer xsp;
        public string modPath;
        private ObservableCollection<Package> PackageList;
        private ObservableCollection<DisplayedMetadata> DisplayedPackages;
        private binMerge binMerger;
        private tblPatch tblPatcher;
        private PacUnpacker pacUnpacker;
        public bool emptySND;
        public bool tbl;
        public string p4gPath;
        public string reloadedPath;

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            DisplayedMetadata package = checkBox?.DataContext as DisplayedMetadata;

            if (package != null)
            {
                package.enabled = true;
                foreach (var p in PackageList)
                {
                    if (p.name == package.name)
                        p.enabled = true;
                }
                updateConfig();
            }
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            DisplayedMetadata package = checkBox?.DataContext as DisplayedMetadata;

            if (package != null)
            {
                package.enabled = false;
                foreach (var p in PackageList)
                {
                    if (p.name == package.name)
                        p.enabled = false;
                }
                updateConfig();
            }
        }

        private void OnHyperlinkClick(object sender, RoutedEventArgs e)
        {
            var destination = ((Hyperlink)e.OriginalSource).NavigateUri;

            if (destination != null)
            {
                try
                {
                    Process.Start(destination.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Invalid Mod Page link. Perhaps missing \'www\' ({ex.Message})");
                }

            }
        }

        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            ConsoleOutput.ScrollToEnd();
        }


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            // Set stdout to console in window
            outputter = new TextBoxOutputter(ConsoleOutput);
            Console.SetOut(outputter);

            binMerger = new binMerge();
            tblPatcher = new tblPatch();
            pacUnpacker = new PacUnpacker();
            DisplayedPackages = new ObservableCollection<DisplayedMetadata>();
            PackageList = new ObservableCollection<Package>();

            // Initial image
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream iconStream = asm.GetManifestResourceStream("AemulusModManager.Preview.png");
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = iconStream;
            bitmap.EndInit();
            Preview.Source = bitmap;

            config = new Config();
            // Construct an instance of the XmlSerializer with the type
            // of object that is being deserialized.
            xs = new XmlSerializer(typeof(Config));
            xsp = new XmlSerializer(typeof(Metadata));

            Console.WriteLine("[INFO] Initializing packages from Config.xml");
            // Load in Config if it exists
            if (File.Exists(@"Config.xml"))
            {
                using (FileStream streamWriter = File.Open(@"Config.xml", FileMode.Open))
                {
                    // Call the Deserialize method and cast to the object type.
                    config = (Config)xs.Deserialize(streamWriter);
                    reloadedPath = config.reloadedPath;
                    p4gPath = config.exePath;
                    modPath = config.modDir;
                    emptySND = config.emptySND;
                    PackageList = config.package;
                    tbl = config.tbl;
                }

                foreach (var package in PackageList)
                {
                    string xml = $@"Packages\{package.path}\Package.xml";
                    Metadata m;
                    DisplayedMetadata dm = new DisplayedMetadata();
                    try
                    {
                        if (File.Exists(xml))
                    {
                        m = new Metadata();
                        
                            using (FileStream streamWriter = File.Open(xml, FileMode.Open))
                            {
                                m = (Metadata)xsp.Deserialize(streamWriter);
                                dm.name = m.name;
                                dm.id = m.id;
                                dm.author = m.author;
                                Version v;
                                if (Version.TryParse(m.version, out v))
                                    dm.version = m.version;
                                dm.link = m.link;
                                dm.description = m.description;
                                foreach (var p in PackageList.ToList())
                                {
                                    if (p.name == dm.name)
                                        dm.path = p.path;
                                }

                            }
                        
                    }
                    dm.enabled = package.enabled;
                    DisplayedPackages.Add(dm);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for package {package.name}");
                        continue;
                    }
                }

                ModGrid.ItemsSource = DisplayedPackages;
            }

            if (modPath == null)
            {
                MergeButton.IsEnabled = false;
            }

            if (config.modDir != null)
                modPath = config.modDir;

            // Create Packages directory if it doesn't exist
            if (!Directory.Exists("Packages"))
            {
                Directory.CreateDirectory("Packages");
            }
            
            if (!Directory.Exists("Original"))
            {
                Directory.CreateDirectory("Original");
            }
            
            Refresh();
            checkVersion();
            updateConfig();

            // Check if Original Folder is unpacked
            if (!Directory.EnumerateFileSystemEntries("Original").Any())
            {
                Console.WriteLine("[WARNING] Aemulus can't find your Vanilla files in the Original folder.");
                Console.WriteLine("Please click the Config button and select \"Unpack data00004.pac\" before building.");
            }
        }

        public Task pacUnpack(string directory)
        {
            return Task.Run(() =>
            {
                pacUnpacker.Unpack(directory);
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    ConfigButton.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    MergeButton.IsEnabled = true;
                    LaunchButton.IsEnabled = true;
                });
            }
            ) ;
        }
        
        private void LaunchClick(object sender, RoutedEventArgs e)
        {
            if (p4gPath != null && reloadedPath != null)
            {
                Console.WriteLine("[INFO] Launching game!");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = reloadedPath;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = "--launch \"" + p4gPath + "\"";
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();

                    process.WaitForExit();
                }
            }
            else
                Console.WriteLine("[ERROR] Please setup shortcut in config menu.");
        }

        private async void RefreshClick(object sender, RoutedEventArgs e)
        {
            await RefreshTask();
        }

        private void ConfigWdwClick(object sender, RoutedEventArgs e)
        {
            ConfigWindow cWindow = new ConfigWindow(this) { Owner = this };
            cWindow.DataContext = this;
            cWindow.ShowDialog();
        }

        private Task RefreshTask()
        {
            return Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    RefreshButton.IsEnabled = false;
                });
                Refresh();
                checkMetadata();
                checkVersion();
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    updateConfig();
                    RefreshButton.IsEnabled = true;
                });
            });
        }

        // Update displayedpackages if xml's are edited while its open
        private void checkMetadata()
        {
            List<DisplayedMetadata> temp = DisplayedPackages.ToList();
            foreach (var package in PackageList)
            {
                if (File.Exists($@"Packages\{package.path}\Package.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = File.Open($@"Packages\{package.path}\Package.xml", FileMode.Open))
                        {
                            Metadata m = (Metadata)xsp.Deserialize(streamWriter);
                            package.name = m.name;

                            foreach (var dm in temp)
                            {
                                if (dm.name == package.name)
                                {
                                    dm.name = m.name;
                                    dm.author = m.author;
                                    Version v;
                                    if (Version.TryParse(m.version, out v))
                                        dm.version = m.version;
                                    dm.id = m.id;
                                    dm.link = m.link;
                                    dm.description = m.description;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for {package.name} ({ex.Message}), not updating...");
                    }
                }
            }
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                DisplayedPackages = new ObservableCollection<DisplayedMetadata>(temp);
                ModGrid.ItemsSource = DisplayedPackages;
                // Trigger select event to refresh
                ModGrid.SetSelectedItem(ModGrid.GetSelectedItem());
            });
        }

        private void checkVersion()
        {
            List<DisplayedMetadata> temp = DisplayedPackages.ToList();
            // Group up all 
            foreach (var t in temp.Where(x => x.version != null && x.version.Length > 0).GroupBy(x => x.id).ToList())
            {
                foreach (var tt in t.OrderBy(x => Version.Parse(x.version)).Take(t.Count()-1).ToList())
                {
                    Console.WriteLine($"[WARNING] Multiple packages of same ID found, removing {tt.name} v{tt.version}");
                    temp.Remove(tt);
                    PackageList.RemoveAt(DisplayedPackages.IndexOf(tt));
                }
            }
            
            DisplayedPackages = new ObservableCollection<DisplayedMetadata>(temp);

            // Update DisplayedPackages
            // TODO: Update PackageList as well and/or move to different folder to not keep doing that
            // Find out why it doesn't use this at startup
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ModGrid.ItemsSource = DisplayedPackages;
            });
            
        }

        private void Refresh()
        {
            Console.WriteLine($"[INFO] Refreshing...");
            // Create a modlist based on the directories
            if (PackageList == null || PackageList.Count == 0)
            {
                // Get all mod names based on their folder
                foreach (var package in Directory.EnumerateDirectories("Packages"))
                {
                    Metadata m;
                    DisplayedMetadata dm = new DisplayedMetadata();
                    Package p = new Package();
                    try { 
                    if (File.Exists($@"{package}\Package.xml"))
                    {
                        m = new Metadata();
                        using (FileStream streamWriter = File.Open($@"{package}\Package.xml", FileMode.Open))
                        {
                            m = (Metadata)xsp.Deserialize(streamWriter);
                            dm.name = m.name;
                            dm.author = m.author;
                            dm.version = m.version;
                            dm.link = m.link;
                            dm.id = m.id;
                            dm.description = m.description;
                            dm.path = Path.GetFileName(package);
                        }
                        p.name = m.name;
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] No Package.xml found in {package}. Creating barebones Package.xml");
                        p.name = Path.GetFileName(package);
                        dm.name = p.name;
                        dm.path = p.name;
                        m = new Metadata();
                        m.name = p.name;
                        m.id = "";
                        m.author = "";
                        m.version = "";
                        m.link = "";
                        m.description = "";
                        using (FileStream streamWriter = File.Create($@"{package}\Package.xml"))
                        {
                            xsp.Serialize(streamWriter, m);
                        }
                    }
                    Console.WriteLine($"[INFO] Adding {Path.GetFileName(package)}");
                    p.path = Path.GetFileName(package);
                    p.enabled = false;
                    dm.enabled = false;
                    DisplayedPackages.Add(dm);
                    PackageList.Add(p);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for package {package} ({ex.Message})");
                        continue;
                    }
                }
            }
            else
            {
                // Add new mods
                foreach (var package in Directory.EnumerateDirectories("Packages"))
                {
                    Package p = new Package();
                    Metadata n;
                    if (File.Exists($@"{package}\Package.xml"))
                    {
                        try
                        {
                            using (FileStream streamWriter = File.Open($@"{package}\Package.xml", FileMode.Open))
                            {
                                // Call the Deserialize method and cast to the object type.
                                n = (Metadata)xsp.Deserialize(streamWriter);
                            }
                            // Add to Mods if it doesn't exist
                            if (!PackageList.Any(p => p.name == n.name))
                            {
                                DisplayedMetadata dm = new DisplayedMetadata();
                                dm.name = n.name;
                                dm.author = n.author;
                                dm.version = n.version;
                                dm.link = n.link;
                                dm.id = n.id;
                                dm.description = n.description;
                                dm.enabled = false;
                                dm.path = Path.GetFileName(package);

                                p.name = n.name;
                                p.path = Path.GetFileName(package);
                                p.enabled = false;
                                App.Current.Dispatcher.Invoke((Action)delegate
                                {
                                    PackageList.Add(p);
                                    DisplayedPackages.Add(dm);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Invalid Package.xml for {package} ({ex.Message}), skipping...");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] No Package.xml found in {package}. Creating barebones Package.xml");
                        p.name = Path.GetFileName(package);
                        DisplayedMetadata dm = new DisplayedMetadata();
                        dm.name = p.name;
                        dm.path = p.name;
                        n = new Metadata();
                        n.name = p.name;
                        n.author = "";
                        n.version = "";
                        n.link = "";
                        n.description = "";
                        using (FileStream streamWriter = File.Create($@"{package}\Package.xml"))
                        {
                            xsp.Serialize(streamWriter, n);
                        }
                        Console.WriteLine($"[INFO] Adding {Path.GetFileName(package)}");
                        p.path = Path.GetFileName(package);
                        p.enabled = false;
                        dm.enabled = false;
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            PackageList.Add(p);
                            DisplayedPackages.Add(dm);
                        });
                    }
                    
                    
                }
                // Remove mods no longer in directory
                var dirNames = Directory.EnumerateDirectories("Packages");
                foreach (Package package in PackageList.ToList())
                {
                    if (!dirNames.Contains($@"Packages\{package.path}"))
                    {
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            foreach (var dm in DisplayedPackages.ToList())
                            {
                                if (dm.path == package.path)
                                    DisplayedPackages.Remove(dm);
                            }
                            PackageList.Remove(package);
                        }); 
                    }
                }
            }
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ModGrid.ItemsSource = DisplayedPackages;
            });
        }

        private void NewClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] Creating new package!");
            CreatePackage newPackage = new CreatePackage();
            newPackage.ShowDialog();
            if (newPackage.metadata != null)
            {
                string path = $@"Packages\{newPackage.metadata.name}";
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        using (FileStream streamWriter = File.Create($@"{path}\Package.xml"))
                        {
                            xsp.Serialize(streamWriter, newPackage.metadata);
                        }
                        Refresh();
                        checkVersion();
                        updateConfig();
                        ProcessStartInfo StartInformation = new ProcessStartInfo();
                        StartInformation.FileName = path;
                        Process process = Process.Start(StartInformation);
                        Console.WriteLine("[INFO] Opened new package folder.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Couldn't create directory/Package.xml. ({ex.Message})");
                    }
                }
                else
                {
                    Console.WriteLine($"[ERROR] {newPackage.metadata.name} already exists, not creating new package.");
                }
            }
        }

        private async void MergeClick(object sender, RoutedEventArgs e)
        {
            if (!Directory.EnumerateFileSystemEntries("Original").Any())
            {
                Console.WriteLine("[WARNING] Aemulus can't find your Vanilla files in the Original folder.");
                Console.WriteLine("Please click the Config button and select \"Unpack data00004.pac\" before building.");
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    MessageBoxResult result = MessageBox.Show("Aemulus can't find your Vanilla files in the Original folder. Please click the Config button and select \"Unpack data00004.pac\" before building.",
                                          "Aemulus Package Manager",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Exclamation);
                });
                return;
            }

            RefreshButton.IsEnabled = false;
            MergeButton.IsEnabled = false;
            LaunchButton.IsEnabled = false;

            await unpackThenMerge();

            RefreshButton.IsEnabled = true;
            MergeButton.IsEnabled = true;
            LaunchButton.IsEnabled = true;
            
        }

        private async Task unpackThenMerge()
        {
            await Task.Run(() => {
                Refresh();
                checkVersion();
                List<string> packages = new List<string>();
                foreach (Package m in PackageList)
                {
                    if (m.enabled)
                    {
                        packages.Add($@"Packages\{m.path}");
                    }
                }
                packages.Reverse();
                if (packages.Count == 0)
                    Console.WriteLine("[ERROR] No packages to build!");
                else if (!Directory.Exists(modPath))
                    Console.WriteLine("[ERROR] Current output folder doesn't exist! Please select it again.");
                else
                {
                    binMerger.Restart(modPath, emptySND);
                    binMerger.Unpack(packages, modPath);
                    binMerger.Merge(modPath);

                    if (tbl)
                    {
                        tblPatcher.Patch(packages, modPath);
                    }
                       
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MessageBoxResult result = MessageBox.Show("Finished Building!",
                                          "Aemulus Package Manager",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Information);
                    });
                    
                    
                }
            });
        }

        public void updateConfig()
        {
            config.package = PackageList;
            using (FileStream streamWriter = File.Create(@"Config.xml"))
            {
                xs.Serialize(streamWriter, config);
            }
        }

        private void rowSelected(object sender, SelectionChangedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            if (row != null)
            {
                if (row.description != null && row.description.Length > 0)
                    Description.Text = row.description;
                else
                    Description.Text = "Aemulus means \"Rival\" in Latin. It was chosen since it sounds cool. (You are seeing this message because no mod package is selected or the package has no description).";
                string path = $@"Packages\{row.path}";
                if (File.Exists($@"{path}\Preview.png"))
                    Preview.Source = new ImageSourceConverter().ConvertFromString($@"{path}\Preview.png") as ImageSource;
                else
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    Stream iconStream = asm.GetManifestResourceStream("AemulusModManager.Preview.png");
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = iconStream;
                    bitmap.EndInit();
                    Preview.Source = bitmap;
                }
                    
            }
        }

        // Update config order when rows are changed
        private void ModGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DisplayedMetadata dm = (DisplayedMetadata)e.Row.Item;
            foreach (var p in PackageList.ToList())
            {
                if (dm.path == p.path)
                {
                    Package temp = p;
                    PackageList.Remove(p);
                    PackageList.Insert(DisplayedPackages.IndexOf(dm), temp);
                }
            }
            updateConfig();
        }
    }
}