using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace JemExtensions.WPF.Attributes
{
    public static class CustomDesignAttributes
    {
        private static bool? _isInDesignMode;

        public static DependencyProperty VerticalScrollToProperty = DependencyProperty.RegisterAttached(
          "VerticalScrollTo",
          typeof(double),
          typeof(CustomDesignAttributes),
          new PropertyMetadata(ScrollToChanged));

        public static DependencyProperty HorizontalScrollToProperty = DependencyProperty.RegisterAttached(
          "HorizontalScrollTo",
          typeof(double),
          typeof(CustomDesignAttributes),
          new PropertyMetadata(ScrollToChanged));

        private static bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                {
                    var prop = DesignerProperties.IsInDesignModeProperty;
                    _isInDesignMode =
                      (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
                }

                return _isInDesignMode.Value;
            }
        }

        public static void SetVerticalScrollTo(UIElement element, double value)
        {
            element.SetValue(VerticalScrollToProperty, value);
        }

        public static double GetVerticalScrollTo(UIElement element)
        {
            return (double)element.GetValue(VerticalScrollToProperty);
        }

        public static void SetHorizontalScrollTo(UIElement element, double value)
        {
            element.SetValue(HorizontalScrollToProperty, value);
        }

        public static double GetHorizontalTo(UIElement element)
        {
            return (double)element.GetValue(HorizontalScrollToProperty);
        }

        private static void ScrollToChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!IsInDesignMode)
                return;
            ScrollViewer viewer = d as ScrollViewer;
            if (viewer == null)
                return;
            if (e.Property == VerticalScrollToProperty)
            {
                viewer.ScrollToVerticalOffset((double)e.NewValue);
            }
            else if (e.Property == HorizontalScrollToProperty)
            {
                viewer.ScrollToHorizontalOffset((double)e.NewValue);
            }
        }
    }
}

