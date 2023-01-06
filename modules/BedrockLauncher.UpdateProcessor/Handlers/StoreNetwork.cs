using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using BedrockLauncher.UpdateProcessor.Classes;
using BedrockLauncher.UpdateProcessor.Enums;
using Xml = BedrockLauncher.UpdateProcessor.Extensions.NetworkExtensions;

namespace BedrockLauncher.UpdateProcessor.Handlers
{
    public class StoreNetwork
    {

        private static XNamespace NAMESPACE_SOAP = "http://www.w3.org/2003/05/soap-envelope";
        private static XNamespace NAMESPACE_ADDRESSING = "http://www.w3.org/2005/08/addressing";
        private static XNamespace NAMESPACE_WSSECURITY_SECEXT = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        private static XNamespace NAMESPACE_WSSECURITY_UTILITY = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private static XNamespace NAMESPACE_WU_AUTHORIZATION = "http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization";
        private static XNamespace NAMESPACE_WU_SERVICE = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService";

        private static string MINECRAFT_APP_ID = "d25480ca-36aa-46e6-b76b-39608d49558c";
        private static string MINECRAFT_PREVIEW_APP_ID = "188f32fc-5eaa-45a8-9f78-7dde4322d131";
        private static string PRIMARY_URL = "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx";
        private static string SECURED_URL = "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured";

        private string UserToken { get; set; }

        public void setMSAUserToken(string token)
        {
            UserToken = token;
        }
        public void buildCommonHeader(ref XDocument doc, ref XElement header, string url, string actionName, VersionType versionType) 
        {
            var action = Xml.CreateElement(NAMESPACE_ADDRESSING + "Action", actionName);
            header.Add(action);
            action.Add(Xml.CreateAttribute(NAMESPACE_SOAP + "mustUnderstand", "1"));
            var messageId = Xml.CreateElement(NAMESPACE_ADDRESSING + "MessageID", "urn:uuid:a68d4c75-ab85-4ca8-87db-136d281a2e28");
            header.Add(messageId);
            var to = Xml.CreateElement(NAMESPACE_ADDRESSING + "To", url);
            header.Add(to);
            to.Add(Xml.CreateAttribute(NAMESPACE_SOAP + "mustUnderstand", "1"));

            var security = Xml.CreateElement(NAMESPACE_WSSECURITY_SECEXT + "Security");
            header.Add(security);
            security.Add(Xml.CreateAttribute(NAMESPACE_SOAP + "mustUnderstand", "1"));

            var timestamp = Xml.CreateElement(NAMESPACE_WSSECURITY_UTILITY + "Timestamp");
            security.Add(timestamp);
            timestamp.Add(Xml.CreateElement(NAMESPACE_WSSECURITY_UTILITY + "Created", "2019-01-01T00:00:00.000Z"));
            timestamp.Add(Xml.CreateElement(NAMESPACE_WSSECURITY_UTILITY + "Expires", "2100-01-01T00:00:00.000Z"));

            var ticketsToken = Xml.CreateElement(NAMESPACE_WU_AUTHORIZATION + "WindowsUpdateTicketsToken");
            security.Add(ticketsToken);
            ticketsToken.Add(Xml.CreateAttribute(NAMESPACE_WSSECURITY_UTILITY + "id", "ClientMSA"));

            if (UserToken != null && UserToken.Length > 0)
            {
                if (versionType == VersionType.Beta)
                {
                    var msaToken = Xml.CreateElement("TicketType");
                    ticketsToken.Add(msaToken);
                    msaToken.Add(Xml.CreateAttribute("Name", "MSA"));
                    msaToken.Add(Xml.CreateAttribute("Version", "1.0"));
                    msaToken.Add(Xml.CreateAttribute("Policy", "MBI_SSL"));
                    msaToken.Add(Xml.CreateElement("User", UserToken.ToString()));
                }
            }

            var aadToken = Xml.CreateElement("TicketType");
            ticketsToken.Add(aadToken);
            aadToken.Add(Xml.CreateAttribute("Name", "AAD"));
            aadToken.Add(Xml.CreateAttribute("Version", "1.0"));
            aadToken.Add(Xml.CreateAttribute("Policy", "MBI_SSL"));          
        }
        public string buildGetConfigRequest(VersionType versionType = VersionType.Release) 
        {
            XDocument doc = new XDocument();
            var envelope = Xml.CreateElement(NAMESPACE_SOAP + "Envelope");
            doc.Add(envelope);

            var header = Xml.CreateElement(NAMESPACE_SOAP + "Header");
            envelope.Add(header);

            buildCommonHeader(ref doc, ref header, PRIMARY_URL, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetConfig", versionType);

            var body = Xml.CreateElement(NAMESPACE_SOAP + "Body");
            envelope.Add(body);

            var request = Xml.CreateElement(NAMESPACE_WU_SERVICE + "GetConfig");
            body.Add(request);

            request.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "protocolVersion", "1.81"));

