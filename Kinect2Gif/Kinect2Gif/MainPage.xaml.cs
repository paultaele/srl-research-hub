﻿using System;
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

namespace Kinect2Gif
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
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
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

        private async void MyLoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFolder();
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private async void LoadFolder()
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
                motion = SketchTransformation.ScaleProportional(motion, MyInkCanvas.ActualHeight * 0.75);
                motion = SketchTransformation.TranslateMedian(motion, new Point(MyInkCanvas.ActualWidth / 2, MyInkCanvas.ActualHeight / 2));
                strokes = motion.Strokes;
                strokes[0].DrawingAttributes = LEFT_HAND_VISUALS;
                strokes[1].DrawingAttributes = RIGHT_HAND_VISUALS;
                strokes[2].DrawingAttributes = HEAD_VISUALS;
                strokes[3].DrawingAttributes = SPINE_MID_VISUALS;

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

        #endregion

        #region Helper Methods

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
            List<Point> leftHandPoints = new List<Point>();
            List<Point> rightHandPoints = new List<Point>();
            List<Point> headPoints = new List<Point>();
            List<Point> spineMidPoints = new List<Point>();

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

                        leftHandPoints.Add(new Point(x, y));
                    }

                    else if (jointTypeElement.Value.Equals("HandRight"))
                    {
                        XElement positionElement = jointElement.Elements().ToList()[1];
                        List<XElement> xyzElements = positionElement.Elements().ToList();
                        double x = Double.Parse(xyzElements[0].Value);
                        double y = Double.Parse(xyzElements[1].Value);

                        rightHandPoints.Add(new Point(x, y));
                    }

                    else if (jointTypeElement.Value.Equals("Head"))
                    {
                        XElement positionElement = jointElement.Elements().ToList()[1];
                        List<XElement> xyzElements = positionElement.Elements().ToList();
                        double x = Double.Parse(xyzElements[0].Value);
                        double y = Double.Parse(xyzElements[1].Value);

                        headPoints.Add(new Point(x, y));
                    }

                    else if (jointTypeElement.Value.Equals("SpineMid"))
                    {
                        XElement positionElement = jointElement.Elements().ToList()[1];
                        List<XElement> xyzElements = positionElement.Elements().ToList();
                        double x = Double.Parse(xyzElements[0].Value);
                        double y = Double.Parse(xyzElements[1].Value);

                        spineMidPoints.Add(new Point(x, y));
                    }
                }
            }

            InkStroke leftHandStroke = builder.CreateStroke(leftHandPoints);
            InkStroke righHandStroke = builder.CreateStroke(rightHandPoints);
            InkStroke headStroke = builder.CreateStroke(headPoints);
            InkStroke spineMidStroke = builder.CreateStroke(spineMidPoints);
            strokes.Add(leftHandStroke);
            strokes.Add(righHandStroke);
            strokes.Add(headStroke);
            strokes.Add(spineMidStroke);

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
                InkPoint point = points[i];
                List<Point> dotPoint = new List<Point> { new Point(point.Position.X, point.Position.Y) };
                InkStroke dotStroke = builder.CreateStroke(dotPoint);
                if (i % 20 != 0) { dotStroke.DrawingAttributes = SMALL_DOT_VISUALS; }
                dotStrokes.Add(dotStroke);
            }
            for (int i = 0; i < points.Count; ++i)
            {
                InkPoint point = points[i];
                List<Point> dotPoint = new List<Point> { new Point(point.Position.X, point.Position.Y) };
                InkStroke dotStroke = builder.CreateStroke(dotPoint);
                if (i % 20 == 0) { dotStroke.DrawingAttributes = LARGE_DOT_VISUALS; }
                dotStrokes.Add(dotStroke);
            }

            return dotStrokes;
        }

        private List<InkStroke> Gridify()
        {
            List<InkStroke> gridlines = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();

            double maxX = MyInkCanvas.ActualWidth;
            double maxY = MyInkCanvas.ActualHeight;

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
                InkStroke line = builder.CreateStroke(new List<Point> { top, bottom });
                line.DrawingAttributes = LINE_VISUALS;
                gridlines.Add(line);
            }

            return gridlines;
        }

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }

        #endregion

        #region Fields

        public InkDrawingAttributes PEN_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Black,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(10, 10),
        };

        public InkDrawingAttributes SMALL_DOT_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Black,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(15, 15),
        };

        public InkDrawingAttributes LARGE_DOT_VISUALS = new InkDrawingAttributes()
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

        public InkDrawingAttributes HEAD_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Purple,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(50, 50),
        };

        public InkDrawingAttributes SPINE_MID_VISUALS = new InkDrawingAttributes()
        {
            Color = Colors.Orange,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(50, 50),
        };

        #endregion
    }
}
