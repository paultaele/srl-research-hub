using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SketchDataViewer
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
            InitializeInkCanvas();

            //
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            MyInkCanvas.InkPresenter.IsInputEnabled = false;
        }

        private void InitializeInkCanvas()
        {
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;

            // define ink canvas' attributes
            StrokeVisuals = new InkDrawingAttributes();
            StrokeVisuals.Color = Colors.Black;
            StrokeVisuals.IgnorePressure = true;
            StrokeVisuals.PenTip = PenTipShape.Circle;
            StrokeVisuals.Size = new Size(15, 15);
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
        }

        #endregion

        #region Button Interactions

        private async void MyPreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // load the previous sketch
            --Indexer;
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(await ReadXml(myFiles[Indexer]));

            //
            MyPreviousButton.IsEnabled = Indexer - 1 >= 0 ? true : false; ;
            MyNextButton.IsEnabled = true;
        }

        private async void MyNextButton_Click(object sender, RoutedEventArgs e)
        {
            // load the next sketch
            ++Indexer;
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(await ReadXml(myFiles[Indexer]));

            //
            MyPreviousButton.IsEnabled = true;
            MyNextButton.IsEnabled = Indexer + 1 < myFiles.Count ? true : false;
        }

        private async void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            // open the folder picker dialog window and select the folder with the images
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // get the folder selection result
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Debug.WriteLine("Picked folder: " + folder.Name);

                // application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                // get all the sketches
                myFiles = new List<StorageFile>();
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                foreach (StorageFile file in files)
                {
                    if (Path.GetExtension(file.Name).EndsWith(".xml"))
                    {
                        myFiles.Add(file);
                    }
                }

                // load the first sketch
                MyInkCanvas.InkPresenter.StrokeContainer.Clear();
                //MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(mySketches[0]);
                MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(await ReadXml(myFiles[0]));

                Indexer = 0;
                MyPreviousButton.IsEnabled = false;
                MyNextButton.IsEnabled = Indexer + 1 < myFiles.Count ? true : false;
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        #endregion

        #region Helper Methods

        private async Task<List<InkStroke>> ReadXml(StorageFile file)
        {
            // create a new XML document
            // get the text from the XML file
            // load the file's text into an XML document 
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            // 
            string label = document.Root.Attribute("label").Value;

            // itereate through each stroke element
            InkStrokeBuilder builder = new InkStrokeBuilder();
            InkStroke stroke;
            List<InkStroke> strokes = new List<InkStroke>();
            foreach (XElement element in document.Root.Elements())
            {
                // initialize the point and time lists
                List<Point> points = new List<Point>();
                List<long> times = new List<long>();

                // iterate through each point element
                double x, y;
                Point point;
                long time;
                foreach (XElement pointElement in element.Elements())
                {
                    x = Double.Parse(pointElement.Attribute("x").Value);
                    y = Double.Parse(pointElement.Attribute("y").Value);
                    point = new Point(x, y);
                    time = Int64.Parse(pointElement.Attribute("time").Value);

                    points.Add(point);
                    times.Add(time);
                }

                //
                stroke = builder.CreateStroke(points);
                stroke.DrawingAttributes = StrokeVisuals;

                //
                strokes.Add(stroke);
            }

            return strokes;
        }

        private List<InkStroke> CloneStrokes(List<InkStroke> originals)
        {
            List<InkStroke> strokes = new List<InkStroke>();

            InkStroke stroke;
            InkStrokeBuilder builder = new InkStrokeBuilder();
            Point point;
            List<Point> points;
            foreach (InkStroke original in originals)
            {
                points = new List<Point>();
                foreach (InkPoint inkPoint in original.GetInkPoints())
                {
                    point = new Point(inkPoint.Position.X, inkPoint.Position.Y);
                    points.Add(point);
                }

                stroke = builder.CreateStroke(points);
                stroke.DrawingAttributes = StrokeVisuals;
                strokes.Add(original);
            }

            return strokes;
        }

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }
        private int Indexer { get; set; }

        #endregion

        #region Fields

        private List<StorageFile> myFiles;

        #endregion
    }
}
