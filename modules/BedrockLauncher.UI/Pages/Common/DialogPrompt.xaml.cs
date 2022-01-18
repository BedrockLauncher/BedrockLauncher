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

        public static async Task<DialogResult> ShowDialog_YesNo(string title, string content, params object[] args)
        {

            var prompt = new DialogPrompt();

            prompt.DialogTitle.Text = string.Format(title, args);
            prompt.DialogText.Text = string.Format(content, args);

            prompt.CancelButton.Visibility = Visibility.Collapsed;

            Handler.SetDialogFrame(prompt);

            return await Task.Run(prompt.DialogWait);
        }

        public static async Task<DialogResult> ShowDialog_YesNoCancel(string title, string content)
        {
            var prompt = new DialogPrompt();

            prompt.DialogTitle.Text = title;
            prompt.DialogText.Text = content;

            Handler.SetDialogFrame(prompt);

            return await Task.Run(prompt.DialogWait);
        }

        private DialogResult DialogWait()
        {
            while (DialogResult == DialogResult.None) { }
            return DialogResult;
        }

        public static IDialogHander Handler { get; private set; }

        public static void SetHandler(IDialogHander _handler)
        {
            Handler = _handler;
        } 

        public DialogPrompt()
        {
            InitializeComponent();
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
    }
}
