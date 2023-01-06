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

        public ObservableCollection<AuthenticationAccount> CurrentAccounts { get; set; } = new ObservableCollection<AuthenticationAccount>();

        public void GetWUUsers()
        {
            List<AuthenticationAccount> results = new List<AuthenticationAccount>();
            List<AuthenticationAccount> previousUsers = CurrentAccounts.ToList();

            int count = AuthenticationTokenHelper.GetTotalWUAccounts();

            AuthenticationAccount default_account = new AuthenticationAccount();
            default_account.UserName = "Default Account";
            default_account.AccountType = "(No Authentication)";
            results.Add(default_account);

            for (int i = 0; i < count; i++)
            {
                AuthenticationAccount account = new AuthenticationAccount();
                account.UserName = AuthenticationTokenHelper.GetWUAccountUserName(i);
                account.AccountType = AuthenticationTokenHelper.GetWUProviderName(i);
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
            int status = AuthenticationTokenHelper.GetWUToken(index, out token);
            AuthenticationTokenException.Test(status);
            return token;
        }

    }

}
