// if you insist on using the deprecated show overloads, uncomment the next line.
//#define __OVERLOADED_SHOW__

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MsgBoxEx
{
	/// <summary>
	/// Dispays an configurable Message Box.
	/// </summary>
	public partial class MessageBoxEx : Window, INotifyPropertyChanged
	{
		#region static fields

		private static double               screenWidth    = SystemParameters.WorkArea.Width - 100;

		private static bool                 enableCloseButton = true;
		private static ContentControl       parentWindow;
		private static string               buttonTemplateName;
		private static SolidColorBrush      messageBackground;
		private static SolidColorBrush      messageForeground;
		private static SolidColorBrush      buttonBackground;
		private static double               maxFormWidth   = screenWidth;
		private static bool                 isSilent       = false;
		private static Visibility           showDetailsBtn = Visibility.Collapsed;
		private static string               detailsText;
		private static Visibility           showCheckBox   = Visibility.Collapsed;
		private static MsgBoxExCheckBoxData checkBoxData   = null;
		private static System.Windows.Media.FontFamily  msgFontFamily  = new System.Windows.Media.FontFamily("Segoe UI");
		private static double               msgFontSize    = 12;
		private static Uri                  url            = null;
		private static Visibility           showUrl        = Visibility.Collapsed;
		private static string               urlDisplayName = null;
		private static SolidColorBrush      urlForeground  = new SolidColorBrush(DefaultUrlForegroundColor);
		private static string               delegateToolTip;
		private static List<string>         installedFonts = new List<string>();
		public static MessageBoxButtonDefault staticButtonDefault;

		#endregion static fields

		#region static properties

		public static System.Windows.Media.Color DefaultUrlForegroundColor { get { return System.Windows.Media.Colors.Blue; }  }

		/// <summary>
		/// Get/set the icon tooltip text
		/// </summary>
		private static string             MsgBoxIconToolTip    { get; set; }
		/// <summary>
		/// Get/set the external icon delegate object
		/// </summary>
		protected static MsgBoxExDelegate DelegateObj          { get; set; }
		/// <summary>
		/// Get/set the flag that indicates whether the parent messagebox is closed after the 
		/// external action is finished.
		/// </summary>
		protected static bool             ExitAfterErrorAction { get; set; }

		/// <summary>
		/// Get/set the parent content control
		/// </summary>
		public static ContentControl       ParentWindow       { get { return parentWindow;       } set { parentWindow       = value; } }
		/// <summary>
		/// Get/set the button template name (for styling buttons)
		/// </summary>
		public static string               ButtonTemplateName { get { return buttonTemplateName; } set { buttonTemplateName = value; } }
		/// <summary>
		/// Get/set the brush for the message text background
		/// </summary>
		public static SolidColorBrush      MessageBackground  { get { return messageBackground;  } set { messageBackground  = value; } }
		/// <summary>
		/// Get/set the brush for the message text foreground
		/// </summary>
		public static SolidColorBrush      MessageForeground  { get { return messageForeground;  } set { messageForeground  = value; } }
		/// <summary>
		/// Get/set the brush for the button panel background
		/// </summary>
		public static SolidColorBrush      ButtonBackground	  { get { return buttonBackground;   } set { buttonBackground   = value; } }
		/// <summary>
		/// Get/set max form width
		/// </summary>
		public static double               MaxFormWidth       { get { return maxFormWidth;       } set { maxFormWidth       = value; } }
		/// <summary>
		/// Get the visibility of the no button
		/// </summary>
		public static Visibility           ShowDetailsBtn     { get { return showDetailsBtn;     } set { showDetailsBtn     = value; } }
		/// <summary>
		/// Get/set details text
		/// </summary>
		public static string               DetailsText        { get { return detailsText;        } set { detailsText        = value; } }
		/// <summary>
		/// Get/set the visibility of the checkbox
		/// </summary>
		public static Visibility           ShowCheckBox       { get { return showCheckBox;       } set { showCheckBox       = value; } }
		/// <summary>
		/// Get/set the checkbox data object
		/// </summary>
		public static MsgBoxExCheckBoxData CheckBoxData       { get { return checkBoxData;       } set { checkBoxData       = value; } }
		/// <summary>
		/// Get/set the font family
		/// </summary>
		public static System.Windows.Media.FontFamily  MsgFontFamily { get { return msgFontFamily;      } set { msgFontFamily      = value; } }
		/// <summary>
		/// Get/set the font size
		/// </summary>
		public static double               MsgFontSize        { get { return msgFontSize;        } set { msgFontSize        = value; } }
		/// <summary>
		/// Get/set the uri object that represents the desired URL
		/// </summary>
		public static Uri                  Url                { get { return url;                } set { url                = value; } }
		/// <summary>
		/// Get/set the visibility of the checkbox
		/// </summary>
		public static Visibility           ShowUrl            { get { return showUrl;            } set { showUrl            = value; } }
		/// <summary>
		/// Get/set the optional url display name
		/// </summary>
		public static string               UrlDisplayName     { get { return urlDisplayName;     } set { urlDisplayName     = value; } }
		/// <summary>
		/// Get/set the brush for the message text background
		/// </summary>
		public static SolidColorBrush      UrlForeground      { get { return urlForeground;      } set { urlForeground      = value; } }
		/// <summary>
		/// Get/set the delegate tooltip text
		/// </summary>
		public static string               DelegateToolTip    { get { return delegateToolTip;    } set { delegateToolTip    = value; } }

		#endregion static properties

		#region Show and ShowEx

		/// <summary>
		/// Does the work of actually opening the messagebox
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="title"></param>
		/// <param name="buttons"></param>
		/// <param name="image"></param>
		/// <returns></returns>
		private static MessageBoxResult OpenMessageBox(Window owner, string msg, string title, MessageBoxButton buttons, MessageBoxImage image)
		{
			if (owner == null)
			{
				owner = (Application.Current.MainWindow.Visibility == Visibility.Visible) ? Application.Current.MainWindow : null;
			}

			MessageBoxEx form = new MessageBoxEx(msg, title, buttons, image) { Owner = owner };

			form.ShowDialog();
			return form.MessageResult;
		}

		///<summary>
		///Does the work of actually opening the messagebox with extended functionality
		///</summary>
		///<param name="msg"></param>
		///<param name="title"></param>
		///<param name="buttons"></param>
		///<param name="image"></param>
		///<returns></returns>
		private static MessageBoxResultEx OpenMessageBox(Window owner, string msg, string title, MessageBoxButtonEx buttons, MessageBoxImage image)
		{
			if (owner == null)
			{
				owner = (Application.Current.MainWindow.Visibility == Visibility.Visible) ? Application.Current.MainWindow : null;
			}
			MessageBoxEx form = new MessageBoxEx(msg, title, buttons, image) { Owner = owner };
			form.ShowDialog();
			return form.MessageResultEx;
		}

		/// <summary>
		/// Show the message box with the same characteristics as the standard WPF message box. The args <br/> 
		/// parameter can accept one or more of the following parameters. Sequence is not a factor, and <br/>
		/// once a value that is NOT the default is encountered, evaluation stops for that object type. <br /><br/>
		/// -- expected args: <see cref="string"/> (title), <see cref="Window"/> (owner), <see cref="MessageBoxButton"/>, <see cref="MessageBoxImage"/>, <see cref="MessageBoxButtonDefault"/><br /><br />
		/// -------- 
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="args">An object array that contains 1 or more objects (see summary).</param>
		/// <returns>Button that was clicked to dismiss the form.</returns>
		public static MessageBoxResult Show(string msg, params object[] args)
		{
			string                  title         = null;
			Window                  owner         = null;
			MessageBoxButton        buttons       = MessageBoxButton.OK;
			MessageBoxImage         image         = MessageBoxImage.None;
			MessageBoxButtonDefault buttonDefault = MessageBoxButtonDefault.Forms;
			foreach (object item in args)
			{
				if (item is string                  ttl && !string.IsNullOrEmpty(title)                  ) { title         = ttl; }
				if (item is MessageBoxButton        btn && buttons       == MessageBoxButton.OK          ) { buttons       = btn; }
				if (item is MessageBoxImage         img && image         == MessageBoxImage.None         ) { image         = img; }
				if (item is MessageBoxButtonDefault def && buttonDefault == MessageBoxButtonDefault.Forms) { buttonDefault = def; }
				if (item is Window                  wnd && owner         == null                         ) { owner         = wnd; }
			}
			staticButtonDefault = buttonDefault;

			title = (string.IsNullOrEmpty(title))?string.Empty:title.Trim();
			if (string.IsNullOrEmpty(title))
			{
				if (image != MessageBoxImage.None)
				{
					title = image.ToString();
				}
				else
				{
					title = _DEFAULT_CAPTION;
				}
			}
			return OpenMessageBox(owner, msg, title, buttons, image);
		}

		/// <summary>
		/// Show the message box with extended message box functionality. The args parameter can accept <br />
		/// one or more of the following parameters. Sequence is not a factor, and once a value that is NOT <br />
		/// the default is encountered, evaluation stops for that object type. <br /><br/>
		/// -- expected args: <see cref="string"/> (title), <see cref="Window"/> (owner), <see cref="MessageBoxButtonEx"/>, <see cref="MessageBoxImage"/>, <see cref="MessageBoxButtonDefault"/>, <see cref="MsgBoxExtendedFunctionality"/><br /><br />
		/// -------- 
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="args">An object array that contains 1 or more objects (see summary).</param>
		/// <returns>Button that was clicked to dismiss the form.</returns>
		public static MessageBoxResultEx ShowEx(string msg, params object[] args)
		{
			string                      title         = null;
			Window                      owner         = null;
			MessageBoxImage             image         = MessageBoxImage.None;
			MessageBoxButtonEx          buttons       = MessageBoxButtonEx.OK;
			MessageBoxButtonDefault     buttonDefault = MessageBoxButtonDefault.Forms;
			MsgBoxExtendedFunctionality data          = null;
			foreach (object item in args)
			{
				if (item is string                      ttl && !string.IsNullOrEmpty(title)                  ) { title         = ttl; }
				if (item is MessageBoxButtonEx          btn && buttons       == MessageBoxButtonEx.OK        ) { buttons       = btn; }
				if (item is MessageBoxImage             img && image         == MessageBoxImage.None         ) { image         = img; }
				if (item is MessageBoxButtonDefault     def && buttonDefault == MessageBoxButtonDefault.Forms) { buttonDefault = def; }
				if (item is Window                      wnd && owner         == null                         ) { owner         = wnd; }
				if (item is MsgBoxExtendedFunctionality mef && data          == null                         ) { data          = mef; }
			}
			staticButtonDefault = buttonDefault;

			if (data != null)
			{
				// details text ===================
				DetailsText = data.DetailsText;

				// checkbox =======================
				ShowCheckBox = Visibility.Collapsed;
				CheckBoxData = data.CheckBoxData;

				// clickable icon =================
				DelegateObj          = data.MessageDelegate;
				ExitAfterErrorAction = data.ExitAfterAction;
				DelegateToolTip      = data.DelegateToolTip;

				// url ============================
				// assume we're not showing a url
				ShowUrl        = Visibility.Collapsed;
				Url            = null;
				UrlDisplayName = null;
				// now, see if we want to
				if (data.URL != null)
				{
					// if the url is ultimately null, no url will be displayed, and none of the following 
					// settings really mean anything
					Url            = data.URL.URL;
					ShowUrl        = (Url == null) ? Visibility.Collapsed : Visibility.Visible;
					UrlDisplayName = (Url == null) ? null : data.URL.DisplayName;
					// make sure we actually set a color. If the color was not included, use the message text color
					UrlForeground  = (data.URL.Foreground != null) ? new SolidColorBrush(data.URL.Foreground) : new SolidColorBrush(DefaultUrlForegroundColor);
				}
			}

			title = (string.IsNullOrEmpty(title))?string.Empty:title.Trim();
			if (string.IsNullOrEmpty(title.Trim()))
			{
				if (image != MessageBoxImage.None)
				{
					title = image.ToString();
				}
				else
				{
					title = _DEFAULT_CAPTION;
				}
			}
			return OpenMessageBox(owner, msg, title, buttons, image);
		}

		#endregion Show and ShowEx

#if __OVERLOADED_SHOW__

		// I essentially abandoned this code because the overloads were becoming burdonsome 
		// and tedious If you reenable it, you are on your own.

		#region static Show() methods

		/////////////////////////////////////// without icons

		/// <summary>
		/// Show a messagebox, using default caption and just the OK button
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg)
		{
			return OpenMessageBox(null, msg, _DEFAULT_CAPTION, MessageBoxButton.OK, MessageBoxImage.None);
		}

		public static MessageBoxResult Show(Window owner, string msg)
		{
			return OpenMessageBox(owner, msg, _DEFAULT_CAPTION, MessageBoxButton.OK, MessageBoxImage.None);
		}

		/// <summary>
		/// Show a messagebox with the specified caption, and just the OK button
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The messagebox caption</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg, string title)
		{
			title = (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title;
			return OpenMessageBox(null, msg, title, MessageBoxButton.OK, MessageBoxImage.None);
		}

		public static MessageBoxResult Show(Window owner, string msg, string title)
		{
			return OpenMessageBox(owner, msg, (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title, MessageBoxButton.OK, MessageBoxImage.None);
		}


		/// <summary>
		/// Show a messagebox with the default caption and the specified buttons
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="buttons">The buttons to display</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg, MessageBoxButton buttons)
		{
			return OpenMessageBox(null, msg, _DEFAULT_CAPTION, buttons, MessageBoxImage.None);
		}
		public static MessageBoxResult Show(Window owner, string msg, MessageBoxButton buttons)
		{
			return OpenMessageBox(owner, msg, _DEFAULT_CAPTION, buttons, MessageBoxImage.None);
		}

		/// <summary>
		/// Show a mesagebox with the specified caption and button(s)
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The title for the message box</param>
		/// <param name="parentWindow">The parent window that supplies the font family/size</param>
		/// <param name="buttons">The buttons to display</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg, string title, MessageBoxButton buttons)
		{
			return OpenMessageBox(null, msg, (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title, buttons, MessageBoxImage.None);
		}
		public static MessageBoxResult Show(Window owner, string msg, string title, MessageBoxButton buttons)
		{
			return OpenMessageBox(null, msg, (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title, buttons, MessageBoxImage.None);
		}

		/////////////////////////////////////// with icons

		/// <summary>
		/// Show a messagebox, using default caption, the OK button, and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg, MessageBoxImage image)
		{
			return OpenMessageBox(null, msg, _DEFAULT_CAPTION, MessageBoxButton.OK, image);
		}

		/// <summary>
		/// Show a messagebox with the specified caption, the OK button, and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The messagebox caption</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg, string title, MessageBoxImage image)
		{
			title = (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title;
			return OpenMessageBox(null, msg, title, MessageBoxButton.OK, image);
		}

		/// <summary>
		/// Show a messagebox with the default caption, the specified button(s), and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="buttons">The buttons to display</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg, MessageBoxButton buttons, MessageBoxImage image)
		{
			return OpenMessageBox(null, msg, _DEFAULT_CAPTION, buttons, image);
		}

		/// <summary>
		/// Show a mesagebox with the specified caption, the specified button(s), and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The title for the message box</param>
		/// <param name="parentWindow">The parent window that supplies the font family/size</param>
		/// <param name="buttons">The buttons to display</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResult Show(string msg, string title, MessageBoxButton buttons, MessageBoxImage image)
		{
			return OpenMessageBox(null, msg, title, buttons, image);
		}

		#endregion static Show() methods

		#region static ShowEx() methods with MessageBoxButtonsEx and returning MessageBoxResultEx
		
		/// <summary>
		/// Show a messagebox, using default caption and just the OK button
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg)
		{
			return OpenMessageBox(msg, _DEFAULT_CAPTION, MessageBoxButtonEx.OK, MessageBoxImage.None);
		}

		/// <summary>
		/// Show a messagebox with the specified caption, and just the OK button
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The messagebox caption</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg, string title)
		{
			title = (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title;
			return OpenMessageBox(msg, title, MessageBoxButtonEx.OK, MessageBoxImage.None);
		}

		/// <summary>
		/// Show a messagebox with the default caption and the specified buttons
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="buttons">The buttons to display</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg, MessageBoxButtonEx buttons)
		{
			return OpenMessageBox(msg, _DEFAULT_CAPTION, buttons, MessageBoxImage.None);
		}

		/// <summary>
		/// Show a mesagebox with the specified caption and button(s)
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The title for the message box</param>
		/// <param name="parentWindow">The parent window that supplies the font family/size</param>
		/// <param name="buttons">The buttons to display</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg, string title, MessageBoxButtonEx buttons)
		{
			title = (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title;
			return OpenMessageBox(msg, title, buttons, MessageBoxImage.None);
		}

		/////////////////////////////////////// with icons

		/// <summary>
		/// Show a messagebox, using default caption, the OK button, and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg, MessageBoxImage image)
		{
			return OpenMessageBox(msg, _DEFAULT_CAPTION, MessageBoxButtonEx.OK, image);
		}

		/// <summary>
		/// Show a messagebox with the specified caption, the OK button, and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The messagebox caption</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg, string title, MessageBoxImage image)
		{
			title = (string.IsNullOrEmpty(title)) ? _DEFAULT_CAPTION : title;
			return OpenMessageBox(msg, title, MessageBoxButtonEx.OK, image);
		}

		/// <summary>
		/// Show a messagebox with the default caption, the specified button(s), and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="buttons">The buttons to display</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg, MessageBoxButtonEx buttons, MessageBoxImage image)
		{
			return OpenMessageBox(msg, _DEFAULT_CAPTION, buttons, image);
		}

		/// <summary>
		/// Show a mesagebox with the specified caption, the specified button(s), and the specified icon
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">The title for the message box</param>
		/// <param name="parentWindow">The parent window that supplies the font family/size</param>
		/// <param name="buttons">The buttons to display</param>
		/// <param name="image">The message box icon to diplay</param>
		/// <returns>The button that was clicked to dismiss the messagebox</returns>
		public static MessageBoxResultEx ShowEx(string msg, string title, MessageBoxButtonEx buttons, MessageBoxImage image)
		{
			return OpenMessageBox(msg, title, buttons, image);
		}
		
		#endregion static Show() methods with MessageBoxButtonsEx and returning MessageBoxResultEx

		#region show with extended functionality

		/// <summary>
		/// Show messagebox with one or more extended functionality items enabled
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message to present</param>
		/// <param name="title">The window caption</param>
		/// <param name="buttons">The buttons to present</param>
		/// <param name="image">The icon to present</param>
		/// <returns>If the ext param is null, a normal messageboxex will be presented.</returns>
		public static MessageBoxResult ShowExtBase(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxButton buttons, MessageBoxImage image)
		{
			// we ultimately want to always display a message box, so we try to gracefully degrade 
			// all of the "super" functionality.
			if (ext != null)
			{
				// details text ===================
				DetailsText = ext.DetailsText;

				// checkbox =======================
				ShowCheckBox = Visibility.Collapsed;
				CheckBoxData = ext.CheckBoxData;

				// clickable icon =================
				DelegateObj          = ext.MessageDelegate;
				ExitAfterErrorAction = ext.ExitAfterAction;
				DelegateToolTip      = ext.DelegateToolTip;

				// url ============================
				// assume we're not showing a url
				ShowUrl        = Visibility.Collapsed;
				Url            = null;
				UrlDisplayName = null;
				// now, see if we want to
				if (ext.URL != null)
				{
					// if the url is ultimately null, no url will be displayed, and none of the following 
					// settings really mean anything
					Url            = ext.URL.URL;
					ShowUrl        = (Url == null) ? Visibility.Collapsed : Visibility.Visible;
					UrlDisplayName = (Url == null) ? null : ext.URL.DisplayName;
					// make sure we actually set a color. If the color was not included, use the message text color
					UrlForeground  = (ext.URL.Foreground != null) ? new SolidColorBrush(ext.URL.Foreground) : new SolidColorBrush(DefaultUrlForegroundColor);
				}
			}
			return OpenMessageBox(message, title, buttons, image);
		}

		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, MessageBoxButton.OK, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message, string title)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, MessageBoxButton.OK, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="buttons">The message button(s)</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message, MessageBoxButton buttons)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, buttons, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <param name="buttons">The message button(s)</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxButton buttons)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, buttons, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, MessageBoxButton.OK, image);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, MessageBoxButton.OK, image);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="buttons">The message button(s)</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message, MessageBoxButton buttons, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, buttons, image);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <param name="buttons">The message button(s)</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResult ShowExt(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxButton buttons, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, buttons, image);
		}

		#endregion show with extended functionality
