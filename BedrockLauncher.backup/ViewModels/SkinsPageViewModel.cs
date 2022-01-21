using BedrockLauncher.Classes.SkinPack;
using System.Collections.ObjectModel;
using PostSharp.Patterns.Model;

namespace BedrockLauncher.ViewModels
{
    /// <summary>
    /// Interaction logic for SkinsPage.xaml
    /// </summary>
    /// 
    [NotifyPropertyChanged(ExcludeExplicitProperties = Constants.Debugging.ExcludeExplicitProperties)]
    public class SkinsPageViewModel
    {
        public MCSkinPack CurrentSkinPack { get; set; }
        public MCSkin CurrentSkin { get; set; }
        public string CurrentSkinName
        {
            get
            {
                Depends.On(CurrentSkinPack);
                Depends.On(CurrentSkin);
                return CurrentSkinPack != null && CurrentSkin != null ? CurrentSkinPack.GetLocalizedSkinName(CurrentSkin.localization_name) : "NULL";
            }
        }
        public string CurrentSkinPath
        {
            get
            {
                Depends.On(CurrentSkinPack);
                Depends.On(CurrentSkin);
                return CurrentSkin != null ? CurrentSkin.texture_path : string.Empty;
            }
        }
        public MCSkinGeometry CurrentSkinType
        {
            get
            {
                Depends.On(CurrentSkinPack);
                Depends.On(CurrentSkin);
                return CurrentSkin != null ? CurrentSkin.skin_type : MCSkinGeometry.Normal;
            }
        }

        public ObservableCollection<MCSkinPack> SkinPacks { get; set; } = new ObservableCollection<MCSkinPack>();
        public ObservableCollection<MCSkin> Skins
        {
            get
            {
                Depends.On(CurrentSkinPack);
                if (CurrentSkinPack != null) return CurrentSkinPack.Content.skins;
                else return new ObservableCollection<MCSkin>(); 
            }
        }
    }
}
