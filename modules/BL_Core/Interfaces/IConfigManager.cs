using BL_Core.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL_Core.Interfaces
{
    public interface IConfigManager
    {
        MCVersionList Versions { get; }
    }
}
