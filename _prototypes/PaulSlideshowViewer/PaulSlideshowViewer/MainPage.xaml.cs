using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PaulSlideshowViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Initializers

        public MainPage()
        {
            InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            //
            double width = MyBorder.ActualWidth;
            double height = MyBorder.ActualHeight;
            double length = width < height ? width : height;
            MyBorder.Width = length;
            MyBorder.Height = width;
        }

        #endregion

        #region Button Interactions

        private void MyBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImages) { return; }

            InteractiveTools.SetImage(MyImage, myImageFiles[--ImageIndex]);
            MyNextButton.IsEnabled = true;
            if (ImageIndex <= 0) { MyBackButton.IsEnabled = false; }
        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (!HasImages) { return; }

            InteractiveTools.SetImage(MyImage, myImageFiles[++ImageIndex]);
            MyBackButton.IsEnabled = true;
            if (ImageIndex >= myImageFiles.Count - 1) { MyNextButton.IsEnabled = false; }
        }

        private async void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            //
            List<StorageFile> files = await InteractiveTools.GetFiles();
            if (files == null || files.Count == 0) { return; }

            //
            myImageFiles = new List<StorageFile>();
            foreach (StorageFile file in files)
            {
                string name = file.Name;
                if (name.EndsWith(".png") || name.EndsWith(".jpg") || name.EndsWith(".gif"))
                {
                    myImageFiles.Add(file);
                }
            }

            //
            ImageIndex = 0;
            InteractiveTools.SetImage(MyImage, myImageFiles[ImageIndex]);
            HasImages = true;
            MyBackButton.IsEnabled = false;
            if (myImageFiles.Count > 1) { MyNextButton.IsEnabled = true; }
            else { MyNextButton.IsEnabled = false; }
        }

        #endregion

        #region Properties

        private int ImageIndex { get; set; }
        private bool HasImages { get; set; }

        #endregion

        #region Fields

        private List<StorageFile> myImageFiles;

        #endregion
    }
}