            return doc.ToString();
        }
        public string buildCookieRequest(string configLastChanged, VersionType versionType) 
        {
            DateTime now = DateTime.UtcNow;
            XDocument doc = new XDocument();
            var envelope = Xml.CreateElement(NAMESPACE_SOAP + "Envelope");
            doc.Add(envelope);

            var header = Xml.CreateElement(NAMESPACE_SOAP + "Header");
            envelope.Add(header);

            buildCommonHeader(ref doc, ref header, PRIMARY_URL, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie", versionType);

            var body = Xml.CreateElement(NAMESPACE_SOAP + "Body");
            envelope.Add(body);

            var request = Xml.CreateElement(NAMESPACE_WU_SERVICE + "GetCookie");
            body.Add(request);

            request.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "lastChange", configLastChanged));
            request.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "currentTime", now.ToString("o")));
            request.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "protocolVersion", "1.81"));

            return doc.ToString();
        }
        public string buildSyncRequest(CookieData cookieData, VersionType versionType) 
        {

            var id = versionType == VersionType.Preview ? MINECRAFT_PREVIEW_APP_ID : MINECRAFT_APP_ID;

            XDocument doc = new XDocument();
            var envelope = Xml.CreateElement(NAMESPACE_SOAP + "Envelope");
            doc.Add(envelope);

            var header = Xml.CreateElement(NAMESPACE_SOAP + "Header");
            envelope.Add(header);

            buildCommonHeader(ref doc, ref header, PRIMARY_URL, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates", versionType);

            var body = Xml.CreateElement(NAMESPACE_SOAP + "Body");
            envelope.Add(body);

            var request = Xml.CreateElement(NAMESPACE_WU_SERVICE + "SyncUpdates");
            body.Add(request);

            var cookie = Xml.CreateElement(NAMESPACE_WU_SERVICE + "cookie");
            request.Add(cookie);
            cookie.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "Expiration", cookieData.expiration.ToString()));
            cookie.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "EncryptedData", cookieData.encryptedData.ToString()));

            var parameters = Xml.CreateElement(NAMESPACE_WU_SERVICE + "parameters");
            request.Add(parameters);

            parameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "ExpressQuery", "false"));
            buildInstalledNonLeafUpdateIDs(ref doc, ref parameters);
            parameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "SkipSoftwareSync", "false"));
            parameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "NeedTwoGroupOutOfScopeUpdates", "true"));
            var filterAppCategoryIds = Xml.CreateElement(NAMESPACE_WU_SERVICE + "FilterAppCategoryIds");
            parameters.Add(filterAppCategoryIds);
            var filterAppCatId = Xml.CreateElement(NAMESPACE_WU_SERVICE + "CategoryIdentifier");
            filterAppCategoryIds.Add(filterAppCatId);
            filterAppCatId.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "Id", id));
            parameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "TreatAppCategoryIdsAsInstalled", "true"));
            parameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "AlsoPerformRegularSync", "false"));
            parameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "ComputerSpec", ""));
            var extendedUpdateInfoParams = Xml.CreateElement(NAMESPACE_WU_SERVICE + "ExtendedUpdateInfoParameters");
            parameters.Add(extendedUpdateInfoParams);
            var xmlUpdateFragmentTypes = Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentTypes");
            extendedUpdateInfoParams.Add(xmlUpdateFragmentTypes);
            xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "Extended"));
            xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "LocalizedProperties"));
            xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "Eula"));
            var extendedUpdateLocales = Xml.CreateElement(NAMESPACE_WU_SERVICE + "Locales");
            extendedUpdateInfoParams.Add(extendedUpdateLocales);
            extendedUpdateLocales.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "string", "en-US"));
            extendedUpdateLocales.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "string", "en"));

            var clientPreferredLanguages = Xml.CreateElement(NAMESPACE_WU_SERVICE + "ClientPreferredLanguages");
            parameters.Add(clientPreferredLanguages);
            clientPreferredLanguages.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "string", "en-US"));

            var productsParameters = Xml.CreateElement(NAMESPACE_WU_SERVICE + "ProductsParameters");
            parameters.Add(productsParameters);
            productsParameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "SyncCurrentVersionOnly", "false"));
            productsParameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "DeviceAttributes", "E:BranchReadinessLevel=CBB&DchuNvidiaGrfxExists=1&ProcessorIdentifier=Intel64%20Family%206%20Model%2063%20Stepping%202&CurrentBranch=rs4_release&DataVer_RS5=1942&FlightRing=Retail&AttrDataVer=57&InstallLanguage=en-US&DchuAmdGrfxExists=1&OSUILocale=en-US&InstallationType=Client&FlightingBranchName=&Version_RS5=10&UpgEx_RS5=Green&GStatus_RS5=2&OSSkuId=48&App=WU&InstallDate=1529700913&ProcessorManufacturer=GenuineIntel&AppVer=10.0.17134.471&OSArchitecture=AMD64&UpdateManagementGroup=2&IsDeviceRetailDemo=0&HidOverGattReg=C%3A%5CWINDOWS%5CSystem32%5CDriverStore%5CFileRepository%5Chidbthle.inf_amd64_467f181075371c89%5CMicrosoft.Bluetooth.Profiles.HidOverGatt.dll&IsFlightingEnabled=0&DchuIntelGrfxExists=1&TelemetryLevel=1&DefaultUserRegion=244&DeferFeatureUpdatePeriodInDays=365&Bios=Unknown&WuClientVer=10.0.17134.471&PausedFeatureStatus=1&Steam=URL%3Asteam%20protocol&Free=8to16&OSVersion=10.0.17134.472&DeviceFamily=Windows.Desktop"));
            productsParameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "CallerAttributes", "E:Interactive=1&IsSeeker=1&Acquisition=1&SheddingAware=1&Id=Acquisition%3BMicrosoft.WindowsStore_8wekyb3d8bbwe&"));
            productsParameters.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "Products"));

            return doc.ToString();
        }
        public string buildDownloadLinkRequest(string updateId, int revisionNumber, VersionType versionType) 
        {
            XDocument doc = new XDocument();
            var envelope = Xml.CreateElement(NAMESPACE_SOAP + "Envelope");
            doc.Add(envelope);

            var header = Xml.CreateElement(NAMESPACE_SOAP + "Header");
            envelope.Add(header);

            buildCommonHeader(ref doc, ref header, SECURED_URL, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtendedUpdateInfo2", versionType);

            var body = Xml.CreateElement(NAMESPACE_SOAP + "Body");
            envelope.Add(body);

            var request = Xml.CreateElement(NAMESPACE_WU_SERVICE + "GetExtendedUpdateInfo2");
            body.Add(request);

            var updateIds = Xml.CreateElement(NAMESPACE_WU_SERVICE + "updateIDs");
            request.Add(updateIds);
            var updateIdNode = Xml.CreateElement(NAMESPACE_WU_SERVICE + "UpdateIdentity");
            updateIds.Add(updateIdNode);
            updateIdNode.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "UpdateID", updateId));
            updateIdNode.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "RevisionNumber", revisionNumber.ToString()));

            var xmlUpdateFragmentTypes = Xml.CreateElement(NAMESPACE_WU_SERVICE + "infoTypes");
            request.Add(xmlUpdateFragmentTypes);
            xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "FileUrl"));
            //xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "FileDecryption"));
            //xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "EsrpDecryptionInformation"));
            //xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "PiecesHashUrl"));
            //xmlUpdateFragmentTypes.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "XmlUpdateFragmentType", "BlockMapUrl"));

            request.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "deviceAttributes", "E:BranchReadinessLevel=CBB&DchuNvidiaGrfxExists=1&ProcessorIdentifier=Intel64%20Family%206%20Model%2063%20Stepping%202&CurrentBranch=rs4_release&DataVer_RS5=1942&FlightRing=Retail&AttrDataVer=57&InstallLanguage=en-US&DchuAmdGrfxExists=1&OSUILocale=en-US&InstallationType=Client&FlightingBranchName=&Version_RS5=10&UpgEx_RS5=Green&GStatus_RS5=2&OSSkuId=48&App=WU&InstallDate=1529700913&ProcessorManufacturer=GenuineIntel&AppVer=10.0.17134.471&OSArchitecture=AMD64&UpdateManagementGroup=2&IsDeviceRetailDemo=0&HidOverGattReg=C%3A%5CWINDOWS%5CSystem32%5CDriverStore%5CFileRepository%5Chidbthle.inf_amd64_467f181075371c89%5CMicrosoft.Bluetooth.Profiles.HidOverGatt.dll&IsFlightingEnabled=0&DchuIntelGrfxExists=1&TelemetryLevel=1&DefaultUserRegion=244&DeferFeatureUpdatePeriodInDays=365&Bios=Unknown&WuClientVer=10.0.17134.471&PausedFeatureStatus=1&Steam=URL%3Asteam%20protocol&Free=8to16&OSVersion=10.0.17134.472&DeviceFamily=Windows.Desktop"));

            return doc.ToString();
        }
        public void buildInstalledNonLeafUpdateIDs(ref XDocument doc, ref XElement paramsNode)
        {
            // Mostly random updates, took from my primary Windows installation + the detectoids for ARM
            int[] installedNonLeafUpdateIDs = {1, 2, 3, 11, 19, 2359974, 5169044, 8788830, 23110993, 23110994, 59830006,
                                       59830007, 59830008, 60484010, 62450018, 62450019, 62450020, 98959022, 98959023,
                                       98959024, 98959025, 98959026, 129905029, 130040030, 130040031, 130040032,
                                       130040033, 138372035, 138372036, 139536037, 158941041, 158941042, 158941043,
                                       158941044,
                                       // ARM
                                       133399034, 2359977
            };
            var node = Xml.CreateElement(NAMESPACE_WU_SERVICE + "InstalledNonLeafUpdateIDs");
            paramsNode.Add(node);
            foreach (int i in installedNonLeafUpdateIDs)
            {
                node.Add(Xml.CreateElement(NAMESPACE_WU_SERVICE + "int", i.ToString()));
            }
        }
        private async Task<string> doHttpRequest(string url, string data) 
        {
            string ret = string.Empty;

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Length", data.Length.ToString());
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("Windows-Update-Agent/10.0.10011.16384 Client-Protocol/1.81");
            var response = await client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/soap+xml"));
            using (var streamReader = new StreamReader(response.Content.ReadAsStream())) ret = streamReader.ReadToEnd();
            return ret;
        }
        private void maybeThrowSOAPFault(XDocument doc)
        {
            string code = null;
            try
            {
                var envelope = Xml.first_node_or_throw(doc.Root, NAMESPACE_SOAP + "Envelope");
                var body = Xml.first_node_or_throw(envelope, NAMESPACE_SOAP + "Body");
                var fault = Xml.first_node_or_throw(body, NAMESPACE_SOAP + "Fault");
                var detail = Xml.first_node_or_throw(fault, NAMESPACE_SOAP + "Detail");
                var errorCode = Xml.first_node_or_throw(detail, "ErrorCode");
                code = errorCode.Value;
            }
            catch (Exception) 
            {

            }

            if (!string.IsNullOrEmpty(code)) throw new SOAPError(code);       
        }
        public async Task dumpConfig()
        {
            string request = buildGetConfigRequest();
            string ret = await doHttpRequest(PRIMARY_URL, request);
            XDocument doc = XDocument.Parse(ret);
        }
        public async Task<string> fetchConfigLastChanged()
        {
            string request = buildGetConfigRequest();
            string ret = await doHttpRequest(PRIMARY_URL, request);
            XDocument doc = XDocument.Parse(ret);
            var envelope = Xml.first_node_or_throw(doc.Root, NAMESPACE_SOAP + "Envelope");
            var body = Xml.first_node_or_throw(envelope, NAMESPACE_SOAP + "Body");
            var resp = Xml.first_node_or_throw(body, NAMESPACE_WU_SERVICE + "GetConfigResponse");
            var res = Xml.first_node_or_throw(resp, NAMESPACE_WU_SERVICE + "GetConfigResult");
            return Xml.first_node_or_throw(res, NAMESPACE_WU_SERVICE + "LastChange").Value;
        }
        public async Task<CookieData> fetchCookie(string configLastChanged, VersionType versionType)
        {
            string request = buildCookieRequest(configLastChanged, versionType);
            string ret = await doHttpRequest(PRIMARY_URL, request);
            XDocument doc = XDocument.Parse(ret);
            var envelope = Xml.first_node_or_throw(doc.Root, NAMESPACE_SOAP + "Envelope");
            var body = Xml.first_node_or_throw(envelope, NAMESPACE_SOAP + "Body");
            var resp = Xml.first_node_or_throw(body, NAMESPACE_WU_SERVICE + "GetCookieResponse");
            var res = Xml.first_node_or_throw(resp, NAMESPACE_WU_SERVICE + "GetCookieResult");

            CookieData data;
            data.encryptedData = Xml.first_node_or_throw(res, NAMESPACE_WU_SERVICE + "EncryptedData").Value;
            data.expiration = Xml.first_node_or_throw(res, NAMESPACE_WU_SERVICE + "Expiration").Value;
            return data;
        }
        public async Task<SyncResult> syncVersion(CookieData cookie, VersionType versionType)
        {
            string request = buildSyncRequest(cookie, versionType);
            string ret = await doHttpRequest(PRIMARY_URL, request);
            XDocument doc = XDocument.Parse(ret);

            try
            {
                var envelope = Xml.first_node_or_throw(doc.Root, NAMESPACE_SOAP + "Envelope");
                var body = Xml.first_node_or_throw(envelope, NAMESPACE_SOAP + "Body");
                var resp = Xml.first_node_or_throw(body, NAMESPACE_WU_SERVICE + "SyncUpdatesResponse");
                var res = Xml.first_node_or_throw(resp, NAMESPACE_WU_SERVICE + "SyncUpdatesResult");
                var newUpdates = Xml.first_node_or_throw(res, NAMESPACE_WU_SERVICE + "NewUpdates");
                SyncResult data = new SyncResult();
                for (var it = Xml.first_node(newUpdates, NAMESPACE_WU_SERVICE + "UpdateInfo"); it != null; it = Xml.next_sibling(it, NAMESPACE_WU_SERVICE + "UpdateInfo"))
                {
                    UpdateInfo info = new UpdateInfo();
                    info.serverId = Xml.first_node_or_throw(it, NAMESPACE_WU_SERVICE + "ID").Value;
                    info.addXmlInfo(Xml.first_node_or_throw(it, NAMESPACE_WU_SERVICE + "Xml").Value);
                    data.newUpdates.Add(info);
                }

                var newCookie = Xml.first_node(res, NAMESPACE_WU_SERVICE + "NewCookie");
                if (newCookie != null)
                {
                    data.newCookie.encryptedData = Xml.first_node_or_throw(newCookie, NAMESPACE_WU_SERVICE + "EncryptedData").Value;
                    data.newCookie.expiration = Xml.first_node_or_throw(newCookie, NAMESPACE_WU_SERVICE + "Expiration").Value;
                }
                return data;
            }
            catch (Exception e) 
            {
                maybeThrowSOAPFault(doc);
                throw new Exception("syncVersion", e);
            }

        }
        public async Task<DownloadLinkResult> getDownloadLinks(string updateIdentity, int revisionNumber, VersionType versionType)
        {
            try
            {
                string request = buildDownloadLinkRequest(updateIdentity, revisionNumber, versionType);
                string ret = await doHttpRequest(SECURED_URL, request);
                XDocument doc = XDocument.Parse(ret);
                var envelope = Xml.first_node_or_throw(doc.Root, NAMESPACE_SOAP + "Envelope");
                var body = Xml.first_node_or_throw(envelope, NAMESPACE_SOAP + "Body");
                var resp = Xml.first_node_or_throw(body, NAMESPACE_WU_SERVICE + "GetExtendedUpdateInfo2Response");
                var res = Xml.first_node_or_throw(resp, NAMESPACE_WU_SERVICE + "GetExtendedUpdateInfo2Result");
                var fileLocations = Xml.first_node_or_throw(res, NAMESPACE_WU_SERVICE + "FileLocations");
                DownloadLinkResult data;
                data.files = new List<FileLocation>();
                for (var it = Xml.first_node(fileLocations, NAMESPACE_WU_SERVICE + "FileLocation"); it != null; it = Xml.next_sibling(it, NAMESPACE_WU_SERVICE + "FileLocation"))
                {
                    FileLocation info;
                    info.url = Xml.first_node_or_throw(it, NAMESPACE_WU_SERVICE + "Url").Value;
                    data.files.Add(info);
                }
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to Find Download Links", ex);
            }

        }
        public async Task<string> getDownloadLink(string updateIdentity, int revisionNumber, VersionType versionType)
        {
            var result = await getDownloadLinks(updateIdentity, revisionNumber, versionType);
            foreach (var s in result.files)
            {
                if (s.url.StartsWith("http://tlu.dl.delivery.mp.microsoft.com/")) return s.url;
            }
            return null;
        }
    }
}
