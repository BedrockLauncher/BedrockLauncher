using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.Language
{
    public static class AvaliableLanuages
    {
        public static List<string> GetAll()
        {
            return new List<string>()
            {
                "en-US",
                "pt-PT",
                "ru-RU",
                "pt-BR"
            };
        }
    }
}
