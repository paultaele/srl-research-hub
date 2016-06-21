using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
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

namespace SketchDataCollection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Initializers

        public MainPage()
        {
            //
            InitializeComponent();
            InitializeSettings();
            InitializeInkCanvas();

            //
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            //
            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;
        }

        private async void InitializeSettings()
        {
            SettingsDialog settingsDialog = new SettingsDialog();

            ContentDialogResult result = await settingsDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                myLoadFolder = settingsDialog.LoadFolder;
                mySaveFolder = settingsDialog.SaveFolder;

                int iterationsCount = settingsDialog.IterationsCount;
                bool isSquareArea = settingsDialog.IsSquareArea;
                bool canDisplayTraceImage = settingsDialog.CanDisplayTraceImage;
                bool canDisplayPreviewImage = settingsDialog.CanDisplayPreviewImage;
                bool canDisplayRandomImages = settingsDialog.CanDisplayRandomImages;

                if (isSquareArea)
                {
                    double width = MyInkCanvasBorder.ActualWidth;
                    double height = MyInkCanvasBorder.ActualHeight;
                    double length = width < height ? width : height;

                    MyInkCanvasBorder.Width = length;
                    MyInkCanvasBorder.Height = length;
                    MyPromptsBorder.Width = length;
                    MyInteractionsBorder.Width = length;
                }

                //
                myImageFiles = new List<StorageFile>();

                // extract image files
                var readonlyFiles = await myLoadFolder.GetFilesAsync();
                foreach (var readonlyFile in readonlyFiles)
                {
                    myImageFiles.Add(readonlyFile);
                }

                // duplicate image files
                if (iterationsCount > 1)
                {
                    List<StorageFile> files = new List<StorageFile>();

                    foreach (StorageFile file in myImageFiles)
                    {
                        int count = iterationsCount;
                        while (count > 0)
                        {
                            files.Add(file);
                            --count;
                        }
                    }

                    myImageFiles = files;
                }

                // randomize image files
                if (canDisplayRandomImages)
                {
                    Random random = new Random();
                    var files = from item in myImageFiles
                               orderby random.Next()
                               select item;

                    myImageFiles = files.ToList();
                }

                // initialize the indexer
                Indexer = 0;

                // display current image
                StorageFile testFile = myImageFiles[Indexer];
                BitmapImage bitmap = new BitmapImage();
                FileRandomAccessStream stream = (FileRandomAccessStream)await testFile.OpenAsync(FileAccessMode.Read);
                bitmap.SetSource(stream);
                MyImage.Source = bitmap;

                // display current prompt
                string label = Path.GetFileNameWithoutExtension(myImageFiles[Indexer].Path);
                MyPromptText.Text = "Please draw the following: " + label;

                // increment the indexer to the next image
                ++Indexer;
            }
        }

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

        #region Button Interactions

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            //
            IReadOnlyList<InkStroke> strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            //
            if (strokes.Count == 0)
            {
                DateTimeOffset = 0;
                return;
            }

            //
            strokes[strokes.Count - 1].Selected = true;
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();

            //
            myTimeCollection.RemoveAt(myTimeCollection.Count - 1);
        }

        private async void MySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // save the sketch
            string currentLabel = Path.GetFileNameWithoutExtension(myImageFiles[Indexer-1].Path);
            string fileName = currentLabel + "_" + DateTime.Now.Ticks + ".xml";
            StorageFile currentFile = await mySaveFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting); ;
            List<InkStroke> currentStrokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList();
            List<List<long>> currentTimeCollection = myTimeCollection;
            double frameMinX = 0;
            double frameMinY = 0;
            double frameMaxX = MyInkCanvas.ActualWidth;
            double frameMaxY = MyInkCanvas.ActualHeight;

            //WriteXml(currentFile, currentLabel, MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes(), myTimeCollection);
            SketchTools.SketchToXml(currentFile, currentLabel, currentStrokes, currentTimeCollection, frameMinX, frameMinY, frameMaxX, frameMaxY);

            // clear and reset the sketch
            MyClearButton_Click(null, null);

            // stop the data collection if no more images left
            if (Indexer < myImageFiles.Count)
            {
                // display the next prompt
                string nextLabel = Path.GetFileNameWithoutExtension(myImageFiles[Indexer].Path);
                MyPromptText.Text = "Please draw the following: " + nextLabel;

                // display the next image
                StorageFile testFile = myImageFiles[Indexer];
                BitmapImage bitmap = new BitmapImage();
                FileRandomAccessStream stream = (FileRandomAccessStream)await testFile.OpenAsync(FileAccessMode.Read);
                bitmap.SetSource(stream);
                MyImage.Source = bitmap;

                // increment the indexer to the next image
                ++Indexer;
            }
            else
            {
                MySubmitButton.IsEnabled = false;

                MessageDialog dialog = new MessageDialog("You have completed the data collection.");
                dialog.Commands.Add(new UICommand("Okay") { Id = 0 });
                dialog.DefaultCommandIndex = 0;

                var result = await dialog.ShowAsync();
                if ((int)result.Id == 0)
                {
                    Application.Current.Exit();
                }
            }
        }

        #endregion

        #region Stroke Interactions

        private void StrokeInput_StrokeStarted(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(true, false);
        }

        private void StrokeInput_StrokeContinued(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(false, false);
        }

        private void StrokeInput_StrokeEnded(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(false, true);
        }

        #endregion

        #region Helper Methods

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            if (hasStarted && hasEnded) { throw new Exception("Cannot start and end stroke at the same time."); }

            if (hasStarted) { myTimes = new List<long>(); }

            long time = DateTime.Now.Ticks - DateTimeOffset;
            myTimes.Add(time);

            if (hasEnded) { myTimeCollection.Add(myTimes); }
        }

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }
        private int Indexer { get; set; }
        private long DateTimeOffset { get; set; }

        #endregion

        #region Fields

        private List<StorageFile> myImageFiles;
        private List<long> myTimes;
        private List<List<long>> myTimeCollection;
        private StorageFolder myLoadFolder;
        private StorageFolder mySaveFolder;

        #endregion
    }
}
