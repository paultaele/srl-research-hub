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
            InitializeComponent();
            InitializeInkCanvas();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void InitializeInkCanvas()
        {
            StrokeVisuals = PEN_VISUALS;
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

        private async void MyLoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            //
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".xml");
            StorageFile dataFile = await picker.PickSingleFileAsync();
            if (dataFile == null) { return; }

            //
            List<InkStroke> strokes = await ReadMotionXml(dataFile);

            //
            Sketch motion = new Sketch(strokes, null);
            motion = SketchTransformation.ScaleProportional(motion, MyBorder.ActualHeight * 0.75);
            motion = SketchTransformation.TranslateMedian(motion, new Point(MyBorder.ActualWidth / 2, MyBorder.ActualHeight / 2));
            strokes = motion.Strokes;
            strokes[0].DrawingAttributes = LEFT_HAND_VISUALS;
            strokes[1].DrawingAttributes = RIGHT_HAND_VISUALS;

            //
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(Gridify());
            MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(strokes);
            MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(Dotify(strokes));
        }

        private async void MyLoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // open the folder picker dialog window and select the folder with the images
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // get the folder selection result
            StorageFolder loadFolder = await folderPicker.PickSingleFolderAsync();
            if (loadFolder == null)
            {
                Debug.WriteLine("Operation cancelled.");
                return;
            }

            // application now has read/write access to all contents in the picked folder (including other sub-folder contents)
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", loadFolder);

            // get all the sketches
            List<StorageFile> dataFiles = new List<StorageFile>();
            List<StorageFile> loadFiles = (await loadFolder.GetFilesAsync()).ToList();
            foreach (StorageFile file in loadFiles)
            {
                if (Path.GetExtension(file.Name).EndsWith(".xml"))
                {
                    dataFiles.Add(file);
                }
            }

            //
            foreach (StorageFile dataFile in dataFiles)
            {
                //
                List<InkStroke> strokes = await ReadMotionXml(dataFile);

                //
                Sketch motion = new Sketch(strokes, null);
                motion = SketchTransformation.ScaleProportional(motion, MyBorder.ActualHeight * 0.75);
                motion = SketchTransformation.TranslateMedian(motion, new Point(MyBorder.ActualWidth / 2, MyBorder.ActualHeight / 2));
                strokes = motion.Strokes;
                strokes[0].DrawingAttributes = LEFT_HAND_VISUALS;
                strokes[1].DrawingAttributes = RIGHT_HAND_VISUALS;

                //
                MyInkCanvas.InkPresenter.StrokeContainer.Clear();
                MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(Gridify());
                MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(strokes);
                MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(Dotify(strokes));

                //
                StorageFile saveFile = await loadFolder.CreateFileAsync(dataFile.Name + ".gif", CreationCollisionOption.ReplaceExisting);
                try
                {
                    using (IRandomAccessStream stream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await MyInkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                    }
                }
                catch (Exception exception)
                {
                    ;
                }
            }
        }

        private async void MyLoadFolderButton2_Click(object sender, RoutedEventArgs e)
        {
            // open the folder picker dialog window and select the folder with the images
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // get the folder selection result
            StorageFolder loadFolder = await folderPicker.PickSingleFolderAsync();
            if (loadFolder == null)
            {
                Debug.WriteLine("Operation cancelled.");
                return;
            }

            // application now has read/write access to all contents in the picked folder (including other sub-folder contents)
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", loadFolder);

            // get all the sketches
            List<StorageFile> dataFiles = new List<StorageFile>();
            List<StorageFile> loadFiles = (await loadFolder.GetFilesAsync()).ToList();
            foreach (StorageFile file in loadFiles)
            {
                if (Path.GetExtension(file.Name).EndsWith(".xml"))
                {
                    dataFiles.Add(file);
                }
            }

            //
            foreach (StorageFile dataFile in dataFiles)
            {
                //
                List<InkStroke> strokes = await ReadXml(dataFile);

                //
                MyInkCanvas.InkPresenter.StrokeContainer.Clear();
                MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(Gridify());
                MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(strokes);
                MyInkCanvas.InkPresenter.StrokeContainer.AddStrokes(Dotify(strokes));

                //
                StorageFile saveFile = await loadFolder.CreateFileAsync(dataFile.Name + ".gif", CreationCollisionOption.ReplaceExisting);
                try
                {
                    using (IRandomAccessStream stream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await MyInkCanvas.InkPresenter.StrokeContainer.SaveAsync(stream);
                    }
                }
                catch (Exception exception)
                {
                    ;
                }
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

        private async Task<List<InkStroke>> ReadMotionXml(StorageFile file)
        {
            // create a new XML document
            // get the text from the XML file
            // load the file's text into an XML document 
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            // itereate through each stroke element
            InkStrokeBuilder builder = new InkStrokeBuilder();
            List<InkStroke> strokes = new List<InkStroke>();
            List<Point> leftPoints = new List<Point>();
            List<Point> rightPoints = new List<Point>();

            List<XElement> rootElements = document.Root.Elements().ToList();
            XElement framesElement = rootElements[1];
            foreach (XElement frameElement in framesElement.Elements())
            {
                XElement jointsElement = frameElement.Elements().ToList()[0];

                foreach (XElement jointElement in jointsElement.Elements())
                {
                    XElement jointTypeElement = jointElement.Elements().ToList()[0];

                    if (jointTypeElement.Value.Equals("HandLeft"))
                    {
                        XElement positionElement = jointElement.Elements().ToList()[1];
                        List<XElement> xyzElements = positionElement.Elements().ToList();
                        double x = Double.Parse(xyzElements[0].Value);
                        double y = Double.Parse(xyzElements[1].Value);

                        leftPoints.Add(new Point(x, y));
                    }

                    if (jointTypeElement.Value.Equals("HandRight"))
                    {
                        XElement positionElement = jointElement.Elements().ToList()[1];
                        List<XElement> xyzElements = positionElement.Elements().ToList();
                        double x = Double.Parse(xyzElements[0].Value);
                        double y = Double.Parse(xyzElements[1].Value);

                        rightPoints.Add(new Point(x, y));
                    }
                }
            }

            InkStroke leftStroke = builder.CreateStroke(leftPoints);
            InkStroke rightStroke = builder.CreateStroke(rightPoints);
            strokes.Add(leftStroke);
            strokes.Add(rightStroke);

            return strokes;
        }

        private List<InkStroke> Dotify(List<InkStroke> strokes)
        {
            List<InkPoint> points = new List<InkPoint>();
            foreach (InkStroke stroke in strokes)
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
                if (i % 20 != 0) { continue; }

                InkPoint point = points[i];
                List<Point> dotPoint = new List<Point> { new Point(point.Position.X, point.Position.Y) };
                InkStroke dotStroke = builder.CreateStroke(dotPoint);
                dotStroke.DrawingAttributes = DOT_VISUALS;
                dotStrokes.Add(dotStroke);
            }

            return dotStrokes;
        }

        private List<InkStroke> Gridify()
        {
            List<InkStroke> gridlines = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();

            double maxX = MyBorder.ActualWidth;
            double maxY = MyBorder.ActualHeight;

            for (int x = 0; x < maxX; x += 50)
            {
                Point left = new Point(x, 0);
                Point right = new Point(x, maxY);
                InkStroke line = builder.CreateStroke(new List<Point> { left, right });
                line.DrawingAttributes = LINE_VISUALS;
                gridlines.Add(line);
            }

            for (int y = 0; y < maxY; y += 50)
            {
                Point top = new Point(0, y);
                Point bottom = new Point(maxX, y);
                InkStroke line = builder.CreateStroke(new List<Point> { top, bottom});
                line.DrawingAttributes = LINE_VISUALS;
                gridlines.Add(line);
            }

            return gridlines;
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
            Color = Colors.Green,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(30, 30),
        };

        public InkDrawingAttributes LINE_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Gray,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(1, 1),
        };

        public InkDrawingAttributes LEFT_HAND_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Red,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(10, 10),
        };

        public InkDrawingAttributes RIGHT_HAND_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Blue,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(10, 10),
        };

        #endregion
    }
}