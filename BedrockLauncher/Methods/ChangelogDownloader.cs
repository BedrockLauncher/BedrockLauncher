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
using BedrockLauncher.Classes.MediaWiki;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BedrockLauncher.Methods
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

        private IEnumerable<HtmlNode> NodeSearch(HtmlNode htmlNode, Func<HtmlNode, bool> Function)
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
                catch (Exception ex) { Program.LogConsoleLine(ex); }

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
                                        PublishingDateString = GetPublishedDate(current_result),
                                        Content = string.Format(HTMLFormat, UpdateHTMLHeader(new List<string>()), content.InnerHtml),
                                    };

                                    AddPatch(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.LogConsoleLine(ex);
                            }



                        }
                    }
                    currentPage += 1;
                }
            }


            string GetPublishedDate(HtmlDocument doc)
            {
                string postedDate = string.Empty;

                Func<HtmlNode, bool> description_meta = (x => x.Name == "meta" && x.HasAttributes);

                foreach (var sub_node in NodeSearch(doc.DocumentNode, description_meta))
                {
                    string property = sub_node.GetAttributeValue("property", "");
                    string content = sub_node.GetAttributeValue("content", "");
                    if (property == "og:description")
                    {
                        if (content.StartsWith("Posted:"))
                        {
                            string constructed_date = content.Split(new[] { '\r', '\n' }).FirstOrDefault().Replace("Posted:", string.Empty);
                            constructed_date = RemoveDaySuffixes(constructed_date);
                            if (DateTime.TryParse(constructed_date, out DateTime extractedDate))
                            {
                                postedDate = extractedDate.ToString();
                            }

                            string RemoveDaySuffixes(string date_string)
                            {
                                string result = Regex.Replace(date_string, @"\b(\d+)(?:st|nd|rd|th)\b", "$1");
                                return result;
                            }

                        }
                    }
                }

                return postedDate;
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

        private string UpdateHTMLHeader(List<string> styles)
        {
            string stylesheets = String.Join(Environment.NewLine, styles);
            return string.Format(HTMLHeader, stylesheets, HTMLStyle);
        }

        private List<string> GetStylesheets(HtmlDocument current_result)
        {
            var header_node = current_result.DocumentNode.SelectNodes("/html/head").FirstOrDefault();
            var inline_stylelist = header_node.Descendants().Where(x => x.Name == "style").ToList();
            var stylesheet_list = header_node.Descendants().Where(x => x.Name == "link" && x.GetAttributeValue("rel", "nostylesheet") == "stylesheet").ToList();
            var merged_styles = inline_stylelist.Concat(stylesheet_list);

            var final_list = new List<string>();

            foreach (var style in merged_styles)
            {
                final_list.Add(style.OuterHtml);
            }

            return final_list;

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
                Program.LogConsoleLine(ex);
                IsRefreshable = true;
            }
        }
    }
}
