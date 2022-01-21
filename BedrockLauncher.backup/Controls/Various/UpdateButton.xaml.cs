using BedrockLauncher.Extensions;
using BedrockLauncher.ViewModels;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BedrockLauncher.Controls.Various
{
    /// <summary>
    /// Interaction logic for UpdateButton.xaml
    /// </summary>
    public partial class UpdateButton : Grid
    {
        public UpdateButton()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
        }
        public void ShowUpdateButton()
        {
            Dispatcher.Invoke(ShowAdvancementButton);
        }
        public async void ShowUpdateButton(int time = 5000)
        {
            ShowAdvancementButton();
            await Task.Delay(time);
            HideAdvancementButton();
        }
        private void HideAdvancementButton()
        {
            if (this.Visibility == Visibility.Visible)
            {
                // hide update 'advancement'
                Storyboard storyboard2 = new Storyboard();
                ThicknessAnimation animation2 = new ThicknessAnimation
                {
                    From = new Thickness(0, 5, 5, 0),
                    To = new Thickness(0, -75, 5, 0),
                    Duration = new Duration(TimeSpan.FromMilliseconds(500))
                };
                storyboard2.Children.Add(animation2);
                storyboard2.Completed += Storyboard2_Completed;
                Storyboard.SetTargetProperty(animation2, new PropertyPath(Border.MarginProperty));
                Storyboard.SetTarget(animation2, this);
                storyboard2.Begin();

            }

            void Storyboard2_Completed(object sender, EventArgs e)
            {
                this.Visibility = Visibility.Collapsed;
            }

        }

        private void ShowAdvancementButton()
        {
            if (this.Visibility == Visibility.Collapsed)
            {
                this.Visibility = Visibility.Visible;
                // show update 'advancement'
                Storyboard storyboard = new Storyboard();
                ThicknessAnimation animation = new ThicknessAnimation
                {
                    From = new Thickness(0, -75, 5, 0),
                    To = new Thickness(0, 5, 5, 0),
                    BeginTime = TimeSpan.FromSeconds(2),
                    Duration = new Duration(TimeSpan.FromMilliseconds(500))
                };
                storyboard.Children.Add(animation);
                Storyboard.SetTargetProperty(animation, new PropertyPath(Border.MarginProperty));
                Storyboard.SetTarget(animation, this);
                storyboard.Begin();
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Updater.UpdateButton_Click(sender, e);
        }
    }
}
