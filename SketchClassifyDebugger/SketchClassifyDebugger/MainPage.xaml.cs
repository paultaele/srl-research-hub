using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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

namespace SketchClassifyDebugger
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

            // launch app in full screen
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            // set ink cankas' drawing attributes
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(PEN_DRAWING_ATTRIBUTES);

            // enable pen, mouse, and touch input
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;

            // create stroke input handlers
            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            // set the transformation attributes
            N = 128;
            Size = 500;
            K = new Point(0, 0);
            myClassifier = new PDollar(N, Size, K);

            //
            List<Sketch> originalTemplates = null;
            Task task = Task.Run(async () => originalTemplates = await ReadXml(@"\Assets\Templates\"));
            task.Wait();
            myClassifier.Train(originalTemplates);

            // set time variables
            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;
        }

        private async Task<List<Sketch>> ReadXml(string relativePath)
        {
            // initialize the list of sketches and path
            string root = Package.Current.InstalledLocation.Path;
            string path = root + relativePath;

            // get the list of files
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(path);
            List<Sketch> sketches = new List<Sketch>();
            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                // case: file is an XML file
                if (file.Name.EndsWith(".xml"))
                {
                    Sketch sketch = await SketchTools.XmlToSketch(file, PEN_DRAWING_ATTRIBUTES);
                    sketches.Add(sketch);
                }
            }

            return sketches;
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

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            if (hasStarted && hasEnded) { throw new Exception("Cannot start and end stroke at the same time."); }

            if (hasStarted) { myTimes = new List<long>(); }

            long time = DateTime.Now.Ticks - DateTimeOffset;
            myTimes.Add(time);

            if (hasEnded) { myTimeCollection.Add(myTimes); }
        }

        #endregion

        #region Button Interactions

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            MyOutputText.Text = "";

            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            MyOutputText.Text = "";

            // get strokes
            List<InkStroke> strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList();
            if (strokes.Count == 0) { DateTimeOffset = 0; return; }

            // remove last stroke andtime from respective lists
            strokes[strokes.Count - 1].Selected = true;
            myTimeCollection.RemoveAt(myTimeCollection.Count - 1);

            // remove last stroke from ink canvas
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        private void MyClassifyButton_Click(object sender, RoutedEventArgs e)
        {
            //
            Sketch input = new Sketch("Unknown", MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList(), myTimeCollection);

            //
            myClassifier.Run(input);

            //
            List<string> labels = myClassifier.Labels;
            List<double> scores = myClassifier.Scores;

            //
            string output = "";
            for (int i = 0; i < labels.Count; ++i)
            {
                output += $"{i+1}. {labels[i]}: {scores[i]}\n";
            }
            MyOutputText.Text = output;
        }

        #endregion

        #region Helper Methods

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }
        private long DateTimeOffset { get; set; }

        private int N { get; set; }
        private double Size { get; set; }
        private Point K { get; set; }

        #endregion

        #region Fields

        private List<long> myTimes;
        private List<List<long>> myTimeCollection;

        private PDollar myClassifier;

        public InkDrawingAttributes PEN_DRAWING_ATTRIBUTES = new InkDrawingAttributes() { Color = Colors.Black, IgnorePressure = true, PenTip = PenTipShape.Circle, Size = new Size(10, 10), };

        #endregion
    }
}
