using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Security.Principal;
using BedrockLauncher.UpdateProcessor.Authentication;

namespace BedrockLauncher.UpdateProcessor.Authentication
{

    public class AuthenticationManager
    {

        public static AuthenticationManager Default { get; set; } = new AuthenticationManager();

        public string GetWUToken(int relativeIndex)
        {
            int index = relativeIndex - 1;
            if (index <= -1) return string.Empty;

            string token;
            int status = AuthenticationTokenHelper.GetWUToken(index, out token);
            AuthenticationTokenException.Test(status);
            return token;
        }
    }
}
