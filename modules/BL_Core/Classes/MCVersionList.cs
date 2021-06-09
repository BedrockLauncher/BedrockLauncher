using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BL_Core.Classes
{
    public class MCVersionList : ObservableCollection<MCVersion>
    {
        public readonly string cacheFile;
        public readonly string userCacheFile;

        public MCVersionList(string _cacheFile, string _userCacheFile)
        {
            this.cacheFile = _cacheFile;
            this.userCacheFile = _userCacheFile;
        }
    }
}
