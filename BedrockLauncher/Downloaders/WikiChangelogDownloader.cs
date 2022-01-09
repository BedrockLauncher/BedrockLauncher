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
using BedrockLauncher.Components;
using ExtensionsDotNET;
using BedrockLauncher.Methods;

namespace BedrockLauncher.Downloaders
{
    public class WikiChangelogDownloader
    {
        private const string MCWiki = @"https://minecraft.fandom.com/wiki/";
        private const string MCWikiBase = @"https://minecraft.fandom.com";
        private const string MCWikiAPI = @"https://minecraft.fandom.com/api.php";

        private const string HTMLHeader = "<head>{0}{1}</head>";
        private const string HTMLFormat = "<!DOCTYPE html><html>{0}<body>{1}</body></html>";

        private List<string> Pages_Params = new List<string>()
        {
            "action=query",
            "list=categorymembers",
            "cmtitle=Category:Bedrock%20Edition%20versions",
            "cmlimit=max",
            "format=json",
            "cmcontinue="
        };
        public ObservableCollection<MCPatchNotesItem> PatchNotes { get; set; } = new ObservableCollection<MCPatchNotesItem>();

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
    }
}
