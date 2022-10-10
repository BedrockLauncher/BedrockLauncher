using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BedrockLauncher.Pages.General
{
    /// <summary>
    /// Interaction logic for Marquee.xaml
    /// </summary>
    public partial class General_Marquee : ContentControl
    {
        private bool isPlaying = false;
        private DoubleAnimation _doubleAnimation { get; set; } = new DoubleAnimation();
        private Storyboard _storyBoard { get; set; } = new Storyboard();
        private FrameworkElement _contentPresenter { get; set; }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.RegisterAttached(nameof(Duration), typeof(Duration), typeof(General_Marquee));
        private Duration _Duration { get; set; } = Duration.Automatic;
        public Duration Duration
        {
            get
            {
                return _Duration;
            }
            set
            {
                _Duration = value;
                SetValue(DurationProperty, value);
                Animate();
            }
        }

        public General_Marquee()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            _contentPresenter = Template.FindName("PART_Content", this) as FrameworkElement;
        }

        private void Animate()
        {
            if (isPlaying)
            {
                _storyBoard.Stop(_contentPresenter);
                _storyBoard.Children.Clear();
                _storyBoard = null;
                _storyBoard = new Storyboard();
                _doubleAnimation = null;
                _doubleAnimation = new DoubleAnimation();
                isPlaying = false;
            }

            if (IsLoaded)
            {
                _doubleAnimation.From = this.ActualWidth;
                _doubleAnimation.To = -_contentPresenter.ActualWidth;

                _doubleAnimation.RepeatBehavior = RepeatBehavior.Forever;
                _doubleAnimation.Duration = Duration;
                Storyboard.SetTargetProperty(_doubleAnimation, new PropertyPath("(Canvas.Left)"));
                _storyBoard.Children.Add(_doubleAnimation);

                _storyBoard.Begin(_contentPresenter, true);
                isPlaying = true;
            }
        }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            Animate();
        }

        private void ContentControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Animate();
        }

        private void ContentControl_LayoutUpdated(object sender, EventArgs e)
        {

        }

        private void PART_Content_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Animate();
        }
    }
}
