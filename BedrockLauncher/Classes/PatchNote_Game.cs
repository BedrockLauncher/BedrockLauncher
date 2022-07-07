using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Classes.Launcher
{
    public class PatchNotes_Game_Item
    {
        public string fallback_image => (patchNoteType == "preview" || patchNoteType == "beta") ? 
            Constants.PATCHNOTE_BETA_IMG : Constants.PATCHNOTE_RELEASE_IMG;
        public bool isBeta => (patchNoteType == "preview" || patchNoteType == "beta");
        public string image_url => Constants.PATCHNOTES_IMGPREFIX_URL + image?.url ?? "null.png";

        public string title { get; set; }
        public string version { get; set; }
        public string patchNoteType { get; set; }
        public string date { get; set; }
        public PatchNotes_Game_Image image { get; set; }
        public string body { get; set; }
        public string id { get; set; }
        public string contentPath { get; set; }
    }

    public class PatchNotes_Game_Image
    {
        public string url { get; set; }
        public string title { get; set; }

    }

    public class PatchNotes_Game_Root
    {
        public int version { get; set; }
        public List<PatchNotes_Game_Item> entries { get; set; }
    }
}
