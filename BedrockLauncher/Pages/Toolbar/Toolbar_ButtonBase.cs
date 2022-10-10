using System;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Pages.Toolbar
{
    public class Toolbar_ButtonBase : Grid
    {

        public event EventHandler Click;
        protected void ToolbarButtonBase_Click(object sender, RoutedEventArgs e)
        {
            if (this.Click != null) this.Click(sender, e);
        }
    }
}
