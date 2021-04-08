using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int SW_RESTORE = 9;
        protected static bool AlreadyRunning()
        {
            bool running = false;
            try
            {
                // Getting collection of process  
                Process currentProcess = Process.GetCurrentProcess();

                // Check with other process already running   
                foreach (var p in Process.GetProcesses())
                {
                    if (p.Id != currentProcess.Id) // Check running process   
                    {
                        if (p.ProcessName.Equals(currentProcess.ProcessName) && p.MainModule.FileName.Equals(currentProcess.MainModule.FileName))
                        {
                            running = true;
                            NotificationBox message = new NotificationBox($"Aemulus is already running!");
                            message.ShowDialog();
                            IntPtr hFound = p.MainWindowHandle;
                            if (IsIconic(hFound)) // If application is in ICONIC mode then  
                                ShowWindow(hFound, SW_RESTORE);
                            SetForegroundWindow(hFound); // Activate the window, if process is already running  
                            Application.Current.Shutdown(0);
                            break;
                        }
                    }
                }
            }
            catch { }
            return running;
        }
        public static bool InstallGBHandler()
        {
            string AppPath = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe");
            string protocolName = $"Aemulus";
            try
            {
                var reg = Registry.CurrentUser.CreateSubKey(@"Software\Classes\aemulus");
                reg.SetValue("", $"URL:{protocolName}");
                reg.SetValue("URL Protocol", "");
                reg = reg.CreateSubKey(@"shell\open\command");
                reg.SetValue("", $"\"{AppPath}\" \"%1\"");
                reg.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            //InstallGBHandler();
            MainWindow mw = new MainWindow();
            bool running = AlreadyRunning();
            if (!running)
            {
                mw.Show();
            }
            //if (e.Args.Length > 0)
                //MessageBox.Show($"{e.Args[0]}");
        }
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception occured:\n{e.Exception.Message}\n\nInner Exception:\n{e.Exception.InnerException}\n\nStack Trace:\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK,
                             MessageBoxImage.Error);

            e.Handled = true;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                foreach (var button in ((MainWindow)Current.MainWindow).buttons)
                {
                    button.IsHitTestVisible = true;
                    if (((MainWindow)Current.MainWindow).game == "Persona 3 FES")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0x4f, 0xa4, 0xff));
                    else if (((MainWindow)Current.MainWindow).game == "Persona 4 Golden")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xfe, 0xed, 0x2b));
                    else if (((MainWindow)Current.MainWindow).game == "Persona 5")
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                    else
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x37, 0x00));
                }
                ((MainWindow)Current.MainWindow).ModGrid.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).GameBox.IsHitTestVisible = true;
                Mouse.OverrideCursor = null;
            });
        }

    }
}
