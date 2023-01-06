using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Linq.Expressions;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using System.Text.RegularExpressions;
using JemExtensions;
using JemExtensions.WPF;

namespace BedrockLauncher.Dungeons.Classes
{
    public class DungeonsMod : NotifyPropertyChangedBase
    {

        public DungeonsMod(FileInfo file)
        {
            this.Name = file.Name.Split('.')[0];
            this.Directory = file.DirectoryName;
            OnPropertyChanged(nameof(IsEnabled));
        }

        public string Name { get; set; }
        private string Directory { get; set; }

        private string FilePath
        {
            get
            {
                string EnabledFile = Path.Combine(Directory, Name + ".pak");
                string DisabledFile = Path.Combine(Directory, Name + ".pak.disable");

                if (File.Exists(EnabledFile)) return EnabledFile;
                else if (File.Exists(DisabledFile)) return DisabledFile;
                else return string.Empty;
            }
        }

        public bool IsEnabled
        {
            get
            {
                string EnabledFile = Path.Combine(Directory, Name + ".pak");
                string DisabledFile = Path.Combine(Directory, Name + ".pak.disable");

                if (File.Exists(EnabledFile)) return true;
                else if (File.Exists(DisabledFile)) return false;
                else return false;
            }
            set
            {
                try
                {
                    string EnabledFile = Path.Combine(Directory, Name + ".pak");
                    string DisabledFile = Path.Combine(Directory, Name + ".pak.disable");

                    if (File.Exists(EnabledFile))
                    {
                        var result = new FileInfo(EnabledFile);
                        result.Rename(Name + ".pak.disable");
                    }
                    else if (File.Exists(DisabledFile))
                    {
                        var result = new FileInfo(DisabledFile);
                        result.Rename(Name + ".pak");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, ex.HResult.ToString());                    
                }

                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public string Size
        {
            get
            {
                GetInstallSize();
                return _StoredInstallationSize;
            }
        }

        private string _StoredInstallationSize = "N/A";
        private bool RequireSizeRecalculation = true;

        private void GetInstallSize()
        {
            if (!RequireSizeRecalculation)
            {
                return;
            }

            if (File.Exists(FilePath))
            {
                FileInfo dirInfo = new FileInfo(FilePath);
                long dirSize = dirInfo.Length;

                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = dirSize;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                _StoredInstallationSize = String.Format("{0:0.##} {1}", len, sizes[order]);
                RequireSizeRecalculation = false;
            }
            else
            {
                _StoredInstallationSize = "N/A";
                RequireSizeRecalculation = false;
            }
            OnPropertyChanged(nameof(Size));

        }

        public string IconPath
        {
            get
            {
                return "/BedrockLauncher.Dungeons;component/Resources/images/ui/icons/dungeons_icon.png";
            }
        }


        public void OpenInExplorer()
        {
            try
            {
                string EnabledFile = Path.Combine(Directory, Name + ".pak");
                string DisabledFile = Path.Combine(Directory, Name + ".pak.disable");

                if (File.Exists(EnabledFile)) Process.Start("explorer.exe", string.Format("/select, \"{0}\"", EnabledFile));
                else if (File.Exists(DisabledFile)) Process.Start("explorer.exe", string.Format("/select, \"{0}\"", DisabledFile));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, ex.HResult.ToString());
            }
        }

        public void Delete()
        {
            try
            {
                string EnabledFile = Path.Combine(Directory, Name + ".pak");
                string DisabledFile = Path.Combine(Directory, Name + ".pak.disable");

                if (File.Exists(EnabledFile)) File.Delete(EnabledFile);
                else if (File.Exists(DisabledFile)) File.Delete(DisabledFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace, ex.HResult.ToString());
            }
        }
    }
}
