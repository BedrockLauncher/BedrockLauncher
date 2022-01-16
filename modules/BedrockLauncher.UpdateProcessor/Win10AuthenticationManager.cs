using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Web.Security;
using BedrockLauncher.UpdateProcessor.Auth;

namespace BedrockLauncher.UpdateProcessor
{

    public class Win10AuthenticationManager
    {

        public static Win10AuthenticationManager Default { get; set; } = new Win10AuthenticationManager();

        public ObservableCollection<WUAccount> CurrentAccounts { get; set; } = new ObservableCollection<WUAccount>();

        public void GetWUUsers()
        {
            List<WUAccount> results = new List<WUAccount>();
            List<WUAccount> previousUsers = CurrentAccounts.ToList();

            int count = WUTokenHelper.GetTotalWUAccounts();

            WUAccount default_account = new WUAccount();
            default_account.UserName = "Default Account";
            default_account.AccountType = "(No Authentication)";
            results.Add(default_account);

            for (int i = 0; i < count; i++)
            {
                WUAccount account = new WUAccount();
                account.UserName = WUTokenHelper.GetWUAccountUserName(i);
                account.AccountType = WUTokenHelper.GetWUProviderName(i);
                results.Add(account);
            }

            //Remove Users that no longer exist
            foreach (var previousUser in previousUsers)
                if (!results.Contains(previousUser)) CurrentAccounts.Remove(previousUser);

            //Add New Users
            foreach (var result in results)
                if (!CurrentAccounts.Contains(result)) CurrentAccounts.Add(result);
        }
        public string GetWUToken(int relativeIndex)
        {
            int index = relativeIndex - 1;
            if (index <= -1) return string.Empty;

            string token;
            int status = WUTokenHelper.GetWUToken(index, out token);
            WUTokenException.Test(status);
            return token;
        }

    }

}
