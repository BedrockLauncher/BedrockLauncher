using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MsgBoxEx
{
	// The MessageBoxEx class we getting kinda crowded, so I took advantage of the fact that it's a 
	// partial class, and segregated the static from the non-static. The code looks less chatotic 
	// and is easier to maintain as a result.

	/// <summary>
	/// Non-static interaction logic for MessageBoxEx.xaml
	/// </summary>
	public partial class MessageBoxEx : Window, INotifyPropertyChanged
	{
		#region INotifyPropertyChanged

		private bool isModified = false;
		public bool IsModified { get { return this.isModified; } set { if (value != this.isModified) { this.isModified = true; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Notifies that the property changed, and sets IsModified to true.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				if (propertyName != "IsModified")
				{
					this.IsModified = true;
				}
            }
        }
	
		#endregion INotifyPropertyChanged

		// so we can run the browser when the optional URL is clicked
		[DllImport("shell32.dll")]
		public static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation
												 , string lpFile, string lpParameters
												 , string lpDirectory, int nShowCmd);

		private const string _DEFAULT_CAPTION = "Application Message";

		#region fields

		private double             screenHeight;
		private string             message;
		private string             messageTitle;
		private MessageBoxButton?   buttons;
		private MessageBoxResult   messageResult;
		private MessageBoxButtonEx? buttonsEx;
		private MessageBoxResultEx messageResultEx;
		private ImageSource        messageIcon;
		private MessageBoxImage    msgBoxImage;
		private double             buttonWidth = 0d;
		private bool               expanded = false;
		private bool               isDefaultOK;
		private bool               isDefaultCancel;
		private bool               isDefaultYes;
		private bool               isDefaultNo;
		private bool               isDefaultAbort;
		private bool               isDefaultRetry;
		private bool               isDefaultIgnore;

		private bool               usingExButtons = false;

		#endregion fields

		#region properties

		/// <summary>
		/// Get/set the screen's work area height
		/// </summary>
		public double             ScreenHeight       { get { return this.screenHeight;     } set { if (value != this.screenHeight    ) { this.screenHeight     = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the message text
		/// </summary>
		public string             Message	           { get { return this.message;          } set { if (value != this.message         ) { this.message          = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the form caption 
		/// </summary>
		public string             MessageTitle       { get { return this.messageTitle;     } set { if (value != this.messageTitle    ) { this.messageTitle     = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the message box result (which button was pressed to dismiss the form)
		/// </summary>
		public MessageBoxResult   MessageResult      { get { return this.messageResult;    } set { this.messageResult = value;   } } 
		public MessageBoxResultEx MessageResultEx    { get { return this.messageResultEx;  } set { this.messageResultEx = value; } } 
		/// <summary>
		/// Get/set the buttons ued in the form (and update visibility for them)
		/// </summary>
		public MessageBoxButton?  Buttons            
		{ 
			get { return this.buttons;          } 
			set 
			{ 
				if (value != this.buttons         ) 
				{ 
					this.buttons          = value; 
					this.NotifyPropertyChanged(); 
					this.NotifyPropertyChanged("ShowOk"); 
					this.NotifyPropertyChanged("ShowCancel"); 
					this.NotifyPropertyChanged("ShowYes"); 
					this.NotifyPropertyChanged("ShowNo"); 
					
				} 
			} 
		}
		public MessageBoxButtonEx?  ButtonsEx            
		{ 
			get { return this.buttonsEx;          } 
			set 
			{ 
				if (value != this.buttonsEx         ) 
				{ 
					this.buttonsEx          = value; 
					this.NotifyPropertyChanged(); 
					this.NotifyPropertyChanged("ShowOk"); 
					this.NotifyPropertyChanged("ShowCancel"); 
					this.NotifyPropertyChanged("ShowYes"); 
					this.NotifyPropertyChanged("ShowNo"); 
					this.NotifyPropertyChanged("ShowAbort"); 
					this.NotifyPropertyChanged("ShowRetry"); 
					this.NotifyPropertyChanged("ShowIgnore"); 
				} 
			} 
		}
		/// <summary>
		/// Get the visibility of the ok button
		/// </summary>
		public Visibility        ShowOk             { get { return (!this.usingExButtons && this.Buttons  == MessageBoxButton.OK       || 
																	!this.usingExButtons && this.Buttons  == MessageBoxButton.OKCancel ||
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.OK     || 
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.OKCancel) ? Visibility.Visible : Visibility.Collapsed; }}
		/// <summary>
		/// Get the visibility of the cancel button
		/// </summary>
		public Visibility        ShowCancel         { get { return (!this.usingExButtons && this.Buttons  == MessageBoxButton.OKCancel      || 
																	!this.usingExButtons && this.Buttons  == MessageBoxButton.YesNoCancel   ||
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.OKCancel    || 
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNoCancel ||
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.RetryCancel) ? Visibility.Visible : Visibility.Collapsed; }}
		/// <summary>
		/// Get the visibility of the yes button
		/// </summary>
		public Visibility        ShowYes            { get { return (!this.usingExButtons && this.Buttons  == MessageBoxButton.YesNo       || 
																	!this.usingExButtons && this.Buttons  == MessageBoxButton.YesNoCancel ||
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNo     || 
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed; }}
		/// <summary>
		/// Get the visibility of the no button
		/// </summary>
		public Visibility        ShowNo             { get { return (!this.usingExButtons && this.Buttons  == MessageBoxButton.YesNo       || 
																	!this.usingExButtons && this.Buttons  == MessageBoxButton.YesNoCancel ||
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNo    || 
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed; }}
		/// <summary>
		/// Get the visibility of the retry button
		/// </summary>
		public Visibility        ShowRetry          { get { return (this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.AbortRetryIgnore || 
																	this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.RetryCancel) ? Visibility.Visible : Visibility.Collapsed; }}
		/// <summary>
		/// Get the visibility of the abort button
		/// </summary>
		public Visibility        ShowAbort          { get { return (this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.AbortRetryIgnore) ? Visibility.Visible : Visibility.Collapsed; }}
		/// <summary>
		/// Get the visibility of the ignore button
		/// </summary>
		public Visibility        ShowIgnore         { get { return (this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.AbortRetryIgnore) ? Visibility.Visible : Visibility.Collapsed; }}

		/// <summary>
		/// Get this visibility of the message box icon
		/// </summary>
		public Visibility        ShowIcon           { get { return (this.MessageIcon != null ) ? Visibility.Visible : Visibility.Collapsed; }}
		/// <summary>
		/// Get/set the icon specified by the user
		/// </summary>
		public ImageSource       MessageIcon        { get { return this.messageIcon;     } set { if (value != this.messageIcon    ) { this.messageIcon = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the width of the largest button (so all buttons are the same width as the widest button)
		/// </summary>
		public double            ButtonWidth        { get { return this.buttonWidth;     } set { if (value != this.buttonWidth    ) { this.buttonWidth = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the flag inidcating whether the expander is expanded
		/// </summary>
		public bool              Expanded           { get { return this.expanded;        } set { if (value != expanded            ) { this.expanded    = value; this.NotifyPropertyChanged(); } } }

		// default button flags

		/// <summary>
		/// Get/set the flag indicating whether OK is the default button
		/// </summary>
		public bool IsDefaultOK     { get { return this.isDefaultOK    ; } set { if (value != this.isDefaultOK    ) { this.isDefaultOK     = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the flag indicating whether Cancel is the default button
		/// </summary>
		public bool IsDefaultCancel { get { return this.isDefaultCancel; } set { if (value != this.isDefaultCancel) { this.isDefaultCancel = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the flag indicating whether Yes is the default button
		/// </summary>
		public bool IsDefaultYes    { get { return this.isDefaultYes   ; } set { if (value != this.isDefaultYes   ) { this.isDefaultYes    = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the flag indicating whether No is the default button
		/// </summary>
		public bool IsDefaultNo     { get { return this.isDefaultNo    ; } set { if (value != this.isDefaultNo    ) { this.isDefaultNo     = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the flag indicating whether Abort is the default button
		/// </summary>
		public bool IsDefaultAbort  { get { return this.isDefaultAbort ; } set { if (value != this.isDefaultAbort ) { this.isDefaultAbort  = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the flag indicating whether Retry is the default button
		/// </summary>
		public bool IsDefaultRetry  { get { return this.isDefaultRetry ; } set { if (value != this.isDefaultRetry ) { this.isDefaultRetry  = value; this.NotifyPropertyChanged(); } } }
		/// <summary>
		/// Get/set the flag indicating whether Ignore is the default button
		/// </summary>
		public bool IsDefaultIgnore { get { return this.isDefaultIgnore; } set { if (value != this.isDefaultIgnore) { this.isDefaultIgnore = value; this.NotifyPropertyChanged(); } } }


		#endregion properties

		#region constructors

		/// <summary>
		/// Default constructor for VS designer
		/// </summary>
		private MessageBoxEx()
		{
			this.InitializeComponent();
			this.DataContext = this;
			this.LargestButtonWidth();
		}

		/// <summary>
		/// Constructor for standard buttons
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="title"></param>
		/// <param name="buttons">(Optinal) Message box button(s) to be displayed (default = OK)</param>
		/// <param name="image">(Optional) Message box image to display (default = None)</param>
		public MessageBoxEx(string msg, string title, MessageBoxButton buttons=MessageBoxButton.OK, MessageBoxImage image=MessageBoxImage.None)
		{
			this.InitializeComponent();
			this.DataContext  = this;
			this.Init(msg, title, buttons, image);
		}

		/// <summary>
		/// Constructor for extended buttons
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="title"></param>
		/// <param name="buttons">(Optinal) Message box button(s) to be displayed (default = OK)</param>
		/// <param name="image">(Optional) Message box image to display (default = None)</param>
		public MessageBoxEx(string msg, string title, MessageBoxButtonEx buttons=MessageBoxButtonEx.OK, MessageBoxImage image=MessageBoxImage.None)
		{
			this.InitializeComponent();
			this.DataContext  = this;
			this.Init(msg, title, buttons, image);
		}

		#endregion constructors

		#region non-static methods

		/// <summary>
		/// Performs message box initialization when using standard message box buttons
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">Window title</param>
		/// <param name="buttons">What buttons are to be displayed</param>
		/// <param name="image">What message box icon image is to be displayed</param>
		protected virtual void Init(string msg, string title, MessageBoxButton buttons, MessageBoxImage image)
		{
			this.InitTop(msg,title);
			this.usingExButtons = false;
			this.ButtonsEx      = null;
			this.Buttons        = buttons;
			this.SetButtonTemplates();
			this.InitBottom(image);
			this.FindDefaultButton(staticButtonDefault);
		}

		/// <summary>
		/// Performs message box initialization when using extended message box buttons
		/// </summary>
		/// <param name="msg">The message to display</param>
		/// <param name="title">Window title</param>
		/// <param name="buttons">What buttons are to be displayed</param>
		/// <param name="image">What message box icon image is to be displayed</param>
		protected virtual void Init(string msg, string title, MessageBoxButtonEx buttons, MessageBoxImage image)
		{
			this.InitTop(msg,title);
			this.usingExButtons = true;
			this.Buttons = null;
			this.ButtonsEx      = buttons;
			this.SetButtonTemplates();
			this.InitBottom(image);
			this.FindDefaultButtonEx(staticButtonDefault);
		}

		/// <summary>
		/// Init the common stuff BEFORE buttons are processed
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="title"></param>
		private void InitTop(string msg, string title)
		{
			// determine whether or not to show the details pane and checkbox
			ShowDetailsBtn = (string.IsNullOrEmpty(DetailsText)) ? Visibility.Collapsed : Visibility.Visible;
			ShowCheckBox   = (CheckBoxData == null) ? Visibility.Collapsed : Visibility.Visible;

			// Well, the binding for family/size don't appear to be working, so I have to set them 
			// manually. Weird...
			this.FontFamily = MsgFontFamily;
			this.FontSize   = MsgFontSize;
			this.LargestButtonWidth();

			// determine the screen area height, and the height of the textblock
			this.ScreenHeight = SystemParameters.WorkArea.Height - 150;

			// configure the form based on specified criteria
			this.Message      = msg;
			this.MessageTitle = (string.IsNullOrEmpty(title.Trim())) ? _DEFAULT_CAPTION : title;

			// url (if specified)
			if (Url != null)
			{
				this.tbUrl.Text    = (string.IsNullOrEmpty(UrlDisplayName)) ? Url.ToString() : UrlDisplayName;
				this.tbUrl.ToolTip = new ToolTip() { Content=Url.ToString() };
			}
		}

		/// <summary>
		/// Init common stuff AFTER buttons are processed
		/// </summary>
		/// <param name="image"></param>
		private void InitBottom(MessageBoxImage image)
		{
			// set the form's colors (you can also set these colors in your program's startup code 
			// (either in app.xaml.cs or MainWindow.cs) before you use the MessageBox for the 
			// first time
			MessageBackground = (MessageBackground == null) ? new SolidColorBrush(Colors.White) : MessageBackground;
			MessageForeground = (MessageForeground == null) ? new SolidColorBrush(Colors.Black) : MessageForeground;
			ButtonBackground  = (ButtonBackground  == null) ? new SolidColorBrush(ColorFromString("#cdcdcd")) : ButtonBackground;

			this.MessageIcon = null;

			this.msgBoxImage = image;

			if (DelegateObj != null)
			{
				Style style = (Style)(this.FindResource("ImageOpacityChanger"));
				if (style != null)
				{
					this.imgMsgBoxIcon.Style = style;
					if (!string.IsNullOrEmpty(DelegateToolTip))
					{
						ToolTip tooltip = new ToolTip() { Content = DelegateToolTip };
						// for some reason, Image elements can't do tooltips, so I assign the tootip 
						// to the parent grid. This seems to work fine.
						this.imgGrid.ToolTip = tooltip; 
					}
				}
			}

			// multiple images have the same ordinal value, and are indicated in the comments below. 
			// WTF Microsoft? 
			switch ((int)image)
			{
				case 16 : // MessageBoxImage.Error, MessageBoxImage.Stop, MessageBox.Image.Hand
					{
						this.MessageIcon = this.GetIcon(SystemIcons.Error);       
						if (!isSilent) { SystemSounds.Hand.Play(); }
					}
					break;

				case 64 : // MessageBoxImage.Information, MessageBoxImage.Asterisk 
					{
						this.MessageIcon = this.GetIcon(SystemIcons.Information); 
						if (!isSilent) { SystemSounds.Asterisk.Play(); }
					}
					break;

				case 32 : // MessageBoxImage.Question
					{
						this.MessageIcon = this.GetIcon(SystemIcons.Question);    
						if (!isSilent) { SystemSounds.Question.Play(); }
					}
					break;

				case 48 : // MessageBoxImage.Warning, MessageBoxImage.Exclamation
					{
						this.MessageIcon = this.GetIcon(SystemIcons.Warning);     
						if (!isSilent) { SystemSounds.Exclamation.Play(); }
					}
					break;
				default                          : 
					this.MessageIcon = null;
					break;
			}
		}

		/// <summary>
		/// Converts the specified icon into a WPF-comptible ImageSource object.
		/// </summary>
		/// <param name="icon"></param>
		/// <returns>An ImageSource object that represents the specified icon.</returns>
		public ImageSource GetIcon(System.Drawing.Icon icon)
		{
            var image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            return image;
		}

		// The form is rendered and position BEFORE the SizeToContent property takes effect, 
		// so we have to take stepts to re-center it after the size changes. This code takes care 
		// of the re-positioning, and is called from the SizeChanged event handler.
		/// <summary>
		/// Center the form on the screen.
		/// </summary>
		protected virtual void CenterInScreen()
		{
			double width  = this.ActualWidth;
			double height = this.ActualHeight;
			this.Left     = (SystemParameters.WorkArea.Width  - width ) / 2 + SystemParameters.WorkArea.Left;
			this.Top      = (SystemParameters.WorkArea.Height - height) / 2 + SystemParameters.WorkArea.Top;
		}

		/// <summary>
		/// Calculate the width of the largest button.
		/// </summary>
		protected void LargestButtonWidth()
		{
			// we base the width on the width of the content. This allows us to avoid the problems 
			// with button width/actualwidth properties, especially when a given button is 
			// Collapsed.
			Typeface typeface = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch);

			StackPanel panel = (StackPanel)this.stackButtons.Child;
			double width = 0;
			string largestName = string.Empty;
			foreach (Button button in panel.Children)
			{
				// Using the FormattedText object 
				// will strip whitespace before measuring the text, so we convert spaces to double 
				// hyphens to compensate (I like to pad button Content with a leading and trailing 
				// space) so that the button is wide enough to present a more padded appearance.
				FormattedText formattedText = new FormattedText((button.Name=="btnDetails") ? "--Details--" : ((string)(button.Content)).Replace(" ", "--"), 
																CultureInfo.CurrentUICulture, 
																FlowDirection.LeftToRight, 
																typeface, 
																FontSize = this.FontSize,
																System.Windows.Media.Brushes.Black, 
																VisualTreeHelper.GetDpi(this).PixelsPerDip);
				if (width < formattedText.Width)
				{
					largestName = button.Name;
				}
				width = Math.Max(width, formattedText.Width);
			}
			this.ButtonWidth = Math.Ceiling(width/*width + polyArrow.Width+polyArrow.Margin.Right+Margin.Left*/);
		}

		/// <summary>
		/// Sets the custom button template if necessary and possible
		/// </summary>
		private void SetButtonTemplates()
		{
			// set the button template (if specified)
			if (!string.IsNullOrEmpty(ButtonTemplateName))
			{
				bool foundResource = true;
				try
				{
					this.FindResource(ButtonTemplateName);
				}
				catch (Exception)
				{
					foundResource = false;
				}
				if (foundResource)
				{
					this.btnOK.SetResourceReference    (Control.TemplateProperty, ButtonTemplateName);
					this.btnYes.SetResourceReference   (Control.TemplateProperty, ButtonTemplateName);
					this.btnNo.SetResourceReference    (Control.TemplateProperty, ButtonTemplateName);
					this.btnCancel.SetResourceReference(Control.TemplateProperty, ButtonTemplateName);
					this.btnAbort.SetResourceReference (Control.TemplateProperty, ButtonTemplateName);
					this.btnRetry.SetResourceReference (Control.TemplateProperty, ButtonTemplateName);
					this.btnIgnore.SetResourceReference(Control.TemplateProperty, ButtonTemplateName);
				}
			}
		}

		/// <summary>
		/// Find the default button based on the extended buttons displayed, and the default button specified
		/// </summary>
		/// <param name="buttonDefault"></param>
		private void FindDefaultButtonEx(MessageBoxButtonDefault buttonDefault)
		{
			// determine default button
			this.IsDefaultOK     = false;
			this.IsDefaultCancel = false;
			this.IsDefaultYes    = false;
			this.IsDefaultNo     = false;
			this.IsDefaultAbort  = false;
			this.IsDefaultRetry  = false;
			this.IsDefaultIgnore = false;
			if (buttonDefault != MessageBoxButtonDefault.None)
			{
				switch (this.ButtonsEx)
				{
					case MessageBoxButtonEx.OK       :                       this.IsDefaultOK = true; break;
					case MessageBoxButtonEx.OKCancel :         
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.OK            :
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultOK     = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.Cancel        :
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultCancel = true; break;

								// windows.forms.messagebox default
								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultOK     = true; break;
							}
						}
						break;
					case MessageBoxButtonEx.YesNoCancel :      
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.Yes           : break;
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultYes    = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.No            : this.IsDefaultNo     = true; break;

								case MessageBoxButtonDefault.Button3       :
								case MessageBoxButtonDefault.Cancel        : 
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultCancel = true; break;

								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultYes    = true; break;
							}
						}
						break;
					case MessageBoxButtonEx.YesNo :            
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.Yes           :
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultYes = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.No            : 
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultNo  = true; break;

								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultYes = true; break;
							}
						}
						break;
					case MessageBoxButtonEx.RetryCancel :      
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.Retry         : 
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultRetry  = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.Cancel        : 
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultCancel = true; break;

								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultRetry  = true; break;
							}
						}
						break;
					case MessageBoxButtonEx.AbortRetryIgnore : 
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.Abort         : 
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultAbort  = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.Retry         : this.IsDefaultRetry  = true; break;

								case MessageBoxButtonDefault.Button3       :
								case MessageBoxButtonDefault.Ignore        :
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultIgnore = true; break;

								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultAbort  = true; break;
							}
						}
						break;
				}
			}
		}

		/// <summary>
		/// Find the default button based on the standard buttons displayed, and the default button specified
		/// </summary>
		/// <param name="buttonDefault"></param>
		private void FindDefaultButton(MessageBoxButtonDefault buttonDefault)
		{
			// determine default button
			this.IsDefaultOK     = false;
			this.IsDefaultCancel = false;
			this.IsDefaultYes    = false;
			this.IsDefaultNo     = false;
			this.IsDefaultAbort  = false;
			this.IsDefaultRetry  = false;
			this.IsDefaultIgnore = false;
			if (buttonDefault != MessageBoxButtonDefault.None)
			{
				switch (this.Buttons)
				{
					case MessageBoxButton.OK       :                       this.IsDefaultOK = true; break;
					case MessageBoxButton.OKCancel :         
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.OK            :
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultOK     = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.Cancel        :
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultCancel = true; break;

								// windows.forms.messagebox default
								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultOK     = true; break;
							}
						}
						break;
					case MessageBoxButton.YesNoCancel :      
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.Yes           : break;
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultYes    = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.No            : this.IsDefaultNo     = true; break;

								case MessageBoxButtonDefault.Button3       :
								case MessageBoxButtonDefault.Cancel        : 
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultCancel = true; break;

								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultYes    = true; break;
							}
						}
						break;
					case MessageBoxButton.YesNo :            
						{
							switch (buttonDefault)
							{
								case MessageBoxButtonDefault.Button1       :
								case MessageBoxButtonDefault.Yes           :
								case MessageBoxButtonDefault.MostPositive  : this.IsDefaultYes = true; break;

								case MessageBoxButtonDefault.Button2       :
								case MessageBoxButtonDefault.No            : 
								case MessageBoxButtonDefault.LeastPositive : this.IsDefaultNo  = true; break;

								case MessageBoxButtonDefault.Forms         :
								default                                    : this.IsDefaultYes = true; break;
							}
						}
						break;
				}
			}
		}

		#endregion non-static methods

		////////////////////////////////////////////////////////////////////////////////////////////
		// Form events
		////////////////////////////////////////////////////////////////////////////////////////////

		#region event handlers

		#region buttons

		/// <summary>
		/// Handle the click event for the OK button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnOK_Click(object sender, RoutedEventArgs e)
		{
			this.MessageResult   = MessageBoxResult.OK;
			this.MessageResultEx = MessageBoxResultEx.OK;
			this.DialogResult    = true;
		}

		/// <summary>
		/// Handle the click event for the Yes button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnYes_Click(object sender, RoutedEventArgs e)
		{
			this.MessageResult   = MessageBoxResult.Yes;
			this.MessageResultEx = MessageBoxResultEx.Yes;
			this.DialogResult    = true;
		}

		/// <summary>
		/// Handle the click event for the No button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnNo_Click(object sender, RoutedEventArgs e)
		{
			this.MessageResult   = MessageBoxResult.No;
			this.MessageResultEx = MessageBoxResultEx.No;
			this.DialogResult    = true;
		}

		private void BtnAbort_Click(object sender, RoutedEventArgs e)
		{
			this.MessageResult   = MessageBoxResult.None;
			this.MessageResultEx = MessageBoxResultEx.Abort;
			this.DialogResult    = true;
		}

		private void BtnRetry_Click(object sender, RoutedEventArgs e)
		{
			this.MessageResult   = MessageBoxResult.None;
			this.MessageResultEx = MessageBoxResultEx.Retry;
			this.DialogResult    = true;
		}

		private void BtnIgnore_Click(object sender, RoutedEventArgs e)
		{
			this.MessageResult   = MessageBoxResult.None;
			this.MessageResultEx = MessageBoxResultEx.Ignore;
			this.DialogResult    = true;
		}

		/// <summary>
		/// Handle the click event for the Cancel button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.MessageResult   = MessageBoxResult.Cancel;
			this.MessageResultEx = MessageBoxResultEx.Cancel;
			this.DialogResult    = true;
		}

		#endregion buttons

		/// <summary>
		/// Handle the size changed event so we can re-center the form on the screen
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NotifiableWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// we have to do this because the SizeToContent property is evaluated AFTER the window 
			// is positioned.
			this.CenterInScreen();
		}

		/// <summary>
		/// Handle the window loaded event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// if this in an error message box, this tooltip will be displayed. The intent is to set 
			// this value one time, and use it throughout the application session. However, you can 
			// certainly set it before displaying the messagebox to something that is contextually 
			// appropriate, but you'll have to clear it or reset it each time you use the MessageBox.
			this.imgMsgBoxIcon.ToolTip = (this.msgBoxImage == MessageBoxImage.Error) ? MsgBoxIconToolTip : null;
		}

		/// <summary>
		///  Handles the window closing event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			// we always clear the details text and checkbox data. 
			DetailsText         = null;
			CheckBoxData        = null;
			// reset the default button to Forms.
			staticButtonDefault = MessageBoxButtonDefault.Forms;

			// if the user didn't click a button to close the form, we set the MessageResult to the 
			// most negative button value that was available.
			if (this.MessageResult == MessageBoxResult.None)
			{
				if (usingExButtons)
				{
					switch (this.ButtonsEx)
					{
						case MessageBoxButtonEx.OK               : this.MessageResultEx = MessageBoxResultEx.OK;     break;
						case MessageBoxButtonEx.YesNoCancel      : 
						case MessageBoxButtonEx.OKCancel         : 
						case MessageBoxButtonEx.RetryCancel      : 
						case MessageBoxButtonEx.AbortRetryIgnore : this.MessageResultEx = MessageBoxResultEx.Cancel; break;
						case MessageBoxButtonEx.YesNo            : this.MessageResultEx = MessageBoxResultEx.No;     break;
					}
				}
				else
				{
					switch (this.Buttons)
					{
						case MessageBoxButton.OK          : this.MessageResult = MessageBoxResult.OK; break;
						case MessageBoxButton.YesNoCancel : 
						case MessageBoxButton.OKCancel    : this.MessageResult = MessageBoxResult.Cancel; break;
						case MessageBoxButton.YesNo       : this.MessageResult = MessageBoxResult.No; break;
					}
				}
			}
		}

		/// <summary>
		/// Since an icon isn't a button, we have to look for the left-mouse-up event to know it's 
		/// been clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ImgMsgBoxIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// we only want to allow the click if this is an error message, and the delegate 
			// object has been specified.
			if (DelegateObj != null && this.msgBoxImage == MessageBoxImage.Error && this.Buttons == MessageBoxButton.OK)
			{
				DelegateObj.PerformAction(this.Message);
				//despite the result of the method, we close this message
				if (ExitAfterErrorAction)
				{
					// make it like the user clicked the titlebar close button
					this.MessageResult = MessageBoxResult.None;
					this.DialogResult = true;
				}
			}
		}

		/// <summary>
		/// Handle the left mouse up event for the url textblock
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TbUrl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{

			ShellExecute(IntPtr.Zero, "open", Url.ToString(), "", "", 5);
		}

		// disables close button
		[DllImport( "user32.dll" )]
		private static extern IntPtr GetSystemMenu( IntPtr hWnd, bool bRevert );
		[DllImport( "user32.dll" )]
		private static extern bool EnableMenuItem( IntPtr hMenu, uint uIDEnableItem, uint uEnable );

		private const uint MF_BYCOMMAND  = 0x00000000;
		private const uint MF_GRAYED     = 0x00000001;
		private const uint SC_CLOSE      = 0xF060;
		private const int  WM_SHOWWINDOW = 0x00000018;

		private void Window_SourceInitialized(object sender, EventArgs e)
		{
			if (!enableCloseButton)
			{
				var hWnd = new WindowInteropHelper( this );
				var sysMenu = GetSystemMenu( hWnd.Handle, false );
				EnableMenuItem( sysMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED );
			}
		}

		#endregion event handlers

	}
}
