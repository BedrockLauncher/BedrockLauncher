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

namespace BedrockLauncher.Extensions
{
    public static class MCSkinExtensions
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
            if (legacy && HatLayer) return;

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
            int preview_x = 12;
            int preview_y = 8;

            if (legacy)
            {
                if (!HatLayer)
                {
                    int source_x = 44;
                    int source_y = 20;
                    int source_width = (slim ? 3 : 4);
                    int source_height = 12;

                    var clip = new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale);
                    Bitmap part = skin.Clone(clip, skin.PixelFormat);
                    part.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImage(part, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale));
                    part.Dispose();
                }
                return;
            }
            else
            {
                int source_x = (HatLayer ? 52 : 36);
                int source_y = (HatLayer ? 52 : 52);
                int source_width = (slim ? 3 : 4);
                int source_height = 12;

                g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
            }
        }

        public static void DrawLegR(int scale, bool legacy, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            int preview_x = 8;
            int preview_y = 20;

            if (legacy)
            {
                if (!HatLayer)
                {
                    int source_x = 4;
                    int source_y = 20;
                    int source_width = 4;
                    int source_height = 12;

                    var clip = new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale);
                    Bitmap part = skin.Clone(clip, skin.PixelFormat);
                    part.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    g.DrawImage(part, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale));
                    part.Dispose();
                }
                return;
            }
            else
            {
                int source_x = (HatLayer ? 4 : 20);
                int source_y = (HatLayer ? 52 : 52);
                int source_width = 4;
                int source_height = 12;

                g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
            }


        }

        public static void DrawLegL(int scale, bool legacy, Graphics g, System.Drawing.Bitmap skin, bool HatLayer = false)
        {
            if (legacy && HatLayer) return;

            int preview_x = 4;
            int preview_y = 20;

            int source_x = (HatLayer ? 4 : 4);
            int source_y = (HatLayer ? 36 : 20);
            int source_width = 4;
            int source_height = 12;

            g.DrawImage(skin, new Rectangle(preview_x * scale, preview_y * scale, source_width * scale, source_height * scale), new Rectangle(source_x * scale, source_y * scale, source_width * scale, source_height * scale), GraphicsUnit.Pixel);
        }



    }
}
