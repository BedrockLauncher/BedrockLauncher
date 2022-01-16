using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace BedrockLauncher.Classes.Launcher
{
    public class AppPatchNote
    {
        public string buildTitle { get; set; } = "BuildTitle";
        public string buildVersion { get; set; } = "v0.0.0";
        public string buildChanges { get; set; } = string.Empty;
        public string buildDate { get; set; } = "MM/YYYY";
        public SolidColorBrush buildTitle_Foreground { get; set; } = Brushes.Gray;
        public Visibility CurrentBox_Visibility { get; set; } = Visibility.Collapsed;
    }
}
