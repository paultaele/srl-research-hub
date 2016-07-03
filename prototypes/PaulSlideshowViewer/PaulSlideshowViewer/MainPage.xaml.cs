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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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

        }

        #endregion

        #region Button Interactions

        private void MyPrevButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyNextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder folder = InteractiveTools.GetFolder();
            if (folder == null) { return; }

            List<StorageFile> files = new List<StorageFile>();
            Task task = Task.Run(async () => files = (await folder.GetFilesAsync()).ToList() );

            List<StorageFile> imageFiles = new List<StorageFile>();
            foreach (StorageFile file in files)
            {
                string name = file.Name;
                if (name.EndsWith(".png") || name.EndsWith(".jpg") || name.EndsWith(".gif"))
                {
                    imageFiles.Add(file);
                }
            }

            foreach (StorageFile imageFile in imageFiles)
            {
                Debug.WriteLine(imageFile.Name);
            }
        }

        #endregion
    }
}
