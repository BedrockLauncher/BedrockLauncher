using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace ServerTab
{
    public class ServerList
    {
        public Dictionary<string, List<Server>> Servers { get; set; }
    }
    public class Server
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public DateTime TimeToExpired { get; set; }
        public DateTime AddedTime { get; set; }
    }
    public class ServersTab
    {
        public ServersTab()
        {
            Console.WriteLine("Loading server stuff");
            //createServer();
            //readServers();
            //Console.WriteLine("Server stuff loaded");
        }
        private void createServer()
        {
            string json;
            ServerList serverList = new ServerList();
            serverList.Servers = new Dictionary<string, List<Server>>();
            List<Server> server = new List<Server>();
            server.Add(new Server
            {
                Name = "Lootmc",
                Link = "https://github.com/XlynxX/BedrockLauncherPaidServers/blob/main/lootmc.json",
                AddedTime = DateTime.Now,
                TimeToExpired = DateTime.Now.AddDays(30)
            });
            serverList.Servers.Add("Lootmc", server);

            json = JsonConvert.SerializeObject(serverList, Formatting.Indented);
            Console.WriteLine(json);
            File.WriteAllText("servers_paid.json", json);

            Console.WriteLine("test server created");
        }

        public void readServers()
        {
            string json = File.ReadAllText("servers_paid.json");
            ServerList Servers = JsonConvert.DeserializeObject<ServerList>(json);
            Console.WriteLine("Server count: " + Servers.Servers.Count);
            foreach (List<Server> serverList in Servers.Servers.Values)
            {
                foreach (Server server in serverList)
                {
                    Console.WriteLine("\nServer found!:");
                    Console.WriteLine("Name: " + server.Name);
                    Console.WriteLine("Link: " + server.Link);
                    Console.WriteLine("Time to expired: " + server.TimeToExpired);
                    Console.WriteLine("Time when added: " + server.AddedTime);
                    Console.WriteLine("Expire in: " + server.AddedTime.CompareTo(server.TimeToExpired) + " days");

                    string data = getServerData(server.Link) ?? "no data";
                    Console.WriteLine("Server data: " + data);
                }
            }
        }

        public ServerList getServerList()
        {
            string json = File.ReadAllText("servers_paid.json");
            ServerList Servers = JsonConvert.DeserializeObject<ServerList>(json);
            return Servers;
        }

        private string getServerData(string url)
        {
            string urlAddress = url
                .Replace("https://github.com/", "https://raw.githubusercontent.com/")
                .Replace("blob/", "");
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream receiveStream = response.GetResponseStream();
                    StreamReader readStream = null;

                    if (String.IsNullOrWhiteSpace(response.CharacterSet))
                        readStream = new StreamReader(receiveStream);
                    else
                        readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                    string
                        data = readStream.ReadToEnd();

                    response.Close();
                    readStream.Close();
                    return data;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }
    }
}
