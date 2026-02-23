using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using BedrockLauncher.ViewModels;

namespace BedrockLauncher.Pages.FetchLatestVersion
{
    public partial class FetchLatestVersionPage : Page
    {
        public static FetchLatestVersionPage FetchLatestVersionContainer { get; private set; }

        public FetchLatestVersionPage()
        {
            InitializeComponent();
        }

        public void Page_Initialized(object sender, EventArgs e)
        {
            FetchLatestVersionContainer = this;
        }
    }
}

