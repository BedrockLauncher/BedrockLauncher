using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace JemExtensions.WPF
{
    public static class WindowExtensions
    {
        public static bool? ShowDialogUntilTaskCompletion(this Window window, Task task, int minDurationMsec = 500)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (minDurationMsec < 0)
                throw new ArgumentOutOfRangeException(nameof(minDurationMsec));

            var closeDelay = Task.Delay(minDurationMsec);
            HandleTaskCompletion();
            return window.ShowDialog();

            async void HandleTaskCompletion()
            {
                try
                {
                    await Task.Yield(); // Ensure that the completion is asynchronous
                    await task;
                }
                catch { } // Ignore exception
                finally
                {
                    try
                    {
                        await closeDelay;
                        window.Close();
                    }
                    catch { } // Ignore exception
                }
            }
        }

    }
}
