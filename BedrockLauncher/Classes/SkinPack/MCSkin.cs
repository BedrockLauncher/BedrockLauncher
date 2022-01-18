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
using BedrockLauncher.Extensions;

namespace BedrockLauncher.Classes.SkinPack
{
    public class MCSkin : ICloneable
    {
        public string geometry { get; set; }
        public string texture { get; set; }
        public string type { get; set; }
        public string localization_name { get; set; }

        #region Internal Values (Not part of the original format)

        public MCSkin(string directory)
        {
            this.skin_directory = directory;
        }

        [JsonIgnore]
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


        [JsonIgnore]
        public string skin_directory { get; set; }

        [JsonIgnore]
        public string texture_path
        {
            get
            {
                if (skin_directory == null || texture == null) return string.Empty;
                return Path.Combine(skin_directory, texture);
            }
        }

        [JsonIgnore]
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

                    MCSkinExtensions.DrawHead(scale, isLegacy, g, skin, false);
                    MCSkinExtensions.DrawBody(scale, isLegacy, g, skin, false);
                    MCSkinExtensions.DrawArmL(scale, isLegacy, isSlim, g, skin, false);
                    MCSkinExtensions.DrawArmR(scale, isLegacy, isSlim, g, skin, false);
                    MCSkinExtensions.DrawLegL(scale, isLegacy, g, skin, false);
                    MCSkinExtensions.DrawLegR(scale, isLegacy, g, skin, false);


                    MCSkinExtensions.DrawHead(scale, isLegacy, g, skin, true);
                    MCSkinExtensions.DrawBody(scale, isLegacy, g, skin, true);
                    MCSkinExtensions.DrawArmL(scale, isLegacy, isSlim, g, skin, true);
                    MCSkinExtensions.DrawArmR(scale, isLegacy, isSlim, g, skin, true);
                    MCSkinExtensions.DrawLegL(scale, isLegacy, g, skin, true);
                    MCSkinExtensions.DrawLegR(scale, isLegacy, g, skin, true);


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

        public object Clone()
        {
            return MemberwiseClone();
        }

        #endregion
    }
}
