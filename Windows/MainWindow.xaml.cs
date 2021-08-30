using AemulusModManager.Utilities.KT;
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
using Newtonsoft.Json;
using TypeFilter = AemulusModManager.Utilities.TypeFilter;
using System.Net.Http;
using System.Windows.Controls.Primitives;
using AemulusModManager.Utilities.PackageUpdating;
using Vlc.DotNet.Core;
using AemulusModManager.Windows;
using AemulusModManager.Utilities.Windows;
using System.ComponentModel;
using AemulusModManager.Utilities.FileMerging;

namespace AemulusModManager
{
    public partial class MainWindow : Window
    {
        public AemulusConfig config;
        public ConfigP3F p3fConfig;
        public ConfigP4G p4gConfig;
        public ConfigP5 p5Config;
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
        private ObservableCollection<ComboBoxItem> LoadoutItems;
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
        public string selectedLoadout;
        private BitmapImage bitmap;
        public List<FontAwesome5.ImageAwesome> buttons;
        private PackageUpdater packageUpdater;
        private string aemulusVersion;
        private bool updating = false;
        private CancellationTokenSource cancellationToken;
        private Loadouts loadoutUtils;
        private string lastLoadout;
        private string lastGame;
        public Prop<bool> showHidden { get; set; }

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
                Title = $"Aemulus Package Manager";

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

                // Retrieve initial thumbnail from resource
                bitmap = new BitmapImage(new Uri("pack://application:,,,/AemulusPackageManager;component/Assets/Preview.png"));
                ImageBehavior.SetAnimatedSource(Preview, bitmap);

                // Initialize config
                config = new AemulusConfig();
                p5Config = new ConfigP5();
                p5sConfig = new ConfigP5S();
                p4gConfig = new ConfigP4G();
                p3fConfig = new ConfigP3F();
                config.p4gConfig = p4gConfig;
                config.p3fConfig = p3fConfig;
                config.p5Config = p5Config;
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
                buttons.Add(VisibilityButton);
                buttons.Add(EditLoadoutButton);

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
                            if (game != "Persona 4 Golden" && game != "Persona 3 FES" && game != "Persona 5" && game != "Persona 5 Strikers")
                            {
                                game = "Persona 4 Golden";
                                config.game = "Persona 4 Golden";
                            }
                            lastGame = config.game;

                            bottomUpPriority = config.bottomUpPriority;

                            if (config.p3fConfig != null)
                                p3fConfig = config.p3fConfig;
                            if (config.p4gConfig != null)
                                p4gConfig = config.p4gConfig;
                            if (config.p5Config != null)
                                p5Config = config.p5Config;

                            if (game == "Persona 4 Golden")
                            {
                                // Default
                                if (cpkLang == null)
                                {
                                    cpkLang = "data_e.cpk";
                                    config.p4gConfig.cpkLang = "data_e.cpk";
                                }
                                modPath = config.p4gConfig.modDir;
                                selectedLoadout = config.p4gConfig.loadout;
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
                                selectedLoadout = config.p3fConfig.loadout;
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
                                selectedLoadout = config.p5Config.loadout;
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
                            else if (game == "Persona 5 Strikers")
                            {
                                modPath = config.p5sConfig.modDir;
                                selectedLoadout = config.p5sConfig.loadout;
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
                    catch (Exception)
                    {
                    }

                    if (config.p4gConfig == null)
                        config.p4gConfig = p4gConfig;
                    if (config.p3fConfig == null)
                        config.p3fConfig = p3fConfig;
                    if (config.p5Config == null)
                        config.p5Config = p5Config;
                    if (config.p5sConfig == null)
                        config.p5sConfig = p5sConfig;

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
                        case "Persona 5 Strikers":
                            GameBox.SelectedIndex = 3;
                            break;
                    }

                    // Initialise loadouts
                    loadoutUtils = new Loadouts(game);
                    loadoutHandled = true;
                    LoadoutBox.ItemsSource = loadoutUtils.LoadoutItems;

                    if (LoadoutBox.Items.Contains(selectedLoadout))
                    {
                        LoadoutBox.SelectedItem = selectedLoadout;
                    }
                    else
                    {
                        LoadoutBox.SelectedIndex = 0;
                    }
                    loadoutHandled = false;

                    lastLoadout = LoadoutBox.SelectedItem.ToString();

                    // Update the config
                    selectedLoadout = LoadoutBox.SelectedItem.ToString();
                    switch (game)
                    {
                        case "Persona 3 FES":
                            config.p3fConfig.loadout = selectedLoadout;
                            break;
                        case "Persona 4 Golden":
                            config.p4gConfig.loadout = selectedLoadout;
                            break;
                        case "Persona 5":
                            config.p5Config.loadout = selectedLoadout;
                            break;
                        case "Persona 5 Strikers":
                            config.p5sConfig.loadout = selectedLoadout;
                            break;
                    }
                    updateConfig();

                    // Load the current loadout
                    if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{LoadoutBox.SelectedItem}.xml"))
                    {
                        try
                        {
                            using (FileStream streamWriter = FileIOWrapper.Open($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{LoadoutBox.SelectedItem}.xml", FileMode.Open))
                            {
                                // Call the Deserialize method and cast to the object type.
                                packages = (Packages)xp.Deserialize(streamWriter);
                                PackageList = packages.packages;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Invalid package loadout {LoadoutBox.SelectedItem}.xml ({ex.Message})");
                        }
                    }

                    showHidden = new Prop<bool>();
                    showHidden.Value = packages.showHiddenPackages;

                    VisibilityButton.DataContext = showHidden;
                    ShowHiddenText.DataContext = showHidden;

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
                                    package.name = m.name;
                                    package.link = m.link;
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
                        dm.hidden = package.hidden;
                        DisplayedPackages.Add(dm);
                    }
                    ModGrid.ItemsSource = DisplayedPackages;

                }
                else // No config found
                {
                    game = "Persona 4 Golden";
                    config.game = "Persona 4 Golden";
                    lastGame = "Persona 4 Golden";
                    cpkLang = "data_e.cpk";
                    config.p4gConfig.cpkLang = "data_e.cpk";
                    foreach (var button in buttons)
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));

                    // Initialise loadouts
                    loadoutUtils = new Loadouts(game);
                    loadoutHandled = true;
                    LoadoutBox.ItemsSource = loadoutUtils.LoadoutItems;
                    LoadoutBox.SelectedIndex = 0;
                    loadoutHandled = false;
                }

                if (game == "Persona 4 Golden" && config.p4gConfig.modDir != "" && config.p4gConfig.modDir != null)
                    modPath = config.p4gConfig.modDir;
                else if (game == "Persona 3 FES" && config.p3fConfig.modDir != "" && config.p3fConfig.modDir != null)
                    modPath = config.p3fConfig.modDir;
                else if (game == "Persona 5" && config.p5Config.modDir != "" && config.p5Config.modDir != null)
                    modPath = config.p5Config.modDir;
                else if (game == "Persona 5 Strikers" && config.p5sConfig.modDir != "" && config.p5sConfig.modDir != null)
                    modPath = config.p5sConfig.modDir;

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
                    PriorityPopup.Text = "Switch Order to Prioritize\nthe Bottom of the Grid";
                }
                else
                {
                    TopArrow.Visibility = Visibility.Collapsed;
                    BottomArrow.Visibility = Visibility.Visible;
                    PriorityPopup.Text = "Switch Order to Prioritize\nthe Top of the Grid";
                }

                // Apply saved window settings
                if (config.Height != null && config.Height >= MinHeight)
                    Height = (double)config.Height;
                if (config.Width != null && config.Width >= MinWidth)
                    Width = (double)config.Width;
                if (config.Maximized)
                    WindowState = WindowState.Maximized;
                if (config.TopGridHeight != null)
                    MainGrid.RowDefinitions[0].Height = new GridLength((double)config.TopGridHeight, GridUnitType.Star);
                if (config.BottomGridHeight != null)
                    MainGrid.RowDefinitions[2].Height = new GridLength((double)config.BottomGridHeight, GridUnitType.Star);
                if (config.LeftGridWidth != null)
                    MainGrid.ColumnDefinitions[0].Width = new GridLength((double)config.LeftGridWidth, GridUnitType.Star);
                if (config.RightGridWidth != null)
                    MainGrid.ColumnDefinitions[2].Width = new GridLength((double)config.RightGridWidth, GridUnitType.Star);
                if (config.RightTopGridHeight != null)
                    RightGrid.RowDefinitions[0].Height = new GridLength((double)config.RightTopGridHeight, GridUnitType.Star);
                if (config.RightBottomGridHeight != null)
                    RightGrid.RowDefinitions[2].Height = new GridLength((double)config.RightBottomGridHeight, GridUnitType.Star);

                LaunchPopup.Text = $"Launch {game}\n(Ctrl+L)";
                FileSystemWatcher fileSystemWatcher = new FileSystemWatcher($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}");
                fileSystemWatcher.Filter = "refresh.aem";
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.Created += FileSystemWatcher_Created;
                if (!oneClick)
                    UpdateAllAsync();

