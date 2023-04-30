using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using BedrockLauncher.Classes.Launcher;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using JemExtensions;
using PostSharp.Patterns.Model;
using System.Windows;

namespace BedrockLauncher.Downloaders
{

    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]    //196 Lines
    public class ChangelogDownloader
    {


        public bool ShowBetas { get; set; } = true;
        public bool ShowReleases { get; set; } = true;
        public bool IsRefreshable { get; set; } = true;
        public ObservableCollection<PatchNotes_Game_Item> PatchNotes { get; set; } = new ObservableCollection<PatchNotes_Game_Item>();

        //TODO: Wait for Mojang to Properly Implement Betas into the JSON before reimplemting
        #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<bool> GetBedrockOfTheWeekStatus()
        #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return false;
        }
        private void ClearPatchList()
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                PatchNotes.Clear();
            });
        }
        private void AddPatch(PatchNotes_Game_Item patch)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                PatchNotes.Add(patch);
            });
        }
        public async Task UpdatePatchNotes()
        {
            ClearPatchList();

            PatchNotes_Game_Root result = null;
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var json = await httpClient.GetStringAsync(Constants.PATCHNOTES_MAIN_URL);
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<PatchNotes_Game_Root>(json);
                }
                catch
                {
                    result = new PatchNotes_Game_Root();
                }

            }
            if (result == null) result = new PatchNotes_Game_Root();
            if (result.entries == null) result.entries = new List<PatchNotes_Game_Item>();

            await Application.Current.Dispatcher.InvokeAsync(() => {
                foreach (PatchNotes_Game_Item item in result.entries)
                {
                    AddPatch(item);
                }
            });

        }
        public async void UpdateList()
        {
            if (!IsRefreshable) return;

            try
            {
                IsRefreshable = false;
                await Task.Run(UpdatePatchNotes);
                IsRefreshable = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
                IsRefreshable = true;
            }
        }
    }
}
