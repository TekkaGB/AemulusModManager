using System.Threading;
using System.Windows;

namespace AemulusModManager.Windows
{
    /// <summary>
    /// Interaction logic for UpdateProgressBox.xaml
    /// </summary>
    public partial class UpdateProgressBox : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        public bool finished = false;
        public UpdateProgressBox(CancellationTokenSource cancellationTokenSource)
        {
            InitializeComponent();
            this.cancellationTokenSource = cancellationTokenSource;
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!finished)
            {
                NotificationBox notification = new NotificationBox("Are you sure you want to cancel the download?\nAll progress will be lost.", false);
                notification.ShowDialog();
                notification.Activate();
                if (!notification.YesNo)
                {
                    e.Cancel = true;
                }
                else
                {
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}
