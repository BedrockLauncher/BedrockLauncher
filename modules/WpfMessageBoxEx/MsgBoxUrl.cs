using System;

namespace MsgBoxEx
{
	/// <summary>
	/// Represents a class that simplifies internal moving around of URL-specific info 
	/// </summary>
	public class MsgBoxUrl
	{
		/// <summary>
		/// Get/set the web link. Any Uri type other than "http" is ignored. The URL is also used for the tooltip.
		/// </summary>
		public Uri                        URL         { get; set; }
		/// <summary>
		/// Get/set the optional display name for the web link
		/// </summary>
		public string                     DisplayName { get; set; }
		/// <summary>
		/// Get/set the foreground color for the web link
		/// </summary>
		public System.Windows.Media.Color Foreground  { get; set; }

		public MsgBoxUrl()
		{
			// default color
			this.Foreground = MessageBoxEx.DefaultUrlForegroundColor;
		}
	}
}
