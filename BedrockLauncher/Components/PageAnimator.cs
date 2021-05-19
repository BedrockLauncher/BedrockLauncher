using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using BedrockLauncher.Downloaders;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Components
{
    public static class PageAnimator
    {
        private static double stored_width { get; set; }
        private static double stored_height { get; set; }
        private static double stored_min_width { get; set; }
        private static double stored_min_height { get; set; }

        private static bool inProgress = false;

        private static Page CurrentContent { get; set; }

        public static void FrameFadeIn(Frame frame, object content)
        {
            frame.Opacity = 0;
            frame.Navigate(content);
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };

            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Frame.OpacityProperty));
            Storyboard.SetTarget(animation, frame);
            storyboard.Begin();
        }

        public static void FrameFadeOut(Frame frame, object content)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };
            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Frame.OpacityProperty));
            Storyboard.SetTarget(animation, frame);
            storyboard.Completed += (sender, e) => frame.Navigate(content);
            storyboard.Begin();
        }

        public static void FrameSwipe(Frame frame, object content, ExpandDirection direction)
        {
            bool isPage = (content is Page);
            if (inProgress || !isPage) return;
            inProgress = true;

            CurrentContent = content as Page;

            stored_width = CurrentContent.ActualWidth;
            stored_height = CurrentContent.ActualHeight;

            stored_min_width = CurrentContent.MinWidth;
            stored_min_height = CurrentContent.MinHeight;

            Duration _duration = new Duration(TimeSpan.FromMilliseconds(175));

            ThicknessAnimation animation0 = new ThicknessAnimation();

            switch (direction)
            {
                case ExpandDirection.Left:
                    animation0.From = new Thickness(0, 0, 100, 0);
                    break;
                case ExpandDirection.Right:
                    animation0.From = new Thickness(100, 0, 0, 0);
                    break;
                case ExpandDirection.Up:
                    animation0.From = new Thickness(0, 0, 0, 100);
                    break;
                case ExpandDirection.Down:
                    animation0.From = new Thickness(0, 100, 0, 0);
                    break;
            }




            animation0.To = new Thickness(0, 0, 0, 0);
            animation0.Completed += Animation0_Completed;
            animation0.Duration = _duration;

            DoubleAnimation animation1 = new DoubleAnimation();
            animation1.From = 0;
            animation1.To = 1;
            animation1.Duration = _duration;

            CurrentContent.MinWidth = stored_width;
            CurrentContent.MinHeight = stored_height;

            frame.BeginAnimation(Frame.OpacityProperty, animation1);
            frame.BeginAnimation(Frame.MarginProperty, animation0);
        }

        private static void Animation0_Completed(object sender, EventArgs e)
        {
            CurrentContent.MinWidth = stored_min_width;
            CurrentContent.MinHeight = stored_min_height;
            inProgress = false;
        }
    }
}
