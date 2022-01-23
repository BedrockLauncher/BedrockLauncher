using BedrockLauncher.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using System.Net.Cache;

namespace BedrockLauncher.Controls.Items.Launcher
{
    /// <summary>
    /// Interaction logic for LocalizationItem.xaml
    /// </summary>
    public partial class LocalizationItem : UserControl
    {
        public LocalizationItem()
        {
            InitializeComponent();
        }

        public Visibility ButtonPanelVisibility
        {
            get
            {
                return ButtonPanel != null ? ButtonPanel.Visibility : Visibility.Collapsed;
            }
            set
            {
                if (ButtonPanel != null) ButtonPanel.Visibility = value;
            }
        }

        public static readonly DependencyProperty ButtonPanelVisibilityProperty = DependencyProperty.Register("ButtonPanelVisibility", typeof(Visibility), typeof(LocalizationItem), new PropertyMetadata(Visibility.Collapsed, new PropertyChangedCallback(ChangePanelVisibility)));

        private static void ChangePanelVisibility(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LocalizationItem).ButtonPanelVisibility = (Visibility)e.NewValue;
        }

        private Pages.Preview.EditSkinPackScreen GetParent()
        {
            return this.Tag as Pages.Preview.EditSkinPackScreen;
        }

        private void DeleteLangButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var lang_name = button.DataContext as string;
            string filePath = GetParent().ValidateLangFile(lang_name, false);
            try
            {
                if (File.Exists(filePath)) File.Delete(filePath);
                GetParent().CurrentSkinPack.Texts.RemoveLang(lang_name);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }
            GetParent().UpdateLocalizationList();
        }
        private void EditLangButton_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = sender as MenuItem;
            var lang_name = button.DataContext as string;
            string filePath = GetParent().ValidateLangFile(lang_name);
            Process.Start("notepad.exe", filePath);
        }
        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {

        }
        private void More_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            var installation = button.DataContext as string;
            GetParent().LocalizationList.SelectedItem = installation;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.DataContext = installation;
            button.ContextMenu.IsOpen = true;
        }
    }
}
