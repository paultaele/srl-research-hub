using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TraceDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Constructor and Loader

        public MainPage()
        {
            InitializeComponent();
            InitializeInkCanvas();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        private async void InitializeImages()
        {
            // 1. Get the directory of images directly.
            string folderPath = @"Assets/Images/";
            DirectoryInfo directory = new DirectoryInfo(folderPath);

            // 2. Collect all the file names in the directory.
            List<string> fileNames = new List<string>();
            foreach (var fileInfo in directory.GetFiles())
            {
                fileNames.Add(fileInfo.Name);
            }

            // 3. Iterate through each file name.
            StorageFile file;
            IRandomAccessStream fileStream;
            BitmapImage bitmap;
            Button symbolButton;
            Uri location;
            foreach (var fileName in fileNames)
            {
                // create the image
                location = new Uri(this.BaseUri, folderPath + fileName);
                //file = await StorageFile.GetFileFromApplicationUriAsync(location);
                //fileStream = await file.OpenAsync(FileAccessMode.Read);
                //bitmap = new BitmapImage();
                //bitmap.SetSource(fileStream);
                //MyImage.Source = bitmap;

                //// set the image
                //Image image = new Image();
                //image.Source = bitmap;

                // create the button
                symbolButton = new Button()
                {
                    // set the background color
                    Background = new SolidColorBrush(Colors.Transparent),

                    // set the image content
                    Content = new Image
                    {
                        Source = new BitmapImage(location),
                        VerticalAlignment = VerticalAlignment.Center,
                    }
                };
                symbolButton.Click += SymbolButton_Click;

                //
                MyImagesPanel.Children.Add(symbolButton);
            }
        }

        private void SymbolButton_Click(object sender, RoutedEventArgs e)
        {
            //
            MyClearButton_Click(null, null);

            //
            Image image = (Image)(sender as Button).Content;

            //
            MyImage.Source = image.Source;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeImages();
        }

        #endregion

        #region Button Interactions

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            //
            IReadOnlyList<InkStroke> strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            //
            if (strokes.Count == 0) { return; }

            //
            strokes[strokes.Count - 1].Selected = true;
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        #endregion

        #region Helper Methods

        private void InitializeInkCanvas()
        {
            // enable pen, mouse, and touch input
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;

            // define ink canvas' attributes
            StrokeVisuals = new InkDrawingAttributes();
            StrokeVisuals.Color = Colors.Red;
            StrokeVisuals.IgnorePressure = true;
            StrokeVisuals.PenTip = PenTipShape.Circle;
            StrokeVisuals.Size = new Size(10, 10);
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
        }

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }

        #endregion
    }
}
