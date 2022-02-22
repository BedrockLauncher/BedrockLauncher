using System;
using System.IO;
using System.Diagnostics;

namespace BedrockLauncher.Uninstaller
{
    public class Program
    {
        static void Main(string[] args)
        {
            Process[] prs = Process.GetProcesses();
            foreach (Process pr in prs)
            {
                if (pr.ProcessName == "BedrockLauncher")
                {
                    pr.Kill();
                }

            }

            Console.WriteLine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
            deleteAll();
            Console.ReadLine();

            void deleteAll()
            {
                string currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                System.IO.DirectoryInfo di = new DirectoryInfo(currentPath);

                foreach (FileInfo file in di.GetFiles())
                {
                    if (file.Name != "Uninstaller.exe") { file.Delete(); }
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
        }
    }
}
