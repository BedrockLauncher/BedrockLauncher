using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace BedrockLauncher.Core.Components
{
    public static class PageAnimator
    {
        private class AnimationArgs
        {
           public double stored_width { get; set; }
           public double stored_height { get; set; }
           public double stored_min_width { get; set; }
           public double stored_min_height { get; set; }
           public double stored_max_width { get; set; }
           public double stored_max_height { get; set; }
           public Page CurrentContent { get; set; }
        }

        private static bool SuperSmoothAnimations = false;

        #region General Methods

        private static void StorePageValues(AnimationArgs args)
        {
            if (args.CurrentContent == null) return;

            args.stored_min_width = args.CurrentContent.MinWidth;
            args.stored_min_height = args.CurrentContent.MinHeight;

            args.stored_max_width = args.CurrentContent.MaxWidth;
            args.stored_max_height = args.CurrentContent.MaxHeight;
        }
        private static void SetPageValuesForAnimation(AnimationArgs args)
        {
            if (args.CurrentContent == null) return;
            args.CurrentContent.MinWidth = args.stored_width;
            args.CurrentContent.MinHeight = args.stored_height;
            args.CurrentContent.MaxWidth = args.stored_width;
            args.CurrentContent.MaxHeight = args.stored_height;
        }
        private static void ResetPageValues(AnimationArgs args)
        {
            if (args.CurrentContent == null) return;
            args.CurrentContent.MinWidth = args.stored_min_width;
            args.CurrentContent.MinHeight = args.stored_min_height;
            args.CurrentContent.MaxWidth = args.stored_max_width;
            args.CurrentContent.MaxHeight = args.stored_max_height;
        }

        private static IEasingFunction GetSwipeAnimationEasingFunction()
        {

            if (SuperSmoothAnimations) return new QuarticEase() { EasingMode = EasingMode.EaseOut };
            else return null;
        }

        private static int GetFadeSpeed()
        {
            if (SuperSmoothAnimations) return 500;
            else return 200;
        }

        private static int GetSwipeSpeed()
        {
            if (SuperSmoothAnimations) return 1000;
            else return 175;
        }

        private static int GetSwipeSize()
        {
            if (SuperSmoothAnimations) return 200;
            else return 150;
        }

        private static ThicknessAnimation GetSwipeAnimation(AnimationArgs animationArgs, double size, Duration duration, ExpandDirection direction)
        {
            ThicknessAnimation animation0 = new ThicknessAnimation();

            switch (direction)
            {
                case ExpandDirection.Left:
                    animation0.From = new Thickness(-size, 0, size, 0);
                    break;
                case ExpandDirection.Right:
                    animation0.From = new Thickness(size, 0, -size, 0);
                    break;
                case ExpandDirection.Up:
                    animation0.From = new Thickness(0, -size, 0, size);
                    break;
                case ExpandDirection.Down:
                    animation0.From = new Thickness(0, size, 0, -size);
                    break;
            }

            animation0.To = new Thickness(0, 0, 0, 0);
            animation0.Completed += (sender, e) =>
            {
                ResetPageValues(animationArgs);
            };
            animation0.Duration = duration;
            animation0.EasingFunction = GetSwipeAnimationEasingFunction();
            return animation0;
        }
        private static DoubleAnimation GetFadeAnimation(Duration duration, bool fadeIn)
        {
            DoubleAnimation animation1 = new DoubleAnimation();
            animation1.From = (fadeIn ? 0 : 1);
            animation1.To = (fadeIn ? 1 : 0);
            animation1.Duration = duration;
            return animation1;
        }
        private static void SetCurrentPage(AnimationArgs animationArgs, Frame frame, object content)
        {
            if (content is Page) animationArgs.CurrentContent = content as Page;
            else animationArgs.CurrentContent = null;

            animationArgs.stored_width = frame.ActualWidth;
            animationArgs.stored_height = frame.ActualHeight;
        }

        #endregion

        #region Fade Animations
        public static void FrameFadeIn(Frame frame, object content)
        {
            frame.Opacity = 0;
            frame.Navigate(content);
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = GetFadeAnimation(new Duration(TimeSpan.FromMilliseconds(GetFadeSpeed())), true);
            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Frame.OpacityProperty));
            Storyboard.SetTarget(animation, frame);
            storyboard.Begin();
        }
        public static void FrameFadeOut(Frame frame, object content)
        {
            Storyboard storyboard = new Storyboard();
            DoubleAnimation animation = GetFadeAnimation(new Duration(TimeSpan.FromMilliseconds(GetFadeSpeed())), false);
            storyboard.Children.Add(animation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Frame.OpacityProperty));
            Storyboard.SetTarget(animation, frame);
            storyboard.Completed += (sender, e) => frame.Navigate(content);
            storyboard.Begin();
        }
        #endregion

        #region Swipe Animations
        public static async void FrameSwipe_OverlayIn(Frame frame, object content)
        {
            var storyboard = await Task.Run(() => FrameSwipe_Base(frame, content, ExpandDirection.Up, true, true));
            await frame.Dispatcher.InvokeAsync(() => frame.Navigate(content));
            await storyboard.Dispatcher.InvokeAsync(() => storyboard.Begin());
        }
        public static async void FrameSwipe_OverlayOut(Frame frame, object content)
        {
            var storyboard = await FrameSwipe_Base(frame, frame.Content, ExpandDirection.Up, true, false);
            storyboard.Completed += (sender, e) => frame.Navigate(content);
            await storyboard.Dispatcher.InvokeAsync(() => storyboard.Begin());
        }
        public static async void FrameSwipe(Frame frame, object content, ExpandDirection direction)
        {
            await frame.Dispatcher.InvokeAsync(() => frame.Navigate(content));
            var storyboard = await Task.Run(() => FrameSwipe_Base(frame, content, direction, true, true));
            await storyboard.Dispatcher.InvokeAsync(() => storyboard.Begin());
        }
        public static async Task<Storyboard> FrameSwipe_Base(Frame frame, object content, ExpandDirection direction, bool useFade, bool fadeIn)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AnimationArgs animationArgs = new AnimationArgs();

                SetCurrentPage(animationArgs, frame, content);
                StorePageValues(animationArgs);
                SetPageValuesForAnimation(animationArgs);

                Storyboard storyboard = new Storyboard();
                Duration duration = new Duration(TimeSpan.FromMilliseconds(GetSwipeSpeed()));

                if (useFade)
                {
                    var fadeAnim = GetFadeAnimation(duration, fadeIn);
                    storyboard.Children.Add(fadeAnim);
                    Storyboard.SetTargetProperty(fadeAnim, new PropertyPath(Frame.OpacityProperty));
                    Storyboard.SetTarget(fadeAnim, frame);
                }

                var swipeAnim = GetSwipeAnimation(animationArgs, GetSwipeSize(), duration, direction);
                storyboard.Children.Add(swipeAnim);
                Storyboard.SetTargetProperty(swipeAnim, new PropertyPath(Frame.MarginProperty));
                Storyboard.SetTarget(swipeAnim, frame);
                return storyboard;
            });
        }
        #endregion




    }
}
