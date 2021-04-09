﻿using AemulusModManager.Utilities.KT;
using AemulusModManager.Utilities;
using GongSolutions.Wpf.DragDrop.Utilities;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using WpfAnimatedGif;

namespace AemulusModManager
{
    public partial class MainWindow : Window
    {
        public AemulusConfig config;
        public ConfigP3F p3fConfig;
        public ConfigP4G p4gConfig;
        public ConfigP5 p5Config;
        public ConfigP5R p5rConfig;
        public ConfigP5S p5sConfig;
        public Packages packages;
        public string game;
        private XmlSerializer xs;
        private XmlSerializer xp;
        private XmlSerializer xsp;
        private XmlSerializer xsm;
        public string modPath;
        private ObservableCollection<Package> PackageList;
        private ObservableCollection<DisplayedMetadata> DisplayedPackages;
        public bool emptySND;
        public bool useCpk;
        public bool buildWarning;
        public bool buildFinished;
        public bool updateChangelog;
        public bool updateAll;
        public bool updatesEnabled;
        public bool deleteOldVersions;
        public bool fromMain;
        public bool bottomUpPriority;
        public string gamePath;
        public string launcherPath;
        public string elfPath;
        public string cpkLang;
        private BitmapImage bitmap;
        public List<FontAwesome5.ImageAwesome> buttons;
        private PackageUpdater packageUpdater;
        private string aemulusVersion;
        private bool updating = false;
        private CancellationTokenSource cancellationToken;

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
            dm.skippedVersion = m.skippedVersion;
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
                updatePackages();
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
                updatePackages();
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

        private StreamWriter sw;
        private TextBoxOutputter outputter;

        public string infoColor;
        public string warningColor;
        public string errorColor;
        public string normalColor;

