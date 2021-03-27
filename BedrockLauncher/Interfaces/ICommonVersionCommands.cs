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
using System.Windows.Forms.Design;
using Newtonsoft.Json;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Windows.Data;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Core;
using Windows.Management.Deployment;
using Windows.System;

namespace BedrockLauncher.Interfaces
{
    public interface ICommonVersionCommands
    {

        ICommand LaunchCommand { get; }

        ICommand DownloadCommand { get; }

        ICommand RemoveCommand { get; }

    }
}
