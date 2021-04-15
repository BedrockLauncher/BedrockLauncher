using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using BedrockLauncher.Classes;

namespace BedrockLauncher.Controls
{

    /// <summary>
    /// Interaction logic for SkinPreview.xaml
    /// </summary>
    public partial class SkinPreview : UserControl
    {

        #region Constants

        public const string LegacyPreview = "resources://Controls/SkinPreview/Html/ItemPreview_Legacy.html";
        public const string LegacyPreview_Slim = "resources://Controls/SkinPreview/Html/ItemPreview_SlimLegacy.html";
        public const string NormalPreview = "resources://Controls/SkinPreview/Html/ItemPreview_Normal.html";
        public const string NormalPreview_Slim = "resources://Controls/SkinPreview/Html/ItemPreview_Slim.html";

        public const string NoSkin = "resources://Controls/SkinPreview/Html/NoSkin.png";

        #endregion

        #region Accesors

        public static readonly DependencyProperty PathProperty = 
            DependencyProperty.RegisterAttached(nameof(Path), typeof(string), typeof(SkinPreview), new FrameworkPropertyMetadata(NoSkin));

        public static readonly DependencyProperty TypeProperty = 
            DependencyProperty.RegisterAttached(nameof(Type), typeof(MCSkinGeometry), typeof(SkinPreview), new FrameworkPropertyMetadata(MCSkinGeometry.Normal));

        
        public string Path
        {
            get 
            { 
                return _Path; 
            }
            set 
            { 
                SetValue(PathProperty, value);
                _Path = value;
                RefreshView(); 
            }
        }
        public MCSkinGeometry Type
        {
            get 
            { 
                return _Type; 
            }
            set 
            { 
                SetValue(TypeProperty, value);
                _Type = value;
                RefreshView(); 
            }
        }

        private MCSkinGeometry _Type = MCSkinGeometry.Normal;

        private string _Path = NoSkin;

        private bool isRenderable
        {
            get
            {
                return (Type == Classes.MCSkinGeometry.Custom ? false : true);
            }
        }

        #endregion

        private void Init()
        {
            InitializeComponent();
            InitializeChromium();
        }

        public SkinPreview()
        {
            Init();
            RefreshView();
        }
        public SkinPreview(Classes.MCSkin Skin)
        {
            Init();
            Path = Skin.texture_path;
            Type = Skin.skin_type;
            RefreshView();
        }
        private void InitializeChromium()
        {
            Renderer.BrowserSettings = new BrowserSettings
            {
                FileAccessFromFileUrls = CefState.Enabled,
                UniversalAccessFromFileUrls = CefState.Enabled
            };
        }
        public void UpdateSkin()
        {
            Path = "NoSkin";
            Type = MCSkinGeometry.Custom;
            RefreshView();
        }
        public void UpdateSkin(Classes.MCSkin Skin)
        {
            Path = Skin.texture_path;
            Type = Skin.skin_type;
            RefreshView();
        }
        private void RefreshView()
        {
            bool SlimArms = (Type == Classes.MCSkinGeometry.Slim ? true : false);
            bool ModernSkin = DetectSkinType(Path);

            string ViewerMode = string.Empty;
            if (ModernSkin) ViewerMode = (SlimArms ? NormalPreview_Slim : NormalPreview);
            else ViewerMode = (SlimArms ? LegacyPreview_Slim : LegacyPreview);

            this.Renderer.Address = ViewerMode;

            bool DetectSkinType(string filePath)
            {
                if (File.Exists(filePath))
                {
                    var image = new Bitmap(filePath);
                    if (image != null) return (image.Width == image.Height ? true : false);
                }

                return false;
            }
        }
        private async void Renderer_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading == false && isRenderable)
            {
                try
                {
                    var uri = new System.Uri(Path);
                    var converted = uri.AbsoluteUri;
                    var fix = converted.Replace("'", "%27").Replace("file://", "localfiles://");
                    var result = await Renderer.EvaluateScriptAsync("SetSkin", new object[] { fix });
                }
                catch (Exception ex)
                {

                }

            }
        }
    }
}