        void consoleWriter_WriteLineEvent(object sender, ConsoleWriterEventArgs e)
        {
            string text = (string)e.Value;
            this.Dispatcher.Invoke(() =>
            {
            if (text.StartsWith("[INFO]"))
                ConsoleOutput.AppendText($"[{DateTime.Now}] {text}\n", infoColor);
            else if (text.StartsWith("[WARNING]"))
                ConsoleOutput.AppendText($"[{DateTime.Now}] {text}\n", warningColor);
            else if (text.StartsWith("[ERROR]"))
                ConsoleOutput.AppendText($"[{DateTime.Now}] {text}\n", errorColor);
            else
                ConsoleOutput.AppendText($"[{DateTime.Now}] {text}\n", normalColor);
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

        public MainWindow(bool running, bool oneClick)
        {
            if (!running)
            {
                InitializeComponent();
                DataContext = this;

                sw = new StreamWriter($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\AemulusLog.txt", false, Encoding.UTF8, 4096);
                outputter = new TextBoxOutputter(sw);
                packages = new Packages();

                outputter.WriteEvent += consoleWriter_WriteEvent;
                outputter.WriteLineEvent += consoleWriter_WriteLineEvent;
                Console.SetOut(outputter);

                // Set Aemulus Version
                aemulusVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                var version = aemulusVersion.Substring(0, aemulusVersion.LastIndexOf('.'));
                Title = $"Aemulus Package Manager v{version}";

                infoColor = "#52FF00";
                warningColor = "#FFFF00";
                errorColor = "#FFB0B0";
                normalColor = "#F2F2F2";

                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages");
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original");
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config");

                DisplayedPackages = new ObservableCollection<DisplayedMetadata>();
                PackageList = new ObservableCollection<Package>();

                // Initialise package updater
                packageUpdater = new PackageUpdater(this);

                // Retrieve initial thumbnail from embedded resource
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream iconStream = asm.GetManifestResourceStream("AemulusModManager.Assets.Preview.png");
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = iconStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImageBehavior.SetAnimatedSource(Preview, bitmap);


                // Initialize config
                config = new AemulusConfig();
                p5Config = new ConfigP5();
                p5rConfig = new ConfigP5R();
                p5sConfig = new ConfigP5S();
                p4gConfig = new ConfigP4G();
                p3fConfig = new ConfigP3F();
                config.p4gConfig = p4gConfig;
                config.p3fConfig = p3fConfig;
                config.p5Config = p5Config;
                config.p5rConfig = p5rConfig;
                config.p5sConfig = p5sConfig;

                // Initialize xml serializers
                XmlSerializer oldConfigSerializer = new XmlSerializer(typeof(Config));
                xs = new XmlSerializer(typeof(AemulusConfig));
                xp = new XmlSerializer(typeof(Packages));
                xsp = new XmlSerializer(typeof(Metadata));
                xsm = new XmlSerializer(typeof(ModXmlMetadata));

                buttons = new List<FontAwesome5.ImageAwesome>();
                buttons.Add(NewButton);
                buttons.Add(SwapButton);
                buttons.Add(FolderButton);
                buttons.Add(MergeButton);
                buttons.Add(ConfigButton);
                buttons.Add(LaunchButton);
                buttons.Add(RefreshButton);
                buttons.Add(DarkMode);

                // Load in Config if it exists

                string file = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\Config.xml";
                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\Config.xml") || FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = FileIOWrapper.Open(file, FileMode.Open))
                        {
                            // Call the Deserialize method and cast to the object type.
                            if (file == $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config.xml")
                            {
                                Config oldConfig = (Config)oldConfigSerializer.Deserialize(streamWriter);
                                p4gConfig.reloadedPath = oldConfig.reloadedPath;
                                p4gConfig.exePath = oldConfig.exePath;
                                p4gConfig.modDir = oldConfig.modDir;
                                p4gConfig.emptySND = oldConfig.emptySND;
                                p4gConfig.cpkLang = oldConfig.cpkLang;
                                p4gConfig.useCpk = oldConfig.useCpk;

                                config.p4gConfig = p4gConfig;
                            }
                            else
                                config = (AemulusConfig)xs.Deserialize(streamWriter);
                            game = config.game;
                            if (game != "Persona 4 Golden" && game != "Persona 3 FES" && game != "Persona 5" && game != "Persona 5 Royal" && game != "Persona 5 Strikers")
                            {
                                game = "Persona 4 Golden";
                                config.game = "Persona 4 Golden";
                            }

                            bottomUpPriority = config.bottomUpPriority;

                            if (config.p3fConfig != null)
                                p3fConfig = config.p3fConfig;
                            if (config.p4gConfig != null)
                                p4gConfig = config.p4gConfig;
                            if (config.p5Config != null)
                                p5Config = config.p5Config;
                            if (config.p5rConfig != null)
                                p5rConfig = config.p5rConfig;

                            if (game == "Persona 4 Golden")
                            {
                                // Default
                                if (cpkLang == null)
                                {
                                    cpkLang = "data_e.cpk";
                                    config.p4gConfig.cpkLang = "data_e.cpk";
                                }
                                modPath = config.p4gConfig.modDir;
                                gamePath = config.p4gConfig.exePath;
                                launcherPath = config.p4gConfig.reloadedPath;
                                emptySND = config.p4gConfig.emptySND;
                                cpkLang = config.p4gConfig.cpkLang;
                                useCpk = config.p4gConfig.useCpk;
                                buildWarning = config.p4gConfig.buildWarning;
                                buildFinished = config.p4gConfig.buildFinished;
                                updateChangelog = config.p4gConfig.updateChangelog;
                                updateAll = config.p4gConfig.updateAll;
                                updatesEnabled = config.p4gConfig.updatesEnabled;
                                deleteOldVersions = config.p4gConfig.deleteOldVersions;
                                foreach (var button in buttons)
                                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                            }
                            else if (game == "Persona 3 FES")
                            {
                                modPath = config.p3fConfig.modDir;
                                gamePath = config.p3fConfig.isoPath;
                                elfPath = config.p3fConfig.elfPath;
                                launcherPath = config.p3fConfig.launcherPath;
                                buildWarning = config.p3fConfig.buildWarning;
                                buildFinished = config.p3fConfig.buildFinished;
                                updateChangelog = config.p3fConfig.updateChangelog;
                                updateAll = config.p3fConfig.updateAll;
                                updatesEnabled = config.p3fConfig.updatesEnabled;
                                deleteOldVersions = config.p3fConfig.deleteOldVersions;
                                useCpk = false;
                                ConvertCPK.Visibility = Visibility.Collapsed;
                                foreach (var button in buttons)
                                    button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                            }
                            else if (game == "Persona 5")
                            {
                                modPath = config.p5Config.modDir;
                                gamePath = config.p5Config.gamePath;
                                launcherPath = config.p5Config.launcherPath;
                                buildWarning = config.p5Config.buildWarning;
                                buildFinished = config.p5Config.buildFinished;
                                updateChangelog = config.p5Config.updateChangelog;
                                updateAll = config.p5Config.updateAll;
                                updatesEnabled = config.p5Config.updatesEnabled;
                                deleteOldVersions = config.p5Config.deleteOldVersions;
                                useCpk = false;
                                ConvertCPK.Visibility = Visibility.Collapsed;
                                foreach (var button in buttons)
                                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                            }
                            else if (game == "Persona 5 Royal")
                            {
                                modPath = config.p5rConfig.modDir;
                                gamePath = config.p5rConfig.gamePath;
                                launcherPath = config.p5rConfig.launcherPath;
                                buildWarning = config.p5rConfig.buildWarning;
                                buildFinished = config.p5rConfig.buildFinished;
                                updateChangelog = config.p5rConfig.updateChangelog;
                                updateAll = config.p5rConfig.updateAll;
                                updatesEnabled = config.p5rConfig.updatesEnabled;
                                deleteOldVersions = config.p5rConfig.deleteOldVersions;
                                useCpk = false;
                                ConvertCPK.Visibility = Visibility.Collapsed;
                                foreach (var button in buttons)
                                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                            }
                            else if (game == "Persona 5 Strikers")
                            {
                                modPath = config.p5sConfig.modDir;
                                gamePath = null;
                                launcherPath = null;
                                buildWarning = config.p5sConfig.buildWarning;
                                buildFinished = config.p5sConfig.buildFinished;
                                updateChangelog = config.p5sConfig.updateChangelog;
                                updateAll = config.p5sConfig.updateAll;
                                updatesEnabled = config.p5sConfig.updatesEnabled;
                                deleteOldVersions = config.p5sConfig.deleteOldVersions;
                                useCpk = false;
                                ConvertCPK.Visibility = Visibility.Collapsed;
                                foreach (var button in buttons)
                                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                            }
                        }
                        if (file == $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config.xml")
                            FileIOWrapper.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"Invalid Config.xml ({ex.Message})");
                    }


                    SwitchThemes();

                    Console.WriteLine($"[INFO] Launched Aemulus v{version}!");

                    switch (game)
                    {
                        case "Persona 3 FES":
                            GameBox.SelectedIndex = 0;
                            break;
                        case "Persona 4 Golden":
                            GameBox.SelectedIndex = 1;
                            break;
                        case "Persona 5":
                            GameBox.SelectedIndex = 2;
                            break;
                        case "Persona 5 Royal":
                            GameBox.SelectedIndex = 3;
                            break;
                        case "Persona 5 Strikers":
                            GameBox.SelectedIndex = 4;
                            break;
                    }

                    if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game.Replace(" ", "")}Packages.xml"))
                    {
                        try
                        {
                            using (FileStream streamWriter = FileIOWrapper.Open($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game.Replace(" ", "")}Packages.xml", FileMode.Open))
                            {
                                // Call the Deserialize method and cast to the object type.
                                packages = (Packages)xp.Deserialize(streamWriter);
                                PackageList = packages.packages;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Invalid Packages.xml ({ex.Message})");
                        }
                    }


                    if (!Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}"))
                    {
                        Console.WriteLine($@"[INFO] Creating Packages\{game}");
                        Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}");
                    }

                    // Create displayed metadata from packages in PackageList and their respective Package.xml's
                    foreach (var package in PackageList.ToList())
                    {
                        string xml = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml";
                        Metadata m;
                        DisplayedMetadata dm = new DisplayedMetadata();
                        if (FileIOWrapper.Exists(xml))
                        {
                            m = new Metadata();
                            try
                            {
                                using (FileStream streamWriter = FileIOWrapper.Open(xml, FileMode.Open))
                                {
                                    try
                                    {
                                        m = (Metadata)xsp.Deserialize(streamWriter);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                        continue;
                                    }
                                    dm.name = m.name;
                                    dm.id = m.id;
                                    dm.author = m.author;
                                    dm.version = m.version;
                                    dm.link = m.link;
                                    dm.description = m.description;
                                    dm.skippedVersion = m.skippedVersion;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                continue;
                            }
                        }

                        dm.path = package.path;
                        dm.enabled = package.enabled;
                        DisplayedPackages.Add(dm);
                    }
                    ModGrid.ItemsSource = DisplayedPackages;

                }
                else // No config found
                {
                    game = "Persona 4 Golden";
                    config.game = "Persona 4 Golden";
                    cpkLang = "data_e.cpk";
                    config.p4gConfig.cpkLang = "data_e.cpk";
                    foreach (var button in buttons)
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                }



                if (game == "Persona 4 Golden" && config.p4gConfig.modDir != "" && config.p4gConfig.modDir != null)
                    modPath = config.p4gConfig.modDir;
                else if (game == "Persona 3 FES" && config.p3fConfig.modDir != "" && config.p3fConfig.modDir != null)
                    modPath = config.p3fConfig.modDir;
                else if (game == "Persona 5" && config.p5Config.modDir != "" && config.p5Config.modDir != null)
                    modPath = config.p5Config.modDir;
                else if (game == "Persona 5 Royal" && config.p5rConfig.modDir != "" && config.p5rConfig.modDir != null)
                    modPath = config.p5rConfig.modDir;
                else if (game == "Persona 5 Strikers" && config.p5sConfig.modDir != "" && config.p5sConfig.modDir != null)
                    modPath = config.p5sConfig.modDir;

                if (modPath == "" || modPath == null)
                {
                    MergeButton.IsHitTestVisible = false;
                    MergeButton.Foreground = new SolidColorBrush(Colors.Gray);
                }
                // Create Packages directory if it doesn't exist
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages");
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\Persona 3 FES");
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\Persona 4 Golden");
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\Persona 5");
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\Persona 5 Strikers");
                Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original");

                Refresh();
                updateConfig();
                updatePackages();

                Description.Document = ConvertToFlowDocument("Aemulus means \"Rival\" in Latin. It was chosen since it " +
                    "was made to rival Mod Compendium.\n\n(You are seeing this message because no package is selected or " +
                    "the package has no description.)");

                if (!bottomUpPriority)
                {
                    TopArrow.Visibility = Visibility.Visible;
                    BottomArrow.Visibility = Visibility.Collapsed;
                }
                else
                {

                    TopArrow.Visibility = Visibility.Collapsed;
                    BottomArrow.Visibility = Visibility.Visible;
                }

                LaunchButton.ToolTip = $"Launch {game}";
                if (!oneClick)
                    UpdateAllAsync();
                FileSystemWatcher fileSystemWatcher = new FileSystemWatcher($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}");
                fileSystemWatcher.Filter = "refresh.aem";
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.Created += FileSystemWatcher_Created;
            }

        }
        private static async Task<bool> IsFileReady(string filename)
        {
            var isReady = false;
            await Task.Run(() =>
            {
                if (File.Exists(filename))
                {

                    while (!isReady)
                    {
                        // If the file can be opened for exclusive access it means that the file
                        // is no longer locked by another process.
                        try
                        {
                            using (FileStream inputStream =
                                File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                                isReady = inputStream.Length > 0;
                        }
                        catch (Exception e)
                        {
                            // Check if the exception is related to an IO error.
                            if (e.GetType() == typeof(IOException))
                            {
                                isReady = false;
                            }
                            else
                            {
                                Console.WriteLine($"[ERROR] Couldn't access {filename} ({e.Message})");
                                break;
                            }
                        }
                    }
                }
            });
            return isReady;
        }
        private async void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var game = "";
            if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\refresh.aem"))
            {
                Refresh();
                if (await IsFileReady($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\refresh.aem"))
                {
                    game = FileIOWrapper.ReadAllText($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\refresh.aem");
                    try
                    {
                        FileIOWrapper.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\refresh.aem");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"[ERROR] Couldn't delete refresh.aem ({ex.Message})");
                    }
                }
            }

            if (game != "")
            {                
                var index = -1;
                switch (game)
                {
                    case "Persona 3 FES":
                        index = 0;
                        break;
                    case "Persona 4 Golden":
                        index = 1;
                        break;
                    case "Persona 5":
                        index = 2;
                        break;
                    case "Persona 5 Royal":
                        index = 3;
                        break;
                    case "Persona 5 Strikers":
                        index = 4;
                        break;
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (index != -1)
                    {
                        if (index != GameBox.SelectedIndex)
                            GameBox.SelectedIndex = index;
                        Activate();
                    }
                });
            }
        }

        public Task pacUnpack(string directory)
        {
            return Task.Run(() =>
            {
                if (game == "Persona 4 Golden")
                    PacUnpacker.Unpack(directory, cpkLang);
                else if (game == "Persona 3 FES")
                    PacUnpacker.Unzip(directory);
                else if (game == "Persona 5" || game == "Persona 5 Royal")
                    PacUnpacker.UnpackCPK(directory);
                else if (game == "Persona 5 Strikers")
                    Merger.Backup(directory);

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    foreach (var button in buttons)
                    {
                        button.IsHitTestVisible = true;
                        if (game == "Persona 3 FES")
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                        else if (game == "Persona 4 Golden")
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                        else if (game == "Persona 5 Royal")
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                        else if (game == "Persona 5 Strikers")
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                        else
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                    }
                    ModGrid.IsHitTestVisible = true;
                    GameBox.IsHitTestVisible = true;
                    if (!fromMain && buildFinished)
                    {
                        NotificationBox notification = new NotificationBox("Finished Unpacking!");
                        notification.ShowDialog();
                        Activate();
                    }
                });
                if ((game == "Persona 4 Golden" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{Path.GetFileNameWithoutExtension(cpkLang)}"))
                    || (game == "Persona 3 FES" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\DATA")
                    && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL"))
                    || (game == "Persona 5" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}"))
                    || (game == "Persona 5 Royal" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}"))
                    || (game == "Persona 5 Strikers" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\motor_rsc")))
                    Console.WriteLine($@"[ERROR] Failed to unpack everything from {game}! Please check if you have all prerequisites installed!");
            });
        }

        private void LaunchClick(object sender, RoutedEventArgs e)
        {
            if ((gamePath != "" && gamePath != null && launcherPath != "" && launcherPath != null)
                || (elfPath != "" && elfPath != null && launcherPath != "" && launcherPath != null))
            {
                if (game != "Persona 3 FES")
                    Console.WriteLine($"[INFO] Launching {gamePath} with {launcherPath}");
                else
                    Console.WriteLine($"[INFO] Launching {elfPath} with {launcherPath}");
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.FileName = launcherPath;
                if (!FileIOWrapper.Exists(launcherPath))
                {
                    Console.WriteLine($"[ERROR] Couldn't find {launcherPath}. Please correct the file path in config.");
                    return;
                }
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                if (game == "Persona 4 Golden")
                {
                    if (!FileIOWrapper.Exists(gamePath))
                    {
                        Console.WriteLine($"[ERROR] Couldn't find {gamePath}. Please correct the file path in config.");
                        return;
                    }
                    startInfo.Arguments = $"--launch \"{gamePath}\"";
                }
                else if (game == "Persona 3 FES")
                {
                    string tempElfPath = null, tempGamePath = null;

                    if (p3fConfig.advancedLaunchOptions)
                    {
                        NotificationBox notification = new NotificationBox("Would you like to choose a custom ELF/SLUS to launch with? To use the executable included in the ISO, choose \"No\".", false);
                        notification.ShowDialog();
                        Activate();
                        if (notification.YesNo)
                        {
                            CommonOpenFileDialog tempElfDialog = new CommonOpenFileDialog();
                            tempElfDialog.InitialDirectory = new FileInfo(elfPath).DirectoryName;
                            if (tempElfDialog.ShowDialog() == CommonFileDialogResult.Ok)
                            {
                                tempElfPath = tempElfDialog.FileName;
                            }
                        }
                        notification = new NotificationBox("Would you like to choose a custom ISO to launch with? If you're using HostFS, choose \"No\".", false);
                        notification.ShowDialog();
                        Activate();
                        if (notification.YesNo)
                        {
                            CommonOpenFileDialog tempGameDialog = new CommonOpenFileDialog();
                            tempGameDialog.InitialDirectory = new FileInfo(gamePath).DirectoryName;
                            if (tempGameDialog.ShowDialog() == CommonFileDialogResult.Ok)
                            {
                                tempGamePath = tempGameDialog.FileName;
                            }
                        }

                        // If the user said "No" to both options, we'll fall back to the old behavior
                        // of just launching the ELF selected in the config.
                        if (tempElfPath == null && tempGamePath == null)
                        {
                            tempElfPath = elfPath;
                        }
                    }
                    else
                    {
                        // If the user doesn't want to be prompted for extra options,
                        // just automatically launch the ELF selected in the config.
                        tempElfPath = elfPath;
                    }

                    // Build the PCSX2 launch arguments based on what we've chosen/what's non-null
                    startInfo.Arguments = "--nogui";
                    if (tempElfPath != null)
                    {
                        if (!FileIOWrapper.Exists(tempElfPath))
                        {
                            Console.WriteLine($"[ERROR] Couldn't find {tempElfPath}. Please correct the file path in config.");
                            return;
                        }
                        startInfo.Arguments += $" --elf=\"{tempElfPath}\"";
                    }
                    if (tempGamePath != null)
                    {
                        if (!FileIOWrapper.Exists(tempGamePath))
                        {
                            Console.WriteLine($"[ERROR] Couldn't find {tempGamePath}. Please correct the file path in config.");
                            return;
                        }
                        startInfo.Arguments += $" \"{tempGamePath}\"";
                    }
                }
                else if (game == "Persona 5" || game == "Persona 5 Royal")
                {
                    if (!FileIOWrapper.Exists(gamePath))
                    {
                        Console.WriteLine($"[ERROR] Couldn't find {gamePath}. Please correct the file path in config.");
                        return;
                    }
                    Console.WriteLine($"[INFO] If the game is lagging set the global config to your special config for Persona 5.");
                    startInfo.Arguments = $"--no-gui \"{gamePath}\"";
                }

                foreach (var button in buttons)
                {
                    button.IsHitTestVisible = false;
                    button.Foreground = new SolidColorBrush(Colors.Gray);
                }
                GameBox.IsHitTestVisible = false;
                ModGrid.IsHitTestVisible = false;

            try 
            {
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    //process.WaitForExit(); // Freezes aemulus
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }

            foreach (var button in buttons)
                {
                    button.IsHitTestVisible = true;
                    if (game == "Persona 3 FES")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                    else if (game == "Persona 4 Golden")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                    else if (game == "Persona 5 Royal")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                    else if (game == "Persona 5 Strikers")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                    else
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                }
                ModGrid.IsHitTestVisible = true;
                GameBox.IsHitTestVisible = true;
            }
            else if (game == "Persona 5 Strikers")
                Process.Start("steam://rungameid/1382330/option0");
            else
                Console.WriteLine("[ERROR] Please setup shortcut in config menu.");
        }

        private void ConfigWdwClick(object sender, RoutedEventArgs e)
        {

            if (game == "Persona 4 Golden")
            {
                ConfigWindowP4G cWindow = new ConfigWindowP4G(this) { Owner = this };
                cWindow.DataContext = this;
                cWindow.ShowDialog();
            }
            else if (game == "Persona 3 FES")
            {
                ConfigWindowP3F cWindow = new ConfigWindowP3F(this) { Owner = this };
                cWindow.DataContext = this;
                cWindow.ShowDialog();
            }
            else if (game == "Persona 5")
            {
                ConfigWindowP5 cWindow = new ConfigWindowP5(this) { Owner = this };
                cWindow.DataContext = this;
                cWindow.ShowDialog();
            }
            else if (game == "Persona 5 Royal")
            {
                ConfigWindowP5R cWindow = new ConfigWindowP5R(this) { Owner = this };
                cWindow.DataContext = this;
                cWindow.ShowDialog();
            }
            else if (game == "Persona 5 Strikers")
            {
                ConfigWindowP5S cWindow = new ConfigWindowP5S(this) { Owner = this };
                cWindow.DataContext = this;
                cWindow.ShowDialog();
            }
        }

        private void UpdateMetadata()
        {
            // Update metadata
            List<DisplayedMetadata> temp = DisplayedPackages.ToList();
            foreach (var package in temp)
            {
                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = FileIOWrapper.Open($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml", FileMode.Open))
                        {
                            Metadata metadata = null;
                            try
                            {
                                metadata = (Metadata)xsp.Deserialize(streamWriter);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                continue;
                            }
                            package.name = metadata.name;
                            package.id = metadata.id;
                            package.author = metadata.author;
                            package.version = metadata.version;
                            package.link = metadata.link;
                            package.description = metadata.description;
                            package.skippedVersion = metadata.skippedVersion;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                        continue;
                    }
                }
            }
            DisplayedPackages = new ObservableCollection<DisplayedMetadata>(temp);
        }

        public static void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (FileIOWrapper.Exists(targetFile)) FileIOWrapper.Delete(targetFile);
                    FileIOWrapper.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }

        // Refresh both PackageList and DisplayedPackages
        private void Refresh()
        {
            Metadata metadata;
            // First remove all deleted packages and update package id's to match metadata
            foreach (var package in PackageList.ToList())
            {
                if (!Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}"))
                {
                    PackageList.Remove(package);
                    List<DisplayedMetadata> temp = DisplayedPackages.ToList();
                    temp.RemoveAll(x => x.path == package.path);
                    DisplayedPackages = new ObservableCollection<DisplayedMetadata>(temp);
                }
                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = FileIOWrapper.Open($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml", FileMode.Open))
                        {
                            try
                            {
                                metadata = (Metadata)xsp.Deserialize(streamWriter);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                continue;
                            }
                            package.id = metadata.id;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                        continue;
                    }
                }
            }

            UpdateMetadata();

            // Get all packages from Packages folder (Adding packages)
            foreach (var package in Directory.GetDirectories($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}"))
            {
                if (FileIOWrapper.Exists($@"{package}\Package.xml"))
                {
                    using (FileStream streamWriter = FileIOWrapper.Open($@"{package}\Package.xml", FileMode.Open))
                    {
                        try
                        {
                            metadata = (Metadata)xsp.Deserialize(streamWriter);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Invalid Package.xml for {package} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                            continue;
                        }
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
                    // Create metadata
                    Metadata newMetadata = new Metadata();
                    newMetadata.name = Path.GetFileName(package);
                    newMetadata.id = newMetadata.name.Replace(" ", "").ToLower();


                    List<string> dirFiles = Directory.GetFiles(package).ToList();
                    List<string> dirFolders = Directory.GetDirectories(package, "*", SearchOption.TopDirectoryOnly).ToList();
                    dirFiles = dirFiles.Concat(dirFolders).ToList();
                    if (FileIOWrapper.Exists($@"{package}\Mod.xml") && Directory.Exists($@"{package}\Data"))
                    {
                        Console.WriteLine($"[INFO] Converting {Path.GetFileName(package)} from Mod Compendium structure...");
                        //If mod folder contains Data folder and mod.xml, import mod compendium mod.xml...
                        string modXml = $@"{package}\Mod.xml";
                        using (FileStream streamWriter = FileIOWrapper.Open(modXml, FileMode.Open))
                        {
                            //Deserialize Mod.xml & Use metadata
                            ModXmlMetadata m = null;
                            try
                            {
                                m = (ModXmlMetadata)xsm.Deserialize(streamWriter);
                                newMetadata.id = m.Author.ToLower().Replace(" ", "") + "." + m.Title.ToLower().Replace(" ", "");
                                newMetadata.author = m.Author;
                                newMetadata.version = m.Version;
                                newMetadata.link = m.Url;
                                newMetadata.description = m.Description;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Invalid Mod.xml for {package} ({ex.Message})");
                                continue;
                            }
                        }
                        //Move files out of Data folder
                        string dataDir = $@"{package}\Data";
                        if (Directory.Exists(dataDir))
                        {
                            setAttributesNormal(new DirectoryInfo(dataDir));
                            MoveDirectory(dataDir, $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp");
                            MoveDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp", package);
                        }

                        if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp"))
                        {
                            try
                            {
                                setAttributesNormal(new DirectoryInfo($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp"));
                                DeleteDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($@"[ERROR] Couldn't delete temp ({ex.Message})");
                            }
                        }
                        //Make sure Data folder is gone
                        if (Directory.Exists(dataDir) && !Directory.EnumerateFileSystemEntries(dataDir).Any())
                            DeleteDirectory(dataDir);
                        //Goodbye old friend
                        FileIOWrapper.Delete(modXml);
                    }
                    else
                    {
                        if (game == "Persona 4 Golden" && !(Directory.Exists($@"{package}\data")
                        || Directory.Exists($@"{package}\data") || Directory.Exists($@"{package}\data_e")
                        || Directory.Exists($@"{package}\data_c") || Directory.Exists($@"{package}\data_k")
                        || Directory.Exists($@"{package}\data00000") || Directory.Exists($@"{package}\data00001")
                        || Directory.Exists($@"{package}\data00002") || Directory.Exists($@"{package}\data00003")
                        || Directory.Exists($@"{package}\data00004") || Directory.Exists($@"{package}\data00005")
                        || Directory.Exists($@"{package}\data00006") || Directory.Exists($@"{package}\movie")
                        || Directory.Exists($@"{package}\movie00000") || Directory.Exists($@"{package}\movie00001")
                        || Directory.Exists($@"{package}\movie00002") || Directory.Exists($@"{package}\preappfile")
                        || Directory.Exists($@"{package}\snd") || Directory.Exists($@"{package}\patches")))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                NotificationBox notificationBox = new NotificationBox($"No Package.xml found for {Path.GetFileName(package)}. " +
                                $"This mod has been installed incorrectly.\nPlease check that the packages have data_x, data0000x, snd or patches folders in the root " +
                                $"before building.");
                                notificationBox.ShowDialog();
                                Activate();
                            });
                            // Open the location of the bad package
                            try
                            {
                                ProcessStartInfo StartInformation = new ProcessStartInfo();
                                StartInformation.FileName = package;
                                Process process = Process.Start(StartInformation);
                                Console.WriteLine($@"[INFO] Opened Packages\{game}\{Path.GetFileName(package)}.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($@"[ERROR] Couldn't open Packages\{game}\{Path.GetFileName(package)} ({ex.Message})");
                            }
                        }

                        Console.WriteLine($"[WARNING] No Package.xml found for {Path.GetFileName(package)}, creating one...");
                        newMetadata.author = "";
                        newMetadata.version = "";
                        newMetadata.link = "";
                        newMetadata.description = "";
                    }
                    using (FileStream streamWriter = FileIOWrapper.Create($@"{package}\Package.xml"))
                    {
                        try
                        {
                            xsp.Serialize(streamWriter, newMetadata);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Invalid Package.xml for {package} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                        }
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

            // Remove older versions of the same id
            CheckVersioning();

            // Update DisplayedPackages
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ModGrid.ItemsSource = DisplayedPackages;
                // Trigger select event to refresh description and Preview.png
                ModGrid.SetSelectedItem(ModGrid.GetSelectedItem());
            });
            Console.WriteLine($"[INFO] Refreshed!");
        }

        private static Version Parse(string version)
        {
            if (Version.TryParse(version, out Version result))
                return result;
            else
                return null;
        }

        private void CheckVersioning()
        {
            var latestVersions = DisplayedPackages
                .GroupBy(t => t.id)
                .Select(g => g.OrderByDescending(t => Parse(t.version)) // Order by version, null values are least
                              .ThenByDescending(t => new DirectoryInfo($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{t.path}").LastWriteTime).First()) // Order by time modified if they're the same version
                .ToList();

            // Enable package if older version was enabled
            foreach (var package in latestVersions)
            {
                if (DisplayedPackages.Where(x => x.id == package.id).Any(y => y.enabled))
                    package.enabled = true;
            }

            DisplayedPackages = new ObservableCollection<DisplayedMetadata>(latestVersions);

            // Update PackageList to match DisplayedPackages
            var temp = PackageList.ToList();
            temp.RemoveAll(x => !DisplayedPackages.Select(y => y.path).Contains(x.path));
            PackageList = new ObservableCollection<Package>(temp);

            // Delete older versions if config was set
            if (deleteOldVersions)
            {
                foreach (var package in Directory.GetDirectories($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}"))
                    if (!PackageList.Select(t => t.path).Contains(Path.GetFileName(package)))
                    {
                        try
                        {
                            Console.WriteLine($"[INFO] Deleting {package}...");
                            Directory.Delete(package, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"[ERROR] Couldn't delete {package} ({e.Message})");
                        }
                    }
            }

        }

        private async void RefreshClick(object sender, RoutedEventArgs e)
        {
            foreach (var button in buttons)
            {
                button.IsHitTestVisible = false;
                button.Foreground = new SolidColorBrush(Colors.Gray);
            }
            GameBox.IsHitTestVisible = false;
            ModGrid.IsHitTestVisible = false;
            Refresh();
            updateConfig();
            updatePackages();
            await UpdateAllAsync();
            ModGrid.IsHitTestVisible = true;
            foreach (var button in buttons)
            {
                button.IsHitTestVisible = true;
                if (game == "Persona 3 FES")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                else if (game == "Persona 4 Golden")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                else if (game == "Persona 5 Royal")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                else if (game == "Persona 5 Strikers")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                else
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
            }
            GameBox.IsHitTestVisible = true;
        }

        private void NewClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[INFO] Creating new package!");
            CreatePackage newPackage = new CreatePackage(null);
            newPackage.ShowDialog();
            if (newPackage.metadata != null)
            {
                string path;
                if (DisplayedPackages.Where(p => Version.TryParse(newPackage.metadata.version, out Version version1) && Version.TryParse(p.version, out Version version2) && p.id == newPackage.metadata.id)
                    .Any(x => Version.Parse(x.version) > Version.Parse(newPackage.metadata.version)))
                {
                    Console.WriteLine($"[ERROR] Package ID {newPackage.metadata.id} already exists with a higher version number");
                    return;
                }
                if (newPackage.metadata.version != "" && newPackage.metadata.version.Length > 0)
                    path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{newPackage.metadata.name} {newPackage.metadata.version}";
                else
                    path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{newPackage.metadata.name}";
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        using (FileStream streamWriter = FileIOWrapper.Create($@"{path}\Package.xml"))
                        {
                            try
                            {
                                xsp.Serialize(streamWriter, newPackage.metadata);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($@"[ERROR] Couldn't create {path}\Package.xml. ({ex.Message})");
                            }
                        }
                        if (FileIOWrapper.Exists(newPackage.thumbnailPath))
                        {
                            string extension = Path.GetExtension(newPackage.thumbnailPath).ToLower();
                            FileIOWrapper.Copy(newPackage.thumbnailPath, $@"{path}\Preview{extension}", true);
                        }
                        Refresh();
                        updateConfig();
                        updatePackages();
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

        private string selectExe(string title, string extension)
        {
            string type = "Application";
            if (extension == ".iso")
                type = "PS2 Disc";
            else if (extension == ".bin")
                type = "EBOOT";
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

        private async void MergeClick(object sender, RoutedEventArgs e)
        {
            if ((game == "Persona 4 Golden" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{Path.GetFileNameWithoutExtension(cpkLang)}"))
                    || (game == "Persona 3 FES" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\DATA")
                    && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL"))
                    || (game == "Persona 5" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}"))
                    || (game == "Persona 5 Royal" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}")))
            {
                Console.WriteLine("[WARNING] Aemulus can't find your Base files in the Original folder.");
                Console.WriteLine($"[WARNING] Attempting to unpack/backup base files first.");

                if (gamePath == "" || gamePath == null)
                {
                    string selectedPath;
                    if (game == "Persona 4 Golden")
                    {
                        selectedPath = selectExe("Select P4G.exe to unpack", ".exe");
                        if (selectedPath != null && Path.GetFileName(selectedPath) == "P4G.exe")
                        {
                            gamePath = selectedPath;
                            config.p4gConfig.exePath = gamePath;
                            updateConfig();
                        }
                        else
                            Console.WriteLine("[ERROR] Incorrect file chosen.");
                    }
                    else if (game == "Persona 3 FES")
                    {
                        selectedPath = selectExe("Select P3F's iso to unpack", ".iso");
                        if (selectedPath != null)
                        {
                            gamePath = selectedPath;
                            config.p3fConfig.isoPath = gamePath;
                            updateConfig();
                        }
                        else
                            Console.WriteLine("[ERROR] Incorrect file chosen.");
                    }
                    else if (game == "Persona 5")
                    {
                        selectedPath = selectExe("Select P5's EBOOT.BIN to unpack", ".bin");
                        if (selectedPath != null && Path.GetFileName(selectedPath) == "EBOOT.BIN")
                        {
                            gamePath = selectedPath;
                            config.p5Config.gamePath = gamePath;
                            updateConfig();
                        }
                        else
                            Console.WriteLine("[ERROR] Incorrect file chosen.");
                    }
                    else if (game == "Persona 5 Royal")
                    {
                        selectedPath = selectExe("Select P5R's EBOOT.BIN to unpack", ".bin");
                        if (selectedPath != null && Path.GetFileName(selectedPath) == "EBOOT.BIN")
                        {
                            gamePath = selectedPath;
                            config.p5rConfig.gamePath = gamePath;
                            updateConfig();
                        }
                        else
                            Console.WriteLine("[ERROR] Incorrect file chosen.");
                    }
                }

                if ((gamePath == "" || gamePath == null) && game != "Persona 5 Strikers")
                    return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                });

                foreach (var button in buttons)
                {
                    button.IsHitTestVisible = false;
                    button.Foreground = new SolidColorBrush(Colors.Gray);
                }
                GameBox.IsHitTestVisible = false;
                ModGrid.IsHitTestVisible = false;

                fromMain = true;

                if (game == "Persona 3 FES")
                    await pacUnpack(gamePath);
                else if (game != "Persona 5 Strikers")
                    await pacUnpack(Path.GetDirectoryName(gamePath));
                fromMain = false;

                if ((game == "Persona 4 Golden" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{Path.GetFileNameWithoutExtension(cpkLang)}"))
                    || (game == "Persona 3 FES" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\DATA")
                    && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL"))
                    || (game == "Persona 5" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}"))
                    || (game == "Persona 5 Royal" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}")))
                {
                    Console.WriteLine($@"[ERROR] Failed to unpack everything from {game}! Please check if you have all prerequisites installed!");
                    return;
                }

                
            }

            if (game == "Persona 5 Strikers")
            {
                bool backedUp = true;
                foreach (var file in Merger.original_data)
                {
                    if (!FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\motor_rsc\data\{file}"))
                    {
                        backedUp = false;
                        break;
                    }
                }
                if (!Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\motor_rsc")
                    || !Directory.EnumerateFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\motor_rsc").Any(x => Path.GetExtension(x).ToLower() == ".rdb"))
                {
                    backedUp = false;
                }

                if (!backedUp)
                {
                    Console.WriteLine("[WARNING] Aemulus can't find your Base files in the Original folder.");
                    Console.WriteLine($"[WARNING] Attempting to unpack/backup base files first.");

                    fromMain = true;
                    await pacUnpack(modPath);
                    fromMain = false;

                    foreach (var file in Merger.original_data)
                    {
                        if (!FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\motor_rsc\data\{file}"))
                        {
                            Console.WriteLine($@"[ERROR] Failed to backup {file} from {game}!, cancelling build...");
                            return;
                        }
                    }
                    if (!Directory.EnumerateFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\motor_rsc").Any(x => Path.GetExtension(x).ToLower() == ".rdb"))
                    {
                        Console.WriteLine($@"[ERROR] Failed to backup any rdbs from {game}, cancelling build...");
                        return;
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            foreach (var button in buttons)
            {
                button.IsHitTestVisible = false;
                button.Foreground = new SolidColorBrush(Colors.Gray);
            }
            GameBox.IsHitTestVisible = false;
            ModGrid.IsHitTestVisible = false;

            await unpackThenMerge();

            ModGrid.IsHitTestVisible = true;
            foreach (var button in buttons)
            {
                button.IsHitTestVisible = true;
                if (game == "Persona 3 FES")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                else if (game == "Persona 4 Golden")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                else if (game == "Persona 5 Royal")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                else if (game == "Persona 5 Strikers")
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                else
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
            }
            GameBox.IsHitTestVisible = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Mouse.OverrideCursor = null;
            });
        }

        private async Task unpackThenMerge()
        {
            await Task.Run(() =>
            {
                Refresh();
                if (!Directory.Exists(modPath))
                {
                    Console.WriteLine("[ERROR] Current output folder doesn't exist! Please select it again.");
                    return;
                }
                List<string> packages = new List<string>();
                foreach (Package m in PackageList.ToList())
                {
                    if (m.enabled)
                    {
                        packages.Add($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{m.path}");
                        Console.WriteLine($@"[INFO] Using {m.path} in loadout");
                        if (game == "Persona 4 Golden" && (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{m.path}\{Path.GetFileNameWithoutExtension(cpkLang)}")
                            || Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{m.path}\movie") || Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{m.path}\preappfile")) && !useCpk)
                        {
                            Console.WriteLine($"[WARNING] {m.path} is using CPK folder paths, setting Use CPK Structure to true");
                            useCpk = true;
                        }
                    }
                }
                if (!bottomUpPriority)
                    packages.Reverse();
                if (packages.Count == 0)
                {
                    Console.WriteLine("[WARNING] No packages enabled in loadout, emptying output folder...");
                    string path = modPath;

                    if (game == "Persona 5" || game == "Persona 5 Royal")
                    {
                        path = $@"{modPath}\mod";
                        Directory.CreateDirectory(path);
                    }

                    if (!Directory.EnumerateFileSystemEntries(path).Any() && game != "Persona 5 Strikers")
                    {
                        Console.WriteLine($"[INFO] Output folder already empty");
                        return;
                    }


                    if (buildWarning)
                    {
                        bool YesNo = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Mouse.OverrideCursor = null;
                            NotificationBox notification = new NotificationBox($"Confirm DELETING THE ENTIRE CONTENTS of {path}?", false);
                            if (game == "Persona 5 Strikers")
                                notification = new NotificationBox($"Confirm DELETING THE MODIFIED CONTENTS of {path}?", false);
                            notification.ShowDialog();
                            YesNo = notification.YesNo;
                            Mouse.OverrideCursor = Cursors.Wait;
                        });
                        if (!YesNo)
                        {
                            Console.WriteLine($"[INFO] Cancelled emptying output folder");
                            return;
                        }
                    }

                    if (game != "Persona 5 Strikers")
                        binMerge.Restart(path, emptySND, game, cpkLang);
                    else
                        Merger.Restart(path);
                    Console.WriteLine("[INFO] Finished emptying output folder!");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Mouse.OverrideCursor = null;
                        if (buildWarning)
                        {
                            NotificationBox notification = new NotificationBox("Finished emptying output folder!");
                            notification.ShowDialog();
                            Activate();
                        }
                    });
                    return;
                }
                else
                {
                    string path = modPath;
                    if (game == "Persona 5" || game == "Persona 5 Royal")
                    {
                        path = $@"{modPath}\mod";
                        Directory.CreateDirectory(path);
                    }

                    if (buildWarning && Directory.EnumerateFileSystemEntries(path).Any())
                    {
                        bool YesNo = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Mouse.OverrideCursor = null;
                            NotificationBox notification = new NotificationBox($"Confirm DELETING THE ENTIRE CONTENTS of {path} before building?", false);
                            if (game == "Persona 5 Strikers")
                                notification = new NotificationBox($"Confirm DELETING THE ENTIRE MODIFIED CONTENTS of {path}?", false);
                            notification.ShowDialog();
                            YesNo = notification.YesNo;
                            Mouse.OverrideCursor = Cursors.Wait;
                        });
                        if (!YesNo)
                        {
                            Console.WriteLine($"[INFO] Cancelled build");
                            return;
                        }
                    }

                    if (game != "Persona 5 Strikers")
                    {
                        binMerge.Restart(path, emptySND, game, cpkLang);
                        binMerge.Unpack(packages, path, useCpk, cpkLang, game);
                        binMerge.Merge(path, game);

                        // Only run if tblpatches exists
                        if (packages.Exists(x => Directory.Exists($@"{x}\tblpatches")))
                        {
                            tblPatch.Patch(packages, path, useCpk, cpkLang, game);
                        }

                        // Only run if tblpatches exists
                        if (game == "Persona 4 Golden" && packages.Exists(x => Directory.Exists($@"{x}\preappfile")))
                        {
                            PreappfileAppend.Append(Path.GetDirectoryName(path), cpkLang);
                        }

                        if (game == "Persona 5" || game == "Persona 5 Royal")
                        {
                            binMerge.MakeCpk(path);
                            if (!FileIOWrapper.Exists($@"{modPath}\mod.cpk"))
                                Console.WriteLine("[ERROR] Failed to build mod.cpk!");
                        }

                        if (game == "Persona 4 Golden" && FileIOWrapper.Exists($@"{modPath}\patches\BGME_Base.patch") && FileIOWrapper.Exists($@"{modPath}\patches\BGME_Main.patch"))
                            Console.WriteLine("[WARNING] BGME_Base.patch and BGME_Main.patch found in your patches folder which will result in no music in battles.");
                    }
                    else
                    {
                        Merger.Restart(path);
                        Merger.Merge(packages, path);
                        Merger.Patch(path);
                    }

                    Console.WriteLine("[INFO] Finished Building!");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Mouse.OverrideCursor = null;
                        if (buildFinished)
                        {
                            NotificationBox notification = new NotificationBox("Finished Building!");
                            notification.ShowDialog();
                            Activate();
                        }
                    });
                }
            });
        }

        public void updateConfig()
        {
            using (FileStream streamWriter = FileIOWrapper.Create($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\Config.xml"))
            {
                try
                {
                    xs.Serialize(streamWriter, config);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"[ERROR] Couldn't update Config\Config.xml ({ex.Message})");
                }
            }
        }

        public void updatePackages()
        {
            packages.packages = PackageList;
            using (FileStream streamWriter = FileIOWrapper.Create($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game.Replace(" ", "")}Packages.xml"))
            {
                try
                {
                    xp.Serialize(streamWriter, packages);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"[ERROR] Couldn't update Config\{game.Replace(" ", "")}Packages.xml ({ex.Message})");
                }
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
                    Description.Document = ConvertToFlowDocument(row.description);
                }
                else
                {
                    Description.Document = ConvertToFlowDocument("Aemulus means \"Rival\" in Latin. It was chosen since it " +
                        "was made to rival Mod Compendium.\n\n(You are seeing this message because no package is selected or " +
                        "the package has no description.)");
                }

                // Set requirement visibility
                if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\patches"))
                    Inaba.Visibility = Visibility.Visible;
                else
                    Inaba.Visibility = Visibility.Collapsed;
                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\SND\HeeHeeHo.uwus"))
                    HHH.Visibility = Visibility.Visible;
                else
                    HHH.Visibility = Visibility.Collapsed;
                if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\patches") || FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\SND\HeeHeeHo.uwus"))
                    Reqs.Visibility = Visibility.Visible;
                else
                    Reqs.Visibility = Visibility.Collapsed;

                // Enable/disable convert to 1.4.0
                ConvertCPK.IsEnabled = false;
                foreach (var folder in Directory.GetDirectories($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}"))
                {
                    if (Path.GetFileName(folder).StartsWith("data0") || Path.GetFileName(folder).StartsWith("movie0"))
                    {
                        ConvertCPK.IsEnabled = true;
                    }
                }

                // Enable/disable check for updates
                UpdateItem.IsEnabled = false;
                if (RowUpdatable(row) && !updating && updatesEnabled)
                    UpdateItem.IsEnabled = true;
                // TODO Fix menu not updating if you right click a not selected item

                // Set image
                string path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}";
                FileInfo[] previewFiles = new DirectoryInfo(path).GetFiles("Preview.*");
                if (previewFiles.Length > 0)
                {
                    try
                    {
                        byte[] imageBytes = FileIOWrapper.ReadAllBytes(previewFiles[0].FullName);
                        var stream = new MemoryStream(imageBytes);
                        var img = new BitmapImage();

                        img.BeginInit();
                        img.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        ImageBehavior.SetAnimatedSource(Preview, img);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] {ex.Message}");
                    }
                }
                else
                    ImageBehavior.SetAnimatedSource(Preview, bitmap);

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
            updatePackages();
        }

        private FlowDocument ConvertToFlowDocument(string text)
        {
            var flowDocument = new FlowDocument();

            var regex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Matches(text).Cast<Match>().Select(m => m.Value).ToList();

            var paragraph = new Paragraph();
            flowDocument.Blocks.Add(paragraph);


            foreach (var segment in regex.Split(text))
            {
                if (matches.Contains(segment))
                {
                    var hyperlink = new Hyperlink(new Run(segment))
                    {
                        NavigateUri = new Uri(segment),
                    };

                    hyperlink.RequestNavigate += (sender, args) => Process.Start(segment);

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    paragraph.Inlines.Add(new Run(segment));
                }
            }

            return flowDocument;
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
                if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}"))
                {
                    try
                    {
                        ProcessStartInfo StartInformation = new ProcessStartInfo();
                        StartInformation.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}";
                        Process process = Process.Start(StartInformation);
                        Console.WriteLine($@"[INFO] Opened Packages\{game}\{row.path}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"[ERROR] Couldn't open Packages\{game}\{row.path} ({ex.Message})");
                    }
                }
            }
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            if (row != null)
            {
                NotificationBox notification = new NotificationBox($@"Are you sure you want to delete Packages\{row.path}?", false);
                notification.ShowDialog();
                Activate();
                if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}") && notification.YesNo)
                {
                    Console.WriteLine($@"[INFO] Deleted Packages\{game}\{row.path}.");
                    try
                    {
                        setAttributesNormal(new DirectoryInfo($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}"));
                        DeleteDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"[ERROR] Couldn't delete Packages\{game}\{row.path} ({ex.Message})");
                    }
                    Refresh();
                    updateConfig();
                    updatePackages();
                    Assembly asm = Assembly.GetExecutingAssembly();
                    Stream iconStream = asm.GetManifestResourceStream("AemulusModManager.Assets.Preview.png");
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = iconStream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImageBehavior.SetAnimatedSource(Preview, bitmap);

                    Description.Document = ConvertToFlowDocument("Aemulus means \"Rival\" in Latin. It was chosen since it " +
                        "was made to rival Mod Compendium.\n\n(You are seeing this message because no package is selected or " +
                        "the package has no description.)");
                }
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            if (row != null && FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\Package.xml"))
            {
                Metadata m = new Metadata();
                m.name = row.name;
                m.author = row.author;
                m.id = row.id;
                m.version = row.version;
                m.link = row.link;
                m.description = row.description;
                m.skippedVersion = row.skippedVersion;
                CreatePackage createPackage = new CreatePackage(m);
                createPackage.ShowDialog();
                if (createPackage.metadata != null)
                {
                    try
                    {
                        using (FileStream streamWriter = FileIOWrapper.Create($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\Package.xml"))
                        {
                            try
                            {
                                xsp.Serialize(streamWriter, createPackage.metadata);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($@"[ERROR] Couldn't serialize Packages\{game}\{row.path}\Package.xml ({ex.Message})");
                            }
                        }
                        if (FileIOWrapper.Exists(createPackage.thumbnailPath))
                        {
                            string path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}";
                            FileInfo[] previewFiles = new DirectoryInfo(path).GetFiles("Preview.*");
                            foreach (var p in previewFiles)
                                FileIOWrapper.Delete(p.FullName);
                            string extension = Path.GetExtension(createPackage.thumbnailPath).ToLower();
                            FileIOWrapper.Copy(createPackage.thumbnailPath, $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\Preview{extension}", true);
                        }

                        Refresh();
                        updateConfig();
                        updatePackages();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] {ex.Message}");
                    }
                }
            }
        }

        private async void ZipItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            if (row != null && Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}"))
            {
                var openFolder = new System.Windows.Forms.SaveFileDialog();
                openFolder.FileName = $"{row.path}.7z";
                openFolder.Title = $"Select a file to zip to";
                openFolder.Filter = "7zip | *.7z";
                if (openFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    await ZipItem($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}", openFolder.FileName);
                    ProcessStartInfo StartInformation = new ProcessStartInfo();
                    StartInformation.FileName = Path.GetDirectoryName(openFolder.FileName);
                    Process process = Process.Start(StartInformation);
                }
            }
        }

        private async Task ZipItem(string path, string output)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
                if (!FileIOWrapper.Exists(startInfo.FileName))
                {
                    Console.Write($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                    return;
                }

                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}";
                startInfo.Arguments = $@"a ""{output}"" ""{Path.GetFileName(path)}/*""";
                Console.WriteLine($@"[INFO] Zipping {path} into {output}\{Path.GetFileName(path)}.7z");
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }

            });
        }

        private void ConvertCPK_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            foreach (var folder in Directory.GetDirectories($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}"))
            {
                if (Path.GetFileName(folder).StartsWith("data0"))
                    MoveDirectory(folder, $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\{Path.GetFileNameWithoutExtension(cpkLang)}");
                else if (Path.GetFileName(folder).StartsWith("movie0"))
                    MoveDirectory(folder, $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\movie");
            }
            // Convert the mods.aem file too
            if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\mods.aem"))
            {
                string text = FileIOWrapper.ReadAllText($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\mods.aem");
                text = Regex.Replace(text, "data0000[0-6]", Path.GetFileNameWithoutExtension(cpkLang));
                FileIOWrapper.WriteAllText($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}\mods.aem", text);
            }
        }

        private void ModGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space && ModGrid.CurrentColumn.Header.ToString() != "Enabled")
            {
                var checkbox = ModGrid.Columns[0].GetCellContent(ModGrid.SelectedItem) as CheckBox;
                checkbox.IsChecked = !checkbox.IsChecked;
            }
        }

        private void GameBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameBox.SelectedIndex != -1 && IsLoaded)
            {
                int index = GameBox.SelectedIndex;
                game = null;
                switch (index)
                {
                    case 0:
                        game = "Persona 3 FES";
                        modPath = config.p3fConfig.modDir;
                        gamePath = config.p3fConfig.isoPath;
                        elfPath = config.p3fConfig.elfPath;
                        launcherPath = config.p3fConfig.launcherPath;
                        buildWarning = config.p3fConfig.buildWarning;
                        buildFinished = config.p3fConfig.buildFinished;
                        updateChangelog = config.p3fConfig.updateChangelog;
                        updateAll = config.p3fConfig.updateAll;
                        updatesEnabled = config.p3fConfig.updatesEnabled;
                        deleteOldVersions = config.p3fConfig.deleteOldVersions;
                        useCpk = false;
                        ConvertCPK.Visibility = Visibility.Collapsed;
                        foreach (var button in buttons)
                        {
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                            button.IsHitTestVisible = true;
                        }
                        break;
                    case 1:
                        game = "Persona 4 Golden";
                        modPath = config.p4gConfig.modDir;
                        gamePath = config.p4gConfig.exePath;
                        launcherPath = config.p4gConfig.reloadedPath;
                        emptySND = config.p4gConfig.emptySND;
                        cpkLang = config.p4gConfig.cpkLang;
                        useCpk = config.p4gConfig.useCpk;
                        buildWarning = config.p4gConfig.buildWarning;
                        buildFinished = config.p4gConfig.buildFinished;
                        updateChangelog = config.p4gConfig.updateChangelog;
                        updateAll = config.p4gConfig.updateAll;
                        updatesEnabled = config.p4gConfig.updatesEnabled;
                        deleteOldVersions = config.p4gConfig.deleteOldVersions;
                        ConvertCPK.Visibility = Visibility.Visible;
                        foreach (var button in buttons)
                        {
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                            button.IsHitTestVisible = true;
                        }
                        break;
                    case 2:
                        game = "Persona 5";
                        modPath = config.p5Config.modDir;
                        gamePath = config.p5Config.gamePath;
                        launcherPath = config.p5Config.launcherPath;
                        buildWarning = config.p5Config.buildWarning;
                        buildFinished = config.p5Config.buildFinished;
                        updateChangelog = config.p5Config.updateChangelog;
                        updateAll = config.p5Config.updateAll;
                        updatesEnabled = config.p5Config.updatesEnabled;
                        deleteOldVersions = config.p5Config.deleteOldVersions;
                        useCpk = false;
                        ConvertCPK.Visibility = Visibility.Collapsed;
                        foreach (var button in buttons)
                        {
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                            button.IsHitTestVisible = true;
                        }
                        break;
                    case 3:
                        game = "Persona 5 Royal";
                        modPath = config.p5rConfig.modDir;
                        gamePath = config.p5rConfig.gamePath;
                        launcherPath = config.p5rConfig.launcherPath;
                        buildWarning = config.p5rConfig.buildWarning;
                        buildFinished = config.p5rConfig.buildFinished;
                        updateChangelog = config.p5rConfig.updateChangelog;
                        updateAll = config.p5rConfig.updateAll;
                        updatesEnabled = config.p5rConfig.updatesEnabled;
                        deleteOldVersions = config.p5rConfig.deleteOldVersions;
                        useCpk = false;
                        ConvertCPK.Visibility = Visibility.Collapsed;
                        foreach (var button in buttons)
                        {
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                            button.IsHitTestVisible = true;
                        }
                        break;
                    case 4:
                        game = "Persona 5 Strikers";
                        if (config.p5sConfig == null)
                            config.p5sConfig = new ConfigP5S();
                        modPath = config.p5sConfig.modDir;
                        gamePath = null;
                        launcherPath = null;
                        buildWarning = config.p5sConfig.buildWarning;
                        buildFinished = config.p5sConfig.buildFinished;
                        updateChangelog = config.p5sConfig.updateChangelog;
                        updateAll = config.p5sConfig.updateAll;
                        updatesEnabled = config.p5sConfig.updatesEnabled;
                        deleteOldVersions = config.p5sConfig.deleteOldVersions;
                        useCpk = false;
                        ConvertCPK.Visibility = Visibility.Collapsed;
                        foreach (var button in buttons)
                        {
                            button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                            button.IsHitTestVisible = true;
                        }
                        break;
                }
                Reqs.Visibility = Visibility.Collapsed;
                HHH.Visibility = Visibility.Collapsed;
                Inaba.Visibility = Visibility.Collapsed;
                config.game = game;
                if (modPath == "" || modPath == null)
                {
                    MergeButton.IsHitTestVisible = false;
                    MergeButton.Foreground = new SolidColorBrush(Colors.Gray);
                }
                LaunchButton.ToolTip = $"Launch {game}";
                if (!Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}"))
                {
                    Console.WriteLine($@"[INFO] Creating Packages\{game}");
                    Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}");
                }
                Console.WriteLine($"[INFO] Game set to {game}.");

                if (!Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}"))
                {
                    Console.WriteLine($@"[INFO] Creating Packages\{game}");
                    Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}");
                }

                PackageList.Clear();
                DisplayedPackages.Clear();

                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game.Replace(" ", "")}Packages.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = FileIOWrapper.Open($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game.Replace(" ", "")}Packages.xml", FileMode.Open))
                        {
                            try
                            {
                                // Call the Deserialize method and cast to the object type.
                                packages = (Packages)xp.Deserialize(streamWriter);
                                PackageList = packages.packages;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($@"[ERROR] Couldn't deseralize Config\{game.Replace(" ", "")}Packages.xml ({ex.Message})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Invalid Packages.xml ({ex.Message})");
                    }
                }

                // Create displayed metadata from packages in PackageList and their respective Package.xml's
                foreach (var package in PackageList.ToList())
                {
                    string xml = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml";
                    Metadata m;
                    DisplayedMetadata dm = new DisplayedMetadata();
                    if (FileIOWrapper.Exists(xml))
                    {
                        m = new Metadata();
                        try
                        {
                            using (FileStream streamWriter = FileIOWrapper.Open(xml, FileMode.Open))
                            {
                                try
                                {
                                    m = (Metadata)xsp.Deserialize(streamWriter);
                                    dm.name = m.name;
                                    dm.id = m.id;
                                    dm.author = m.author;
                                    dm.version = m.version;
                                    dm.link = m.link;
                                    dm.description = m.description;
                                    dm.skippedVersion = m.skippedVersion;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path}. ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path}. ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                            continue;
                        }
                    }

                    dm.path = package.path;
                    dm.enabled = package.enabled;
                    DisplayedPackages.Add(dm);
                }
                ModGrid.ItemsSource = DisplayedPackages;

                Refresh();
                updateConfig();
                updatePackages();

                // Retrieve initial thumbnail from embedded resource
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream iconStream = asm.GetManifestResourceStream("AemulusModManager.Assets.Preview.png");
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = iconStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImageBehavior.SetAnimatedSource(Preview, bitmap);

                Description.Document = ConvertToFlowDocument("Aemulus means \"Rival\" in Latin. It was chosen since it " +
                    "was made to rival Mod Compendium.\n\n(You are seeing this message because no package is selected or " +
                    "the package has no description.)");
            }


        }

        private void Kofi_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://www.ko-fi.com/tekka");
        }

        private void Setup_Click(object sender, MouseButtonEventArgs e)
        {
            if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Aemulus_Setup.pdf"))
                Process.Start($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Aemulus_Setup.pdf");
            else
                Console.WriteLine("[ERROR] Aemulus_Setup.pdf not found.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (updating)
            {
                NotificationBox notification = new NotificationBox("There are currently running updates\nAre you sure you want to exit?", false);
                notification.ShowDialog();
                notification.Activate();
                if (!notification.YesNo)
                {
                    e.Cancel = true;
                    return;
                }
                else
                {
                    cancellationToken.Cancel();
                }
            }
            outputter.Close();
            Application.Current.Shutdown();
        }

        private void FolderButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}"))
            {
                try
                {
                    ProcessStartInfo StartInformation = new ProcessStartInfo();
                    StartInformation.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}";
                    Process process = Process.Start(StartInformation);
                    Console.WriteLine($@"[INFO] Opened Packages\{game}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($@"[ERROR] Couldn't open Packages\{game} ({ex.Message})");
                }
            }
        }

        private void SwapButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            bottomUpPriority = !bottomUpPriority;
            if (!bottomUpPriority)
            {
                TopArrow.Visibility = Visibility.Visible;
                BottomArrow.Visibility = Visibility.Collapsed;
            }
            else
            {

                TopArrow.Visibility = Visibility.Collapsed;
                BottomArrow.Visibility = Visibility.Visible;
            }

            config.bottomUpPriority = bottomUpPriority;
            updateConfig();
            for (int i = 0; i < DisplayedPackages.Count; i++)
            {
                DisplayedPackages.Move(DisplayedPackages.Count - 1, i);
                PackageList.Move(PackageList.Count - 1, i);
            }
            updatePackages();
            Console.WriteLine("[INFO] Switched priority.");

        }

        private void MouseEnterColorChange(object sender, MouseEventArgs e)
        {
            var button = e.OriginalSource as FontAwesome5.ImageAwesome;
            if (button.IsHitTestVisible)
            {
                switch (game)
                {
                    case "Persona 3 FES":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0x52, 0x80));
                        break;
                    case "Persona 4 Golden":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x77, 0x1a));
                        break;
                    case "Persona 5":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x00, 0x00));
                        break;
                    case "Persona 5 Royal":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x00, 0x5d));
                        break;
                    case "Persona 5 Strikers":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x1c, 0x00));
                        break;
                }
            }
        }

        private void MouseLeaveColorChange(object sender, MouseEventArgs e)
        {
            var button = e.OriginalSource as FontAwesome5.ImageAwesome;
            if (button.IsHitTestVisible)
            {
                switch (game)
                {
                    case "Persona 3 FES":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                        break;
                    case "Persona 4 Golden":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                        break;
                    case "Persona 5":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                        break;
                    case "Persona 5 Royal":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                        break;
                    case "Persona 5 Strikers":
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                        break;
                }
            }
        }

        public void setAttributesNormal(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                setAttributesNormal(subDir);
                subDir.Attributes = FileAttributes.Normal;
            }
            foreach (var file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }
        }

        public static void DeleteDirectory(string path)
        {

            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }
            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        private void Add_Enter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = DragDropEffects.Move;
        }

        private async Task ExtractPackages(string[] fileList)
        {
            await Task.Run(() =>
            {
                bool dropped = false;
                foreach (var file in fileList)
                {
                    if (Directory.Exists(file))
                    {
                        Console.WriteLine($@"[INFO] Moving {file} into Packages\{game}");
                        string path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileName(file)}";
                        int index = 2;
                        while (Directory.Exists(path))
                        {
                            path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileName(file)} ({index})";
                            index += 1;
                        }
                        MoveDirectory(file, path);
                        dropped = true;
                    }
                    else if (Path.GetExtension(file).ToLower() == ".7z" || Path.GetExtension(file).ToLower() == ".rar" || Path.GetExtension(file).ToLower() == ".zip")
                    {
                        Directory.CreateDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp");
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.CreateNoWindow = true;
                        startInfo.FileName = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Dependencies\7z\7z.exe";
                        if (!FileIOWrapper.Exists(startInfo.FileName))
                        {
                            Console.Write($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                            return;
                        }

                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.UseShellExecute = false;
                        startInfo.Arguments = $@"x -y ""{file}"" -o{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp";
                        Console.WriteLine($@"[INFO] Extracting {file} into Packages\{game}");
                        using (Process process = new Process())
                        {
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();
                        }
                        // Put in folder if extraction comes in multiple files/folders
                        if (Directory.GetFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp").Length > 1)
                        {
                            setAttributesNormal(new DirectoryInfo($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp"));
                            string path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileNameWithoutExtension(file)}";
                            int index = 2;
                            while (Directory.Exists(path))
                            {
                                path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileNameWithoutExtension(file)} ({index})";
                                index += 1;
                            }
                            MoveDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp", path);
                        }
                        // Move folder if extraction is just a folder
                        else if (Directory.GetFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp").Length == 1 && Directory.Exists(Directory.GetFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp")[0]))
                        {
                            setAttributesNormal(new DirectoryInfo($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp"));
                            string path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileName(Directory.GetFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp")[0])}";
                            int index = 2;
                            while (Directory.Exists(path))
                            {
                                path = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{Path.GetFileName(Directory.GetFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp")[0])} ({index})";
                                index += 1;
                            }
                            MoveDirectory(Directory.GetFileSystemEntries($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp")[0], path);
                        }
                        //FileIOWrapper.Delete(file);
                        dropped = true;
                    }
                    else if (Path.GetFileName(file) == $"{game.Replace(" ", "")}Packages.xml")
                    {
                        try
                        {
                            packages = new Packages();
                            DisplayedPackages = new ObservableCollection<DisplayedMetadata>();
                            PackageList = new ObservableCollection<Package>();

                            using (FileStream streamWriter = FileIOWrapper.Open(file, FileMode.Open))
                            {
                                // Call the Deserialize method and cast to the object type.
                                packages = (Packages)xp.Deserialize(streamWriter);
                                PackageList = packages.packages;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Invalid Packages.xml ({ex.Message})");
                        }

                        // Create displayed metadata from packages in PackageList and their respective Package.xml's
                        foreach (var package in PackageList)
                        {
                            string xml = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml";
                            Metadata m;
                            DisplayedMetadata dm = new DisplayedMetadata();
                            if (FileIOWrapper.Exists(xml))
                            {
                                m = new Metadata();
                                try
                                {
                                    using (FileStream streamWriter = FileIOWrapper.Open(xml, FileMode.Open))
                                    {
                                        try
                                        {
                                            m = (Metadata)xsp.Deserialize(streamWriter);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                            continue;
                                        }
                                        dm.name = m.name;
                                        dm.id = m.id;
                                        dm.author = m.author;
                                        dm.version = m.version;
                                        dm.link = m.link;
                                        dm.description = m.description;
                                        dm.skippedVersion = m.skippedVersion;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                    continue;
                                }
                            }
                            dm.path = package.path;
                            dm.enabled = package.enabled;
                            DisplayedPackages.Add(dm);
                        }
                        dropped = true;
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] {file} isn't a folder, .zip, .7z, or .rar, or {game.Replace(" ", "")}Packages.xml, skipping...");
                    }
                }
                if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp"))
                    DeleteDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp");

                if (dropped)
                {
                    Refresh();
                    updatePackages();
                }
            });
        }

        private async void Add_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                });

                foreach (var button in buttons)
                {
                    button.IsHitTestVisible = false;
                    button.Foreground = new SolidColorBrush(Colors.Gray);
                }
                GameBox.IsHitTestVisible = false;
                ModGrid.IsHitTestVisible = false;

                await ExtractPackages(fileList);

                ModGrid.IsHitTestVisible = true;
                foreach (var button in buttons)
                {
                    button.IsHitTestVisible = true;
                    if (game == "Persona 3 FES")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                    else if (game == "Persona 4 Golden")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                    else if (game == "Persona 5 Royal")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x5d));
                    else if (game == "Persona 5 Strikers")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                    else
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                }
                GameBox.IsHitTestVisible = true;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
            }
        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }

            ContextMenu contextMenu = element.ContextMenu;
            if (ModGrid.SelectedItem == null)
                element.ContextMenu.Visibility = Visibility.Collapsed;
            else
                element.ContextMenu.Visibility = Visibility.Visible;
        }

        private void UpdateItem_Click(object sender, RoutedEventArgs e)
        {
            DisplayedMetadata row = (DisplayedMetadata)ModGrid.SelectedItem;
            UpdateItemAsync(row);
        }

        private async Task UpdateItemAsync(DisplayedMetadata row)
        {
            if (!updatesEnabled)
            {
                return;
            }
            cancellationToken = new CancellationTokenSource();
            updating = true;
            Console.WriteLine($"[INFO] Checking for updates for {row.name}");
            await packageUpdater.CheckForUpdate(new DisplayedMetadata[] { row }, game, cancellationToken);
            updating = false;
            Refresh();
            updateConfig();
            updatePackages();
        }

        private async Task UpdateAllAsync()
        {
            if (updating)
            {
                Console.WriteLine($"[INFO] Packages are already being updated, ignoring request to check for updates");
                return;
            }
            if (config.updateAemulus)
                await UpdateAemulus();
            if (!updatesEnabled)
            {
                return;
            }
            if (updateAll)
            {
                updating = true;
                cancellationToken = new CancellationTokenSource();
                Console.WriteLine($"[INFO] Checking for updates for all applicable packages");
                DisplayedMetadata[] updatableRows = DisplayedPackages.Where(RowUpdatable).ToArray();
                await packageUpdater.CheckForUpdate(updatableRows, game, cancellationToken);
                updating = false;
                Refresh();
                updateConfig();
                updatePackages();
            }
        }

        private async Task UpdateAemulus()
        {
            updating = true;
            cancellationToken = new CancellationTokenSource();
            Console.WriteLine($"[INFO] Checking for updates for Aemulus");
            if (await packageUpdater.CheckForAemulusUpdate(aemulusVersion, cancellationToken))
            {
                updating = false;
                // Restart the application
                Close();
            }
            updating = false;
        }

        private bool RowUpdatable(DisplayedMetadata row)
        {
            if (row.link == "")
                return false;
            if (row.skippedVersion != null && row.skippedVersion == "all")
                return false;
            string host = UrlConverter.Convert(row.link);
            return (host == "GameBanana" || host == "GitHub") && row.version != "";
        }

        private void SwitchThemes()
        {
            BrushConverter bc = new BrushConverter();
            SolidColorBrush darkModeBackground = (SolidColorBrush)(bc.ConvertFrom("#202020"));
            SolidColorBrush darkModeForeground = (SolidColorBrush)(bc.ConvertFrom("#f2f2f2"));
            SolidColorBrush darkModeForeground2 = (SolidColorBrush)(bc.ConvertFrom("#9c9c9c"));
            SolidColorBrush lightModeBackground = new SolidColorBrush(Colors.White);
            SolidColorBrush lightModeBackground2 = (SolidColorBrush)(bc.ConvertFrom("#f2f2f2"));
            SolidColorBrush lightModeForeground = (SolidColorBrush)(bc.ConvertFrom("#121212"));
            if (!config.darkMode)
            {
                infoColor = "#046300";
                warningColor = "#764E00";
                errorColor = "#AE1300";
                normalColor = "Black";
                ModGrid.Background = lightModeBackground;
                ModGrid.Foreground = lightModeForeground;
                ModGrid.RowBackground = lightModeBackground;
                ModGrid.AlternatingRowBackground = lightModeBackground2;
                ConsoleOutput.Background = lightModeBackground;
                Description.Background = lightModeBackground;
                Description.Foreground = lightModeForeground;
                SupportIcon.Foreground = lightModeForeground;
                SupportText.Foreground = lightModeForeground;
                SetupIcon.Foreground = lightModeForeground;
                SetupText.Foreground = lightModeForeground;
                Extras.Background = lightModeBackground2;
                PriorityBox.Background = lightModeBackground2;
                PriorityText.Foreground = lightModeForeground;
                TopArrow.Foreground = lightModeForeground;
                BottomArrow.Foreground = lightModeForeground;
                DarkMode.ToolTip = "Switch to Dark Mode";
            }
            else
            {
                infoColor = "#52FF00";
                warningColor = "#FFFF00";
                errorColor = "#FFB0B0";
                normalColor = "#F2F2F2";
                ModGrid.Background = darkModeBackground;
                ModGrid.Foreground = darkModeForeground;
                ModGrid.RowBackground = darkModeBackground;
                ModGrid.AlternatingRowBackground = (SolidColorBrush)(bc.ConvertFrom("#2a2a2a"));
                ConsoleOutput.Background = darkModeBackground;
                Description.Background = darkModeBackground;
                Description.Foreground = darkModeForeground;
                SupportIcon.Foreground = darkModeForeground2;
                SupportText.Foreground = darkModeForeground2;
                SetupIcon.Foreground = darkModeForeground2;
                SetupText.Foreground = darkModeForeground2;
                Extras.Background = darkModeBackground;
                PriorityBox.Background = darkModeBackground;
                PriorityText.Foreground = darkModeForeground2;
                TopArrow.Foreground = darkModeForeground2;
                BottomArrow.Foreground = darkModeForeground2;
                DarkMode.ToolTip = "Switch to Light Mode";
            }
        }

        private void LightMode_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            config.darkMode = !config.darkMode;
            updateConfig();
            SwitchThemes();
            if (!config.darkMode)
                Console.WriteLine("[INFO] Switched to light mode.");
            else
                Console.WriteLine("[INFO] Switched to dark mode.");
        }
    }
}