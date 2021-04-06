using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace BedrockLauncher.Classes
{

    public enum MCSkinGeometry
    {
        Normal,
        Custom,
        Slim
    }

    public static class MCSkinUtilites
    {
        public static void DrawHead(int scale, bool legacy, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            int preview_x = 4;
            int preview_y = 0;

            int source_x = (HatLayer ? 40 : 8);
            int source_y = (HatLayer ? 8 : 8);
            int source_width = 8;
            int source_height = 8;


            g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
        }

        public static void DrawBody(int scale, bool legacy, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            if (legacy && HatLayer) return;

            int preview_x = 4;
            int preview_y = 8;

            int source_x = (HatLayer ? 20 : 20);
            int source_y = (HatLayer ? 36 : 20);
            int source_width = 8;
            int source_height = 12;

            g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
        }

        public static void DrawArmL(int scale, bool legacy, bool slim, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            if (legacy)
            {
                if (!HatLayer) DrawLegacyArmL(scale, legacy, slim, g, skin, HatLayer); 
                return;
            }

            int preview_x = (slim ? 1 : 0);
            int preview_y = 8;

            int source_x = (HatLayer ? 44 : 44);
            int source_y = (HatLayer ? 36 : 20);
            int source_width = (slim ? 3 : 4);
            int source_height = 12;

            g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
        }

        public static void DrawArmR(int scale, bool legacy, bool slim, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            if (legacy && HatLayer) return;

            int preview_x = 12;
            int preview_y = 8;

            int source_x = (HatLayer ? 52 : 36);
            int source_y = (HatLayer ? 52 : 52);
            int source_width = (slim ? 3 : 4);
            int source_height = 12;

            g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
        }

        public static void DrawLegR(int scale, bool legacy, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            if (legacy && HatLayer) return;

            int preview_x = 8;
            int preview_y = 20;

            int source_x = (HatLayer ? 4 : 20);
            int source_y = (HatLayer ? 52 : 52);
            int source_width = 4;
            int source_height = 12;

            g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
        }

        public static void DrawLegL(int scale, bool legacy, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            if (legacy)
            {
                if (!HatLayer) DrawLegacyLegL(scale, legacy, g, skin, HatLayer);
                return;
            }

            int preview_x = 4;
            int preview_y = 20;

            int source_x = (HatLayer ? 4 : 4);
            int source_y = (HatLayer ? 36 : 20);
            int source_width = 4;
            int source_height = 12;

            g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
        }

        public static void DrawLegacyLegL(int scale, bool legacy, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            int preview_x = 4;
            int preview_y = 20;

            int source_x = (HatLayer ? 4 : 20);
            int source_y = (HatLayer ? 52 : 52);
            int source_width = 4;
            int source_height = 12;

            var clip = new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale);
            Bitmap part = skin.Clone(clip, skin.PixelFormat);
            part.RotateFlip(RotateFlipType.RotateNoneFlipX);
            g.DrawImage(part, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale));
            part.Dispose();
        }

        public static void DrawLegacyArmL(int scale, bool legacy, bool slim, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            int preview_x = (slim ? 1 : 0);
            int preview_y = 8;

            int source_x = (HatLayer ? 44 : 44);
            int source_y = (HatLayer ? 36 : 20);
            int source_width = (slim ? 3 : 4);
            int source_height = 12;

            var clip = new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale);
            Bitmap part = skin.Clone(clip, skin.PixelFormat);
            part.RotateFlip(RotateFlipType.RotateNoneFlipX);
            g.DrawImage(part, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale));
            part.Dispose();
        }



    }

    public class MCSkin
    {
        public string geometry { get; set; }
        public string texture { get; set; }
        public string type { get; set; }
        public string localization_name { get; set; }

        //Internal Values (Not part of the original format)

        public MCSkinGeometry skin_type 
        {
            get
            {
                switch (geometry)
                {
                    case "geometry.humanoid.customSlim":
                        return MCSkinGeometry.Slim;
                    case "geometry.humanoid.custom":
                        return MCSkinGeometry.Normal;
                    default:
                        return MCSkinGeometry.Custom;
                }
            }
        }
        public string skin_directory { get; set; }
        public string texture_path
        {
            get
            {
                return Path.Combine(skin_directory, texture);
            }
        }

        public BitmapImage rendered_preview
        {
            get
            {


                System.Drawing.Bitmap skin = new Bitmap(texture_path);
                if (skin == null) return null;

                bool isLegacy = skin.Width != skin.Height;
                int scale = skin.Width / 64;
                bool isSlim = skin_type == MCSkinGeometry.Slim;

                int preview_width = 16 * scale;
                int preview_height = 32 * scale;

                Bitmap preview = new Bitmap(preview_width, preview_height);

                using (Graphics g = Graphics.FromImage(preview))
                {
                    g.FillRectangle(Brushes.Transparent, new Rectangle(0, 0, preview.Width, preview.Height));

                    MCSkinUtilites.DrawHead(scale, isLegacy, g, skin, false);
                    MCSkinUtilites.DrawBody(scale, isLegacy, g, skin, false);
                    MCSkinUtilites.DrawArmL(scale, isLegacy, isSlim, g, skin, false);
                    MCSkinUtilites.DrawArmR(scale, isLegacy, isSlim, g, skin, false);
                    MCSkinUtilites.DrawLegL(scale, isLegacy, g, skin, false);
                    MCSkinUtilites.DrawLegR(scale, isLegacy, g, skin, false);

                    
                    MCSkinUtilites.DrawHead(scale, isLegacy, g, skin, true);
                    MCSkinUtilites.DrawBody(scale, isLegacy, g, skin, true);
                    MCSkinUtilites.DrawArmL(scale, isLegacy, isSlim, g, skin, true);
                    MCSkinUtilites.DrawArmR(scale, isLegacy, isSlim, g, skin, true);
                    MCSkinUtilites.DrawLegL(scale, isLegacy, g, skin, true);
                    MCSkinUtilites.DrawLegR(scale, isLegacy, g, skin, true);
                    

                }

                var bitmapImage = new BitmapImage();

                using (var ms = new MemoryStream())
                {
                    preview.Save(ms, ImageFormat.Png);
                    preview.Dispose();
                    ms.Seek(0, SeekOrigin.Begin);
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                }

                return bitmapImage;
            }
        }
    }

    public class MCSkinPack
    {
        public class Skins
        {
            public string geometry { get; set; }
            public List<MCSkin> skins { get; set; } = new List<MCSkin>();
            public string serialize_name { get; set; }
            public string localization_name { get; set; }
        }

        public class Mainfest
        {
            public int format_version { get; set; }
            public Header header { get; set; }

            public class Header
            {
                public string name { get; set; }
                public string uuid { get; set; }
                public int[] version { get; set; }
            }
        }

        public MCSkinPack()
        {

        }


        public Mainfest Metadata { get; private set; }

        public Skins Content { get; private set; }

        public string Directory { get; private set; }

        public bool isDev { get; private set; }

        public string IconPath
        {
            get
            {
                return Path.Combine(Directory, "pack_icon.png");
            }
        }

        public string DisplayName
        {
            get
            {
                return Metadata?.header?.name ?? "pack.name" + (isDev ? " (DEV)" : "");
            }
        }

        public string VersionName
        {
            get
            {
                var version = Metadata?.header?.version ?? new int[] { 0, 0, 1 };
                return string.Join(".", version);
            }
        }

        public MCSkinPack(string _Directory, Mainfest _Metadata, bool _isDev = false) : base()
        {
            Directory = _Directory;
            Metadata = _Metadata;
            isDev = _isDev;
            Content = ValidateSkins(_Directory);
        }


        public static MCSkinPack ValidatePack(string Directory)
        {
            try 
            {
                string json = File.ReadAllText(Path.Combine(Directory, "manifest.json"));
                Mainfest mainfest = JsonConvert.DeserializeObject<Mainfest>(json);
                return new MCSkinPack(Directory, mainfest);
            }
            catch
            {
                return null;
            }
        }

        public static Skins ValidateSkins(string Directory)
        {
            try
            {
                string json = File.ReadAllText(Path.Combine(Directory, "skins.json"));
                Skins skins_meta = JsonConvert.DeserializeObject<Skins>(json);
                if (skins_meta == null) return null;
                if (skins_meta.skins == null) return null;
                
                foreach (var skin in skins_meta.skins)
                {
                    skin.skin_directory = Directory;
                }

                return skins_meta;
            }
            catch
            {
                return new Skins();
            }
        }
    }
}
