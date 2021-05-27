using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BedrockLauncher.Methods
{
    public static class StoreNetwork
    {
        const string NAMESPACE_SOAP = "http://www.w3.org/2003/05/soap-envelope";
        const string NAMESPACE_ADDRESSING = "http://www.w3.org/2005/08/addressing";
        const string NAMESPACE_WSSECURITY_SECEXT = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        const string NAMESPACE_WSSECURITY_UTILITY = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        const string NAMESPACE_WU_AUTHORIZATION = "http://schemas.microsoft.com/msus/2014/10/WindowsUpdateAuthorization";
        const string MINECRAFT_APP_ID = "d25480ca-36aa-46e6-b76b-39608d49558c";
        const string PRIMARY_URL = "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx";

        /*
        public static void buildCommonHeader(ref XmlDocument doc, ref XmlElement header, string actionName)
        {
            var action = doc.CreateElement("a:Action", actionName);
            header.AppendChild(action);
            action.Attributes.Append(doc.CreateAttribute("s:mustUnderstand", "1"));
            var messageId = doc.CreateElement("a:MessageID", "urn:uuid:a68d4c75-ab85-4ca8-87db-136d281a2e28");
            header.AppendChild(messageId);
            var to = doc.CreateElement("a:To", "https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
            header.AppendChild(to);
            to.Attributes.Append(doc.CreateAttribute("s:mustUnderstand", "1"));

            var security = doc.CreateElement("o:Security");
            header.AppendChild(security);
            security.Attributes.Append(doc.CreateAttribute("s:mustUnderstand", "1"));
            security.Attributes.Append(doc.CreateAttribute("xmlns:o", NAMESPACE_WSSECURITY_SECEXT));

            var timestamp = doc.CreateElement("Timestamp");
            security.AppendChild(timestamp);
            timestamp.Attributes.Append(doc.CreateAttribute("xmlns", NAMESPACE_WSSECURITY_UTILITY));
            timestamp.AppendChild(doc.CreateElement("Created", "2019-01-01T00:00:00.000Z"));
            timestamp.AppendChild(doc.CreateElement("Expires", "2100-01-01T00:00:00.000Z"));

            var ticketsToken = doc.CreateElement("wuws:WindowsUpdateTicketsToken");
            security.AppendChild(ticketsToken);
            ticketsToken.Attributes.Append(doc.CreateAttribute("wsu:id", "ClientMSA"));
            ticketsToken.Attributes.Append(doc.CreateAttribute("xmlns:wsu", NAMESPACE_WSSECURITY_UTILITY));
            ticketsToken.Attributes.Append(doc.CreateAttribute("xmlns:wuws", NAMESPACE_WU_AUTHORIZATION));

            if (userToken.size() > 0)
            {
                var msaToken = doc.CreateElement("TicketType");
                ticketsToken.AppendChild(msaToken);
                msaToken.Attributes.Append(doc.CreateAttribute("Name", "MSA"));
                msaToken.Attributes.Append(doc.CreateAttribute("Version", "1.0"));
                msaToken.Attributes.Append(doc.CreateAttribute("Policy", "MBI_SSL"));
                msaToken.AppendChild(doc.CreateElement("User", userToken.c_str()));
            }

            var aadToken = doc.CreateElement("TicketType");
            ticketsToken.AppendChild(aadToken);
            aadToken.Attributes.Append(doc.CreateAttribute("Name", "AAD"));
            aadToken.Attributes.Append(doc.CreateAttribute("Version", "1.0"));
            aadToken.Attributes.Append(doc.CreateAttribute("Policy", "MBI_SSL"));
        }

        public static string buildGetConfigRequest()
        {
            XmlDocument doc = new XmlDocument();
            var envelope = doc.CreateElement("s:Envelope");
            doc.AppendChild(envelope);
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:a", NAMESPACE_ADDRESSING));
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:s", NAMESPACE_SOAP));

            var header = doc.CreateElement("s:Header");
            envelope.AppendChild(header);

            buildCommonHeader(ref doc, ref header, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetConfig");

            var body = doc.CreateElement("s:Body");
            envelope.AppendChild(body);

            var request = doc.CreateElement("GetConfig");
            body.AppendChild(request);
            request.Attributes.Append(doc.CreateAttribute("xmlns", "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"));

            request.AppendChild(doc.CreateElement("protocolVersion", "1.81"));

            return doc.ToString();
        }

        public static string buildCookieRequest(string configLastChanged)
        {
            XmlDocument doc = new XmlDocument();
            var envelope = doc.CreateElement("s:Envelope");
            doc.AppendChild(envelope);
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:a", NAMESPACE_ADDRESSING));
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:s", NAMESPACE_SOAP));

            var header = doc.CreateElement("s:Header");
            envelope.AppendChild(header);

            buildCommonHeader(ref doc, ref header, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie");

            var body = doc.CreateElement("s:Body");
            envelope.AppendChild(body);

            var request = doc.CreateElement("GetCookie");
            body.AppendChild(request);
            request.Attributes.Append(doc.CreateAttribute("xmlns", "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"));

            request.AppendChild(doc.CreateElement("lastChange", configLastChanged.ToString()));
            request.AppendChild(doc.CreateElement("currentTime", formatTime(DateTime.Now).ToString()));
            request.AppendChild(doc.CreateElement("protocolVersion", "1.81"));

            return doc.ToString();
        }

        public static string buildSyncRequest(CookieData cookieData)
        {
            XmlDocument doc = new XmlDocument();
            var envelope = doc.CreateElement("s:Envelope");
            doc.AppendChild(envelope);
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:a", NAMESPACE_ADDRESSING));
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:s", NAMESPACE_SOAP));

            var header = doc.CreateElement("s:Header");
            envelope.AppendChild(header);

            buildCommonHeader(ref doc, ref header, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates");

            var body = doc.CreateElement("s:Body");
            envelope.AppendChild(body);

            var request = doc.CreateElement("SyncUpdates");
            body.AppendChild(request);
            request.Attributes.Append(doc.CreateAttribute("xmlns", "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"));

            var cookie = doc.CreateElement("cookie");
            request.AppendChild(cookie);
            cookie.AppendChild(doc.CreateElement("Expiration", cookieData.expiration.c_str()));
            cookie.AppendChild(doc.CreateElement("EncryptedData", cookieData.encryptedData.c_str()));

            var _params = doc.CreateElement("parameters");
            request.AppendChild(_params);

            _params.AppendChild(doc.CreateElement("ExpressQuery", "false"));
            buildInstalledNonLeafUpdateIDs(ref doc, ref _params);
            _params.AppendChild(doc.CreateElement("SkipSoftwareSync", "false"));
            _params.AppendChild(doc.CreateElement("NeedTwoGroupOutOfScopeUpdates", "true"));
            var filterAppCategoryIds = doc.CreateElement("FilterAppCategoryIds");
            _params.AppendChild(filterAppCategoryIds);
            var filterAppCatId = doc.CreateElement("CategoryIdentifier");
            filterAppCategoryIds.AppendChild(filterAppCatId);
            filterAppCatId.AppendChild(doc.CreateElement("Id", MINECRAFT_APP_ID));
            _params.AppendChild(doc.CreateElement("TreatAppCategoryIdsAsInstalled", "true"));
            _params.AppendChild(doc.CreateElement("AlsoPerformRegularSync", "false"));
            _params.AppendChild(doc.CreateElement("ComputerSpec", ""));
            var extendedUpdateInfoParams = doc.CreateElement("ExtendedUpdateInfoParameters");
            _params.AppendChild(extendedUpdateInfoParams);
            var xmlUpdateFragmentTypes = doc.CreateElement("XmlUpdateFragmentTypes");
            extendedUpdateInfoParams.AppendChild(xmlUpdateFragmentTypes);
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "Extended"));
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "LocalizedProperties"));
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "Eula"));
            var extendedUpdateLocales = doc.CreateElement("Locales");
            extendedUpdateInfoParams.AppendChild(extendedUpdateLocales);
            extendedUpdateLocales.AppendChild(doc.CreateElement("string", "en-US"));
            extendedUpdateLocales.AppendChild(doc.CreateElement("string", "en"));

            var clientPreferredLanguages = doc.CreateElement("ClientPreferredLanguages");
            _params.AppendChild(clientPreferredLanguages);
            clientPreferredLanguages.AppendChild(doc.CreateElement("string", "en-US"));

            var productsParameters = doc.CreateElement("ProductsParameters");
            _params.AppendChild(productsParameters);
            productsParameters.AppendChild(doc.CreateElement("SyncCurrentVersionOnly", "false"));
            productsParameters.AppendChild(doc.CreateElement("DeviceAttributes", "E:BranchReadinessLevel=CBB&DchuNvidiaGrfxExists=1&ProcessorIdentifier=Intel64%20Family%206%20Model%2063%20Stepping%202&CurrentBranch=rs4_release&DataVer_RS5=1942&FlightRing=Retail&AttrDataVer=57&InstallLanguage=en-US&DchuAmdGrfxExists=1&OSUILocale=en-US&InstallationType=Client&FlightingBranchName=&Version_RS5=10&UpgEx_RS5=Green&GStatus_RS5=2&OSSkuId=48&App=WU&InstallDate=1529700913&ProcessorManufacturer=GenuineIntel&AppVer=10.0.17134.471&OSArchitecture=AMD64&UpdateManagementGroup=2&IsDeviceRetailDemo=0&HidOverGattReg=C%3A%5CWINDOWS%5CSystem32%5CDriverStore%5CFileRepository%5Chidbthle.inf_amd64_467f181075371c89%5CMicrosoft.Bluetooth.Profiles.HidOverGatt.dll&IsFlightingEnabled=0&DchuIntelGrfxExists=1&TelemetryLevel=1&DefaultUserRegion=244&DeferFeatureUpdatePeriodInDays=365&Bios=Unknown&WuClientVer=10.0.17134.471&PausedFeatureStatus=1&Steam=URL%3Asteam%20protocol&Free=8to16&OSVersion=10.0.17134.472&DeviceFamily=Windows.Desktop"));
            productsParameters.AppendChild(doc.CreateElement("CallerAttributes", "E:Interactive=1&IsSeeker=1&Acquisition=1&SheddingAware=1&Id=Acquisition%3BMicrosoft.WindowsStore_8wekyb3d8bbwe&"));
            productsParameters.AppendChild(doc.CreateElement("Products"));

            return doc.ToString();
        }

        public static string buildDownloadLinkRequest(string  updateId, int revisionNumber)
        {
            XmlDocument doc = new XmlDocument();
            var envelope = doc.CreateElement("s:Envelope");
            doc.AppendChild(envelope);
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:a", NAMESPACE_ADDRESSING));
            envelope.Attributes.Append(doc.CreateAttribute("xmlns:s", NAMESPACE_SOAP));

            var header = doc.CreateElement("s:Header");
            envelope.AppendChild(header);

            buildCommonHeader(ref doc, ref header, "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtendedUpdateInfo2");

            var body = doc.CreateElement("s:Body");
            envelope.AppendChild(body);

            var request = doc.CreateElement("GetExtendedUpdateInfo2");
            body.AppendChild(request);
            request.Attributes.Append(doc.CreateAttribute("xmlns", "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService"));

            var updateIds = doc.CreateElement("updateIDs");
            request.AppendChild(updateIds);
            var updateIdNode = doc.CreateElement("UpdateIdentity");
            updateIds.AppendChild(updateIdNode);
            updateIdNode.AppendChild(doc.CreateElement("UpdateID", updateId));
            string revisionNumberStr = revisionNumber.ToString();
            updateIdNode.AppendChild(doc.CreateElement("RevisionNumber", revisionNumberStr));

            var xmlUpdateFragmentTypes = doc.CreateElement("infoTypes");
            request.AppendChild(xmlUpdateFragmentTypes);
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "FileUrl"));
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "FileDecryption"));
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "EsrpDecryptionInformation"));
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "PiecesHashUrl"));
            xmlUpdateFragmentTypes.AppendChild(doc.CreateElement("XmlUpdateFragmentType", "BlockMapUrl"));

            request.AppendChild(doc.CreateElement("deviceAttributes", "E:BranchReadinessLevel=CBB&DchuNvidiaGrfxExists=1&ProcessorIdentifier=Intel64%20Family%206%20Model%2063%20Stepping%202&CurrentBranch=rs4_release&DataVer_RS5=1942&FlightRing=Retail&AttrDataVer=57&InstallLanguage=en-US&DchuAmdGrfxExists=1&OSUILocale=en-US&InstallationType=Client&FlightingBranchName=&Version_RS5=10&UpgEx_RS5=Green&GStatus_RS5=2&OSSkuId=48&App=WU&InstallDate=1529700913&ProcessorManufacturer=GenuineIntel&AppVer=10.0.17134.471&OSArchitecture=AMD64&UpdateManagementGroup=2&IsDeviceRetailDemo=0&HidOverGattReg=C%3A%5CWINDOWS%5CSystem32%5CDriverStore%5CFileRepository%5Chidbthle.inf_amd64_467f181075371c89%5CMicrosoft.Bluetooth.Profiles.HidOverGatt.dll&IsFlightingEnabled=0&DchuIntelGrfxExists=1&TelemetryLevel=1&DefaultUserRegion=244&DeferFeatureUpdatePeriodInDays=365&Bios=Unknown&WuClientVer=10.0.17134.471&PausedFeatureStatus=1&Steam=URL%3Asteam%20protocol&Free=8to16&OSVersion=10.0.17134.472&DeviceFamily=Windows.Desktop"));

                return doc.ToString();
        }

        public static void buildInstalledNonLeafUpdateIDs(ref XmlDocument doc, ref XmlElement paramsNode)
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
            var node = doc.CreateElement("InstalledNonLeafUpdateIDs");
            paramsNode.AppendChild(node);
            for (int i = 0; i < installedNonLeafUpdateIDs.Count(); i++)
            {
                node.AppendChild(doc.CreateElement("int", installedNonLeafUpdateIDs[i].ToString()));
            }
        }

        public static DateTime formatTime(DateTime dateTime)
        {
            //TODO
            //Insure Functionality
            return dateTime;
        }

        public static int httpOnWrite()
        {
            //size_t httpOnWrite(char* ptr, size_t size, size_t nmemb, void* userdata)

            //((string*) userdata)->append(ptr, size * nmemb);
            //return size * nmemb;

            return 0;
        }

        public static void doHttpRequest(string url, string data, ref string ret)
        {

            //printf("Request with body: %s\n", data);
            //
            //    CURL* curl = curl_easy_init();
            //    curl_easy_setopt(curl, CURLOPT_URL, url);
            //
            //struct curl_slist * headers = nullptr;
            //headers = curl_slist_append(headers, "Content-Type: application/soap+xml; charset=utf-8");
            //headers = curl_slist_append(headers, "User-Agent: Windows-Update-Agent/10.0.10011.16384 Client-Protocol/1.81");
            //curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
            //curl_easy_setopt(curl, CURLOPT_POSTFIELDS, data);
            //curl_easy_setopt(curl, CURLOPT_SSL_VERIFYHOST, 0);
            //curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 0);
            //curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, httpOnWrite);
            //curl_easy_setopt(curl, CURLOPT_WRITEDATA, &ret);
            //
            //CURLcode res = curl_easy_perform(curl);
            //
            //curl_easy_cleanup(curl);
            //curl_slist_free_all(headers);
            //
            //printf("Response: %s\n", ret.c_str());
            //
            //if (res != CURLE_OK)
            //    throw std::runtime_error("doHttpRequest: res not ok");

        }
        */

        /*
        void maybeThrowSOAPFault(rapidxml::XmlDocument &doc)
        {
            string code;
            try
            {
                var & envelope = firstNodeOrThrow(doc, "s:Envelope");
                var & body = firstNodeOrThrow(envelope, "s:Body");
                var & fault = firstNodeOrThrow(body, "s:Fault");
                var & detail = firstNodeOrThrow(fault, "s:Detail");
                var & errorCode = firstNodeOrThrow(detail, "ErrorCode");
                code = errorCode.value();
            }
            catch (std::exception &ignored) {
            }
            if (!code.empty())
                throw SOAPError(code);
            }

            void dumpConfig() {
                string request = buildGetConfigRequest();
                string ret;
                doHttpRequest(PRIMARY_URL, request.c_str(), ret);
                XmlDocument doc;
                doc.parse < 0 > (&ret[0]);
            }

            string fetchConfigLastChanged() {
                string request = buildGetConfigRequest();
                string ret;
                doHttpRequest(PRIMARY_URL, request.c_str(), ret);
                XmlDocument doc;
                doc.parse < 0 > (&ret[0]);
                var & envelope = firstNodeOrThrow(doc, "s:Envelope");
                var & body = firstNodeOrThrow(envelope, "s:Body");
                var & resp = firstNodeOrThrow(body, "GetConfigResponse");
                var & res = firstNodeOrThrow(resp, "GetConfigResult");
                return firstNodeOrThrow(res, "LastChange").value();
            }

            CookieData fetchCookie(string const&configLastChanged) {
                string request = buildCookieRequest(configLastChanged);
                string ret;
                doHttpRequest(PRIMARY_URL, request.c_str(), ret);
                XmlDocument doc;
                doc.parse < 0 > (&ret[0]);
                var & envelope = firstNodeOrThrow(doc, "s:Envelope");
                var & body = firstNodeOrThrow(envelope, "s:Body");
                var & resp = firstNodeOrThrow(body, "GetCookieResponse");
                var & res = firstNodeOrThrow(resp, "GetCookieResult");

                CookieData data;
                data.encryptedData = firstNodeOrThrow(res, "EncryptedData").value();
                data.expiration = firstNodeOrThrow(res, "Expiration").value();
                return data;
            }

            SyncResult syncVersion(CookieData const&cookie) {
                string request = buildSyncRequest(cookie);
                string ret;
                doHttpRequest(PRIMARY_URL, request.c_str(), ret);
                XmlDocument doc;
                doc.parse < 0 > (&ret[0]);
                try
                {
                    var & envelope = firstNodeOrThrow(doc, "s:Envelope");
                    var & body = firstNodeOrThrow(envelope, "s:Body");
                    var & resp = firstNodeOrThrow(body, "SyncUpdatesResponse");
                    var & res = firstNodeOrThrow(resp, "SyncUpdatesResult");
                    var & newUpdates = firstNodeOrThrow(res, "NewUpdates");
                    SyncResult data;
                    for (var it = newUpdates.first_node("UpdateInfo"); it != nullptr; it = it->next_sibling("UpdateInfo"))
                    {
                        UpdateInfo info;
                        info.serverId = firstNodeOrThrow(*it, "ID").value();
                        info.addXmlInfo(firstNodeOrThrow(*it, "Xml").value()); // NOTE: destroys the node
                        data.newUpdates.push_back(std::move(info));
                    }
                    var newCookie = res.first_node("NewCookie");
                    if (newCookie != nullptr)
                    {
                        data.newCookie.encryptedData = firstNodeOrThrow(*newCookie, "EncryptedData").value();
                        data.newCookie.expiration = firstNodeOrThrow(*newCookie, "Expiration").value();
                    }
                    return data;
                }
                catch (std::exception &e) {
                    maybeThrowSOAPFault(doc);
                    throw e;
                }
                }

                DownloadLinkResult getDownloadLink(
                        string const &updateId, int revisionNumber) {
                    string request = buildDownloadLinkRequest(updateId, revisionNumber);
                    string ret;
                    doHttpRequest(PRIMARY_URL, request.c_str(), ret);
                    XmlDocument doc;
                    doc.parse < 0 > (&ret[0]);
                    var & envelope = firstNodeOrThrow(doc, "s:Envelope");
                    var & body = firstNodeOrThrow(envelope, "s:Body");
                    var & resp = firstNodeOrThrow(body, "GetExtendedUpdateInfo2Response");
                    var & res = firstNodeOrThrow(resp, "GetExtendedUpdateInfo2Result");
                    var & fileLocations = firstNodeOrThrow(res, "FileLocations");
                    DownloadLinkResult data;
                    for (var it = fileLocations.first_node("FileLocation"); it != nullptr; it = it->next_sibling("FileLocation"))
                    {
                        FileLocation info;
                        info.url = firstNodeOrThrow(*it, "Url").value();
                        data.files.push_back(std::move(info));
                    }
                    return data;
                }

                void UpdateInfo::addXmlInfo(char * val) {
                    printf("%s\n", val);
                    XmlDocument doc;
                    doc.parse < 0 > (val);
                    var & identity = firstNodeOrThrow(doc, "UpdateIdentity");
                    var attr = identity.first_attribute("UpdateID");
                    if (attr != nullptr)
                        updateId = attr->value();
                    var applicability = doc.first_node("ApplicabilityRules");
                    var metadata = applicability != nullptr ? applicability->first_node("Metadata") : nullptr;
                    var metadataPkgAppx = metadata != nullptr ? metadata->first_node("AppxPackageMetadata") : nullptr;
                    var metadataAppx = metadataPkgAppx != nullptr ? metadataPkgAppx->first_node("AppxMetadata") : nullptr;
                    attr = metadataAppx != nullptr ? metadataAppx->first_attribute("PackageMoniker") : nullptr;
                    if (attr != nullptr)
                        packageMoniker = attr->value();
                }
        }*/
    }
}

