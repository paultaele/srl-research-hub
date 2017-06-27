using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DataCollectionSetup
{
    public sealed partial class PromptElement : UserControl
    {
        public PromptElement(int position, int count)
        {
            this.InitializeComponent();

            //
            this.MyPositionText.Text = "" + position;

            //
            this.MyTraceButton.GroupName = DISPLAY_GROUP_NAME + "_" + count;
            this.MyReferenceButton.GroupName = DISPLAY_GROUP_NAME + "_" + count;
            this.MyMemoryButton.GroupName = DISPLAY_GROUP_NAME + "_" + count;
        }

        public bool IsChecked { get { return MyRemoveCheckBox.IsChecked.Value; } }
        public string LoadName {  get { return MyLoadText.Text; } }
        public string LabelName { get { return MyLabelText.Text; } }
        public string CountName { get { return MyCountText.Text; } }
        public string PositionName { get { return MyPositionText.Text; } set { MyPositionText.Text = value; } }

        public static readonly String DISPLAY_GROUP_NAME = "DisplayGroup";
    }
}
