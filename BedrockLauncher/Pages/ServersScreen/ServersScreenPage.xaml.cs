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
using ServerTab;

namespace BedrockLauncher.Pages.ServersScreen
{
    /// <summary>
    /// Логика взаимодействия для PlayScreenPage.xaml
    /// </summary>
    public partial class ServersScreenPage : Page
    {
        private ServersTab serverTab;
        public ServersScreenPage(ServersTab serverTab)
        {
            InitializeComponent();
            this.serverTab = serverTab;
        }
        

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (List<Server> servers in serverTab.getServerList().Servers.Values)
            {
                foreach (Server server in servers)
                {
                    System.Windows.Controls.Button newBtn = new Button();
                    newBtn.Content = server.Name;
                    newBtn.Name = "Button" + server.Name;
                    sp.Children.Add(newBtn);
                }
            }

            //buildVersion.Text = "v" + updater.getLatestTag();
            //buildChanges.Text = updater.getLatestTagDescription();
        }

    }
}
