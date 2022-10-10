using BedrockLauncher.Classes;
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

namespace BedrockLauncher.Pages.Preview.Profile
{
    /// <summary>
    /// Interaction logic for EditProfileScreen.xaml
    /// </summary>
    public partial class EditProfileScreen : Page
    {
        public Component_AddProfileContainer ProfileControl { get; set; }
        public EditProfileScreen(BLProfile editable)
        {
            InitializeComponent();
            ProfileControl = new Component_AddProfileContainer(editable);
            ProfileControl.GoBack += ProfileControl_GoBack;
            ProfileControl.Confirm += ProfileControl_Confirm;
            ProfileControlContainer.Children.Add(ProfileControl);
        }
        public EditProfileScreen()
        {
            InitializeComponent();
            ProfileControl = new Component_AddProfileContainer();
            ProfileControl.GoBack += ProfileControl_GoBack;
            ProfileControl.Confirm += ProfileControl_Confirm;
            ProfileControlContainer.Children.Add(ProfileControl);
        }

        private void ProfileControl_GoBack(object sender, EventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }

        private void ProfileControl_Confirm(object sender, EventArgs e)
        {
            ViewModels.MainViewModel.Default.SetOverlayFrame(null);
        }
    }
}
