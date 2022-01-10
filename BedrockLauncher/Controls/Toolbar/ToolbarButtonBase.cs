using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BedrockLauncher.Controls.Toolbar
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
