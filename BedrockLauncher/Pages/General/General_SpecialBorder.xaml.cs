using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BedrockLauncher.Pages.General
{
    /// <summary>
    /// Interaction logic for SpecialBorder.xaml
    /// </summary>
    public partial class General_SpecialBorder : Grid, INotifyPropertyChanged
    {
        public static DependencyProperty BorderSizeProperty;
        public static DependencyProperty BorderColorProperty;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        static General_SpecialBorder()
        {
            BorderSizeProperty = DependencyProperty.Register("BorderSize", typeof(double), typeof(General_SpecialBorder), new FrameworkPropertyMetadata(2d, FrameworkPropertyMetadataOptions.None));
            BorderColorProperty = DependencyProperty.Register("BorderColor", typeof(SolidColorBrush), typeof(General_SpecialBorder), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.None));
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

        public General_SpecialBorder()
        {
            InitializeComponent();
        }
    }
}
