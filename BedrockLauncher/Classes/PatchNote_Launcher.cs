using BedrockLauncher.Core;
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
    public class PatchNote_Launcher : GithubReleaseInfo
    {
        public bool isLatest { get; set; } = false;
        public SolidColorBrush Title_Foreground
        {
            get
            {
                if (isBeta) return Brushes.Gray;
                else if (prerelease) return Brushes.OrangeRed;
                else return Brushes.Green;
            }
        }
        public Visibility CurrentBox_Visibility => isLatest ? Visibility.Visible : Visibility.Collapsed;

        public PatchNote_Launcher(GithubReleaseInfo original) : base(original)
        {

        }

        public PatchNote_Launcher()
        {

        }
    }
}
