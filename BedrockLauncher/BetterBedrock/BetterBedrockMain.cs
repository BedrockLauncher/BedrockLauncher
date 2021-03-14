using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BedrockLauncher
{
    public class BetterBedrockMain
    {
        private String EvaluatePath(String path)
        {

            try
            {
                String folder = Path.GetDirectoryName(path);
                if (!Directory.Exists(folder))
                {
                    // Try to create the directory.
                    DirectoryInfo di = Directory.CreateDirectory(folder);
                }
            }
            catch (IOException ioex)
            {
                Console.WriteLine(ioex.Message);
                return "";
            }
            return path;
        }
        // yep, its really magic what this thing do
        public void MakeMagic(string path)
        {
            Console.WriteLine("started magic");

            var fontPath = path + @"data\resource_packs\vanilla\font";
            var environmentPath = path + @"data\resource_packs\vanilla\textures\environment";

            File.Delete(fontPath + @"\default8.png");
            File.Delete(fontPath + @"\glyph_00.png");
            File.Delete(fontPath + @"\glyph_04.png");
            File.Delete(fontPath + @"\glyph_E0.png");
            File.Delete(environmentPath + @"\destroy_stage_0.png");
            File.Delete(environmentPath + @"\destroy_stage_1.png");
            File.Delete(environmentPath + @"\destroy_stage_2.png");
            File.Delete(environmentPath + @"\destroy_stage_3.png");
            File.Delete(environmentPath + @"\destroy_stage_4.png");
            File.Delete(environmentPath + @"\destroy_stage_5.png");
            File.Delete(environmentPath + @"\destroy_stage_6.png");
            File.Delete(environmentPath + @"\destroy_stage_7.png");
            File.Delete(environmentPath + @"\destroy_stage_8.png");
            File.Delete(environmentPath + @"\destroy_stage_9.png");

            Properties.Resources.default8.Save(fontPath + @"\default8.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.glyph_04.Save(fontPath + @"\glyph_04.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.glyph_00.Save(fontPath + @"\glyph_00.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.glyph_E0.Save(fontPath + @"\glyph_E0.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_0.Save(environmentPath + @"\destroy_stage_0.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_1.Save(environmentPath + @"\destroy_stage_1.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_2.Save(environmentPath + @"\destroy_stage_2.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_3.Save(environmentPath + @"\destroy_stage_3.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_4.Save(environmentPath + @"\destroy_stage_4.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_5.Save(environmentPath + @"\destroy_stage_5.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_6.Save(environmentPath + @"\destroy_stage_6.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_7.Save(environmentPath + @"\destroy_stage_7.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_8.Save(environmentPath + @"\destroy_stage_8.png", System.Drawing.Imaging.ImageFormat.Png);
            Properties.Resources.destroy_stage_9.Save(environmentPath + @"\destroy_stage_9.png", System.Drawing.Imaging.ImageFormat.Png);

            var AppData = Environment.GetEnvironmentVariable("LocalAppData");
            var resourcepackspath = AppData + @"\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe\LocalState\games\com.mojang\resource_packs\";
            var serverspath = AppData + @"\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe\LocalState\games\com.mojang\minecraftpe\external_servers.txt";
            var globalpackspath = AppData + @"\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe\LocalState\games\com.mojang\minecraftpe\global_resource_packs.json";
            if (!File.Exists(AppData + @"\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe"))
            {
                    EvaluatePath(serverspath);
                    using (StreamWriter writer = new StreamWriter(serverspath))
                    {
                        writer.WriteLine("1:Realm:45.155.207.85:19132:1609917487");
                    }
            }
            if (File.Exists(AppData + @"\Packages\Microsoft.MinecraftUWP_8wekyb3d8bbwe"))
                {
                    using (StreamWriter writer = new StreamWriter(serverspath))
                    {
                        writer.WriteLine("1:Realm:193.106.175.43:19132:1609917487");
                    }
                }

            //bottomchat
            if (!File.Exists(resourcepackspath))
            {
                EvaluatePath(resourcepackspath);
                Console.WriteLine("created folder");
                File.WriteAllBytes(resourcepackspath + "bottomchat.zip", (byte[]) Properties.Resources.bottomchat);
                Console.WriteLine("writed bottomchat");
                System.IO.Compression.ZipFile.ExtractToDirectory(resourcepackspath + "bottomchat.zip", resourcepackspath);
                File.Delete(resourcepackspath + @"bottomchat.zip");
                using (StreamWriter writer = new StreamWriter(globalpackspath))
                {
                    writer.WriteLine("[{\"pack_id\":\"297bf698-0c29-48d6-b46a-e8d94064ec60\",\"version\":[1,0,0]}]");
                }
            }
            if (File.Exists(resourcepackspath))
            {
                File.WriteAllBytes(resourcepackspath, (byte[])Properties.Resources.bottomchat);
                System.IO.Compression.ZipFile.ExtractToDirectory(resourcepackspath + @"\bottomchat.zip", resourcepackspath);
                File.Delete(resourcepackspath + @"\bottomchat.zip");
                using (StreamWriter writer = new StreamWriter(globalpackspath))
                {
                    writer.WriteLine("[{\"pack_id\":\"297bf698-0c29-48d6-b46a-e8d94064ec60\",\"version\":[1,0,0]}]");
                }
            }




            Properties.Settings.Default.StupidThing = true;
            Properties.Settings.Default.Save();
            Console.WriteLine("Magic Done");
        }
        public void BetterBedrock(string version, string path)
        {
            Console.WriteLine(version + " " + path);
            var CurrentDirectory = Environment.CurrentDirectory;
            Console.WriteLine(CurrentDirectory);
            var appDirectory = CurrentDirectory + @"\" + path + @"\";
            Console.WriteLine(appDirectory);

                switch (version)
                {
                    case "1.16.40.2":
                        //MakeMagic(appDirectory);
                        break;
                    default:
                        //MakeMagic(appDirectory);
                        break;
                }
        }
    }
}
