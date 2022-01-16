using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BedrockLauncher.Classes.Launcher
{
    public class DLC
    {

        public string MainImage { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Tag { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public Brush BorderColor { get; set; }
        public List<DLCImages> Images { get; set; }
    }

    public class DLC_DesignTime : DLC
    {
        /*
        public DLC_DesignTime()
        {
            BorderColor = Brushes.Goldenrod;
            Description = "Before Modified Vanilla and Vanilla enhanced. Is an experimental TP that would improve UI and other assets.\n\n" +
            "Actually, is an testing TP made since fall 2016, oriented to port menus and container / inventory GUI textures from Java Edition to Bedrock Edition more easily and efficient!\n" +
            "So, it is not any PC GUI or any Java UI texture.It is a tool that allows you to port Java GUI Containers textures easily while bringing some improvement in some basic interface. And was originally made first since autumn 2016 (Started with HUD and Java Inventory) and continued to autumn 2019 with current features.";
            MainImage = @"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png";
            Tag = "DLC";
            Title = "Vanilla Deluxe UI";
            Author = "CrisXolt";
            Images = new List<DLCImages>()
            {
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png"),
                new DLCImages(@"https://pbs.twimg.com/media/D4OZKrxWsAIMgIQ.png")
            };
            Link = @"https://mcpedl.com/vanilla-deluxe-mixed-ui/";
        }
        */

        public DLC_DesignTime()
        {
            BorderColor = Brushes.Goldenrod;
            Description = "If you are someone who likes colorful things, I have a new pack for you! Introducing the Color Coded Shulker Box GUI Resource Pack! This pack allows you to change the color of the shulker box GUI to match the color of the shulker box rather than using the default gray color. This resource pack was also designed with compatibility in mind, so it is compatible with many popular UI resource packs.";
            MainImage = @"https://api.mcpedl.com/storage/submissions/55302/100/color-coded-shulker-box-gui-resource-pack_1-520x245.png";
            Tag = "DLC";
            Title = "Color Coded Shulker Box GUI Resource Pack";
            Author = "LukasPAH";
            Images = new List<DLCImages>()
            {
                new DLCImages(@"https://api.mcpedl.com/storage/submissions/96512/images/color-coded-shulker-box-gui-resource-pack_6.png"),
                new DLCImages(@"https://api.mcpedl.com/storage/submissions/96512/images/color-coded-shulker-box-gui-resource-pack_7.png"),
                new DLCImages(@"https://api.mcpedl.com/storage/submissions/96512/images/color-coded-shulker-box-gui-resource-pack_8.png"),
                new DLCImages(@"https://api.mcpedl.com/storage/submissions/96512/images/color-coded-shulker-box-gui-resource-pack_2.png"),
                new DLCImages(@"https://api.mcpedl.com/storage/submissions/96512/images/color-coded-shulker-box-gui-resource-pack_3.png"),
                new DLCImages(@"https://api.mcpedl.com/storage/submissions/96512/images/color-coded-shulker-box-gui-resource-pack_4.png"),
                new DLCImages(@"https://api.mcpedl.com/storage/submissions/96512/images/color-coded-shulker-box-gui-resource-pack_5.png")
            };
            Link = @"https://mcpedl.com/vanilla-deluxe-mixed-ui/";
        }
    }

    public class DLCImages
    {
        public DLCImages(string url)
        {
            ImageUrl = url;
        }
        public string ImageUrl { get; set; }
    }


}
