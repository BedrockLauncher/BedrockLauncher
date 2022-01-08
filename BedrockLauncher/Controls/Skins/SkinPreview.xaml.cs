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
using BedrockLauncher.Core.Classes.SkinPack;

namespace BedrockLauncher.Controls.Skins
{

    /// <summary>
    /// Interaction logic for SkinPreview.xaml
    /// </summary>
    public partial class SkinPreview : UserControl
    {
        ChromiumWebBrowser Renderer = new ChromiumWebBrowser();

        #region Constants

        public const string Preview = "skinview3d://SkinView/previews/index.html";

        private const string NoSkin = "img/NoSkin.png";

        #endregion

        #region Accesors

        public static readonly DependencyProperty PathProperty =
            DependencyProperty.RegisterAttached(nameof(Path), typeof(string), typeof(SkinPreview), new FrameworkPropertyMetadata(Preview));

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

        private string ModelType
        {
            get
            {
                string type;
                switch (_Type)
                {
                    case MCSkinGeometry.Normal:
                        type = "default";
                        break;
                    case MCSkinGeometry.Slim:
                        type = "slim";
                        break;
                    default:
                        type = "default";
                        break;
                }
                return type;
            }
        }

        private MCSkinGeometry _Type = MCSkinGeometry.Normal;

        private string _Path = Preview;

        private bool isRenderable
        {
            get
            {
                return (Type == MCSkinGeometry.Custom ? false : true);
            }
        }

        #endregion

        private void Init()
        {
            Renderer.Focusable = false;
            Renderer.LoadingStateChanged += Renderer_LoadingStateChanged;
            this.AddChild(Renderer);
            InitializeComponent();
            InitializeChromium();
        }

        public SkinPreview()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            Init();
            this.Renderer.Address = Preview;
        }
        public SkinPreview(MCSkin Skin)
        {
            Init();
            Path = Skin.texture_path;
            Type = Skin.skin_type;
            this.Renderer.Address = Preview;
        }
        private void InitializeChromium()
        {
            Renderer.FrameLoadEnd += OnBrowserFrameLoadEnd;
        }
        public void UpdateSkin()
        {
            Path = NoSkin;
            Type = MCSkinGeometry.Custom;
        }
        public void UpdateSkin(MCSkin Skin)
        {
            Path = Skin.texture_path;
            Type = Skin.skin_type;
        }

        private async void RefreshView(bool localFile = true)
        {
            try
            {
                if (!Renderer.CanExecuteJavascriptInMainFrame)
                {
                    return;
                }
                else if (!localFile || Path == NoSkin)
                {
                    var result = await Renderer.EvaluateScriptAsync("setSkin", new object[] { Path, ModelType });
                }
                else
                {
                    var uri = new System.Uri(Path);
                    var converted = uri.AbsoluteUri;
                    var fix = converted.Replace("'", "%27").Replace("file://", "localfiles://");
                    var result = await Renderer.EvaluateScriptAsync("setSkin", new object[] { fix, ModelType });
                }


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void OnBrowserFrameLoadEnd(object sender, FrameLoadEndEventArgs args)
        {
            if (args.Frame.IsMain && Renderer.CanExecuteJavascriptInMainFrame)
            {
                args
                    .Browser
                    .MainFrame
                    .ExecuteJavaScriptAsync(
                    "document.body.style.overflow = 'hidden'");
            }
        }

        private void Renderer_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading == false && isRenderable && Renderer.CanExecuteJavascriptInMainFrame) RefreshView();
        }
    }
}
