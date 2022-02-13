using System.Collections.Generic;

namespace BedrockLauncher.UpdateProcessor.Classes
{
    public class SyncResult
    {
        public List<UpdateInfo> newUpdates;
        public CookieData newCookie;

        public SyncResult()
        {
            newUpdates = new List<UpdateInfo>();
            newCookie = new CookieData();
        }
    };
}
