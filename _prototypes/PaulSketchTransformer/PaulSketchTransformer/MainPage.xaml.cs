using Srl;
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
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PaulSketchTransformer
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
        }

        private void MyName_Loaded(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Buttons

        private async void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder folder = await Picker();
            if (folder == null) { return; }

            myLoadFolder = folder;
            MyLoadText.Text = myLoadFolder.Path;

            var files = await myLoadFolder.GetFilesAsync();
            myLoadFiles = new List<StorageFile>();
            foreach (StorageFile file in files)
            {
                if (file.Name.EndsWith(".xml"))
                {
                    myLoadFiles.Add(file);
                }
            }

            if (MySaveText.Text.Equals("")) { MyTransformButton.IsEnabled = true; }
        }

        private async void MySaveButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder folder = await Picker();
            if (folder == null) { return; }

            mySaveFolder = folder;
            MySaveText.Text = mySaveFolder.Path;

            if (MyLoadText.Text.Equals("")) { MyTransformButton.IsEnabled = true; }
        }

        private async Task<StorageFolder> Picker()
        {
            FolderPicker picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            picker.ViewMode = PickerViewMode.List;
            StorageFolder folder = await picker.PickSingleFolderAsync();

            return folder;
        }

        private async void MyTransformButton_Click(object sender, RoutedEventArgs e)
        {
            int resample = int.Parse(MyResampleText.Text);
            double scale = double.Parse(MyScaleText.Text);
            double x = double.Parse(MyTranslateX.Text);
            double y = double.Parse(MyTranslateY.Text);
            Point point = new Point(x, y);

            foreach (StorageFile loadFile in myLoadFiles)
            {
                string loadFileName = loadFile.Name;
                Sketch sketch = await SketchTools.XmlToSketch(loadFile);

                sketch = SketchTransformation.Resample(sketch, resample);
                sketch = SketchTransformation.ScaleFrame(sketch, scale);
                sketch = SketchTransformation.TranslateFrame(sketch, point);

                StorageFile saveFile = await mySaveFolder.CreateFileAsync(loadFileName, CreationCollisionOption.ReplaceExisting);
                SketchTools.SketchToXml(saveFile, loadFileName, sketch.Strokes, sketch.Times, sketch.FrameMinX, sketch.FrameMinY, sketch.FrameMaxX, sketch.FrameMaxY);
            }
        }

        #endregion

        #region

        private StorageFolder myLoadFolder;
        private List<StorageFile> myLoadFiles;

        private StorageFolder mySaveFolder;

        #endregion
    }
}
