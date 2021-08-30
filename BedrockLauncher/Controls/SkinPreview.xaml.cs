//using CefSharp;
//using CefSharp.Wpf;
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
using Microsoft.Web.WebView2.Core;
using System.Net;
using Newtonsoft.Json;

namespace BedrockLauncher.Controls
{

    /// <summary>
    /// Interaction logic for SkinPreview.xaml
    /// </summary>
    public partial class SkinPreview : UserControl
    {

        #region Constants

        private static string AppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static Uri Preview = new Uri($"file://{AppPath}/runtimes/SkinView/previews/index.html");
                                                    
        private static Uri NoSkin = new Uri($"file://{AppPath}/runtimes/SkinView/previews/img/NoSkin.png");

        #endregion

        #region Accesors

        public static readonly DependencyProperty PathProperty =
            DependencyProperty.RegisterAttached(nameof(Path), typeof(Uri), typeof(SkinPreview), new FrameworkPropertyMetadata(NoSkin));

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.RegisterAttached(nameof(Type), typeof(MCSkinGeometry), typeof(SkinPreview), new FrameworkPropertyMetadata(MCSkinGeometry.Normal));


        public Uri Path
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

        private Uri _Path = NoSkin;

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
            InitializeComponent();
            InitializeChromium();
        }

        public SkinPreview()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            Init();
        }
        public SkinPreview(MCSkin Skin)
        {
            Path = new Uri(Skin.texture_path);
            Type = Skin.skin_type;
            Init();
        }
        private async void InitializeChromium()
        {
            var env = await Internals.GetCoreWebView2Environment();
            await Renderer.EnsureCoreWebView2Async(env);
            Renderer.CoreWebView2.Settings.IsScriptEnabled = true;
            Renderer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Renderer.CoreWebView2.Navigate(Preview.AbsoluteUri);
            await Renderer.ExecuteScriptAsync("document.body.style.overflow = 'hidden'");
        }
        public void UpdateSkin()
        {
            Path = NoSkin;
            Type = MCSkinGeometry.Custom;
        }
        public void UpdateSkin(MCSkin Skin)
        {
            Path = new Uri(Skin.texture_path);
            Type = Skin.skin_type;
        }
        
        private async void RefreshView(bool localFile = true)
        {
            
            try
            {
                if (Renderer.CoreWebView2 == null) return;
                var converted = Path.AbsoluteUri;
                var fix = converted.Replace("'", "%27");
                string command = ExecuteScriptFunction("setSkin", new string[] { fix, ModelType });
                var result = await Renderer.CoreWebView2.ExecuteScriptAsync(command);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            

            string ExecuteScriptFunction(string functionName, string[] parameters)
            {
                string script = functionName + "(";
                for (int i = 0; i < parameters.Length; i++)
                {
                    script += JsonConvert.SerializeObject(parameters[i]);
                    if (i < parameters.Length - 1)
                    {
                        script += ", ";
                    }
                }
                script += ");";
                return script;
            }
        }

        private void Renderer_LoadingStateChanged(object sender, Microsoft.Web.WebView2.Core.CoreWebView2ContentLoadingEventArgs e)
        {
            RefreshView();
        }
    }
}
