using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using BedrockLauncher.Dungeons.Classes.Launcher;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using JemExtensions;
using PostSharp.Patterns.Model;
using System.Windows;

namespace BedrockLauncher.Dungeons.Downloaders
{

    [NotifyPropertyChanged]
    public class ChangelogDownloader
    {
        private const string PatchNotesJSON = @"https://launchercontent.mojang.com/dungeonsPatchNotes.json";
        public bool IsRefreshable { get; set; } = true;
        public ObservableCollection<PatchNote> PatchNotes { get; set; } = new ObservableCollection<PatchNote>();


        public async Task GetPatchNotes()
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                PatchNotes.Clear();

                PatchNotesRoot result = null;
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        var json = await httpClient.GetStringAsync(PatchNotesJSON);
                        result = Newtonsoft.Json.JsonConvert.DeserializeObject<PatchNotesRoot>(json);
                    }
                    catch
                    {
                        result = new PatchNotesRoot();
                    }

                }
                if (result == null) result = new PatchNotesRoot();
                if (result.entries == null) result.entries = new List<PatchNote>();

                foreach (var entry in result.entries) PatchNotes.Add(entry);
            });

        }
        public async void UpdateList()
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                if (!IsRefreshable) return;

                try
                {
                    IsRefreshable = false;
                    await Task.Run(GetPatchNotes);
                    IsRefreshable = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex);
                    IsRefreshable = true;
                }
            });

        }

        public class PatchNotesRoot
        {
            public int version { get; set; }
            public List<PatchNote> entries { get; set; }
        }
    }
}
