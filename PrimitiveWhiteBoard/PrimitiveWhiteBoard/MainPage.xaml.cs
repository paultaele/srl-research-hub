using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
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

namespace PrimitiveWhiteBoard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Constructor and Loader

        public MainPage()
        {
            //
            InitializeComponent();
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            //
            InitializeInkCanvas();

            //
            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            //
            myTimeCollection = new List<List<long>>();

            //
            DateTimeOffset = 0;
        }

        #endregion

        #region Stroke Handlers

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

        #region Button Handlers

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

        private async void MySaveButton_Click(object sender, RoutedEventArgs e)
        {
            //
            FileSavePicker picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeChoices.Add("XML File", new List<string>() { ".xml" });
            picker.SuggestedFileName = "Data";

            //
            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                Debug.WriteLine("MySaveButton_Click: File was written.");

                // ???
                WriteXml(file, "", MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes(), myTimeCollection);
            }
            else
            {
                Debug.WriteLine("MySaveButton_Click: Operation was cancelled.");
            }
        }

        private async void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            //
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".xml");

            //
            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Debug.WriteLine("File was picked.");

                //
                MyClearButton_Click(null, null);

                //
                ReadXml(file);

                // 
                int numStrokes = myTimeCollection.Count;
                if (numStrokes > 0)
                {
                    List<long> lastTimes = myTimeCollection[myTimeCollection.Count - 1];
                    DateTimeOffset = lastTimes[lastTimes.Count - 1];
                }
                else
                {
                    DateTimeOffset = 0;
                }
            }
            else
            {
                Debug.WriteLine("Operation was cancelled.");
            }
        }

        #endregion

        #region Helper Methods

        private void InitializeInkCanvas()
        {
            // enable pen, mouse, and touch input
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;

            // define ink canvas' attributes
            StrokeVisuals = new InkDrawingAttributes();
            StrokeVisuals.Color = Colors.Black;
            StrokeVisuals.IgnorePressure = true;
            StrokeVisuals.PenTip = PenTipShape.Circle;
            StrokeVisuals.Size = new Size(5, 5);
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
        }

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            if (hasStarted && hasEnded) { throw new Exception("Cannot start and end stroke at the same time."); }

            if (hasStarted) { myTimes = new List<long>(); }

            long time = DateTime.Now.Ticks - DateTimeOffset;
            myTimes.Add(time);

            if (hasEnded) { myTimeCollection.Add(myTimes); }
        }

        private async void WriteXml(StorageFile file, string label, IReadOnlyList<InkStroke> strokeCollection, List<List<long>> timeCollection)
        {
            // create the string writer as the streaming source of the XML data
            string output = "";
            using (StringWriter stringWriter = new StringWriter())
            {
                // set the string writer as the streaming source for the XML writer
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                {
                    // <xml>
                    xmlWriter.WriteStartDocument();

                    // <sketch>
                    xmlWriter.WriteStartElement("sketch");
                    xmlWriter.WriteAttributeString("label", label);

                    // set up the required variables
                    List<InkStroke> strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList();
                    List<InkPoint> points;
                    List<long> times;
                    InkPoint point;
                    long time;

                    // iterate through each stroke
                    for (int i = 0; i < strokes.Count; ++i)
                    {
                        // <stroke>
                        xmlWriter.WriteStartElement("stroke");

                        // get the current stroke's points and times
                        points = strokes[i].GetInkPoints().ToList();
                        times = myTimeCollection[i];

                        //
                        while (points.Count != times.Count)
                        {
                            if (points.Count > times.Count) { points.RemoveAt(points.Count - 1); }
                            else if (times.Count > points.Count) { times.RemoveAt(times.Count - 1); }
                        }

                        //
                        for (int j = 0; j < points.Count; ++j)
                        {
                            point = points[j];
                            time = times[j];  // TODO: FIX!

                            // <point>
                            xmlWriter.WriteStartElement("point");

                            xmlWriter.WriteAttributeString("x", "" + point.Position.X);
                            xmlWriter.WriteAttributeString("y", "" + point.Position.Y);
                            xmlWriter.WriteAttributeString("time", "" + times[j]);

                            // </point>
                            xmlWriter.WriteEndElement();
                        }


                        // </stroke>
                        xmlWriter.WriteEndElement();
                    }

                    // </sketch>
                    xmlWriter.WriteEndElement();

                    // </xml>
                    xmlWriter.WriteEndDocument();
                }

                output = stringWriter.ToString();
            }

            await FileIO.WriteTextAsync(file, output);
        }

        private async void ReadXml(StorageFile file)
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
                myTimeCollection.Add(times);
                stroke = builder.CreateStroke(points);
                stroke.DrawingAttributes = StrokeVisuals;
                MyInkCanvas.InkPresenter.StrokeContainer.AddStroke(stroke);
            }
        }

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }
        private long DateTimeOffset { get; set; }

        #endregion

        #region Fields

        private List<long> myTimes;
        private List<List<long>> myTimeCollection;

        #endregion
    }
}
