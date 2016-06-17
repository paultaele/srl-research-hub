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
using Windows.Storage.Streams;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Ink2Gif
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Initializers

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void InitializeInkCanvas()
        {
            StrokeVisuals = new InkDrawingAttributes()
            {
                Color = Colors.Black,
                IgnorePressure = true,
                PenTip = PenTipShape.Circle,
                Size = new Size(10, 10),
            };
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
        }

        #endregion

        #region Button Interaction

        private async void MySaveButton_Click(object sender, RoutedEventArgs e)
        {


            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add("Gif with embedded ISF", new List<string> { ".gif" });

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (null != file)
            {
                try
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await MyInkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                    }
                }
                catch (Exception exception)
                {
                    //GenerateErrorMessage();
                }
            }
        }

        private void MyLoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        // TODO
        private async void MyLoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // open the folder picker dialog window and select the folder with the images
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // get the folder selection result
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // application now has read/write access to all contents in the picked folder (including other sub-folder contents)
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                // get all the sketches
                List<StorageFile> files = new List<StorageFile>();
                List<StorageFile> allFiles = (await folder.GetFilesAsync()).ToList();
                foreach (StorageFile file in allFiles)
                {
                    if (Path.GetExtension(file.Name).EndsWith(".xml"))
                    {
                        files.Add(file);
                    }
                }

                //// load the first sketch
                //MyInkCanvas.InkPresenter.StrokeContainer.Clear();
                ////MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(mySketches[0]);
                //MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(await ReadXml(myFiles[0]));
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        private void MyDotButton_Click(object sender, RoutedEventArgs e)
        {

            List<InkStroke> dotStrokes = Dotify(MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList());
            MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(dotStrokes);
        }

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            // clear the canvas
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            // get the strokes
            IReadOnlyList<InkStroke> strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            // reset the time offset and finish
            if (strokes.Count == 0) { return; }

            // select the last stroke and delete it
            strokes[strokes.Count - 1].Selected = true;
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
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

        private List<InkStroke> Dotify(List<InkStroke> strokes)
        {
            List<InkPoint> points = new List<InkPoint>();
            foreach (InkStroke stroke in MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                foreach (InkPoint point in stroke.GetInkPoints())
                {
                    points.Add(point);
                }
            }

            List<InkStroke> dotStrokes = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();
            for (int i = 0; i < points.Count; ++i)
            {
                if (i % 5 != 0) { continue; }

                InkPoint point = points[i];
                List<Point> dotPoint = new List<Point> { new Point(point.Position.X, point.Position.Y) };
                InkStroke dotStroke = builder.CreateStroke(dotPoint);
                dotStroke.DrawingAttributes = DOT_VISUALS;
                dotStrokes.Add(dotStroke);
            }

            return dotStrokes;
        }

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }
        public object PixelFormats { get; private set; }

        #endregion

        #region Fields

        public InkDrawingAttributes PEN_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Black,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(10, 10),
        };

        public InkDrawingAttributes DOT_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Red,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(50, 50),
        };

        #endregion


    }
}