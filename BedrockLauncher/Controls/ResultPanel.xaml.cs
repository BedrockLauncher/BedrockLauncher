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

namespace BedrockLauncher.Controls.Various
{
    public enum ResultPanelType
    {
        NoContent,
        NoNews,
        Error,
        Loading
    }

    public partial class ResultPanel : Grid
    {
        private ResultPanelType _PanelType = ResultPanelType.NoContent;

        public ResultPanelType PanelType
        {
            get
            {
                return _PanelType;
            }
            set
            {
                _PanelType = value;
               if (IsLoaded) UpdatePanel(value);
            }
        }

        public ResultPanel()
        {
            InitializeComponent();
        }

        private void UpdatePanel(ResultPanelType result)
        {
            switch (result)
            {
                case ResultPanelType.NoContent:
                    Text1.SetResourceReference(TextBlock.TextProperty, "Dialog_NoContentFound_NoMatchingResultsTitle");
                    Text2.SetResourceReference(TextBlock.TextProperty, "Dialog_NoContentFound_NoMatchingResultsText");
                    break;
                case ResultPanelType.NoNews:
                    Text1.SetResourceReference(TextBlock.TextProperty, "Dialog_NoNewsContentFound_NoMatchingResultsTitle");
                    Text2.SetResourceReference(TextBlock.TextProperty, "Dialog_NoNewsContentFound_NoMatchingResultsText");
                    break;
                case ResultPanelType.Loading:
                    Text1.SetResourceReference(TextBlock.TextProperty, "Dialog_ResultPanel_Loading_Title");
                    Text2.SetResourceReference(TextBlock.TextProperty, "Dialog_ResultPanel_Loading_Text");
                    break;
                case ResultPanelType.Error:
                    Text1.SetResourceReference(TextBlock.TextProperty, "Dialog_ResultPanel_Error_Title");
                    Text2.SetResourceReference(TextBlock.TextProperty, "Dialog_ResultPanel_Error_Text");
                    break;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePanel(_PanelType);
        }
    }
}
