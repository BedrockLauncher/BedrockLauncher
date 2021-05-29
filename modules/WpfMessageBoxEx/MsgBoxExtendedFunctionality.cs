namespace MsgBoxEx
{
	/// <summary>
	/// Represents one of or more of the extended functionality items
	/// </summary>
	public class MsgBoxExtendedFunctionality
	{
		public MessageBoxButtonDefault ButtonDefault   { get; set; }
		/// <summary>
		/// Get/set the details text to display
		/// </summary>
		public string               DetailsText     { get; set; }

		/// <summary>
		/// Get/set the checkbox data object
		/// </summary>
		public MsgBoxExCheckBoxData CheckBoxData    { get; set; }

		/// <summary>
		/// Get/set the clickable icon delegate object
		/// </summary>
		public MsgBoxExDelegate     MessageDelegate { get; set; }
		/// <summary>
		/// Get/set the flag indicating whether the messagebox is dismissed after the delegate 
		/// action has completed.
		/// </summary>
		public bool                 ExitAfterAction { get; set; }
		public string               DelegateToolTip { get; set; }

		/// <summary>
		/// Get/set the url
		/// </summary>
		public MsgBoxUrl            URL             { get; set; }

		public MsgBoxExtendedFunctionality()
		{
			this.ButtonDefault   = MessageBoxButtonDefault.Forms;
			this.DetailsText     = null;
			this.CheckBoxData    = null;
			this.MessageDelegate = null;
			this.URL             = null;
			this.DelegateToolTip = "Click this icon for additional info/actions.";
		}
	}
}

