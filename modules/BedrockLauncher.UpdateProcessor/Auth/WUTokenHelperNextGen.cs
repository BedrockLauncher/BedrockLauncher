using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.UpdateProcessor.Auth
{
    internal class WUTokenHelperNextGen
    {
        /*
         * PROTOCODE
         * Requires:
         * - Microsoft.Windows.CppWinRT
         * - Mirosoft.Windows.SDK.Contracts
         * - System.Runtime
         * - Windows.Internal.Security.Authentication.Web
         * - System.Runtime.InteropServices.WindowsRuntime
         * 
        public static int GetTotalWUAccounts()
        {
            string assemblyName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Windows.Internal.Security.Authentication.Web.winmd");
            string typeName = "Windows.Internal.Security.Authentication.Web.TokenBrokerInternal";
            string methodName = "FindAllAccountsAsync";


            WindowsRuntimeMetadata.ReflectionOnlyNamespaceResolve += WindowsRuntimeMetadata_ReflectionOnlyNamespaceResolve;

            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(assemblyName);
            Type type = assembly.GetType(typeName);
            var method = type.GetMethod(methodName);
            var result = method.Invoke(null, null);

            return 0;
        }

        private static void WindowsRuntimeMetadata_ReflectionOnlyNamespaceResolve(object sender, NamespaceResolveEventArgs e)
        {
            if (e.NamespaceName == "Windows.Foundation")
            {
                e.ResolvedAssemblies.Add(Assembly.ReflectionOnlyLoadFrom(@"C:\Program Files (x86)\Windows Kits\10\References\10.0.19041.0\Windows.Foundation.FoundationContract\4.0.0.0\Windows.Foundation.FoundationContract.winmd"));
                e.ResolvedAssemblies.Add(Assembly.ReflectionOnlyLoadFrom(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.dll"));
            }
            else throw new NotImplementedException();
        }
        */

    }
}