                InitMediaPlayer();
            }

        }
        private async void InitMediaPlayer()
        {
            await Task.Run(() =>
            {
                var currentAssembly = Assembly.GetEntryAssembly();
                var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
                // Default installation path of VideoLAN.LibVLC.Windows
                var libDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
                string[] options = new string[]
                {
                    "--effect-list=spectrum",
                    "--audio-visual=visual",
                    "--no-visual-peaks",
                    "--no-visual-80-bands"
                };
                /*Application.Current.Dispatcher.Invoke(() =>
                {
                    MusicPlayer.SourceProvider.CreatePlayer(libDirectory, options);
                    MusicPlayer.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;
                    MusicPlayer.SourceProvider.MediaPlayer.Playing += SetProgressMax;
                    MusicPlayer.SourceProvider.MediaPlayer.PositionChanged += (sender, e) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AudioProgress.Value = e.NewPosition * 100;
                            TimeSpan current = TimeSpan.FromMilliseconds(duration * e.NewPosition);
                            TimeSpan total = TimeSpan.FromMilliseconds(duration);
                            AudioDuration.Text = string.Format("{0:D1}:{1:D2} / {2:D1}:{3:D2}",
                                current.Minutes, current.Seconds,
                                total.Minutes, total.Seconds);
                        });
                    };
                    VolumeSlider.ApplyTemplate();
                    Thumb thumb = (VolumeSlider.Template.FindName("PART_Track", VolumeSlider) as Track).Thumb;
                    thumb.MouseEnter += new MouseEventHandler(thumb_MouseEnter);
                    AudioProgress.ApplyTemplate();
                    thumb = (AudioProgress.Template.FindName("PART_Track", AudioProgress) as Track).Thumb;
                    thumb.MouseEnter += new MouseEventHandler(thumb_MouseEnter);
                });*/
            });
        }
        private void thumb_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
            {
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left);
                args.RoutedEvent = MouseLeftButtonDownEvent;
                (sender as Thumb).RaiseEvent(args);
            }
        }
        //private VlcMediaPlayer MusicPlayer.SourceProvider.MediaPlayer;
        private long duration;
        private void SetProgressMax(object sender, VlcMediaPlayerPlayingEventArgs e)
        {
            var vlc = (VlcMediaPlayer)sender;
            duration = vlc.Length;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (PlayAudio.Visibility == Visibility.Visible)
                {
                    PlayAudio.Visibility = Visibility.Collapsed;
                    PauseAudio.Visibility = Visibility.Visible;
                }
                AudioProgress.IsEnabled = true;
            });
        }
        private bool endReached;
        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            endReached = true;
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Emulate completion just in case it ends at 0:20/0:21 for example
                AudioProgress.Value = 100;
                TimeSpan totalTime = TimeSpan.FromMilliseconds(duration);
                AudioDuration.Text = string.Format("{0:D1}:{1:D2} / {0:D1}:{1:D2}",
                    totalTime.Minutes, totalTime.Seconds);
                // Show replay button when done playing
                PauseAudio.Visibility = Visibility.Collapsed;
                ReplayAudio.Visibility = Visibility.Visible;
            });
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
                    if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\temp"))
                    {
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            ReplacePackagesXML(game);
                        });
                    }
                    else
                    {
                        Refresh();
                        updatePackages(lastLoadout);
                    }
                }
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
                else if (game == "Persona 5")
                    PacUnpacker.UnpackCPK(directory);
                else if (game == "Persona 5 Strikers")
                    Merger.Backup(directory);

                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    EnableUI();
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
                    || (game == "Persona 5 Strikers" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\motor_rsc")))
                    Console.WriteLine($@"[ERROR] Failed to unpack everything from {game}! Please check if you have all prerequisites installed!");
            });
        }
        private void LaunchClick(object sender, RoutedEventArgs e)
        {
            LaunchCommand();
        }
        private void LaunchCommand()
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
                else if (game == "Persona 5")
                {
                    if (!FileIOWrapper.Exists(gamePath))
                    {
                        Console.WriteLine($"[ERROR] Couldn't find {gamePath}. Please correct the file path in config.");
                        return;
                    }
                    Console.WriteLine($"[INFO] If the game is lagging set the global config to your special config for Persona 5.");
                    startInfo.Arguments = $"--no-gui \"{gamePath}\"";
                }
                DisableUI();

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
                EnableUI();
            }
            else if (game == "Persona 5 Strikers")
                Process.Start("steam://rungameid/1382330/option0");
            else
                Console.WriteLine("[ERROR] Please setup shortcut in config menu.");
        }
        private void ConfigWdwClick(object sender, RoutedEventArgs e)
        {
            ConfigWdwCommand();
        }

        private void ConfigWdwCommand()
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
        private async void Refresh()
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
                else if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.path}\Package.xml"))
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
                        bool inPackageList = PackageList.ToList().Any(x => x.path == Path.GetFileName(package));
                        bool inDisplayedPackages = DisplayedPackages.ToList().Any(x => x.path == Path.GetFileName(package));
                        // Add new package to collections if they're missing
                        if (!inPackageList)
                        {
                            Package p = new Package();
                            p.enabled = false;
                            p.id = metadata.id;
                            p.path = Path.GetFileName(package);
                            p.name = metadata.name;
                            p.link = metadata.link;
                            PackageList.Add(p);
                        }
                        if (!inDisplayedPackages)
                        {
                            DisplayedMetadata dm = InitDisplayedMetadata(metadata);
                            dm.enabled = false;
                            dm.path = Path.GetFileName(package);
                            DisplayedPackages.Add(dm);
                        }
                        else
                        {
                            // Update the package metadata
                            Package p = PackageList.ToList().Find(x => x.path == Path.GetFileName(package));
                            if (p != null)
                            {
                                p.link = metadata.link;
                                p.name = metadata.name;
                                p.path = Path.GetFileName(package);
                            }
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

            await Task.Run(() =>
            {
                // Update DisplayedPackages
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    ModGrid.ItemsSource = DisplayedPackages;
                    // Trigger select event to refresh description and Preview.png
                    ModGrid.SetSelectedItem(ModGrid.GetSelectedItem());
                });
            });
            Console.WriteLine($"[INFO] Refreshed!");
            UpdateStats();
        }
        private async void UpdateStats()
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                // Set top right stats
                StatText.Text = $"-- packages • -- enabled • -- files • " +
                $"-- Bytes • v{aemulusVersion.Substring(0, aemulusVersion.LastIndexOf('.'))}";
            });
            await Task.Run(() =>
            {
                var numEnabled = PackageList.Where(x => x.enabled).Count();
                var numFiles = Directory.GetFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/Packages/{game}", "*", SearchOption.AllDirectories).Length.ToString("N0");
                var dirSize = StringConverters.FormatSize(new DirectoryInfo($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/Packages/{game}").GetDirectorySize());
                // Update DisplayedPackages
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    // Set top right stats
                    StatText.Text = $"{PackageList.Count} packages • {numEnabled} enabled • {numFiles} files • " +
                    $"{dirSize} • v{aemulusVersion.Substring(0, aemulusVersion.LastIndexOf('.'))}";
                });
            });
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

        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            RefreshCommand();
        }
        private async void RefreshCommand()
        {
            DisableUI();
            Refresh();
            updateConfig();
            updatePackages();
            await UpdateAllAsync();
            EnableUI();
        }
        private void DisableUI()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var button in buttons)
                {
                    button.IsHitTestVisible = false;
                    button.Foreground = new SolidColorBrush(Colors.Gray);
                }
                GameBox.IsHitTestVisible = false;
                ModGrid.IsHitTestVisible = false;
                LoadoutBox.IsHitTestVisible = false;
            });
        }
        private void EnableUI()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ModGrid.IsHitTestVisible = true;
                foreach (var button in buttons)
                {
                    button.IsHitTestVisible = true;
                    if (game == "Persona 3 FES")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                    else if (game == "Persona 4 Golden")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                    else if (game == "Persona 5 Strikers")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                    else
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                }
                GameBox.IsHitTestVisible = true;
                if (String.IsNullOrEmpty(modPath))
                {
                    MergeButton.IsHitTestVisible = false;
                    MergeButton.Foreground = new SolidColorBrush(Colors.Gray);
                }

                LoadoutBox.IsHitTestVisible = true;
            });
        }

        private void NewClick(object sender, RoutedEventArgs e)
        {
            NewCommand();
        }

        private void NewCommand()
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
        private void MergeClick(object sender, RoutedEventArgs e)
        {
            MergeCommand();
        }
        private async void MergeCommand()
        {
            if ((game == "Persona 4 Golden" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{Path.GetFileNameWithoutExtension(cpkLang)}"))
                    || (game == "Persona 3 FES" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\DATA")
                    && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL"))
                    || (game == "Persona 5" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}")))
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
                }

                if ((gamePath == "" || gamePath == null) && game != "Persona 5 Strikers")
                    return;

                DisableUI();

                fromMain = true;

                if (game == "Persona 3 FES")
                    await pacUnpack(gamePath);
                else if (game != "Persona 5 Strikers")
                    await pacUnpack(Path.GetDirectoryName(gamePath));
                fromMain = false;

                if ((game == "Persona 4 Golden" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\{Path.GetFileNameWithoutExtension(cpkLang)}"))
                    || (game == "Persona 3 FES" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\DATA")
                    && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}\BTL"))
                    || (game == "Persona 5" && !Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Original\{game}")))
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

            DisableUI();

            await unpackThenMerge();

            EnableUI();
        }

        private async Task unpackThenMerge()
        {
            await Task.Run(async () =>
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

                    if (game == "Persona 5")
                    {
                        path = $@"{modPath}\{config.p5Config.CpkName}";
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
                    if (game == "Persona 5")
                    {
                        path = $@"{modPath}\{config.p5Config.CpkName}";
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
                        // Merge flow, bmd and pm1 files
                        FlowMerger.Merge(packages, game);
                        BmdMerger.Merge(packages, game);
                        PM1Merger.Merge(packages, game);

                        await Task.Run(() =>
                        {
                            binMerge.Restart(path, emptySND, game, cpkLang);
                            binMerge.Unpack(packages, path, useCpk, cpkLang, game);
                            binMerge.Merge(path, game);
                        });
                        // Only run if tblpatches exists
                        if (packages.Exists(x => Directory.Exists($@"{x}\tblpatches")))
                        {
                            tblPatch.Patch(packages, path, useCpk, cpkLang, game);
                        }

                        if (game == "Persona 4 Golden" && packages.Exists(x => Directory.Exists($@"{x}\preappfile")))
                        {
                            PreappfileAppend.Append(Path.GetDirectoryName(path), cpkLang);
                            PreappfileAppend.Validate(Path.GetDirectoryName(path), cpkLang);
                        }

                        if (game == "Persona 5")
                        {
                            binMerge.MakeCpk(path);
                            if (!FileIOWrapper.Exists($@"{path}.cpk"))
                            {
                                Console.WriteLine($"[ERROR] Failed to build {path}.cpk!");
                            }
                        }

                        // Restore the bmd and pm1 backups
                        Utils.RestoreBackups(packages);

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

        public void updatePackages(string loadout = null)
        {
            packages.packages = PackageList;
            if (loadout == null && LoadoutBox.SelectedItem != null)
                loadout = LoadoutBox.SelectedItem.ToString();
            // Don't update packages if the current loadout is invalid
            if (loadout != null)
            {
                using (FileStream streamWriter = FileIOWrapper.Create($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{loadout}.xml"))
                {
                    try
                    {
                        xp.Serialize(streamWriter, packages);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($@"[ERROR] Couldn't update Config\{game}\{loadout}.xml ({ex.Message})");
                    }
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

                // Show the correct visibility context menu entry
                ShowItem.IsEnabled = row.hidden;
                ShowItem.Visibility = row.hidden ? Visibility.Visible : Visibility.Collapsed;
                HideItem.IsEnabled = !row.hidden;
                HideItem.Visibility = !row.hidden ? Visibility.Visible : Visibility.Collapsed;

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
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();
                        ImageBehavior.SetAnimatedSource(Preview, img);
                        //ImageBehavior.SetAnimatedSource(PreviewBG, img);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] {ex.Message}");
                    }
                }
                else
                {
                    ImageBehavior.SetAnimatedSource(Preview, bitmap);
                    //ImageBehavior.SetAnimatedSource(PreviewBG, null);
                }

            }
        }

        // Update config order when rows are changed
        private void ModGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if(IsLoaded)
            {
                DisplayedMetadata dm = (DisplayedMetadata)e.Row.Item;
                var package = PackageList.FirstOrDefault(package => package.path == dm.path);
                if(package != null)
                {
                    Package temp = package;
                    PackageList.Remove(package);
                    int dmIndex = DisplayedPackages.IndexOf(dm);
                    if (dmIndex != -1 && dmIndex <= PackageList.Count)
                        PackageList.Insert(dmIndex, temp);
                    else
                        PackageList.Add(temp);

                    updateConfig();
                    updatePackages();
                }
            }
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
            foreach (var item in ModGrid.SelectedItems)
            {
                DisplayedMetadata row = (DisplayedMetadata)item;
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
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var deleted = false;
            foreach (var item in ModGrid.SelectedItems)
            {
                var row = (DisplayedMetadata)item;
                if (row != null)
                {
                    NotificationBox notification = new NotificationBox($@"Are you sure you want to delete Packages\{game}\{row.path}?", false);
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
                        deleted = true;
                    }
                }
            }
            if (deleted)
            {
                Refresh();
                updateConfig();
                updatePackages();
                ImageBehavior.SetAnimatedSource(Preview, bitmap);

                Description.Document = ConvertToFlowDocument("Aemulus means \"Rival\" in Latin. It was chosen since it " +
                    "was made to rival Mod Compendium.\n\n(You are seeing this message because no package is selected or " +
                    "the package has no description.)");
            }
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ModGrid.SelectedItems)
            {
                DisplayedMetadata row = (DisplayedMetadata)item;
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
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] {ex.Message}");
                        }
                    }
                }
            }

            Refresh();
            updateConfig();
            updatePackages();
        }

        private async void ZipItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ModGrid.SelectedItems)
            {
                DisplayedMetadata row = (DisplayedMetadata)item;
                if (row != null && Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}"))
                {
                    var openFolder = new System.Windows.Forms.SaveFileDialog();
                    openFolder.FileName = $"{row.path}.7z";
                    openFolder.Title = $"Select a file to zip to";
                    openFolder.Filter = "7zip | *.7z";
                    if (openFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        await ZipItem($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{row.path}", openFolder.FileName);
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
                    Console.WriteLine($"[ERROR] Couldn't find {startInfo.FileName}. Please check if it was blocked by your anti-virus.");
                    return;
                }

                if (FileIOWrapper.Exists(output))
                    FileIOWrapper.Delete(output);

                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}";
                startInfo.Arguments = $@"a ""{output}"" ""{Path.GetFileName(path)}/*""";
                Console.WriteLine($@"[INFO] Zipping {path} into {output}");
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
            foreach (var item in ModGrid.SelectedItems)
            {
                DisplayedMetadata row = (DisplayedMetadata)item;
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
        }

        private void ModGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space && ModGrid.CurrentColumn.Header.ToString() != "Enabled")
            {
                foreach (var item in ModGrid.SelectedItems)
                {
                    var checkbox = ModGrid.Columns[0].GetCellContent(item) as CheckBox;
                    if (checkbox != null)
                        checkbox.IsChecked = !checkbox.IsChecked;
                }
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
                        selectedLoadout = config.p3fConfig.loadout;
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
                        game = "Persona 3 Portable";
                        modPath = config.p3pConfig.modDir;
                        selectedLoadout = config.p3pConfig.loadout;
                        gamePath = config.p3pConfig.isoPath;
                        elfPath = config.p3pConfig.elfPath;
                        launcherPath = config.p3pConfig.launcherPath;
                        buildWarning = config.p3pConfig.buildWarning;
                        buildFinished = config.p3pConfig.buildFinished;
                        updateChangelog = config.p3pConfig.updateChangelog;
                        updateAll = config.p3pConfig.updateAll;
                        updatesEnabled = config.p3pConfig.updatesEnabled;
                        deleteOldVersions = config.p3pConfig.deleteOldVersions;
                        useCpk = true;
                        ConvertCPK.Visibility = Visibility.Collapsed;
                        foreach (var button in buttons)
                        {
                            button.Foreground = new SolidColorBrush(Color.FromRgb(255, 79, 193));
                            button.IsHitTestVisible = true;
                        }
                        break;
                    case 2:
                        game = "Persona 4 Golden";
                        modPath = config.p4gConfig.modDir;
                        selectedLoadout = config.p4gConfig.loadout;
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
                    case 3:
                        game = "Persona 5";
                        modPath = config.p5Config.modDir;
                        selectedLoadout = config.p5Config.loadout;
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
                    case 4:
                        game = "Persona 5 Strikers";
                        if (config.p5sConfig == null)
                            config.p5sConfig = new ConfigP5S();
                        modPath = config.p5sConfig.modDir;
                        selectedLoadout = config.p5sConfig.loadout;
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
                lastGame = game;
                Reqs.Visibility = Visibility.Collapsed;
                HHH.Visibility = Visibility.Collapsed;
                Inaba.Visibility = Visibility.Collapsed;
                config.game = game;
                if (String.IsNullOrEmpty(modPath))
                {
                    MergeButton.IsHitTestVisible = false;
                    MergeButton.Foreground = new SolidColorBrush(Colors.Gray);
                }
                LaunchPopup.Text = $"Launch {game}\n(Ctrl+L)";
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

                // Update the available loadouts
                loadoutUtils.LoadLoadouts(game);
                loadoutHandled = true;
                LoadoutBox.ItemsSource = loadoutUtils.LoadoutItems;
                if (LoadoutBox.Items.Contains(selectedLoadout))
                    LoadoutBox.SelectedItem = selectedLoadout;
                else
                    LoadoutBox.SelectedIndex = 0;
                loadoutHandled = false;
                if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{LoadoutBox.SelectedItem}.xml"))
                {
                    try
                    {
                        using (FileStream streamWriter = FileIOWrapper.Open($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{LoadoutBox.SelectedItem}.xml", FileMode.Open))
                        {
                            try
                            {
                                // Call the Deserialize method and cast to the object type.
                                packages = (Packages)xp.Deserialize(streamWriter);
                                PackageList = packages.packages;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($@"[ERROR] Couldn't deseralize Config\{game}\{LoadoutBox.SelectedItem}.xml ({ex.Message})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Invalid package loadout {LoadoutBox.SelectedItem}.xml ({ex.Message})");
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
                                    package.name = m.name;
                                    package.link = m.link;
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
                    dm.hidden = package.hidden;
                    DisplayedPackages.Add(dm);
                }
                ModGrid.ItemsSource = DisplayedPackages;

                Refresh();
                updateConfig();
                updatePackages();

                ImageBehavior.SetAnimatedSource(Preview, bitmap);
                //ImageBehavior.SetAnimatedSource(PreviewBG, null);

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
                Console.WriteLine($@"[WARNING] User attempted to close window while updates were still running");
                NotificationBox notification = new NotificationBox("Aemulus hasn't finished updating everything.\nAre you sure you want to exit?", false);
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

            // Save Windows Settings
            if (WindowState == WindowState.Maximized)
            {
                config.Height = RestoreBounds.Height;
                config.Width = RestoreBounds.Width;
                config.Maximized = true;
            }
            else
            {
                config.Height = Height;
                config.Width = Width;
                config.Maximized = false;
            }
            config.TopGridHeight = MainGrid.RowDefinitions[0].Height.Value;
            config.BottomGridHeight = MainGrid.RowDefinitions[2].Height.Value;
            config.LeftGridWidth = MainGrid.ColumnDefinitions[0].Width.Value;
            config.RightGridWidth = MainGrid.ColumnDefinitions[2].Width.Value;
            config.RightTopGridHeight = RightGrid.RowDefinitions[0].Height.Value;
            config.RightBottomGridHeight = RightGrid.RowDefinitions[2].Height.Value;
            updateConfig();

            Application.Current.Shutdown();
        }
        private void FolderButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FolderCommand();
        }

        private void FolderCommand()
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
                PriorityPopup.Text = "Switch Order to Prioritize\nthe Bottom of the Grid";
            }
            else
            {

                TopArrow.Visibility = Visibility.Collapsed;
                BottomArrow.Visibility = Visibility.Visible;
                PriorityPopup.Text = "Switch Order to Prioritize\nthe Top of the Grid";
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
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                string loadout = null;
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
                        startInfo.Arguments = $@"x -y ""{file}"" -o""{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp""";
                        Console.WriteLine($"[INFO] {startInfo.Arguments}");
                        startInfo.RedirectStandardOutput = true;
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
                    // Import loadout xml
                    else if (Path.GetExtension(file) == ".xml")
                    {
                        var oldPackageList = PackageList;
                        Console.WriteLine($"[INFO] Trying to import {Path.GetFileName(file)} as a loadout xml");
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
                            Console.WriteLine($"Invalid loadout xml ({ex.Message})");
                        }

                        loadout = Path.GetFileNameWithoutExtension(file);

                        // Ask to rename the loadout if one with the same name already exists
                        if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{loadout}.xml"))
                        {
                            Console.WriteLine("[INFO] A loadout with the same name already exists, please enter a new name for it.");
                            NotificationBox notification = new NotificationBox("A loadout with the same name already exists, please enter a new name for it.");
                            notification.ShowDialog();
                            CreateLoadout createLoadout = new CreateLoadout(game, loadout, true);
                            createLoadout.ShowDialog();
                            if (createLoadout.name == "")
                            {
                                Console.WriteLine($"[INFO] Cancelled importing {Path.GetFileName(file)}");
                                return;
                            }
                            loadout = createLoadout.name;
                        }

                        // Create displayed metadata from packages in PackageList and their respective Package.xml's
                        Dictionary<int, DisplayedMetadata> missingPackages = new Dictionary<int, DisplayedMetadata>();
                        int index = 0;
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
                                        package.name = m.name;
                                        package.link = m.link;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[ERROR] Invalid Package.xml for {package.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                    continue;
                                }
                                dm.path = package.path;
                                dm.enabled = package.enabled;
                                dm.hidden = package.hidden;
                                DisplayedPackages.Add(dm);
                            }
                            else
                            {
                                string linkHost = UrlConverter.Convert(package.link);
                                // Check if the package is actually missing (may have a different path) and has a valid download link
                                if ((linkHost == "GameBanana" || linkHost == "GitHub") && oldPackageList.Where(p => p.id == package.id).Count() == 0)
                                {
                                    DisplayedMetadata packageMetadata = new DisplayedMetadata
                                    {
                                        name = package.name,
                                        link = package.link,
                                        path = package.path,
                                        enabled = package.enabled,
                                        hidden = package.hidden
                                    };
                                    missingPackages.Add(index, packageMetadata);
                                }
                            }
                            index++;
                        }
                        // Ask if Aemulus should try and download missing mods
                        if (missingPackages.Count > 0)
                        {
                            bool multiple = missingPackages.Count > 1;
                            NotificationBox notification = new NotificationBox($"{missingPackages.Count} missing package{(multiple ? "s" : "")} {(multiple ? "were" : "was")} " +
                                $"found whilst importing {loadout}.\nWould you like Aemulus to try and download {(multiple ? "them" : "it")}?", false);
                            notification.ShowDialog();
                            // Try to download missing mods
                            if (notification.YesNo)
                            {
                                updating = true;
                                cancellationToken = new CancellationTokenSource();
                                await packageUpdater.CheckForUpdate(missingPackages.Values.ToArray(), game, cancellationToken, true);
                                updating = false;

                                // Add missing mods to DisplayedPackages with their correct metadata from loadout (enabled, hidden, position, etc)
                                int numNotDownloaded = 0;
                                foreach (var package in missingPackages)
                                {
                                    // Only add them if they were downloaded
                                    string xml = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{package.Value.path}\Package.xml";
                                    if (FileIOWrapper.Exists(xml))
                                    {
                                        // Add the package (copy pasted from above, I know I should make it a seperate function)
                                        var m = new Metadata();
                                        DisplayedMetadata dm = new DisplayedMetadata();
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
                                                    Console.WriteLine($"[ERROR] Invalid Package.xml for {package.Value.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                                    continue;
                                                }
                                                dm.name = m.name;
                                                dm.id = m.id;
                                                dm.author = m.author;
                                                dm.version = m.version;
                                                dm.link = m.link;
                                                dm.description = m.description;
                                                dm.skippedVersion = m.skippedVersion;
                                                package.Value.name = m.name;
                                                package.Value.link = m.link;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[ERROR] Invalid Package.xml for {package.Value.path} ({ex.Message}) Fix or delete the current Package.xml then refresh to use.");
                                            continue;
                                        }
                                        dm.path = package.Value.path;
                                        dm.enabled = package.Value.enabled;
                                        dm.hidden = package.Value.hidden;
                                        DisplayedPackages.Insert(package.Key - numNotDownloaded, dm);
                                    }
                                    else
                                    {
                                        numNotDownloaded++;
                                    }
                                }
                            }
                        }
                        dropped = true;
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] {file} isn't a folder, .zip, .7z, or .rar, or {game.Replace(" ", "")} loadout xml, skipping...");
                    }
                }
                if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp"))
                    DeleteDirectory($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\temp");

                if (dropped)
                {
                    updatePackages(loadout);
                    if (loadout != null)
                    {
                        loadoutUtils.LoadLoadouts(game);
                        loadoutHandled = true;
                        LoadoutBox.ItemsSource = loadoutUtils.LoadoutItems;
                        LoadoutBox.SelectedItem = loadout;
                        loadoutHandled = false;
                    }
                    Refresh();
                }
            });
        }

        private async void Add_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                DisableUI();

                await ExtractPackages(fileList);

                EnableUI();
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

        private void ReplacePackagesXML(string chosenGame = null)
        {
            if (chosenGame == null)
                chosenGame = game;
            var xmls = Directory.GetFiles($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\temp", "*.xml", SearchOption.TopDirectoryOnly)
                .Where(xml => !Path.GetFileName(xml).Equals("Package.xml", StringComparison.InvariantCultureIgnoreCase) && !Path.GetFileName(xml).Equals("Mod.xml", StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (xmls.Count > 0)
            {
                Console.WriteLine("[INFO] Switching over to downloaded loadout... (May take a bit)");
                var lastXml = String.Empty;
                foreach (var xml in xmls)
                {
                    var replacementXml = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{chosenGame}\{Path.GetFileName(xml)}";
                    // Rename original scheme to default
                    if (Path.GetFileName(xml).Equals($"{chosenGame.Replace(" ", "")}Packages.xml", StringComparison.InvariantCultureIgnoreCase))
                        replacementXml = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{chosenGame}\Default.xml";
                    File.Copy(xml, replacementXml, true);
                    lastXml = Path.GetFileNameWithoutExtension(replacementXml);
                }
                Directory.Delete($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\temp", true);
                // Load the new loadout
                loadoutUtils.LoadLoadouts(chosenGame);
                loadoutHandled = true;
                LoadoutBox.ItemsSource = loadoutUtils.LoadoutItems;
                loadoutHandled = false;

                if (game == chosenGame)
                {
                    if (lastXml == (string)LoadoutBox.SelectedItem)
                        UpdateDisplay();
                    else if (loadoutUtils.LoadoutItems.Contains(lastXml))
                        LoadoutBox.SelectedItem = lastXml;
                    else if (loadoutUtils.LoadoutItems.Contains(lastLoadout))
                        LoadoutBox.SelectedItem = lastLoadout;
                    else
                        LoadoutBox.SelectedIndex = 0;
                }
                else
                {
                    switch (chosenGame)
                    {
                        case "Persona 3 FES":
                            config.p3fConfig.loadout = lastXml;
                            break;
                        case "Persona 4 Golden":
                            config.p4gConfig.loadout = lastXml;
                            break;
                        case "Persona 5":
                            config.p5Config.loadout = lastXml;
                            break;
                        case "Persona 5 Strikers":
                            config.p5sConfig.loadout = lastXml;
                            break;
                    }
                }
            }
        }

        private async void UpdateItem_Click(object sender, RoutedEventArgs e)
        {
            DisableUI();
            foreach (var item in ModGrid.SelectedItems)
            {
                DisplayedMetadata row = (DisplayedMetadata)item;
                await UpdateItemAsync(row);
            }
            if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\temp"))
                ReplacePackagesXML();
            Refresh();
            updatePackages();
            EnableUI();
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
        }

        private async Task UpdateAllAsync()
        {
            if (updating)
            {
                Console.WriteLine($"[INFO] Packages are already being updated, ignoring request to check for updates");
                return;
            }
            DisableUI();
            if (config.updateAemulus)
                await UpdateAemulus();
            if (!updatesEnabled)
            {
                EnableUI();
                return;
            }
            if (updateAll)
            {
                updating = true;
                cancellationToken = new CancellationTokenSource();
                Console.WriteLine($"[INFO] Checking for updates for all applicable packages");
                DisplayedMetadata[] updatableRows = DisplayedPackages.Where(RowUpdatable).ToArray();
                if (await packageUpdater.CheckForUpdate(updatableRows, game, cancellationToken))
                {
                    if (Directory.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\temp"))
                        ReplacePackagesXML();
                    Refresh();
                    updatePackages();
                }
                updating = false;
            }
            EnableUI();
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
                DarkModePopup.Text = "Switch to Dark Mode";
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
                DarkModePopup.Text = "Switch to Light Mode";
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

        // Mod browser
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (item.HasDownloads)
                new PackageDownloader().BrowserDownload(item, (GameFilter)GameFilterBox.SelectedIndex);
            else
            {
                var game = "";
                switch ((GameFilter)GameFilterBox.SelectedIndex)
                {
                    case GameFilter.P3:
                        game = "Persona 3 FES";
                        break;
                    case GameFilter.P4G:
                        game = "Persona 4 Golden";
                        break;
                    case GameFilter.P5:
                        game = "Persona 5";
                        break;
                    case GameFilter.P5S:
                        game = "Persona 5 Strikers";
                        break;
                }
                new AltLinkWindow(item.AlternateFileSources, item.Title, game).ShowDialog();
            }
        }
        // Mod browser
        private void AltDownload_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            var game = "";
            switch ((GameFilter)GameFilterBox.SelectedIndex)
            {
                case GameFilter.P3:
                    game = "Persona 3 FES";
                    break;
                case GameFilter.P4G:
                    game = "Persona 4 Golden";
                    break;
                case GameFilter.P5:
                    game = "Persona 5";
                    break;
                case GameFilter.P5S:
                    game = "Persona 5 Strikers";
                    break;
            }
            new AltLinkWindow(item.AlternateFileSources, item.Title, game).ShowDialog();
        }
        private int imageCounter;
        private int imageCount;
        private Uri currentAudio;
        private void MoreInfo_Click(object sender, RoutedEventArgs e)
        {
            HomepageButton.Content = $"{(TypeBox.SelectedValue as ComboBoxItem).Content.ToString().Trim().TrimEnd('s')} Page";
            AudioProgress.IsEnabled = false;
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (item.HasDownloads)
                DownloadButton.Visibility = Visibility.Visible;
            else
                DownloadButton.Visibility = Visibility.Collapsed;
            if (item.HasAltLinks)
                AltButton.Visibility = Visibility.Visible;
            else
                AltButton.Visibility = Visibility.Collapsed;
            DescPanel.DataContext = button.DataContext;
            MediaPanel.DataContext = button.DataContext;
            DescText.ScrollToHome();
            var text = "";
            if (!item.HasDownloads && item.HasAltLinks)
                text += "This mod can't be installed directly through Aemulus from GameBanana. Use the Alt. Downloads button to download from your browser then manually install it.\n\n";
            text += item.ConvertedText;
            DescText.Document = ConvertToFlowDocument(text);
            ImageLeft.IsEnabled = true;
            ImageRight.IsEnabled = true;
            BigImageLeft.IsEnabled = true;
            BigImageRight.IsEnabled = true;
            imageCount = item.Media.Where(x => x.Type == "image").ToList().Count;
            imageCounter = 0;
            AudioPanel.Visibility = Visibility.Collapsed;
            ImagePanel.Visibility = Visibility.Visible;
            if (imageCount > 0)
            {
                var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
                Screenshot.Source = image;
                BigScreenshot.Source = image;
                CaptionText.Text = item.Media[imageCounter].Caption;
                BigCaptionText.Text = item.Media[imageCounter].Caption;
                if (!String.IsNullOrEmpty(CaptionText.Text))
                {
                    BigCaptionText.Visibility = Visibility.Visible;
                    CaptionText.Visibility = Visibility.Visible;
                }
                else
                {
                    BigCaptionText.Visibility = Visibility.Collapsed;
                    CaptionText.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                currentAudio = item.Media[0].Audio;
                // Wait until the music player has been created
                while (MusicPlayer.SourceProvider.MediaPlayer == null) ;
                MusicPlayer.SourceProvider.MediaPlayer.SetMedia(currentAudio);
                MusicPlayer.SourceProvider.MediaPlayer.Audio.Volume = (int)VolumeSlider.Value;
                AudioDuration.Text = "0:00 / 0:00";
                MusicPlayer.SourceProvider.MediaPlayer.Stop();
                ReplayAudio.Visibility = Visibility.Collapsed;
                PauseAudio.Visibility = Visibility.Collapsed;
                PlayAudio.Visibility = Visibility.Visible;
                AudioPanel.Visibility = Visibility.Visible;
                ImagePanel.Visibility = Visibility.Collapsed;
            }
            if (imageCount == 1)
            {
                ImageLeft.IsEnabled = false;
                ImageRight.IsEnabled = false;
                BigImageLeft.IsEnabled = false;
                BigImageRight.IsEnabled = false;
            }

            DescPanel.Visibility = Visibility.Visible;
        }
        private void CloseMedia_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Collapsed;
        }

        private void Image_Click(object sender, RoutedEventArgs e)
        {
            MediaPanel.Visibility = Visibility.Visible;
        }
        private void Homepage_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            try
            {
                var ps = new ProcessStartInfo(item.Link.ToString())
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception)
            {
            }
        }
        private static bool selected = false;
        private static Dictionary<GameFilter, Dictionary<TypeFilter, List<GameBananaCategory>>> cats = new Dictionary<GameFilter, Dictionary<TypeFilter, List<GameBananaCategory>>>();
        private static readonly List<GameBananaCategory> All = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "All",
                ID = null
            }
        }.ToList();
        private static readonly List<GameBananaCategory> None = new GameBananaCategory[]
        {
            new GameBananaCategory()
            {
                Name = "- - -",
                ID = null
            }
        }.ToList();
        private async void InitializeBrowser()
        {
            InitBgs();
            using (var httpClient = new HttpClient())
            {
                LoadingBar.Visibility = Visibility.Visible;
                ErrorPanel.Visibility = Visibility.Collapsed;
                // Initialize games
                var gameIDS = new string[] { "8502", "8263", "7545", "9099" };
                var types = new string[] { "Mod", "Wip", "Sound", "Tool", "Tutorial" };
                var gameCounter = 0;
                foreach (var gameID in gameIDS)
                {
                    // Initialize categories
                    var counter = 0;
                    var totalPages = 0;
                    foreach (var type in types)
                    {
                        var requestUrl = $"https://gamebanana.com/apiv4/{type}Category/ByGame?_aGameRowIds[]={gameID}&_sRecordSchema=Custom" +
                            "&_csvProperties=_idRow,_sName,_sProfileUrl,_sIconUrl,_idParentCategoryRow&_nPerpage=50&_bReturnMetadata=true";
                        string responseString = "";
                        try
                        {
                            var responseMessage = await httpClient.GetAsync(requestUrl);
                            responseString = await responseMessage.Content.ReadAsStringAsync();
                            responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                            var numRecords = responseMessage.GetHeader("X-GbApi-Metadata_nRecordCount");
                            if (numRecords != -1)
                            {
                                totalPages = Convert.ToInt32(Math.Ceiling(numRecords / 50));
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            LoadingBar.Visibility = Visibility.Collapsed;
                            ErrorPanel.Visibility = Visibility.Visible;
                            BrowserRefreshButton.Visibility = Visibility.Visible;
                            switch (Regex.Match(ex.Message, @"\d+").Value)
                            {
                                case "443":
                                    BrowserMessage.Text = "No internet connection is available.";
                                    break;
                                case "500":
                                case "503":
                                case "504":
                                    BrowserMessage.Text = "GameBanana's servers are unavailable.";
                                    break;
                                default:
                                    BrowserMessage.Text = ex.Message;
                                    break;
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            LoadingBar.Visibility = Visibility.Collapsed;
                            ErrorPanel.Visibility = Visibility.Visible;
                            BrowserRefreshButton.Visibility = Visibility.Visible;
                            BrowserMessage.Text = ex.Message;
                            return;
                        }
                        var response = new List<GameBananaCategory>();
                        try
                        {
                            response = JsonConvert.DeserializeObject<List<GameBananaCategory>>(responseString);
                        }
                        catch (Exception)
                        {
                            LoadingBar.Visibility = Visibility.Collapsed;
                            ErrorPanel.Visibility = Visibility.Visible;
                            BrowserRefreshButton.Visibility = Visibility.Visible;
                            BrowserMessage.Text = "Something went wrong while deserializing the categories...";
                            return;
                        }
                        if (!cats.ContainsKey((GameFilter)gameCounter))
                            cats.Add((GameFilter)gameCounter, new Dictionary<TypeFilter, List<GameBananaCategory>>());
                        if (!cats[(GameFilter)gameCounter].ContainsKey((TypeFilter)counter))
                            cats[(GameFilter)gameCounter].Add((TypeFilter)counter, response);
                        // Make more requests if needed
                        if (totalPages > 1)
                        {
                            for (int i = 2; i <= totalPages; i++)
                            {
                                var requestUrlPage = $"{requestUrl}&_nPage={i}";
                                try
                                {
                                    responseString = await httpClient.GetStringAsync(requestUrlPage);
                                    responseString = Regex.Replace(responseString, @"""(\d+)""", @"$1");
                                }
                                catch (HttpRequestException ex)
                                {
                                    LoadingBar.Visibility = Visibility.Collapsed;
                                    ErrorPanel.Visibility = Visibility.Visible;
                                    BrowserRefreshButton.Visibility = Visibility.Visible;
                                    switch (Regex.Match(ex.Message, @"\d+").Value)
                                    {
                                        case "443":
                                            BrowserMessage.Text = "No internet connection is available.";
                                            break;
                                        case "500":
                                        case "503":
                                        case "504":
                                            BrowserMessage.Text = "GameBanana's servers are unavailable.";
                                            break;
                                        default:
                                            BrowserMessage.Text = ex.Message;
                                            break;
                                    }
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    LoadingBar.Visibility = Visibility.Collapsed;
                                    ErrorPanel.Visibility = Visibility.Visible;
                                    BrowserRefreshButton.Visibility = Visibility.Visible;
                                    BrowserMessage.Text = ex.Message;
                                    return;
                                }
                                try
                                {
                                    response = JsonConvert.DeserializeObject<List<GameBananaCategory>>(responseString);
                                }
                                catch (Exception)
                                {
                                    LoadingBar.Visibility = Visibility.Collapsed;
                                    ErrorPanel.Visibility = Visibility.Visible;
                                    BrowserRefreshButton.Visibility = Visibility.Visible;
                                    BrowserMessage.Text = "Something went wrong while deserializing the categories...";
                                    return;
                                }
                                cats[(GameFilter)gameCounter][(TypeFilter)counter] = cats[(GameFilter)gameCounter][(TypeFilter)counter].Concat(response).ToList();
                            }
                        }
                        counter++;
                    }
                    gameCounter++;
                }
            }
            GameFilterBox.SelectedIndex = GameBox.SelectedIndex;
            CatBox.ItemsSource = All.Concat(cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
            SubCatBox.ItemsSource = None;
            BrowserBackground.ImageSource = bgs[GameFilterBox.SelectedIndex];
            filterSelect = true;
            CatBox.SelectedIndex = 0;
            SubCatBox.SelectedIndex = 0;
            filterSelect = false;
            RefreshFilter();
            selected = true;
        }
        // Only fetch categories and items when Browse Mods tab is first selected
        private void OnTabSelected(object sender, RoutedEventArgs e)
        {
            if (!selected)
            {
                InitializeBrowser();
            }
        }
        private static int page = 1;
        private void DecrementPage(object sender, RoutedEventArgs e)
        {
            --page;
            RefreshFilter();
        }
        private void IncrementPage(object sender, RoutedEventArgs e)
        {
            ++page;
            RefreshFilter();
        }
        // Event to refresh browser when it failed to initialize
        private void BrowserRefresh(object sender, RoutedEventArgs e)
        {
            if (!selected)
                InitializeBrowser();
            else
                RefreshFilter();
        }
        // Initializze Resources as BitmapImages
        private static List<BitmapImage> bgs;
        private void InitBgs()
        {
            bgs = new List<BitmapImage>();
            var bgUrls = new string[] {
            "pack://application:,,,/AemulusPackageManager;component/Assets/p3f.png",
            "pack://application:,,,/AemulusPackageManager;component/Assets/p4g.png",
            "pack://application:,,,/AemulusPackageManager;component/Assets/p5.png",
            "pack://application:,,,/AemulusPackageManager;component/Assets/sophia.png"};
            foreach (var bg in bgUrls)
                bgs.Add(new BitmapImage(new Uri(bg)));
        }
        // Used to not trigger events while another event is still functioning
        private static bool filterSelect;
        // Filter events
        private async void RefreshFilter()
        {
            GameFilterBox.IsEnabled = false;
            FilterBox.IsEnabled = false;
            TypeBox.IsEnabled = false;
            CatBox.IsEnabled = false;
            SubCatBox.IsEnabled = false;
            LeftPage.IsEnabled = false;
            RightPage.IsEnabled = false;
            PageBox.IsEnabled = false;
            PerPageBox.IsEnabled = false;
            ErrorPanel.Visibility = Visibility.Collapsed;
            filterSelect = true;
            PageBox.SelectedValue = page;
            filterSelect = false;
            Page.Text = $"Page {page}";
            LoadingBar.Visibility = Visibility.Visible;
            FeedBox.Visibility = Visibility.Collapsed;
            await FeedGenerator.GetFeed(page, (GameFilter)GameFilterBox.SelectedIndex, (TypeFilter)TypeBox.SelectedIndex, (FeedFilter)FilterBox.SelectedIndex, (GameBananaCategory)CatBox.SelectedItem,
                (GameBananaCategory)SubCatBox.SelectedItem, (PerPageBox.SelectedIndex + 1) * 10);
            FeedBox.ItemsSource = FeedGenerator.CurrentFeed.Records;
            if (FeedGenerator.error)
            {
                LoadingBar.Visibility = Visibility.Collapsed;
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Visible;
                switch (Regex.Match(FeedGenerator.exception.Message, @"\d+").Value)
                {
                    case "443":
                        BrowserMessage.Text = "No internet connection is available.";
                        break;
                    case "500":
                    case "503":
                    case "504":
                        BrowserMessage.Text = "GameBanana's servers are unavailable.";
                        break;
                    default:
                        BrowserMessage.Text = FeedGenerator.exception.Message;
                        break;
                }
                return;
            }
            if (page < FeedGenerator.CurrentFeed.TotalPages)
                RightPage.IsEnabled = true;
            if (page != 1)
                LeftPage.IsEnabled = true;
            if (FeedBox.Items.Count > 0)
            {
                FeedBox.ScrollIntoView(FeedBox.Items[0]);
                FeedBox.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorPanel.Visibility = Visibility.Visible;
                BrowserRefreshButton.Visibility = Visibility.Collapsed;
                BrowserMessage.Visibility = Visibility.Visible;
                BrowserMessage.Text = "Aemulus couldn't find any mods.";
            }
            PageBox.ItemsSource = Enumerable.Range(1, FeedGenerator.CurrentFeed.TotalPages);

            LoadingBar.Visibility = Visibility.Collapsed;
            CatBox.IsEnabled = true;
            SubCatBox.IsEnabled = true;
            TypeBox.IsEnabled = true;
            FilterBox.IsEnabled = true;
            PageBox.IsEnabled = true;
            PerPageBox.IsEnabled = true;
            GameFilterBox.IsEnabled = true;
        }

        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                page = 1;
                RefreshFilter();
            }
        }
        private void GameFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                // Change background to match game
                BrowserBackground.ImageSource = bgs[GameFilterBox.SelectedIndex];
                filterSelect = true;
                // Set categories
                if (cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void TypeFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                filterSelect = true;
                // Set categories
                if (cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == 0))
                    CatBox.ItemsSource = All.Concat(cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == 0).OrderBy(y => y.ID));
                else
                    CatBox.ItemsSource = None;
                CatBox.SelectedIndex = 0;
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void MainFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !filterSelect)
            {
                filterSelect = true;
                // Set Categories
                var cat = (GameBananaCategory)CatBox.SelectedValue;
                if (cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Any(x => x.RootID == cat.ID))
                    SubCatBox.ItemsSource = All.Concat(cats[(GameFilter)GameFilterBox.SelectedIndex][(TypeFilter)TypeBox.SelectedIndex].Where(x => x.RootID == cat.ID).OrderBy(y => y.ID));
                else
                    SubCatBox.ItemsSource = None;
                SubCatBox.SelectedIndex = 0;
                filterSelect = false;
                page = 1;
                RefreshFilter();
            }
        }
        private void SubFilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                page = 1;
                RefreshFilter();
            }
        }
        // Change number of columns depending on window width
        private void UniformGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as UniformGrid;
            if (grid.ActualWidth > 2000)
                grid.Columns = 6;
            else if (grid.ActualWidth > 1600)
                grid.Columns = 5;
            else if (grid.ActualWidth > 1200)
                grid.Columns = 4;
            else
                grid.Columns = 3;
        }
        private void GameBanana_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gameID = "";
                switch (GameFilterBox.SelectedIndex)
                {
                    case 0:
                        gameID = "8502";
                        break;
                    case 1:
                        gameID = "8263";
                        break;
                    case 2:
                        gameID = "7545";
                        break;
                    case 3:
                        gameID = "9099";
                        break;
                }
                var ps = new ProcessStartInfo($"https://gamebanana.com/games/{gameID}")
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Couldn't open up GameBanana ({ex.Message})");
            }
        }
        private void PageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!filterSelect && IsLoaded)
            {
                page = (int)PageBox.SelectedValue;
                RefreshFilter();
            }
        }

        private async void CloseDesc_Click(object sender, RoutedEventArgs e)
        {
            DescPanel.Visibility = Visibility.Collapsed;
            await Task.Run(() =>
            {
                if (MusicPlayer.SourceProvider.MediaPlayer != null)
                MusicPlayer.SourceProvider.MediaPlayer.ResetMedia();
            });
            duration = 0;
            AudioProgress.Value = 0;
        }

        private void ImageLeft_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (--imageCounter == -1)
                imageCounter = imageCount - 1;
            var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
            Screenshot.Source = image;
            CaptionText.Text = item.Media[imageCounter].Caption;
            BigScreenshot.Source = image;
            BigCaptionText.Text = item.Media[imageCounter].Caption;
            if (!String.IsNullOrEmpty(CaptionText.Text))
            {
                BigCaptionText.Visibility = Visibility.Visible;
                CaptionText.Visibility = Visibility.Visible;
            }
            else
            {
                BigCaptionText.Visibility = Visibility.Collapsed;
                CaptionText.Visibility = Visibility.Collapsed;
            }
        }

        private void ImageRight_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var item = button.DataContext as GameBananaRecord;
            if (++imageCounter == imageCount)
                imageCounter = 0;
            var image = new BitmapImage(new Uri($"{item.Media[imageCounter].Base}/{item.Media[imageCounter].File}"));
            Screenshot.Source = image;
            CaptionText.Text = item.Media[imageCounter].Caption;
            BigScreenshot.Source = image;
            BigCaptionText.Text = item.Media[imageCounter].Caption;
            if (!String.IsNullOrEmpty(CaptionText.Text))
            {
                BigCaptionText.Visibility = Visibility.Visible;
                CaptionText.Visibility = Visibility.Visible;
            }
            else
            {
                BigCaptionText.Visibility = Visibility.Collapsed;
                CaptionText.Visibility = Visibility.Collapsed;
            }
        }

        private async void PlayAudio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PauseAudio.IsEnabled = true;
            PauseAudio.Visibility = Visibility.Visible;
            PlayAudio.Visibility = Visibility.Collapsed;
            await Task.Run(() =>
            {
                MusicPlayer.SourceProvider.MediaPlayer.Play();
            });
        }

        private async void PauseAudio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PlayAudio.Visibility = Visibility.Visible;
            PauseAudio.Visibility = Visibility.Collapsed;
            await Task.Run(() =>
            {
                MusicPlayer.SourceProvider.MediaPlayer.Pause();
            });
        }

        private async void ReplayAudio_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            endReached = false;
            PauseAudio.IsEnabled = true;
            ReplayAudio.Visibility = Visibility.Collapsed;
            PauseAudio.Visibility = Visibility.Visible;
            await Task.Run(() =>
            {
                MusicPlayer.SourceProvider.MediaPlayer.Stop();
                MusicPlayer.SourceProvider.MediaPlayer.Play();
            });
        }
        private bool IsDragging;

        private async void AudioProgress_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !IsDragging)
            {
                if (endReached)
                    ReplayAudio.Visibility = Visibility.Collapsed;
                IsDragging = true;
                var paused = false;
                if (MusicPlayer.SourceProvider.MediaPlayer.IsPlaying())
                    MusicPlayer.SourceProvider.MediaPlayer.Pause();
                else
                    paused = true;
                AudioProgress.IsEnabled = true;
                PlayAudio.Visibility = Visibility.Visible;
                PauseAudio.Visibility = Visibility.Collapsed;
                // Await on another thread to not freeze main thread
                await Task.Run(() =>
                {
                    TimeSpan total = TimeSpan.FromMilliseconds(duration);
                    while (e.LeftButton == MouseButtonState.Pressed)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TimeSpan current = TimeSpan.FromMilliseconds(duration * (AudioProgress.Value / 100));
                            AudioDuration.Text = string.Format("{0:D1}:{1:D2} / {2:D1}:{3:D2}",
                                current.Minutes, current.Seconds,
                                total.Minutes, total.Seconds);
                        });
                    }
                });
                MusicPlayer.SourceProvider.MediaPlayer.Position = (float)(AudioProgress.Value / 100);
                if (TimeSpan.FromMilliseconds(duration).Seconds > 30)
                    MusicPlayer.SourceProvider.MediaPlayer.Position += 0.11f;
                var pos = MusicPlayer.SourceProvider.MediaPlayer.Position;
                if (endReached)
                {
                    AudioProgress.IsEnabled = false;
                    PauseAudio.Visibility = Visibility.Visible;
                    PlayAudio.Visibility = Visibility.Collapsed;
                    MusicPlayer.SourceProvider.MediaPlayer.Stop();
                    MusicPlayer.SourceProvider.MediaPlayer.Play();
                    MusicPlayer.SourceProvider.MediaPlayer.Position = pos;
                    endReached = false;
                }
                if (!paused)
                {
                    PauseAudio.Visibility = Visibility.Visible;
                    PlayAudio.Visibility = Visibility.Collapsed;
                    MusicPlayer.SourceProvider.MediaPlayer.Play();
                }
                IsDragging = false;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MusicPlayer.SourceProvider.MediaPlayer != null)
                MusicPlayer.SourceProvider.MediaPlayer.Audio.Volume = (int)VolumeSlider.Value;
        }

        private double unmuteVolume;
        private void VolumeIcon_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Mute
            if (VolumeIcon.Icon == FontAwesome5.EFontAwesomeIcon.Solid_VolumeUp)
            {
                VolumeIcon.Height = 30;
                VolumeIcon.Icon = FontAwesome5.EFontAwesomeIcon.Solid_VolumeMute;
                unmuteVolume = VolumeSlider.Value;
                VolumeSlider.Value = 0;
            }
            // Unmute
            else
            {
                VolumeIcon.Height = 38;
                VolumeIcon.Icon = FontAwesome5.EFontAwesomeIcon.Solid_VolumeUp;
                VolumeSlider.Value = unmuteVolume;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BigScreenshot.MaxHeight = ActualHeight - 240;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.R:
                        if (RefreshButton.IsHitTestVisible)
                            RefreshCommand();
                        break;
                    case Key.B:
                        if (MergeButton.IsHitTestVisible)
                            MergeCommand();
                        break;
                    case Key.L:
                        if (LaunchButton.IsHitTestVisible)
                            LaunchCommand();
                        break;
                    case Key.O:
                        if (FolderButton.IsHitTestVisible)
                            FolderCommand();
                        break;
                    case Key.N:
                        if (NewButton.IsHitTestVisible)
                            NewCommand();
                        break;
                    case Key.E:
                        if (ConfigButton.IsHitTestVisible)
                            ConfigWdwCommand();
                        break;
                    case Key.H:
                        if (VisibilityButton.IsHitTestVisible)
                            ToggleHiddenCommand();
                        break;
                }
            }
        }

        bool loadoutHandled = false;
        private void LoadoutBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || loadoutHandled)
                return;
            if (lastLoadout == null)
                lastLoadout = LoadoutBox.Items[0].ToString();
            // Add a new loadout
            if (LoadoutBox.SelectedItem != null && LoadoutBox.SelectedItem.ToString() == "Add new loadout")
            {
                CreateLoadout createLoadout = new CreateLoadout(game);
                createLoadout.ShowDialog();
                if (createLoadout.name == "")
                {
                    LoadoutBox.SelectedItem = lastLoadout;
                    Console.WriteLine("[INFO] Cancelled loadout creation");
                }
                else
                {
                    // Copy existing loadout
                    if ((bool)createLoadout.CopyLoadout.IsChecked)
                    {
                        Console.WriteLine($"[INFO] Copying {lastLoadout} loadout to {createLoadout.name}");
                        string configPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config";
                        FileIOWrapper.Copy($@"{configPath}\{game}\{lastLoadout}.xml", $@"{configPath}\{game}\{createLoadout.name}.xml");
                    }
                    else
                    {
                        // Disable and unhide everything (otherwise the new loadout will copy the current)
                        foreach (var package in PackageList)
                        {
                            package.enabled = false;
                            package.hidden = false;
                        }
                        // Order the package list alphebetically 
                        var packages = PackageList.ToArray();
                        IOrderedEnumerable<Package> orderedPackages = packages.OrderBy(item => item.path);
                        PackageList.Clear();
                        foreach (var item in orderedPackages)
                        {
                            PackageList.Add(item);
                        }
                    }

                    // Add the new loadout
                    var currentLoadout = LoadoutBox.SelectedItem;
                    loadoutUtils.LoadoutItems.RemoveAt(loadoutUtils.LoadoutItems.Count - 1);
                    loadoutUtils.LoadoutItems.Add(createLoadout.name);
                    var loadout = loadoutUtils.LoadoutItems.ToArray();
                    // Sort the loadout alphabetically
                    IOrderedEnumerable<string> orderLoadout = loadout.OrderBy(item => item);
                    loadoutUtils.LoadoutItems.Clear();
                    foreach (string item in orderLoadout)
                    {
                        loadoutUtils.LoadoutItems.Add(item);
                    }


                    // Update loadouts
                    loadoutUtils.LoadoutItems.Add("Add new loadout");
                    LoadoutBox.SelectedItem = createLoadout.name;

                    updatePackages();
                }
            }
            // Actually change the loadout
            else if (LoadoutBox.SelectedItem != null)
            {
                lastLoadout = LoadoutBox.SelectedItem.ToString();

                // Update the config
                selectedLoadout = LoadoutBox.SelectedItem.ToString();
                switch (game)
                {
                    case "Persona 3 FES":
                        config.p3fConfig.loadout = selectedLoadout;
                        break;
                    case "Persona 4 Golden":
                        config.p4gConfig.loadout = selectedLoadout;
                        break;
                    case "Persona 5":
                        config.p5Config.loadout = selectedLoadout;
                        break;
                    case "Persona 5 Strikers":
                        config.p5sConfig.loadout = selectedLoadout;
                        break;
                }
                updateConfig();
                Console.WriteLine($"[INFO] Loadout changed to {LoadoutBox.SelectedItem}");
            }
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            // Load the new loadout xml
            if (FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{LoadoutBox.SelectedItem}.xml") && LoadoutBox.SelectedItem != null && lastGame == game)
            {
                try
                {
                    using (FileStream streamWriter = FileIOWrapper.Open($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config\{game}\{LoadoutBox.SelectedItem}.xml", FileMode.Open))
                    {
                        try
                        {
                            // Call the Deserialize method and cast to the object type.
                            packages = (Packages)xp.Deserialize(streamWriter);
                            PackageList = packages.packages;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($@"[ERROR] Couldn't deseralize Config\{game}\{LoadoutBox.SelectedItem}.xml ({ex.Message})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Invalid package loadout {LoadoutBox.SelectedItem}.xml ({ex.Message})");
                }

                showHidden.Value = packages.showHiddenPackages;

                var oldDisplayedPackages = DisplayedPackages.ToList();
                
                // Recreate DisplayedPackages to match the newly selected loadout
                DisplayedPackages.Clear();

                // Add all the valid packages to the displayed packages
                foreach (var package in PackageList.ToList())
                {
                    // Get the current displayed metadata for the package
                    var updatedPackage = oldDisplayedPackages.Find(dp => dp.id == package.id);
                    if (updatedPackage != null && FileIOWrapper.Exists($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Packages\{game}\{updatedPackage.path}\Package.xml"))
                    {
                        // Change the displayed metadata from the old loadout to match the new one
                        updatedPackage.enabled = package.enabled;
                        updatedPackage.hidden = package.hidden;
                        // Add the updated displayed metadata back to the list
                        DisplayedPackages.Add(updatedPackage);
                    }
                }

                Refresh();
                updatePackages();
            }
        }

        private void HideItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ModGrid.SelectedItems)
            {
                DisplayedMetadata package = (DisplayedMetadata)item;
                if (package != null)
                {
                    Console.WriteLine($"[INFO] Hiding {package.name}");
                    package.hidden = true;
                    foreach (var p in PackageList.ToList())
                    {
                        if (p.path == package.path)

                            p.hidden = true;
                    }
                }

            }
            updatePackages();
            UpdateDisplay();

        }

        private void ToggleHiddenClicked(object sender, RoutedEventArgs e)
        {
            ToggleHiddenCommand();
        }

        private void ToggleHiddenCommand()
        {
            Console.WriteLine($"[INFO] {(showHidden.Value ? "Hiding" : "Showing")} hidden packages");
            showHidden.Value = !showHidden.Value;
            packages.showHiddenPackages = showHidden.Value;
            updatePackages();
        }

        // Class to make the showingHidden bool observable
        public class Prop<T> : INotifyPropertyChanged
        {
            private T _value;
            public T Value
            {
                get { return _value; }
                set { _value = value; NotifyPropertyChanged("Value"); }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            internal void NotifyPropertyChanged(String propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ShowItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ModGrid.SelectedItems)
            {
                DisplayedMetadata package = (DisplayedMetadata)item;
                if (package != null)
                {
                    Console.WriteLine($"[INFO] Showing {package.name}");
                    package.hidden = false;
                    foreach (var p in PackageList.ToList())
                    {
                        if (p.path == package.path)

                            p.hidden = false;
                    }
                }

            }
            updatePackages();
            UpdateDisplay();
        }

        private void EditLoadoutButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine($"[INFO] Began editing of {LoadoutBox.SelectedItem} loadout");
            CreateLoadout createLoadout = new CreateLoadout(game, LoadoutBox.SelectedItem.ToString());
            createLoadout.ShowDialog();
            string configPath = $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\Config";

            if (createLoadout.deleteLoadout)
            {
                FileIOWrapper.Delete($@"{configPath}\{game}\{LoadoutBox.SelectedItem}.xml");
                Console.WriteLine($"[INFO] Successfully deleted {LoadoutBox.SelectedItem} loadout");
                loadoutUtils.LoadoutItems.Remove(LoadoutBox.SelectedItem.ToString());
                LoadoutBox.SelectedIndex = 0;
            }
            else if (createLoadout.name != "")
            {
                // Rename the current loadout to match the new name
                FileIOWrapper.Move($@"{configPath}\{game}\{LoadoutBox.SelectedItem}.xml", $@"{configPath}\{game}\{createLoadout.name}.xml");

                // Add the new loadout
                var currentLoadout = LoadoutBox.SelectedItem;
                loadoutUtils.LoadoutItems.RemoveAt(loadoutUtils.LoadoutItems.Count - 1);
                loadoutUtils.LoadoutItems.Remove(currentLoadout.ToString());
                loadoutUtils.LoadoutItems.Add(createLoadout.name);
                var loadout = loadoutUtils.LoadoutItems.ToArray();
                // Sort the loadout alphabetically
                IOrderedEnumerable<string> orderLoadout = loadout.OrderBy(item => item);
                loadoutUtils.LoadoutItems.Clear();
                foreach (string item in orderLoadout)
                {
                    loadoutUtils.LoadoutItems.Add(item);
                }

                // Update loadouts
                loadoutUtils.LoadoutItems.Add("Add new loadout");
                LoadoutBox.SelectedItem = createLoadout.name;

                Console.WriteLine($"[INFO] Finished editing loadout");
            }
            else
            {
                Console.WriteLine($"[INFO] Cancelled editing of {LoadoutBox.SelectedItem} loadout");
            }

        }
    }
}
