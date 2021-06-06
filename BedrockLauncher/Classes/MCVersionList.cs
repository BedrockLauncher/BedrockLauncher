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
using BedrockLauncher.UpdateProcessor;
using ExtensionsDotNET;

namespace BedrockLauncher.Classes
{
    public class MCVersionList : ObservableCollection<MCVersion>
    {

        public readonly string cacheFile;
        public readonly string userCacheFile;
        private readonly Interfaces.ICommonVersionCommands commands;


        public MCVersionList(string _cacheFile, string _userCacheFile, Interfaces.ICommonVersionCommands _commands)
        {
            this.cacheFile = _cacheFile;
            this.userCacheFile = _userCacheFile;
            this.commands = _commands;
        }

        public void PraseDB(Win10VersionDBManager.Win10VersionJsonDb db)
        {
            foreach (var v in db.list)
            {
                if (!this.ToList().Exists(x => x.UUID == v.uuid || x.Name == v.version)) this.Add(new MCVersion(v.uuid, v.version, v.isBeta, commands));
            }
            this.Sort((x, y) => Compare(x, y));

            int Compare(MCVersion x, MCVersion y)
            {
                try
                {
                    var a = new Version(x.Name);
                    var b = new Version(y.Name);
                    return b.CompareTo(a);
                }
                catch
                {
                    return y.Name.CompareTo(x.Name);
                }

            }
        }





    }
}
