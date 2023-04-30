using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BedrockLauncher.UI.Components
{
    public class Navigator
    {

        public static bool AnimatePageTransitions { get; set; } = true;

        private ExpandDirection rightDirection = ExpandDirection.Down;
        private ExpandDirection leftDirection = ExpandDirection.Up;


        public int CurrentPageIndex { get; set; } = -1;
        public int LastPageIndex { get; set; } = -2;

        public void UpdatePageIndex(int index)
        {
            LastPageIndex = CurrentPageIndex;
            CurrentPageIndex = index;
        }


        public Navigator(bool isUpAndDown = false)
        {
            if (isUpAndDown)
            {
                rightDirection = ExpandDirection.Down;
                leftDirection = ExpandDirection.Up;
            }
            else
            {
                rightDirection = ExpandDirection.Left;
                leftDirection = ExpandDirection.Right;

            }
        }

        public void Navigate(Frame source, object content)
        {
            bool animate = AnimatePageTransitions;

            if (!animate)
            {
                source.Dispatcher.Invoke(() => source.Navigate(content));
                return;
            }
            int current = this.CurrentPageIndex;
            int last = this.LastPageIndex;
            if (current == last) return;

            ExpandDirection direction;

            if (current > last) direction = rightDirection;
            else direction = leftDirection;

            Task.Run(() => BedrockLauncher.UI.Components.PageAnimator.FrameSwipe(source, content, direction));
        }
    }
}
