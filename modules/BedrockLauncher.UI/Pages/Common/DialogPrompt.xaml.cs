using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Threading.Tasks;
using BedrockLauncher.UI.Interfaces;

namespace BedrockLauncher.UI.Pages.Common
{
    /// <summary>
    /// Логика взаимодействия для DialogPrompt.xaml
    /// </summary>
    public partial class DialogPrompt : Page
    {

        public DialogResult DialogResult { get; set; } = DialogResult.None;
        public bool isOptionalChecked { get; set; }

        public static async Task<Tuple<DialogResult, bool>> ShowDialog_YesNo_Optional(string title, string content, string optionalContent, bool optionalState, params object[] args)
        {

            var prompt = new DialogPrompt();

            prompt.DialogTitle.Text = string.Format(title, args);
            prompt.DialogText.Text = string.Format(content, args);
            prompt.DialogOptional.Text = string.Format(optionalContent, args);

            prompt.DialogOptionalCheckbox.IsChecked = optionalState;

            prompt.DialogOptionalCheckbox.Visibility = Visibility.Visible;


            Handler.SetDialogFrame(prompt);

            return await Task.Run(prompt.DialogWaitWithOptional);
        }

        public static async Task<DialogResult> ShowDialog_YesNo(string title, string content, params object[] args)
        {

            var prompt = new DialogPrompt();

            prompt.DialogTitle.Text = string.Format(title, args);
            prompt.DialogText.Text = string.Format(content, args);

            Handler.SetDialogFrame(prompt);

            return await Task.Run(prompt.DialogWait);
        }

        public static async Task<DialogResult> ShowDialog_YesNoCancel(string title, string content)
        {
            var prompt = new DialogPrompt();

            prompt.DialogTitle.Text = title;
            prompt.DialogText.Text = content;
    
            prompt.CancelButton.Visibility = Visibility.Visible;

            Handler.SetDialogFrame(prompt);

            return await Task.Run(prompt.DialogWait);
        }
        private DialogResult DialogWait()
        {
            while (DialogResult == DialogResult.None) { }
            return DialogResult;
        }
        private Tuple<DialogResult, bool> DialogWaitWithOptional()
        {
            while (DialogResult == DialogResult.None) { }
            return new Tuple<DialogResult,bool>(DialogResult, isOptionalChecked);
        }

        public static IDialogHander Handler { get; private set; }

        public static void SetHandler(IDialogHander _handler)
        {
            Handler = _handler;
        } 

        public DialogPrompt()
        {
            InitializeComponent();
            DialogOptionalCheckbox.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Handler.SetDialogFrame(null);
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DialogResult.No;
            Handler.SetDialogFrame(null);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Handler.SetDialogFrame(null);
        }

        private void DialogOptionalCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            isOptionalChecked = (sender as System.Windows.Controls.CheckBox).IsChecked.Value;
        }
    }
}
