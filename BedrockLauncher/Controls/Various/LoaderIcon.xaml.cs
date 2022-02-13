using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BedrockLauncher.Controls.Various
{
    /// <summary>
    /// Interaction logic for LoaderIcon.xaml
    /// </summary>
    public partial class LoaderIcon : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        //public double CanvasWidth 120 
        //public double CubeWidth 60
        //public double CubeDropPoint -65
        //public double CubeWidthNeg -60

        private double CalculateActualWidth()
        {
            if (double.IsNaN(this.ActualWidth) || this.ActualWidth == 0) return 1;
            else return this.ActualWidth;
        }

        public double CanvasWidth
        {
            get
            {
                return CalculateActualWidth();
            }
        } 
        public double CubeWidth
        {
            get
            {
                return CalculateActualWidth() / 2;
            }
        }
        public double CubeDropPoint
        {
            get
            {
                return -(CalculateActualWidth() / 2) - 5;
            }
        }
        public double CubeWidthNeg
        {
            get
            {
                return -(CalculateActualWidth() / 2);
            }
        }


        public LoaderIcon()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            OnPropertyChanged(nameof(CanvasWidth));
            OnPropertyChanged(nameof(CubeWidth));
            OnPropertyChanged(nameof(CubeDropPoint));
            OnPropertyChanged(nameof(CubeWidthNeg));
        }
    }
}
