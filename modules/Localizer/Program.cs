using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.IO;
using System.Windows;
using System.Collections;
using System.Xml;
using System.Windows.Markup;

namespace Localizer
{
    class Program
    {
        static void Main(string[] args)
        {
            InitDirectories();

            bool canExit = false;

            Console.WriteLine("Input an Option:");
            Console.WriteLine("xaml | Convert Resx to XAML");
            Console.WriteLine("resx | Convert XAML to Resx");
            Console.WriteLine("exit | Exit");

            while (!canExit)
            {
                string result = Console.ReadLine();
                switch (result)
                {
                    case "xaml":
                        XamlCommand();
                        break;
                    case "resx":
                        ResxCommand();
                        break;
                    case "exit":
                        canExit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid Input");
                        break;
                }
            }
        }

        private static void ResxCommand()
        {
            foreach (var file in new DirectoryInfo(System.IO.Path.Combine(GetAssemblyDirectory(), "xaml")).GetFiles("*.xaml", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string locale = file.Name.Replace(file.Extension, "");
                    ConvertToResx(locale, ReadXAML(locale));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
        }

        private static void XamlCommand()
        {
            foreach (var file in new DirectoryInfo(System.IO.Path.Combine(GetAssemblyDirectory(), "resx")).GetFiles("*.resx", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string locale = file.Name.Replace(file.Extension, "");
                    ConvertToXAML(locale, ReadResx(locale));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

        }


        private static void InitDirectories()
        {
            Directory.CreateDirectory(System.IO.Path.Combine(GetAssemblyDirectory(), "xaml"));
            Directory.CreateDirectory(System.IO.Path.Combine(GetAssemblyDirectory(), "resx"));
        }


        public static string GetAssemblyDirectory()
        {
            string Assembly = string.Empty;

            int i = 0;

            while (String.IsNullOrEmpty(Assembly) && i < 10)
            {
                var ass = System.Reflection.Assembly.GetEntryAssembly();
                Assembly = ass.Location;
                i++;
            }

            return System.IO.Path.GetDirectoryName(Assembly);
        }
        private static Dictionary<object, object> ReadResx(string Locale)
        {
            string Directory = System.IO.Path.Combine(GetAssemblyDirectory(), "resx");
            string InputFileName = $"{Directory}\\{Locale}.resx";

            Dictionary<object, object> resx = new Dictionary<object, object>();

            using (ResXResourceReader reader = new ResXResourceReader(InputFileName))
            {
                var dict = reader.GetEnumerator();
                while (dict.MoveNext()) resx.Add(dict.Key, dict.Value);
            }

            return resx;
        }
        private static ResourceDictionary ReadXAML(string Locale)
        {
            string Directory = System.IO.Path.Combine(GetAssemblyDirectory(), "xaml");
            string InputFileName = $"{Directory}\\{Locale}.xaml";

            ResourceDictionary dict = new ResourceDictionary();
            dict.Source = new Uri(InputFileName, UriKind.Absolute);
            return dict;
        }
        private static void ConvertToXAML(string Locale, Dictionary<object, object> resx)
        {
            string ExportPath = System.IO.Path.Combine(GetAssemblyDirectory(), "xaml");
            string ExportFileName = $"{ExportPath}\\{Locale}.xaml";
            File.Create(ExportFileName).Close();

            ResourceDictionary dict = new ResourceDictionary();
            foreach (var item in resx)
            {
                dict.Add(item.Key, item.Value);
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(ExportFileName, settings);
            XamlWriter.Save(dict, writer);
        }
        private static void ConvertToResx(string Locale, ResourceDictionary dict)
        {
            string ExportPath = System.IO.Path.Combine(GetAssemblyDirectory(), "resx");
            string ExportFileName = $"{ExportPath}\\{Locale}.resx";
            File.Create(ExportFileName).Close();

            using (ResXResourceWriter resx = new ResXResourceWriter(ExportFileName))
            {
                foreach (var entry in dict.Keys)
                {
                    resx.AddResource(entry.ToString(), dict[entry]);
                }
            }
        }
    }
}
