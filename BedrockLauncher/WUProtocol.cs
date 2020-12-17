using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MCLauncher {
    class WUProtocol {
        private static readonly string DEFAULT_URL = "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx";
        private static readonly string SECURED_URL = "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured";

        private static XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
        private static XNamespace addressing = "http://www.w3.org/2005/08/addressing";
        private static XNamespace secext = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        private static XNamespace secutil = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private static XNamespace wuws = "http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization";
        private static XNamespace wuclient = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService";

        private string _msaUserToken;

        public void SetMSAUserToken(string token) {
            _msaUserToken = token;
        }

        private XElement BuildWUTickets() {
            XElement tickets = new XElement(wuws + "WindowsUpdateTicketsToken",
                        new XAttribute(secutil + "id", "ClientMSA"),
                        new XAttribute(XNamespace.Xmlns + "wsu", secutil),
                        new XAttribute(XNamespace.Xmlns + "wuws", wuws));
            if (_msaUserToken != null) {
                tickets.Add(new XElement("TicketType",
                    new XAttribute("Name", "MSA"),
                    new XAttribute("Version", "1.0"),
                    new XAttribute("Policy", "MBI_SSL"),
                    new XElement("User", _msaUserToken)));
            }
            tickets.Add(new XElement("TicketType", "",
                new XAttribute("Name", "AAD"),
                new XAttribute("Version", "1.0"),
                new XAttribute("Policy", "MBI_SSL")));
            return tickets;
        }

        private XElement BuildHeader(string url, string methodName) {
            DateTime now = DateTime.UtcNow;
            XElement header = new XElement(soap + "Header",
                new XElement(addressing + "Action", "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/" + methodName,
                    new XAttribute(soap + "mustUnderstand", 1)),
                new XElement(addressing + "MessageID", "urn:uuid:5754a03d-d8d5-489f-b24d-efc31b3fd32d"),
                new XElement(addressing + "To", url,
                    new XAttribute(soap + "mustUnderstand", 1)),
                new XElement(secext + "Security",
                    new XAttribute(soap + "mustUnderstand", 1),
                    new XAttribute(XNamespace.Xmlns + "o", secext),
                    new XElement(secutil + "Timestamp",
                        new XElement(secutil + "Created", now.ToString("o")),
                        new XElement(secutil + "Expires", (now.AddMinutes(5)).ToString("o"))),
                    BuildWUTickets()));
            return header;
        }

        public string GetDownloadUrl() {
            return SECURED_URL;
        }

        public XDocument BuildDownloadRequest(string updateIdentity, string revisionNumber) {
            XElement envelope = new XElement(soap + "Envelope");
            envelope.Add(new XAttribute(XNamespace.Xmlns + "a", addressing));
            envelope.Add(new XAttribute(XNamespace.Xmlns + "s", soap));
            envelope.Add(BuildHeader(GetDownloadUrl(), "GetExtendedUpdateInfo2"));
            envelope.Add(new XElement(soap + "Body",
                new XElement(wuclient + "GetExtendedUpdateInfo2",
                    new XElement(wuclient + "updateIDs",
                        new XElement(wuclient + "UpdateIdentity",
                            new XElement(wuclient + "UpdateID", updateIdentity),
                            new XElement(wuclient + "RevisionNumber", revisionNumber))),
                    new XElement(wuclient + "infoTypes",
                        new XElement(wuclient + "XmlUpdateFragmentType", "FileUrl")),
                    new XElement(wuclient + "deviceAttributes", "E:BranchReadinessLevel=CBB&DchuNvidiaGrfxExists=1&ProcessorIdentifier=Intel64%20Family%206%20Model%2063%20Stepping%202&CurrentBranch=rs4_release&DataVer_RS5=1942&FlightRing=Retail&AttrDataVer=57&InstallLanguage=en-US&DchuAmdGrfxExists=1&OSUILocale=en-US&InstallationType=Client&FlightingBranchName=&Version_RS5=10&UpgEx_RS5=Green&GStatus_RS5=2&OSSkuId=48&App=WU&InstallDate=1529700913&ProcessorManufacturer=GenuineIntel&AppVer=10.0.17134.471&OSArchitecture=AMD64&UpdateManagementGroup=2&IsDeviceRetailDemo=0&HidOverGattReg=C%3A%5CWINDOWS%5CSystem32%5CDriverStore%5CFileRepository%5Chidbthle.inf_amd64_467f181075371c89%5CMicrosoft.Bluetooth.Profiles.HidOverGatt.dll&IsFlightingEnabled=0&DchuIntelGrfxExists=1&TelemetryLevel=1&DefaultUserRegion=244&DeferFeatureUpdatePeriodInDays=365&Bios=Unknown&WuClientVer=10.0.17134.471&PausedFeatureStatus=1&Steam=URL%3Asteam%20protocol&Free=8to16&OSVersion=10.0.17134.472&DeviceFamily=Windows.Desktop"))));
            return new XDocument(envelope);
        }

        public string[] ExtractDownloadResponseUrls(XDocument doc) {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
            nsmgr.AddNamespace("wu", "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService");
            XElement result = doc.XPathSelectElement("/s:Envelope/s:Body/wu:GetExtendedUpdateInfo2Response/wu:GetExtendedUpdateInfo2Result", nsmgr);
            if (result == null)
                return new string[0];
            return result.XPathSelectElements("wu:FileLocations/wu:FileLocation/wu:Url", nsmgr).Select(u => u.Value).ToArray();
        }

    }
}
