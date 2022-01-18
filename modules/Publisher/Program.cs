using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Deploying to Zip File...");

            var path_root = new System.IO.DirectoryInfo(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).FullName;

            string inbuildPath = args[0];
            string outbuildPath = System.IO.Path.Combine(path_root, "publish_process");

            if (Directory.Exists(outbuildPath)) Directory.Delete(outbuildPath, true);
            Directory.CreateDirectory(outbuildPath);

            foreach (var file in new System.IO.DirectoryInfo(inbuildPath).GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                if (file.Name != "log.txt") file.CopyTo(Path.Combine(outbuildPath, file.Name));
            }

            foreach (var folder in new System.IO.DirectoryInfo(inbuildPath).GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                if (folder.Name != "data" && folder.Name != "cache")
                {
                    Extensions.DirectoryExtensions.Copy(folder.FullName, Path.Combine(outbuildPath, folder.Name));
                }
            }


            string inPath = outbuildPath;
            string outPath = System.IO.Path.Combine(path_root, "publish_result");
            string outFile = System.IO.Path.Combine(path_root, "publish_result", "build.zip");

            if (Directory.Exists(outPath)) Directory.Delete(outPath, true);
            Directory.CreateDirectory(outPath);

            ZipFile.CreateFromDirectory(inPath, outFile);

            Console.WriteLine($"Deployed to Zip File at \"{outFile}\"");

            Process.Start("explorer.exe", outPath);
        }
    }
}
