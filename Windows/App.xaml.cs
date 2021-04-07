using System;
using System.Diagnostics;
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
        protected override void OnStartup(StartupEventArgs e)
        {
            if (AlreadyRunning())
            {
                return;
            }
            MainWindow mw = new MainWindow();
            mw.Show();
            DispatcherUnhandledException += App_DispatcherUnhandledException;
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
