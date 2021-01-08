using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AemulusModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception occured:\n{e.Exception.Message}\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK,
                             MessageBoxImage.Error);

            e.Handled = true;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                ((MainWindow)Current.MainWindow).ModGrid.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).ConfigButton.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).MergeButton.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).NewButton.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).LaunchButton.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).RefreshButton.IsHitTestVisible = true;
                ((MainWindow)Current.MainWindow).GameBox.IsHitTestVisible = true;
                Mouse.OverrideCursor = null;
            });
        }

    }
}
