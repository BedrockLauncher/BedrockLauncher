using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.WPF;
using TheArtOfDev.HtmlRenderer.Core;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TheArtOfDev.HtmlRenderer.Core.Entities;

namespace BedrockLauncher.Controls
{

    public class HtmlControl2 : HtmlControl
    {
        public HtmlControl2() : base()
        {

        }

        protected override void OnImageLoad(HtmlImageLoadEventArgs e)
        {
            
        }

        protected override void OnRender(DrawingContext context)
        {

        }
    }

    public class HtmlPanel2 : HtmlPanel
    {

        protected Grid Corner;


        public HtmlContainer Container
        {
            get
            {
                return this._htmlContainer;
            }
        }


        static HtmlPanel2()
        {
            
        }

        public HtmlPanel2() : base()
        {
            Corner = new Grid();
            Corner.Name = "Corner";
            Corner.Width = 18;
            Corner.Height = 18;
            Corner.VerticalAlignment = VerticalAlignment.Bottom;
            Corner.HorizontalAlignment = HorizontalAlignment.Right;
            Corner.Visibility = Visibility.Collapsed;

            this.AddVisualChild(Corner);
            this.AddLogicalChild(Corner);
        }

        protected override int VisualChildrenCount
        {
            get { return 3; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
                return _verticalScrollBar;
            else if (index == 1)
                return _horizontalScrollBar;
            else if (index == 2)
                return Corner;
            return null;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            constraint = base.MeasureOverride(constraint);
            if (_verticalScrollBar.Visibility == Visibility.Visible && _horizontalScrollBar.Visibility == Visibility.Visible) Corner.Visibility = Visibility.Visible;
            else Corner.Visibility = Visibility.Collapsed;
            Corner.Background = _verticalScrollBar.Background;
            Panel.SetZIndex(Corner, Panel.GetZIndex(_verticalScrollBar) + 1);
            return constraint;
        }

        protected override Size ArrangeOverride(Size bounds)
        {
            bounds = base.ArrangeOverride(bounds);

            var scrollHeight = HtmlHeight(bounds) + Padding.Top + Padding.Bottom;
            scrollHeight = scrollHeight > 1 ? scrollHeight : 1;
            var scrollWidth = HtmlWidth(bounds) + Padding.Left + Padding.Right;
            scrollWidth = scrollWidth > 1 ? scrollWidth : 1;

            var vScrollBar_area = new Rect(System.Math.Max(bounds.Width - _verticalScrollBar.Width - BorderThickness.Right, 0), BorderThickness.Top, _verticalScrollBar.Width, scrollHeight);
            var hScrollBar_area = new Rect(BorderThickness.Left, System.Math.Max(bounds.Height - _horizontalScrollBar.Height - BorderThickness.Bottom, 0), scrollWidth, _horizontalScrollBar.Height);

            Corner.Arrange(new Rect(vScrollBar_area.X, hScrollBar_area.Y, _verticalScrollBar.Width, _horizontalScrollBar.Height));
            return bounds;
        }
    }
}
