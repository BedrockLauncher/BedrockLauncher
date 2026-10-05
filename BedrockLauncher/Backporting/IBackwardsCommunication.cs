using BedrockLauncher.ViewModels;
using PostSharp.Patterns.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace BedrockLauncher.Backend.Backporting
{
    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public interface IBackwardsCommunication
    {
        public DependencyObject ProgressBarGrid { get; }
        public Task<System.Windows.Forms.DialogResult> ShowDialog_YesNo(string? title, string? content);
        public void errormsg(string dialogTitle, string dialogText, Exception ex2);
        public Task<bool> exceptionmsg(Exception ex);
        public void UpdateAnimatePageTransitions(bool value);
    }
}
