using Srl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PaulAnimationViewer
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
            BorderLength = width < height ? width : height;
            MyBorder.Width = MyBorder.Height = BorderLength;

            //
            LoadImages();
        }

        private async void LoadImages()
        {
            //
            StorageFolder imagesFolder = await Package.Current.InstalledLocation.GetFolderAsync(IMAGES_PATH);

            //
            myImageFiles = new List<StorageFile>();
            foreach (StorageFile file in await imagesFolder.GetFilesAsync())
            {
                string name = file.Name;
                if (name.EndsWith(".png") || name.EndsWith(".jpg") || name.EndsWith(".gif"))
                {
                    myImageFiles.Add(file);

                    MySymbolsComboBox.Items.Add(Path.GetFileNameWithoutExtension(file.Path));
                }
            }

            //
            InteractionTools.SetImage(MyImage, myImageFiles[0]);
            MySymbolsComboBox.SelectedIndex = 0;
        }

        #endregion

        #region Button Interactions

        private void MySymbolsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Combo Box Interactions

        private void MySymbolsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageIndex = MySymbolsComboBox.SelectedIndex;
            InteractionTools.SetImage(MyImage, myImageFiles[ImageIndex]);
        }

        #endregion

        #region Properties

        private int ImageIndex { get; set; }
        public double BorderLength { get; private set; }

        #endregion

        #region Fields

        private List<StorageFile> myImageFiles;

        public readonly string IMAGES_PATH = @"Assets\Images";

        #endregion
    }
}
