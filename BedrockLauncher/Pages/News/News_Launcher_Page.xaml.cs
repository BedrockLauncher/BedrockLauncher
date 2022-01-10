using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BedrockLauncher.Methods;
using BedrockLauncher.Downloaders;
using BedrockLauncher.Handlers;
using MdXaml;

namespace BedrockLauncher.Pages.News
{
    /// <summary>
    /// Interaction logic for News_Launcher_Page.xaml
    /// </summary>
    public partial class News_Launcher_Page : Page
    {

        private UpdateHandler updater;
        private bool HasPreloaded = false;

        public News_Launcher_Page()
        {
            InitializeComponent();
        }

        public News_Launcher_Page(UpdateHandler updater)
        {
            InitializeComponent();
            this.updater = updater;
        }

        private void Page_Loaded(object sender, EventArgs e)
        {
            if (!HasPreloaded)
            {
                Task.Run(RefreshData);
                HasPreloaded = true;
            }
        }

        private async void RefreshData()
        {
            await Dispatcher.InvokeAsync(() => 
            {
                UpdatesList.Children.Clear();
                bool isFirstItem = true;
                string latest_name = this.FindResource("LauncherNewsPage_Title_Text").ToString();
                foreach (var item in updater.Notes)
                {
                    bool isBeta = item.url.Contains(BedrockLauncher.Core.GithubAPI.BETA_URL);
                    if (isFirstItem)
                    {
                        GenerateEntry(latest_name, item, isBeta, true);
                        isFirstItem = false;
                    }
                    else GenerateEntry(item.name, item, isBeta);
                }

                void GenerateEntry(string name, BedrockLauncher.Core.UpdateNote item, bool isBeta, bool isLatest = false)
                {
                    string body = item.body;
                    string tag = item.tag_name;

                    Controls.Items.LauncherUpdateItem launcherUpdateItem = new Controls.Items.LauncherUpdateItem();

                    body = body.Replace("\r\n", "\r\n\r\n");

                    Markdown engine = new Markdown();
                    engine.DocumentStyle = this.FindResource("FlowDocument_Style") as Style;
                    engine.NormalParagraphStyle = this.FindResource("FlowDocument_Style_Paragrath") as Style;
                    engine.CodeStyle = this.FindResource("FlowDocument_CodeBlock") as Style;
                    engine.CodeBlockStyle = this.FindResource("FlowDocument_CodeBlock") as Style;
                    FlowDocument document = engine.Transform(body);

                    if (isLatest) launcherUpdateItem.buildTitle.Foreground = Brushes.Goldenrod;
                    else if (isBeta) launcherUpdateItem.buildTitle.Foreground = Brushes.Gold;
                    else launcherUpdateItem.buildTitle.Foreground = Brushes.White;

                    if (tag == BedrockLauncher.Core.Properties.Settings.Default.Version) launcherUpdateItem.CurrentBox.Visibility = Visibility.Visible;

                    launcherUpdateItem.buildTitle.Text = name;
                    launcherUpdateItem.buildVersion.Text = string.Format("v{0}{1}", tag, (isBeta ? " (Beta)" : ""));
                    launcherUpdateItem.buildChanges.Document = document;
                    launcherUpdateItem.buildDate.Text = item.published_at.ToString();

                    UpdatesList.Children.Add(launcherUpdateItem);
                }
            });


        }

        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await Task.Run(updater.CheckForUpdatesAsync);
        }

        private void ForceUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            updater.UpdateButton_Click(sender, e);
        }

        private void CheckForUpdatesButton_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(RefreshData);
        }
    }
}
