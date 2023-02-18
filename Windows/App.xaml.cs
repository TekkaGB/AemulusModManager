using AemulusModManager.Utilities;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        protected static Process AlreadyRunning()
        {
            Process otherProcess = null;
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
                            otherProcess = p;
                            break;
                        }
                    }
                }
            }
            catch { }
            return otherProcess;
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
            InstallGBHandler();
            var otherProcess = AlreadyRunning();
            var running = otherProcess != null;
            var oneClick = e.Args.Length > 0;
            MainWindow mw = new MainWindow(running, oneClick);
            if (!running)
            {
                mw.Show();
            }
            if (oneClick)
                new PackageDownloader().Download(e.Args[0], running);
            else if (running)
            {
                NotificationBox message = new NotificationBox($"Aemulus is already running!");
                message.ShowDialog();
                IntPtr hFound = otherProcess.MainWindowHandle;
                if (IsIconic(hFound)) // If application is in ICONIC mode then  
                    ShowWindow(hFound, SW_RESTORE);
                SetForegroundWindow(hFound); // Activate the window, if process is already running  
                Application.Current.Shutdown(0);
            }
        }
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception occured:\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK,
                             MessageBoxImage.Error);

            e.Handled = true;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ((MainWindow)Current.MainWindow).EnableUI();
                ((MainWindow)Current.MainWindow).ModGrid.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).GameBox.IsHitTestVisible = true;
                Mouse.OverrideCursor = null;
            });
        }

    }
}
