using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsgBoxEx
{
	// the WPF MessageBoxButtons enum does not include AbortRetryCancel or RetryCancel - WTF microsoft!?
	// these buttons cannot be used with the standard messagebox
	/// <summary>
	/// Message box button groups for MessageBoxEx
	/// </summary>
	public enum MessageBoxButtonEx { OK=0, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel }

	// the wpf message box does not use the DialogResult enum for return values. At the same time, they 
	// don't include these values in the MessageBoxResult enum.  - WTF microsoft!?
	/// <summary>
	/// Message box result for MessageBoxEx
	/// </summary>
	public enum MessageBoxResultEx { None=0, OK, Cancel, Abort, Retry, Ignore, Yes, No }

	/// <summary>
	/// Default button for MessageBoxEx
	/// </summary> 
	public enum MessageBoxButtonDefault 
	{ 
		OK, Cancel, Yes, No, Abort, Retry, Ignore, // specific button
		Button1, Button2, Button3,                 // button by ordinal left-to-right position
		MostPositive, LeastPositive,               // button by positivity
		Forms,                                     // button according to the Windows.Forms standard messagebox
		None                                       // no default button
	}

}
