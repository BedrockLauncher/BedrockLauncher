#if ENABLE_CEFSHARP
using CefSharp;
#endif
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Linq;
using System.Globalization;
using System.Collections;

namespace BedrockLauncher.Components.CefSharp
{
#if ENABLE_CEFSHARP
    public class ResourceSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new ResourceSchemeHandler();
        }

        public static string SchemeName
        {
            get
            {
                return "resources";
            }
        }
    }

    public class SkinViewResourceSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new SkinViewResourceSchemeHandler();
        }

        public static string SchemeName
        {
            get
            {
                return "skinview3d";
            }
        }
    }

    public class FileSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new FileSchemeHandler();
        }

        public static string SchemeName
        {
            get
            {
                return "localfiles";
            }
        }
    }
#endif
}
