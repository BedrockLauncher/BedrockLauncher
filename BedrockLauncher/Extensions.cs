using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Threading;

namespace BedrockLauncher
{


    public static class Extensions
    {

        public static IFrame GetFrame(this ChromiumWebBrowser browser, string FrameName)
        {
            IFrame frame = null;

            var identifiers = browser.GetBrowser().GetFrameIdentifiers();

            foreach (var i in identifiers)
            {
                frame = browser.GetBrowser().GetFrame(i);
                if (frame.Name == FrameName)
                    return frame;
            }

            return null;
        }
        public static string GetAvaliableFileName(string fileName, string directory, string format = "_{0}")
        {
            int i = 0;

            string newFileName = fileName;

            if (!File.Exists(Path.Combine(directory, newFileName))) return newFileName;
            else while (File.Exists(Path.Combine(directory, newFileName))) 
            {
                    i++;
                    newFileName = Path.GetFileNameWithoutExtension(fileName) + string.Format(format, i) + "." + Path.GetExtension(fileName);
            }

            return string.Empty;
        }
    }

    public static class DirectoryExtensions
    {
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            var diSource = new DirectoryInfo(sourceDirectory);
            var diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Program.Log(string.Format(@"Copying {0}\{1}", target.FullName, fi.Name));
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }

    public static class CollectionExtensions
    {
        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }
    }

    public static class WPFExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            foreach (childItem child in FindVisualChildren<childItem>(obj))
            {
                return child;
            }

            return null;
        }
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
        public static string Remove(this string source, string oldString, StringComparison comparison)
        {
            int index = source.IndexOf(oldString, comparison);

            while (index > -1)
            {
                source = source.Remove(index, oldString.Length);
                source = source.Insert(index, string.Empty);

                index = source.IndexOf(oldString, index + string.Empty.Length, comparison);
            }

            return source;
        }
        public static string Replace(this string source, string oldString, string newString, StringComparison comparison)
        {
            int index = source.IndexOf(oldString, comparison);

            while (index > -1)
            {
                source = source.Remove(index, oldString.Length);
                source = source.Insert(index, newString);

                index = source.IndexOf(oldString, index + newString.Length, comparison);
            }

            return source;
        }
    }
}
