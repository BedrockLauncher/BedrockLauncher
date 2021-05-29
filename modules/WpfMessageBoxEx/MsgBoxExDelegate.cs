using System;
using System.Windows;

namespace MsgBoxEx
{
	// This class MUST be inherited, and the PerformAction method MUST be overriden.
	/// <summary>
	/// Represents the object that allows a message box icon to execute code. This class must be 
	/// inherited.
	/// </summary>
	public abstract class MsgBoxExDelegate
	{
		/// <summary>
		/// Get/set the message text from the calling message box
		/// </summary>
		public string   Message     { get; set; }
		/// <summary>
		/// Get/set the details text (if it was specified in the messagebox)
		/// </summary>
		public string   Details     { get; set; }
		/// <summary>
		/// Get/set the message datetime at which this object was created
		/// </summary>
		public DateTime MessageDate { get; set; }

		/// <summary>
		/// Performs the desired action, and returns the result. MUST BE OVERIDDEN IN INHERITING CLASS. 
		/// </summary>
		/// <returns></returns>
		public virtual MessageBoxResult PerformAction(string message, string details = null)
		{
			throw new NotImplementedException();
		}
	}
}
