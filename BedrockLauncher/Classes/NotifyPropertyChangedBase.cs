using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms.Design;
using Newtonsoft.Json;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;

using MainWindow = BedrockLauncher.MainWindow;

namespace BedrockLauncher.Classes
{
    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public void ProgressBarIsIndeterminate(bool isIndeterminate)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)Application.Current.MainWindow).progressSizeHack.IsIndeterminate = isIndeterminate;
            });
        }
        public void ProgressBarUpdate(double downloadedBytes, double totalSize)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)Application.Current.MainWindow).progressSizeHack.Value = downloadedBytes;
                ((MainWindow)Application.Current.MainWindow).progressSizeHack.Maximum = totalSize;
                int.Parse(downloadedBytes.ToString());
                ((MainWindow)Application.Current.MainWindow).ProgressBarText.Text = (Math.Round(downloadedBytes / 1024 / 1024, 2)).ToString() + " MB / " + (Math.Round(totalSize / 1024 / 1024, 2)).ToString() + " MB";
            });
        }

    }
}
