using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BedrockLauncher.Controls.Common
{
    /// <summary>
    /// Interaction logic for MessageBoxEX.xaml
    /// </summary>
    public partial class MessageBoxEX : Window
    {
        #region Control Decleration
        private string Caption
        {
            get
            {
                return Title;
            }
            set
            {
                Title = value;
            }
        }
        private string Message
        {
            get
            {
                return TextBlock_Message.Text;
            }
            set
            {
                TextBlock_Message.Text = value;
            }
        }

        private string OkButtonText
        {
            get
            {
                return Label_Ok.Content.ToString();
            }
            set
            {
                Label_Ok.Content = value.TryAddKeyboardAccellerator();
            }
        }
        private string CancelButtonText
        {
            get
            {
                return Label_Cancel.Content.ToString();
            }
            set
            {
                Label_Cancel.Content = value.TryAddKeyboardAccellerator();
            }
        }
        private string YesButtonText
        {
            get
            {
                return Label_Yes.Content.ToString();
            }
            set
            {
                Label_Yes.Content = value.TryAddKeyboardAccellerator();
            }
        }
        private string NoButtonText
        {
            get
            {
                return Label_No.Content.ToString();
            }
            set
            {
                Label_No.Content = value.TryAddKeyboardAccellerator();
            }
        }
        private string YesToAllButtonText
        {
            get
            {
                return Label_YesToAll.Content.ToString();
            }
            set
            {
                Label_YesToAll.Content = value.TryAddKeyboardAccellerator();
            }
        }
        private string NoToAllButtonText
        {
            get
            {
                return Label_NoToAll.Content.ToString();
            }
            set
            {
                Label_NoToAll.Content = value.TryAddKeyboardAccellerator();
            }
        }
        private string AbortButtonText
        {
            get
            {
                return Label_Abort.Content.ToString();
            }
            set
            {
                Label_Abort.Content = value.TryAddKeyboardAccellerator();
            }
        }
        private string RetryButtonText
        {
            get
            {
                return Label_Retry.Content.ToString();
            }
            set
            {
                Label_Retry.Content = value.TryAddKeyboardAccellerator();
            }
        }

        public MessageBoxEXResult Result { get; set; }

        internal MessageBoxEX(string message)
        {
            InitializeComponent();

            Message = message;
            Image_MessageBox.Visibility = System.Windows.Visibility.Collapsed;
            DisplayButtons(MessageBoxEXButton.OK);
        }

        internal MessageBoxEX(MessageBoxEXArgs param)
        {
            InitializeComponent();

            Message = param.title;
            Caption = param.caption;

            if (param.icon == MessageBoxImage.None) Image_MessageBox.Visibility = System.Windows.Visibility.Collapsed;
            else DisplayImage(param.icon);
            DisplayButtons(param.button);
        }

        internal MessageBoxEX(string message, string caption)
        {
            InitializeComponent();

            Message = message;
            Caption = caption;
            Image_MessageBox.Visibility = System.Windows.Visibility.Collapsed;
            DisplayButtons(MessageBoxEXButton.OK);
        }

        internal MessageBoxEX(string message, string caption, MessageBoxEXButton button)
        {
            InitializeComponent();

            Message = message;
            Caption = caption;
            Image_MessageBox.Visibility = System.Windows.Visibility.Collapsed;

            DisplayButtons(button);
        }

        internal MessageBoxEX(string message, string caption, MessageBoxImage image)
        {
            InitializeComponent();

            Message = message;
            Caption = caption;
            DisplayImage(image);
            DisplayButtons(MessageBoxEXButton.OK);
        }

        internal MessageBoxEX(string message, string caption, MessageBoxEXButton button, MessageBoxImage image)
        {
            InitializeComponent();

            Message = message;
            Caption = caption;
            Image_MessageBox.Visibility = System.Windows.Visibility.Collapsed;

            DisplayButtons(button);
            DisplayImage(image);
        }

        private void DisplayButtons(MessageBoxEXButton button)
        {
            Button_OK.Visibility = button.useOK ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Button_Yes.Visibility = button.useYes ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Button_No.Visibility = button.useNo ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Button_Cancel.Visibility = button.useCancel ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Button_Abort.Visibility = button.useAbort ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Button_Retry.Visibility = button.useRetry ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Button_YesToAll.Visibility = button.useYesToAll ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            Button_NoToAll.Visibility = button.useNoToAll ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            switch (button.Default)
            {
                case MessageBoxEXResult.None:
                    break;
                case MessageBoxEXResult.OK:
                    Button_OK.Focus();
                    break;
                case MessageBoxEXResult.Cancel:
                    Button_Cancel.Focus();
                    break;
                case MessageBoxEXResult.Yes:
                    Button_Yes.Focus();
                    break;
                case MessageBoxEXResult.No:
                    Button_No.Focus();
                    break;
                case MessageBoxEXResult.Abort:
                    Button_Abort.Focus();
                    break;
                case MessageBoxEXResult.Retry:
                    Button_Retry.Focus();
                    break;
                case MessageBoxEXResult.YesToAll:
                    Button_YesToAll.Focus();
                    break;
                case MessageBoxEXResult.NoToAll:
                    Button_NoToAll.Focus();
                    break;
                default:
                    break;
            }
        }

        private void DisplayImage(MessageBoxImage image)
        {
            Icon icon;

            switch (image)
            {
                case MessageBoxImage.Exclamation:       // Enumeration value 48 - also covers "Warning"
                    icon = SystemIcons.Exclamation;
                    break;
                case MessageBoxImage.Error:             // Enumeration value 16, also covers "Hand" and "Stop"
                    icon = SystemIcons.Hand;
                    break;
                case MessageBoxImage.Information:       // Enumeration value 64 - also covers "Asterisk"
                    icon = SystemIcons.Information;
                    break;
                case MessageBoxImage.Question:
                    icon = SystemIcons.Question;
                    break;
                default:
                    icon = SystemIcons.Information;
                    break;
            }

            Image_MessageBox.Source = icon.ToImageSource();
            Image_MessageBox.Visibility = System.Windows.Visibility.Visible;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.OK;
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.Cancel;
            Close();
        }

        private void Button_Yes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.Yes;
            Close();
        }

        private void Button_No_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.No;
            Close();
        }

        private void Button_Retry_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.Retry;
            Close();
        }

        private void Button_Abort_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.Abort;
            Close();
        }

        private void Button_YesToAll_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.YesToAll;
            Close();
        }

        private void Button_NoToAll_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxEXResult.NoToAll;
            Close();
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Displays a message box that has a message and returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult Show(string messageBoxText)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText);
            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message and a title bar caption; and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult Show(string messageBoxText, string caption)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption);
            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box in front of the specified window. The message box displays a message and returns a result.
        /// </summary>
        /// <param name="owner">A System.Windows.Window that represents the owner window of the message box.</param>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult Show(Window owner, string messageBoxText)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText);
            msg.Owner = owner;
            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box in front of the specified window. The message box displays a message and title bar caption; and it returns a result.
        /// </summary>
        /// <param name="owner">A System.Windows.Window that represents the owner window of the message box.</param>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult Show(Window owner, string messageBoxText, string caption)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption);
            msg.Owner = owner;
            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, title bar caption, and button; and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="button">A System.Windows.MessageBoxButton value that specifies which button or buttons to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult Show(string messageBoxText, string caption, MessageBoxEXButton button)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, button);
            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, title bar caption, button, and icon; and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="button">A System.Windows.MessageBoxButton value that specifies which button or buttons to display.</param>
        /// <param name="icon">A System.Windows.MessageBoxImage value that specifies the icon to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult Show(string messageBoxText, string caption, MessageBoxEXButton button, MessageBoxImage icon)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, button, icon);
            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box flexible to your liking.
        /// </summary>
        /// <param name="param">Defines what the messagebox does</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult Show(MessageBoxEXArgs param)
        {
            MessageBoxEX msg = new MessageBoxEX(param);
            if (param.OKText != string.Empty) msg.OkButtonText = param.OKText;
            if (param.YesText != string.Empty) msg.YesButtonText = param.YesText;
            if (param.NoText != string.Empty) msg.NoButtonText = param.NoText;
            if (param.CancelText != string.Empty) msg.CancelButtonText = param.CancelText;
            if (param.RetryText != string.Empty) msg.RetryButtonText = param.RetryText;
            if (param.AbortText != string.Empty) msg.AbortButtonText = param.AbortText;
            if (param.YesToAllText != string.Empty) msg.YesToAllButtonText = param.YesToAllText;
            if (param.NoToAllText != string.Empty) msg.NoToAllButtonText = param.NoToAllText;
            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, title bar caption, and OK button with a custom System.String value for the button's text; and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="okButtonText">A System.String that specifies the text to display within the OK button.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowOK(string messageBoxText, string caption, string okButtonText)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.OK);
            msg.OkButtonText = okButtonText;

            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, title bar caption, OK button with a custom System.String value for the button's text, and icon; and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="okButtonText">A System.String that specifies the text to display within the OK button.</param>
        /// <param name="icon">A System.Windows.MessageBoxImage value that specifies the icon to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowOK(string messageBoxText, string caption, string okButtonText, MessageBoxImage icon)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.OK, icon);
            msg.OkButtonText = okButtonText;

            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, caption, and OK/Cancel buttons with custom System.String values for the buttons' text;
        /// and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="okButtonText">A System.String that specifies the text to display within the OK button.</param>
        /// <param name="cancelButtonText">A System.String that specifies the text to display within the Cancel button.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowOKCancel(string messageBoxText, string caption, string okButtonText, string cancelButtonText)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.OKCancel);
            msg.OkButtonText = okButtonText;
            msg.CancelButtonText = cancelButtonText;

            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, caption, OK/Cancel buttons with custom System.String values for the buttons' text, and icon;
        /// and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="okButtonText">A System.String that specifies the text to display within the OK button.</param>
        /// <param name="cancelButtonText">A System.String that specifies the text to display within the Cancel button.</param>
        /// <param name="icon">A System.Windows.MessageBoxImage value that specifies the icon to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowOKCancel(string messageBoxText, string caption, string okButtonText, string cancelButtonText, MessageBoxImage icon)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.OKCancel, icon);
            msg.OkButtonText = okButtonText;
            msg.CancelButtonText = cancelButtonText;

            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, caption, and Yes/No buttons with custom System.String values for the buttons' text;
        /// and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="yesButtonText">A System.String that specifies the text to display within the Yes button.</param>
        /// <param name="noButtonText">A System.String that specifies the text to display within the No button.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowYesNo(string messageBoxText, string caption, string yesButtonText, string noButtonText)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.YesNo);
            msg.YesButtonText = yesButtonText;
            msg.NoButtonText = noButtonText;

            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, caption, Yes/No buttons with custom System.String values for the buttons' text, and icon;
        /// and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="yesButtonText">A System.String that specifies the text to display within the Yes button.</param>
        /// <param name="noButtonText">A System.String that specifies the text to display within the No button.</param>
        /// <param name="icon">A System.Windows.MessageBoxImage value that specifies the icon to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowYesNo(string messageBoxText, string caption, string yesButtonText, string noButtonText, MessageBoxImage icon)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.YesNo, icon);
            msg.YesButtonText = yesButtonText;
            msg.NoButtonText = noButtonText;

            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, caption, and Yes/No/Cancel buttons with custom System.String values for the buttons' text;
        /// and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="yesButtonText">A System.String that specifies the text to display within the Yes button.</param>
        /// <param name="noButtonText">A System.String that specifies the text to display within the No button.</param>
        /// <param name="cancelButtonText">A System.String that specifies the text to display within the Cancel button.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowYesNoCancel(string messageBoxText, string caption, string yesButtonText, string noButtonText, string cancelButtonText)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.YesNoCancel);
            msg.YesButtonText = yesButtonText;
            msg.NoButtonText = noButtonText;
            msg.CancelButtonText = cancelButtonText;

            msg.ShowDialog();

            return msg.Result;
        }

        /// <summary>
        /// Displays a message box that has a message, caption, Yes/No/Cancel buttons with custom System.String values for the buttons' text, and icon;
        /// and that returns a result.
        /// </summary>
        /// <param name="messageBoxText">A System.String that specifies the text to display.</param>
        /// <param name="caption">A System.String that specifies the title bar caption to display.</param>
        /// <param name="yesButtonText">A System.String that specifies the text to display within the Yes button.</param>
        /// <param name="noButtonText">A System.String that specifies the text to display within the No button.</param>
        /// <param name="cancelButtonText">A System.String that specifies the text to display within the Cancel button.</param>
        /// <param name="icon">A System.Windows.MessageBoxImage value that specifies the icon to display.</param>
        /// <returns>A System.Windows.MessageBoxResult value that specifies which message box button is clicked by the user.</returns>
        public static MessageBoxEXResult ShowYesNoCancel(string messageBoxText, string caption, string yesButtonText, string noButtonText, string cancelButtonText, MessageBoxImage icon)
        {
            MessageBoxEX msg = new MessageBoxEX(messageBoxText, caption, MessageBoxEXButton.YesNoCancel, icon);
            msg.YesButtonText = yesButtonText;
            msg.NoButtonText = noButtonText;
            msg.CancelButtonText = cancelButtonText;

            msg.ShowDialog();

            return msg.Result;
        }
        #endregion
    }

    public class MessageBoxEXButton
    {
        public static MessageBoxEXButton OK = new MessageBoxEXButton { useOK = true, Default = MessageBoxEXResult.OK };
        public static MessageBoxEXButton OKCancel = new MessageBoxEXButton { useOK = true, useCancel = true, Default = MessageBoxEXResult.OK };
        public static MessageBoxEXButton YesNoCancel = new MessageBoxEXButton { useCancel = true, useYes = true, useNo = true, Default = MessageBoxEXResult.Yes };
        public static MessageBoxEXButton YesNo = new MessageBoxEXButton { useYes = true, useNo = true, Default = MessageBoxEXResult.Yes };
        public static MessageBoxEXButton AbortRetry = new MessageBoxEXButton { useAbort = true, useRetry = true, Default = MessageBoxEXResult.Retry };
        public static MessageBoxEXButton AbortRetryCancel = new MessageBoxEXButton { useAbort = true, useRetry = true, useCancel = true, Default = MessageBoxEXResult.Retry };
        public static MessageBoxEXButton YesNoAllCancel = new MessageBoxEXButton { useYes = true, useYesToAll = true, useNoToAll = true, useNo = true, useCancel = true, Default = MessageBoxEXResult.Cancel };

        public MessageBoxEXResult Default { get; set; } = MessageBoxEXResult.None;
        public bool useOK { get; set; } = false;
        public bool useCancel { get; set; } = false;
        public bool useYes { get; set; } = false;
        public bool useNo { get; set; } = false;
        public bool useAbort { get; set; } = false;
        public bool useRetry { get; set; } = false;
        public bool useYesToAll { get; set; } = false;
        public bool useNoToAll { get; set; } = false;
    }

    public enum MessageBoxEXResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7,
        Abort = 8,
        Retry = 9,
        YesToAll = 10,
        NoToAll = 11
    }

    public class MessageBoxEXArgs
    {
        public string title { get; set; }
        public string caption { get; set; }
        public MessageBoxEXButton button { get; set; }
        public MessageBoxImage icon { get; set; } = MessageBoxImage.None;
        public string OKText { get; set; } = string.Empty;
        public string CancelText { get; set; } = string.Empty;
        public string YesText { get; set; } = string.Empty;
        public string NoText { get; set; } = string.Empty;
        public string AbortText { get; set; } = string.Empty;
        public string RetryText { get; set; } = string.Empty;
        public string YesToAllText { get; set; } = string.Empty;
        public string NoToAllText { get; set; } = string.Empty;
    }

    public static class MessageBoxEXUtil
    {
        public static ImageSource ToImageSource(this Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        /// <summary>
        /// Keyboard Accellerators are used in Windows to allow easy shortcuts to controls like Buttons and 
        /// MenuItems. These allow users to press the Alt key, and a shortcut key will be highlighted on the 
        /// control. If the user presses that key, that control will be activated.
        /// This method checks a string if it contains a keyboard accellerator. If it doesn't, it adds one to the
        /// beginning of the string. If there are two strings with the same accellerator, Windows handles it.
        /// The keyboard accellerator character for WPF is underscore (_). It will not be visible.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string TryAddKeyboardAccellerator(this string input)
        {
            const string accellerator = "_";            // This is the default WPF accellerator symbol - used to be & in WinForms

            // If it already contains an accellerator, do nothing
            if (input.Contains(accellerator)) return input;

            return accellerator + input;
        }
    }

}
