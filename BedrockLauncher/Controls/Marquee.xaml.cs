using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BedrockLauncher.Controls
{
    /// <summary>
    /// Interaction logic for Marquee.xaml
    /// </summary>
    public partial class Marquee : ContentControl
    {
        private DoubleAnimation _doubleAnimation { get; set; } = new DoubleAnimation();
        private Storyboard _storyBoard { get; set; } = new Storyboard();
        private FrameworkElement _contentPresenter { get; set; }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.RegisterAttached(nameof(Duration), typeof(Duration), typeof(Marquee));
        private Duration _Duration { get; set; }
        public Duration Duration
        {
            get
            {
                return _Duration;
            }
            set
            {
                SetValue(DurationProperty, value);
                _Duration = value;
                Animate();
            }
        }

        public Marquee()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            _contentPresenter = Template.FindName("PART_Content", this) as FrameworkElement;
        }

        private void Animate()
        {
            if (IsLoaded)
            {
                _doubleAnimation.From = this.ActualWidth;
                _doubleAnimation.To = -_contentPresenter.ActualWidth;

                _doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
                _doubleAnimation.Duration = Duration;
                Storyboard.SetTargetProperty(_doubleAnimation, new PropertyPath("(Canvas.Left)"));
                _storyBoard.Children.Add(_doubleAnimation);

                _storyBoard.Begin(_contentPresenter, true);
            }
        }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            Animate();
        }
    }
}
