using System;
using System.Diagnostics;

namespace JemExtensions
{
    public static class WebExtensions
    {
        public static void LaunchWebLink(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }
    }
}
