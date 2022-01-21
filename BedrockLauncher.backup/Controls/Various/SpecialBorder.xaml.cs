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
using BedrockLauncher.Components;

namespace BedrockLauncher.Controls.Various
{
    /// <summary>
    /// Interaction logic for SpecialBorder.xaml
    /// </summary>
    public partial class SpecialBorder : Grid, INotifyPropertyChanged
    {
        public static DependencyProperty BorderSizeProperty;
        public static DependencyProperty BorderColorProperty;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        static SpecialBorder()
        {
            BorderSizeProperty = DependencyProperty.Register("BorderSize", typeof(double), typeof(SpecialBorder), new FrameworkPropertyMetadata(2d, FrameworkPropertyMetadataOptions.None));
            BorderColorProperty = DependencyProperty.Register("BorderColor", typeof(SolidColorBrush), typeof(SpecialBorder), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.None));
        }

        public double BorderSize
        {
            get  { return (double)GetValue(BorderSizeProperty); }
            set 
            { 
                SetValue(BorderSizeProperty, value);
                OnPropertyChanged(nameof(BorderSize));
                OnPropertyChanged(nameof(BorderSizeG));
            }
        }

        public GridLength BorderSizeG
        {
            get { return new GridLength(BorderSize); }
            set 
            { 
                SetValue(BorderSizeProperty, value.Value);
                OnPropertyChanged(nameof(BorderSize));
                OnPropertyChanged(nameof(BorderSizeG));
            }
        }

        public SolidColorBrush BorderColor
        {
            get { return (SolidColorBrush)GetValue(BorderColorProperty); }
            set 
            { 
                SetValue(BorderColorProperty, value);
                OnPropertyChanged(nameof(BorderColor));
            }
        }

        public SpecialBorder()
        {
            InitializeComponent();
        }
    }
}
