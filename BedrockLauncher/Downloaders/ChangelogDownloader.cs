using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using BedrockLauncher.Classes;
using BL_Core.Classes.MediaWiki;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using BL_Core.Components;
using ExtensionsDotNET;
using BedrockLauncher.Methods;
using BL_Core.Classes;

namespace BedrockLauncher.Downloaders
{
    public class ChangelogDownloader : NotifyPropertyChangedBase
    {

        #region Events

        public event EventHandler RefreshableStateChanged;
        public class RefreshableStateArgs : EventArgs
        {
            public static new RefreshableStateArgs Empty => new RefreshableStateArgs();
        }
        public void OnRefreshableStateChanged(object sender, RefreshableStateArgs e)
        {
            EventHandler handler = RefreshableStateChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        #endregion

        private const string MCWiki = @"https://minecraft.fandom.com/wiki/";
        private const string MCWikiBase = @"https://minecraft.fandom.com";
        private const string MCWikiAPI = @"https://minecraft.fandom.com/api.php";

        private const string HTMLStyle = "<style>body { color: white; } a { color: green; } img { height: auto; max-width: 100%; }</style>";
        private const string HTMLHeader = "<head>{0}{1}</head>";
        private const string HTMLFormat = "<!DOCTYPE html><html>{0}<body>{1}</body></html>";

        private bool _IsRefreshable = true;

        public bool IsRefreshable
        {
            get
            {
                return _IsRefreshable;
            }
            set
            {
                _IsRefreshable = value;
                OnPropertyChanged(nameof(IsRefreshable));
                OnRefreshableStateChanged(this, RefreshableStateArgs.Empty);
            }
        }

        public ObservableCollection<MCPatchNotesItem> PatchNotes { get; set; } = new ObservableCollection<MCPatchNotesItem>();

        private List<string> Pages_Params = new List<string>()
        {
            "action=query",
            "list=categorymembers",
            "cmtitle=Category:Bedrock%20Edition%20versions",
            "cmlimit=max",
            "format=json",
            "cmcontinue="
        };

        public class FeedbackParams
        {
            public string _branch;
            public string _suburl;
            public string keyword;
            public string trimnum;
        }


        #region Helper Functions

        private string GetResult(string url)
        {
            string result = string.Empty;
            using (WebClient wc = new WebClient()) result = wc.DownloadString(url);
            return result;
        }

        private static IEnumerable<HtmlNode> NodeSearch(HtmlNode htmlNode, Func<HtmlNode, bool> Function)
        {
            return htmlNode.DescendantsAndSelf().ToList().Where(Function);
        }

        #endregion

        #region Processing Functions

        private void ClearPatchList()
        {
            if (App.Current == null) return;
            App.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                PatchNotes.Clear();
            });
        }
        private void AddPatch(MCPatchNotesItem patch)
        {
            if (App.Current == null) return;
            App.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                PatchNotes.Add(patch);
                PatchNotes.Sort((a, b) => { return b.Version.CompareTo(a.Version); });
            });
        }

        #endregion

        #region Wiki Patch Notes Downloader

        private void DownloadWikiPatchNotes()
        {
            ClearPatchList();

            bool first_request = true;
            string cmcontinue = null;
            HtmlWeb web = new HtmlWeb();

            while (first_request || cmcontinue != null)
            {
                try 
                {
                    string ListRequestURL = string.Format("{0}?{1}{2}", MCWikiAPI, string.Join("&", Pages_Params), cmcontinue);
                    string ListRequestResult = GetResult(ListRequestURL);

                    RootObject ListRequestData = JsonConvert.DeserializeObject<RootObject>(ListRequestResult);
                    List<Val> CategoryMembers = ListRequestData.query.categorymembers;

                    foreach (var member in CategoryMembers)
                    {
                        var patch = GetPatchNotesItem(member.title, web);
                        if (patch.Version != null) AddPatch(patch);
                    }

                    if (ListRequestData.@continue != null) cmcontinue = ListRequestData.@continue.cmcontinue;
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                if (first_request) first_request = false;
            }

            //Sub-Functions

            MCPatchNotesItem GetPatchNotesItem(string title, HtmlWeb _web)
            {
                string url = MCWiki + title;
                HtmlNode node = GetPatchNotesNode(url, _web);

                MCPatchNotesItem item = new MCPatchNotesItem()
                {
                    Url = url,
                    isBeta = GetPatchNotesBetaState(title),
                    Version = GetPatchNotesVersion(title),
                    ImageUrl = GetWikiPageImage(node),
                    Content = string.Format(HTMLFormat, HTMLHeader, OptimizeWikiPage(node).InnerHtml),
                };


                return item;
            }

            bool GetPatchNotesBetaState(string title)
            {
                if (title.Contains("Beta", StringComparison.OrdinalIgnoreCase)) return true;
                else return false;
            }

            System.Version GetPatchNotesVersion(string title)
            {
                string BedrockEditionString = "Bedrock Edition ";
                string BetaString = "Beta ";
                StringComparison Comparison = StringComparison.OrdinalIgnoreCase;

                string constructed_version = title;
                constructed_version = constructed_version.Remove(BedrockEditionString, Comparison);
                constructed_version = constructed_version.Remove(BetaString, Comparison);

                bool valid_version = System.Version.TryParse(constructed_version, out System.Version current_version);

                if (valid_version) return current_version;
                else return null;

            }

            string GetWikiPageImage(HtmlNode current_node)
            {
                //Get Image
                var image_tag = current_node.DescendantsAndSelf().ToList().Where(x => x.HasClass("infobox-imagearea")).FirstOrDefault();
                if (image_tag != null)
                {
                    var image = image_tag.Descendants("img").FirstOrDefault();
                    if (image != null) return image.GetAttributeValue("src", MCPatchNotesItem.FallbackImageURL);
                }

                return MCPatchNotesItem.FallbackImageURL;
            }

            HtmlNode GetPatchNotesNode(string url, HtmlWeb _web)
            {
                var current_result = _web.Load(url);
                var content_selector = "//*[@id=\"mw-content-text\"]/div";
                return current_result.DocumentNode.SelectNodes(content_selector).FirstOrDefault();
            }
        }

        private HtmlNode OptimizeWikiPage(HtmlNode current_node)
        {

            Func<HtmlNode, bool> navbox = (x => x.HasClass("navbox") && x.HasClass("hlist") && x.HasClass("collapsible"));
            Func<HtmlNode, bool> notaninfobox = (x => x.HasClass("notaninfobox"));
            Func<HtmlNode, bool> toc = (x => x.HasClass("toc"));
            Func<HtmlNode, bool> error = (x => x.HasClass("error"));
            Func<HtmlNode, bool> msgbox = (x => x.HasClass("msgbox"));
            Func<HtmlNode, bool> links = (x => x.Name == "a" && x.HasAttributes);
            Func<HtmlNode, bool> mw_editsection = (x => x.HasClass("mw-editsection"));

            //Remove Navibox
            foreach (var sub_node in NodeSearch(current_node, navbox).ToList()) sub_node.Remove();
            //Remove notaninfobox
            foreach (var sub_node in NodeSearch(current_node, notaninfobox)) sub_node.Remove();
            //Remove toc
            foreach (var sub_node in NodeSearch(current_node, toc)) sub_node.Remove();
            //Remove error
            foreach (var sub_node in NodeSearch(current_node, error)) sub_node.Remove();
            //Remove Message Boxes
            foreach (var sub_node in NodeSearch(current_node, msgbox)) sub_node.Remove();
            //Remove Editing Elements
            foreach (var sub_node in NodeSearch(current_node, mw_editsection)) sub_node.Remove();
            //Fix Links 
            foreach (var sub_node in NodeSearch(current_node, links)) FixLinks(sub_node);
            
            return current_node;

            void FixLinks(HtmlNode sub_node)
            {
                if (sub_node.GetAttributeValue("href", "").Contains("/wiki/"))
                {
                    string currentValue = sub_node.GetAttributeValue("href", "");
                    sub_node.SetAttributeValue("href", MCWikiBase + currentValue);
                }
            }
        }

        #endregion

        #region Feedback Site Patch Notes Downloader

        public static bool GetBedrockOfTheWeekStatus()
        {
            HtmlWeb web = new HtmlWeb();
            DateTime lastBeta = DateTime.MinValue;
            string base_url = "https://feedback.minecraft.net/";
            var branch = new FeedbackParams()
            {
                _branch = "beta",
                _suburl = "/hc/en-us/sections/360001185332",
                keyword = "Windows 10"
            };

            HtmlDocument tree;
            IEnumerable<HtmlNode> vers = new List<HtmlNode>();

            string furl = base_url + branch._suburl + string.Format("?page={0}", 1);
            try { tree = web.Load(furl); }
            catch { return false; }

            vers = NodeSearch(tree.DocumentNode, (x => x.HasClass("article-list-link")));

            if (vers == null || vers.Count() == 0) return false;

            var link = vers.ToList()[0];

            string text = link.InnerText;
            if (text.Contains(branch.keyword))
            {
                string item_url = base_url + link.GetAttributeValue("href", string.Empty);
                try
                {
                    var item = web.Load(item_url);
                    lastBeta = GetPublishedDate(item);
                }
                catch
                {
                    return false;
                }
            }

            return (lastBeta.Date == DateTime.Today.Date);
        }

        public void DownloadFeedbackPatchNotes()
        {
            ClearPatchList();

            HtmlWeb web = new HtmlWeb();

            string base_url = "https://feedback.minecraft.net/";
            List<FeedbackParams> checklist = new List<FeedbackParams>()
            {
                new FeedbackParams()
                {
                    _branch = "release",
                    _suburl = "/hc/en-us/sections/360001186971",
                    keyword = "Bedrock"
                },
                new FeedbackParams()
                {
                    _branch = "beta",
                    _suburl = "/hc/en-us/sections/360001185332",
                    keyword = "Windows 10"
                }
            };

            foreach (var branch in checklist)
            {

                bool isFinished = false;
                int currentPage = 1;

                string furl;
                HtmlDocument tree;
                IEnumerable<HtmlNode> vers = new List<HtmlNode>();

                while (!isFinished)
                {
                    furl = base_url + branch._suburl + string.Format("?page={0}", currentPage);
                    try { tree = web.Load(furl); }
                    catch { isFinished = true; continue; }

                    vers = NodeSearch(tree.DocumentNode, (x => x.HasClass("article-list-link")));

                    if (vers == null || vers.Count() == 0)
                    {
                        isFinished = true;
                        continue;
                    }

                    foreach (HtmlNode link in vers)
                    {
                        string text = link.InnerText;
                        if (text.Contains(branch.keyword))
                        {
                            string item_url = base_url + link.GetAttributeValue("href", string.Empty);
                            try
                            {
                                var current_result = web.Load(item_url);
                                //var stylesheets = GetStylesheets(current_result);
                                var content = OptimizeFeedbackPage(current_result, base_url);
                                bool isBeta = branch._branch == "beta";

                                if (content != null)
                                {
                                    MCPatchNotesItem item = new MCPatchNotesItem()
                                    {
                                        Version = GetVersion(text),
                                        isBeta = isBeta,
                                        Url = item_url,
                                        ImageUrl = (isBeta ? MCPatchNotesItem.FallbackImageURL_Dev : MCPatchNotesItem.FallbackImageURL),
                                        PublishingDateString = GetPublishedDate(current_result).ToString(),
                                        Content = string.Format(HTMLFormat, UpdateHTMLHeader(new List<string>()), content.InnerHtml),
                                    };

                                    AddPatch(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex);
                            }



                        }
                    }
                    currentPage += 1;
                }
            }


            System.Version GetVersion(string title)
            {
                Regex pattern = new Regex("\\d+(\\.\\d+)+");
                Match m = pattern.Match(title);
                string constructed_version = m.Value;
                bool valid_version = System.Version.TryParse(constructed_version, out System.Version current_version);
                if (valid_version) return current_version;
                else return null;
            }

        }

        private static DateTime GetPublishedDate(HtmlDocument doc)
        {
            DateTime postedDate = DateTime.MinValue;

            var content = doc.DocumentNode.InnerText;
            if (content.Contains("Posted:"))
            {
                string detectedDate = content.Substring(content.IndexOf("Posted:")).Replace("Posted:", string.Empty);
                string constructed_date = detectedDate.Split(new[] { '\r', '\n' }).FirstOrDefault();
                constructed_date = RemoveDaySuffixes(constructed_date);
                if (DateTime.TryParse(constructed_date, out DateTime extractedDate))
                {
                    postedDate = extractedDate;
                }

                string RemoveDaySuffixes(string date_string)
                {
                    string result = Regex.Replace(date_string, @"\b(\d+)(?:st|nd|rd|th)\b", "$1");
                    return result;
                }
            }
            return postedDate;
        }

        private string UpdateHTMLHeader(List<string> styles)
        {
            string stylesheets = String.Join(Environment.NewLine, styles);
            return string.Format(HTMLHeader, stylesheets, HTMLStyle);
        }

        private HtmlNode OptimizeFeedbackPage(HtmlDocument current_result, string base_url)
        {
            Func<HtmlNode, bool> image_links = (x => x.Name == "img" && x.HasAttributes);

            var content_selector = "//*[@id=\"article-container\"]/article/section/div/div[1]";
            var current_nodes = current_result.DocumentNode.SelectNodes(content_selector);
            HtmlNode current_node = current_nodes.FirstOrDefault();

            //Fix Image Links 
            foreach (var sub_node in NodeSearch(current_node, image_links)) FixLinks(sub_node);

            return current_node;

            void FixLinks(HtmlNode sub_node)
            {
                if (sub_node.GetAttributeValue("src", "").StartsWith("/hc/"))
                {
                    string currentValue = sub_node.GetAttributeValue("src", "");
                    sub_node.SetAttributeValue("src", base_url + currentValue);
                }
            }
        }

        #endregion

        public async void UpdateList()
        {
            if (!IsRefreshable) return;

            try
            {
                IsRefreshable = false;
                await Task.Run(DownloadFeedbackPatchNotes);
                IsRefreshable = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                IsRefreshable = true;
            }
        }
    }
}
