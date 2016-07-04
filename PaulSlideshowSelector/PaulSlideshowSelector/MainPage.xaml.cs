using Srl;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PaulSlideshowSelector
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

        //private void MyBackButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!HasImages) { return; }

        //    InteractionTools.SetImage(MyImage, myImageFiles[--ImageIndex]);
        //    MyNextButton.IsEnabled = true;
        //    if (ImageIndex <= 0) { MyBackButton.IsEnabled = false; }
        //}

        //private void MyNextButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!HasImages) { return; }

        //    InteractionTools.SetImage(MyImage, myImageFiles[++ImageIndex]);
        //    MyBackButton.IsEnabled = true;
        //    if (ImageIndex >= myImageFiles.Count - 1) { MyNextButton.IsEnabled = false; }
        //}

        private async void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            //
            List<StorageFile> files = await InteractionTools.GetFiles();
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
            InteractionTools.SetImage(MyImage, myImageFiles[ImageIndex]);
            HasImages = true;
            //MyBackButton.IsEnabled = false;
            //if (myImageFiles.Count > 1) { MyNextButton.IsEnabled = true; }
            //else { MyNextButton.IsEnabled = false; }
        }


        private void MySymbolsButton_Click(object sender, RoutedEventArgs e)
        {

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
