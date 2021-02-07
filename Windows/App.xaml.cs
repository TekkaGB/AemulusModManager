using System;
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
        // give the mutex a  unique name
        private const string MutexName = "##||ThisApp||##";
        // declare the mutex
        private readonly Mutex _mutex;
        // overload the constructor
        bool createdNew;
        public App()
        {
            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                // if the mutex already exists, notify and quit
                NotificationBox message = new NotificationBox("This program is already running!");
                message.ShowDialog();
                Application.Current.Shutdown(0);
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!createdNew) return;
            // overload the OnStartup so that the main window 
            // is constructed and visible
            MainWindow mw = new MainWindow();
            mw.Show();
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception occured:\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK,
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
                    else
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
                }
                ((MainWindow)Current.MainWindow).ModGrid.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).GameBox.IsHitTestVisible = true;
                Mouse.OverrideCursor = null;
            });
        }

    }
}
