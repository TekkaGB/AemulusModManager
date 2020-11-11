using GongSolutions.Wpf.DragDrop.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private BitmapImage bitmap;

        public DisplayedMetadata InitDisplayedMetadata(Metadata m)
        {
            DisplayedMetadata dm = new DisplayedMetadata();
            dm.name = m.name;
            dm.id = m.id;
            dm.author = m.author;
            Version v;
            if (Version.TryParse(m.version, out v))
                dm.version = m.version;
            dm.description = m.description;
            dm.link = m.link;
            return dm;
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            DisplayedMetadata package = checkBox?.DataContext as DisplayedMetadata;

            if (package != null)
            {
                package.enabled = true;
                foreach (var p in PackageList.ToList())
                {
                    if (p.path == package.path)
                        p.enabled = true;
                }
                updateConfig();
            }
        }

        // Events for Enabled checkboxes
        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            DisplayedMetadata package = checkBox?.DataContext as DisplayedMetadata;

            if (package != null)
            {
                package.enabled = false;
                foreach (var p in PackageList.ToList())
                {
                    if (p.path == package.path)
                        p.enabled = false;
                }
                updateConfig();
            }
        }

        // Hyperlink click event
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

        private TextBoxOutputter outputter = new TextBoxOutputter();

        void consoleWriter_WriteLineEvent(object sender, ConsoleWriterEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                ConsoleOutput.AppendText($"{e.Value}\n");
            });
        }

        void consoleWriter_WriteEvent(object sender, ConsoleWriterEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                ConsoleOutput.AppendText(e.Value);
            });
        }

        // Autoscrolls to end whenever console updates
        private void ScrollToBottom(object sender, TextChangedEventArgs args)
        {
            ConsoleOutput.ScrollToEnd();
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            outputter.WriteEvent += consoleWriter_WriteEvent;
            outputter.WriteLineEvent += consoleWriter_WriteLineEvent;
            Console.SetOut(outputter);

            binMerger = new binMerge();
            tblPatcher = new tblPatch();
            pacUnpacker = new PacUnpacker();
            DisplayedPackages = new ObservableCollection<DisplayedMetadata>();
            PackageList = new ObservableCollection<Package>();

            // Retrieve initial thumbnail from embedded resource
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream iconStream = asm.GetManifestResourceStream("AemulusModManager.Preview.png");
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = iconStream;
            bitmap.EndInit();
            Preview.Source = bitmap;

            // Initialize config
            config = new Config();

            // Initialize xml serializers
            xs = new XmlSerializer(typeof(Config));
            xsp = new XmlSerializer(typeof(Metadata));

            Console.WriteLine("[INFO] Initializing packages from Config.xml");
            // Load in Config if it exists
            if (File.Exists(@"Config.xml"))
            {
                try
                {
                    using (FileStream streamWriter = File.Open(@"Config.xml", FileMode.Open))
                    {
                        // Call the Deserialize method and cast to the object type.
                        config = (Config)xs.Deserialize(streamWriter);
                        reloadedPath = config.reloadedPath;
                        p4gPath = config.exePath;
                        modPath = config.modDir;
                        emptySND = config.emptySND;
                        // Compatibility with old Config.xml
                        List<Package> temp = config.package.ToList();
                        foreach (var p in temp)
                        {
                            if (p.name != null && p.path == null)
                            {
                                p.path = p.name;
                                p.name = null;
                            }
                        }
                        PackageList = new ObservableCollection<Package>(temp);
                        tbl = config.tbl;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid Config.xml ({ex.Message})");
                }

                // Create displayed metadata from packages in PackageList and their respective Package.xml's
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
                            try
                            {
                                using (FileStream streamWriter = File.Open(xml, FileMode.Open))
                                {
                                    m = (Metadata)xsp.Deserialize(streamWriter);
                                    dm.name = m.name;
                                    dm.id = m.id;
                                    dm.author = m.author;
                                    dm.version = m.version;
                                    dm.link = m.link;
                                    dm.description = m.description;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message})");
                            }
                        }

                        dm.path = package.path;
                        dm.enabled = package.enabled;
                        DisplayedPackages.Add(dm);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for package {package.id} ({ex.Message})");
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
                Directory.CreateDirectory("Packages");

            if (!Directory.Exists("Original"))
                Directory.CreateDirectory("Original");

            Refresh();
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
            );
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
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    updateConfig();
                    RefreshButton.IsEnabled = true;
                });
            });
        }

        private void UpdateMetadata()
        {
            // Update metadata
            List<DisplayedMetadata> temp = DisplayedPackages.ToList();
            foreach (var package in temp)
            {
                if (File.Exists($@"Packages\{package.path}\Package.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = File.Open($@"Packages\{package.path}\Package.xml", FileMode.Open))
                        {
                            Metadata metadata = (Metadata)xsp.Deserialize(streamWriter);
                            package.name = metadata.name;
                            package.id = metadata.id;
                            package.author = metadata.author;
                            package.version = metadata.version;
                            package.link = metadata.link;
                            package.description = metadata.description;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message})");
                    }
                }
            }
            DisplayedPackages = new ObservableCollection<DisplayedMetadata>(temp);
        }

        // Refresh both PackageList and DisplayedPackages
        private void Refresh()
        {
            Metadata metadata;
            // First remove all deleted packages and update package id's to match metadata
            foreach (var package in PackageList.ToList())
            {
                if (!Directory.Exists($@"Packages\{package.path}"))
                {
                    PackageList.Remove(package);
                    List<DisplayedMetadata> temp = DisplayedPackages.ToList();
                    temp.RemoveAll(x => x.path == package.path);
                    DisplayedPackages = new ObservableCollection<DisplayedMetadata>(temp);
                }
                if (File.Exists($@"Packages\{package.path}\Package.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = File.Open($@"Packages\{package.path}\Package.xml", FileMode.Open))
                        {
                            metadata = (Metadata)xsp.Deserialize(streamWriter);
                            package.id = metadata.id;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message})");
                    }
                }
            }

            UpdateMetadata();

            // Get all packages from Packages folder (Adding packages)
            foreach (var package in Directory.EnumerateDirectories("Packages"))
            {
                if (File.Exists($@"{package}\Package.xml"))
                {
                    using (FileStream streamWriter = File.Open($@"{package}\Package.xml", FileMode.Open))
                    {
                        metadata = (Metadata)xsp.Deserialize(streamWriter);
                        // Add package to list if it doesn't exist
                        if (!PackageList.ToList().Any(x => x.path == Path.GetFileName(package))
                            && !DisplayedPackages.ToList().Any(x => x.path == Path.GetFileName(package)))
                        {
                            // Add new package to both collections
                            DisplayedMetadata dm = InitDisplayedMetadata(metadata);
                            Package p = new Package();
                            p.enabled = false;
                            p.id = metadata.id;
                            p.path = Path.GetFileName(package);
                            PackageList.Add(p);
                            dm.enabled = false;
                            dm.path = Path.GetFileName(package);
                            DisplayedPackages.Add(dm);
                        }
                    }
                }
                // Create Package.xml
                else
                {
                    Console.WriteLine($"[WARNING] No Package.xml found for {Path.GetFileName(package)}, creating a simple one...");
                    // Create metadata
                    Metadata newMetadata = new Metadata();
                    newMetadata.name = Path.GetFileName(package);
                    newMetadata.id = newMetadata.name.Replace(" ", "").ToLower();
                    newMetadata.author = "";
                    newMetadata.version = "";
                    newMetadata.link = "";
                    newMetadata.description = "";
                    using (FileStream streamWriter = File.Create($@"{package}\Package.xml"))
                    {
                        xsp.Serialize(streamWriter, newMetadata);
                    }
                    if (!PackageList.ToList().Any(x => x.path == Path.GetFileName(package))
                            && !DisplayedPackages.ToList().Any(x => x.path == Path.GetFileName(package)))
                    {
                        // Create package
                        Package newPackage = new Package();
                        newPackage.enabled = false;
                        newPackage.path = Path.GetFileName(package);
                        newPackage.id = newMetadata.id;
                        PackageList.Add(newPackage);
                        // Create displayedmetadata
                        DisplayedMetadata newDisplayedMetadata = InitDisplayedMetadata(newMetadata);
                        newDisplayedMetadata.enabled = false;
                        newDisplayedMetadata.path = newPackage.path;
                        DisplayedPackages.Add(newDisplayedMetadata);
                    }
                    else
                    {
                        UpdateMetadata();
                    }
                }
            }

            // Update DisplayedPackages
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ModGrid.ItemsSource = DisplayedPackages;
                // Trigger select event to refresh description and Preview.png
                ModGrid.SetSelectedItem(ModGrid.GetSelectedItem());
            });
            Console.WriteLine($"[INFO] Refreshed!");
        }

        private void NewClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] Creating new package!");
            CreatePackage newPackage = new CreatePackage(null);
            newPackage.ShowDialog();
            if (newPackage.metadata != null)
            {
                string path;
                if (newPackage.metadata.version != null && newPackage.metadata.version.Length > 0)
                    path = $@"Packages\{newPackage.metadata.name} {newPackage.metadata.version}";
                else
                    path = $@"Packages\{newPackage.metadata.name}";
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        using (FileStream streamWriter = File.Create($@"{path}\Package.xml"))
                        {
                            xsp.Serialize(streamWriter, newPackage.metadata);
                        }
                        if (File.Exists(newPackage.thumbnailPath))
                            File.Copy(newPackage.thumbnailPath, $@"{path}\Preview.png", true);
                        Refresh();
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
            await Task.Run(() =>
            {
                Refresh();
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
                // Set description
                if (row.description != null && row.description.Length > 0)
                {
                    //Description.IsReadOnly = false;
                    Description.Text = row.description;
                }
                else
                {
                    Description.Text = "Aemulus means \"Rival\" in Latin. It was chosen since it sounds cool. (You are seeing this message because no mod package is selected or the package has no description).";
                    //Description.IsReadOnly = true;
                }

                // Set requirement visibility
                if (Directory.Exists($@"Packages\{row.path}\patches"))
                    Inaba.Visibility = Visibility.Visible;
                else
                    Inaba.Visibility = Visibility.Collapsed;
                if (File.Exists($@"Packages\{row.path}\SND\HeeHeeHo.uwus"))
                    HHH.Visibility = Visibility.Visible;
                else
                    HHH.Visibility = Visibility.Collapsed;
                if (Directory.Exists($@"Packages\{row.path}\patches") || File.Exists($@"Packages\{row.path}\SND\HeeHeeHo.uwus"))
                    Reqs.Visibility = Visibility.Visible;
                else
                    Reqs.Visibility = Visibility.Collapsed;

                // Set image
                string path = $@"Packages\{row.path}";
                if (File.Exists($@"{path}\Preview.png"))
                {
                    try
                    {
                        byte[] imageBytes = File.ReadAllBytes($@"{path}\Preview.png");
                        var stream = new MemoryStream(imageBytes);
                        var img = new BitmapImage();

                        img.BeginInit();
                        img.StreamSource = stream;
                        img.EndInit();
                        Preview.Source = img;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] ex.Message");
                    }
                }
                else
                    Preview.Source = bitmap;

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

        private void Inaba_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://gamebanana.com/tools/6872");
        }

        private void HHH_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://gamebanana.com/gamefiles/12806");
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            if (row != null)
            {
                if (Directory.Exists($@"Packages\{row.path}"))
                {
                    try
                    {
                        ProcessStartInfo StartInformation = new ProcessStartInfo();
                        StartInformation.FileName = $@"Packages\{row.path}";
                        Process process = Process.Start(StartInformation);
                        Console.WriteLine($@"[INFO] Opened Packages\{row.path}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"[ERROR] Couldn't open Packages\{row.path} ({ex.Message})");
                    }
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            if (row != null)
            {
                MessageBoxResult result = MessageBox.Show($@"Are you sure you want to delete Packages\{row.path}?",
                                      "Aemulus Package Manager",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Warning);
                if (Directory.Exists($@"Packages\{row.path}") && result == MessageBoxResult.Yes)
                {
                    Console.WriteLine($@"[INFO] Deleted Packages\{row.path}.");
                    try
                    {
                        Directory.Delete($@"Packages\{row.path}", true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"[ERROR] Couldn't delete Packages\{row.path} ({ex.Message})");
                    }
                    Refresh();
                    updateConfig();
                }
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            if (row != null && File.Exists($@"Packages\{row.path}\Package.xml"))
            {
                Metadata m = new Metadata();
                m.name = row.name;
                m.author = row.author;
                m.id = row.id;
                m.version = row.version;
                m.link = row.link;
                m.description = row.description;
                CreatePackage createPackage = new CreatePackage(m);
                createPackage.ShowDialog();
                if (createPackage.metadata != null)
                {
                    try
                    {
                        using (FileStream streamWriter = File.Create($@"Packages\{row.path}\Package.xml"))
                        {
                            xsp.Serialize(streamWriter, createPackage.metadata);
                        }
                        if (File.Exists(createPackage.thumbnailPath))
                        {
                            File.Copy(createPackage.thumbnailPath, $@"Packages\{row.path}\Preview.png", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] {ex.Message}");
                    }
                    Refresh();
                    updateConfig();
                }
            }
        }
    }
}