/*
		#region ShowEx with extended functionality using MessageBoxButtonsEx and returning MessageBoxResultEx

		/// <summary>
		/// Show messagebox with one or more extended functionality items enabled
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message to present</param>
		/// <param name="title">The window caption</param>
		/// <param name="buttons">The buttons to present</param>
		/// <param name="image">The icon to present</param>
		/// <returns>If the ext param is null, a normal messageboxex will be presented.</returns>
		public static MessageBoxResultEx ShowExtBase(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxButtonEx buttons, MessageBoxImage image)
		{
			// we ultimately want to always display a message box, so we try to gracefully degrade 
			// all of the "super" functionality.
			if (ext != null)
			{
				// details text ===================
				DetailsText = ext.DetailsText;

				// checkbox =======================
				ShowCheckBox = Visibility.Collapsed;
				CheckBoxData = ext.CheckBoxData;

				// clickable icon =================
				DelegateObj          = ext.MessageDelegate;
				ExitAfterErrorAction = ext.ExitAfterAction;
				DelegateToolTip      = ext.DelegateToolTip;

				// url ============================
				// assume we're not showing a url
				ShowUrl        = Visibility.Collapsed;
				Url            = null;
				UrlDisplayName = null;
				// now, see if we want to
				if (ext.URL != null)
				{
					// if the url is ultimately null, no url will be displayed, and none of the following 
					// settings really mean anything
					Url            = ext.URL.URL;
					ShowUrl        = (Url == null) ? Visibility.Collapsed : Visibility.Visible;
					UrlDisplayName = (Url == null) ? null : ext.URL.DisplayName;
					// make sure we actually set a color. If the color was not included, use the message text color
					UrlForeground  = (ext.URL.Foreground != null) ? new SolidColorBrush(ext.URL.Foreground) : new SolidColorBrush(DefaultUrlForegroundColor);
				}

				staticButtonDefault = ext.ButtonDefault;
			}
			return OpenMessageBox(message, title, buttons, image);
		}

		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, MessageBoxButtonEx.OK, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message, string title)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, MessageBoxButtonEx.OK, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="buttons">The message button(s)</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message, MessageBoxButtonEx buttons)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, buttons, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <param name="buttons">The message button(s)</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxButtonEx buttons)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, buttons, MessageBoxImage.None);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, MessageBoxButtonEx.OK, image);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, MessageBoxButtonEx.OK, image);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="buttons">The message button(s)</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message, MessageBoxButtonEx buttons, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, _DEFAULT_CAPTION, buttons, image);
		}
		/// <summary>
		/// Show the MessageBoxEx with the specified extended functions 
		/// </summary>
		/// <param name="ext">The extended functionality object</param>
		/// <param name="message">The message text</param>
		/// <param name="title">The window caption</param>
		/// <param name="buttons">The message button(s)</param>
		/// <param name="image">The message icon</param>
		/// <returns>MessageBoxResult indicating the button used to disnmiss the window</returns>
		public static MessageBoxResultEx ShowExtEx(MsgBoxExtendedFunctionality ext, string message, string title, MessageBoxButtonEx buttons, MessageBoxImage image)
		{
			return ShowExtBase(ext, message, (string.IsNullOrEmpty(title))?_DEFAULT_CAPTION:title, buttons, image);
		}
		#endregion show with extended functionality
*/
#endif

		#region static configuration methods

		// colors

		/// <summary>
		/// Set the background color for the message area
		/// </summary>
		/// <param name="color">The color to set. Can be a name (White) or an octet string(#FFFFFF).</param>
		public static void SetMessageBackground(System.Windows.Media.Color color)
		{
			try
			{
				MessageBackground = new SolidColorBrush(color);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, ex.ToString());
			}
		}

		/// <summary>
		/// Set the foreground color for the message area
		/// </summary>
		/// <param name="color">The color to set. Can be a name (White) or an octet string(#FFFFFF).</param>
		public static void SetMessageForeground(System.Windows.Media.Color color)
		{
			try
			{
				MessageForeground = new SolidColorBrush(color);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, ex.ToString());
			}
		}

		/// <summary>
		/// Set the background color for the button panel area
		/// </summary>
		/// <param name="color">The color to set. Can be a name (White) or an octet string(#FFFFFF).</param>
		public static void SetButtonBackground(System.Windows.Media.Color color)
		{
			try
			{
				ButtonBackground = new SolidColorBrush(color);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, ex.ToString());
			}
		}

		/// <summary>
		///  Create a WPF-compatible Color from an string (such as "White", "white", or "#FFFFFF").
		/// </summary>
		/// <param name="colorOctet"></param>
		/// <returns>A Media.Color. If color is invalid, returns #000000.</returns>
		public static System.Windows.Media.Color ColorFromString(string colorString)
		{
			System.Windows.Media.Color wpfColor = System.Windows.Media.Colors.Black;
			try
			{
				wpfColor = (System.Windows.Media.Color)(System.Windows.Media.ColorConverter.ConvertFromString(colorString));
			}
			catch(Exception){ }
			return wpfColor;
		}

		// font

		/// <summary>
		/// Set the font family and size from the application's main window
		/// </summary>
		public static void SetFont()
		{
			MsgFontFamily = Application.Current.MainWindow.FontFamily;
			MsgFontSize   = Application.Current.MainWindow.FontSize;
		}

		/// <summary>
		/// Set the font family and size from the specified content control (usually the parent window)
		/// </summary>
		/// <param name="parent"></param>
		public static void SetFont(ContentControl parent)
		{
			MsgFontFamily = parent.FontFamily;
			MsgFontSize   = parent.FontSize;
		}

		/// <summary>
		/// Set the font family and size
		/// </summary>
		/// <param name="familyName">The name of the desired font family (will not be set if null/empty)</param>
		/// <param name="size">Size of font (min size is 1)</param>
		public static void SetFont(string familyName, double size)
		{
			if (!IsFontFamilyValid(familyName))
			{
				if (!string.IsNullOrEmpty(familyName))
				{
					MsgFontFamily = new System.Windows.Media.FontFamily(familyName);
				}
			}
			MsgFontSize = Math.Max(1.0, size);
		}

		private static bool IsFontFamilyValid(string name)
		{
			if (installedFonts.Count == 0)
			{
				using (InstalledFontCollection fontsCollection = new InstalledFontCollection())
				{
					installedFonts = (from x in fontsCollection.Families select x.Name).ToList();
				}
			}
			return installedFonts.Contains(name);
		}

		// mechanicals 

		/// <summary>
		/// Set the custom button template *NAME*
		/// </summary>
		/// <param name="name"></param>
		public static void SetButtonTemplateName(string name)
		{
			ButtonTemplateName = name;
		}

		/// <summary>
		/// Sets the max form width to largest of 300 or the specified value
		/// </summary>
		/// <param name="value"></param>
		public static void SetMaxFormWidth(double value)
		{
			MaxFormWidth = Math.Max(value, 300);
			double minWidth = 300;
			MaxFormWidth = Math.Max(minWidth, Math.Min(value, screenWidth));
		}

		/// <summary>
		/// Resets the configuration items to default values
		/// </summary>
		public static void ResetToDefaults()
		{
			MsgFontSize          = 12d;
			MsgFontFamily        = new System.Windows.Media.FontFamily("Segoe UI");
			DelegateObj          = null;
			DetailsText          = null;
			MessageForeground    = null;
			MessageBackground    = null;
			ButtonBackground     = null;
			ParentWindow         = null;
			isSilent             = false;
			enableCloseButton    = true;
			ButtonTemplateName   = null;
			MsgBoxIconToolTip    = null;
			ShowCheckBox         = Visibility.Collapsed;
			CheckBoxData         = null;
			ExitAfterErrorAction = false;
			MaxFormWidth         = 800;
			Url                  = null;
			ShowUrl              = Visibility.Collapsed;
			UrlDisplayName       = null;
			UrlForeground        = new SolidColorBrush(DefaultUrlForegroundColor);
			staticButtonDefault  = MessageBoxButtonDefault.Forms;
		}

		public static void EnableCloseButton(bool enable)
		{
			enableCloseButton = enable;
		}

		// message box icon 
		/// <summary>
		///  Toggle system sounds associated with MessageBoxImage icons
		/// </summary>
		/// <param name="quiet"></param>
		public static void SetAsSilent(bool quiet)
		{
			isSilent = quiet;
		}

		/// <summary>
		/// Specify the button that will be displayed as the default button in the in the message 
		/// box. the message box will return to the default "Forms" setting when it is dismissed.
		/// </summary>
		/// <param name="buttonDefault"></param>
		public static void SetDefaultButton(MessageBoxButtonDefault buttonDefault)
		{
			staticButtonDefault = buttonDefault;
		}

		#endregion static configuration methods

	}

}
