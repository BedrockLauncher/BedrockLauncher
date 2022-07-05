using System;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Controls
{
    public class ToolbarButtonBase : Grid
    {

        public event EventHandler Click;
        protected void ToolbarButtonBase_Click(object sender, RoutedEventArgs e)
        {
            if (this.Click != null) this.Click(sender, e);
        }
    }
}
