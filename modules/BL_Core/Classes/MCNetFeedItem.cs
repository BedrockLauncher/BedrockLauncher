using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.Classes
{
    public class MCNetFeedItem
    {
       public virtual string ImageUrl { get; }
       public virtual double ImageWidth { get; }
       public virtual double ImageHeight { get; }
       public virtual string Title { get; }
       public virtual string Link { get; }
       public virtual string Tag { get;}
       public virtual string Date { get; }
    }
}
