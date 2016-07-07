using Srl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PaulAnimationViewer
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

            // initialize the stroke event handlers
            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted; ;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued; ;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded; ;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            //
            double width = MyBorder.ActualWidth;
            double height = MyBorder.ActualHeight;
            MyBorderLength = width < height ? width : height;
            MyBorder.Width = MyBorder.Height = MyBorderLength;

            // initialize the external timing data structure and offset
            myTimeCollection = new List<List<long>>();
            MyDateTimeOffset = 0;

            //
            MyInkStrokes = MyInkCanvas.InkPresenter.StrokeContainer;
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);

            //
            LoadContents(IMAGES_PATH, out myImageFiles, ".png");
            LoadContents(TEMPLATES_PATH, out myTemplateFiles, ".xml");

            //
            foreach (StorageFile file in myImageFiles) { MySymbolsComboBox.Items.Add(Path.GetFileNameWithoutExtension(file.Path)); }
            MySymbolsComboBox.SelectedIndex = 0;

            //
            myTemplates = new List<Sketch>();
            foreach (StorageFile file in myTemplateFiles)
            {
                Sketch template = null;
                Task task = Task.Run(async () => template = await SketchTools.XmlToSketch(file));
                task.Wait();

                template = SketchTransformation.ScaleFrame(template, MyBorderLength);
                template = SketchTransformation.TranslateFrame(template, new Point(MyBorderLength / 2 - MyBorder.BorderThickness.Left, MyBorderLength / 2 - MyBorder.BorderThickness.Top));
                myTemplates.Add(template);
                foreach (InkStroke stroke in template.Strokes)
                {
                    stroke.DrawingAttributes = StrokeVisuals;
                }
            }

            //
            MyIsReady = true;
        }

        private void LoadContents(string path, out List<StorageFile> targetFiles, string extension)
        {
            //
            Task task;

            //
            StorageFolder folder = null;
            task = Task.Run(async () => folder = await Package.Current.InstalledLocation.GetFolderAsync(path));
            task.Wait();

            //
            IReadOnlyList<StorageFile> files = null;
            task = Task.Run(async () => files = await folder.GetFilesAsync());
            task.Wait();

            //
            targetFiles = new List<StorageFile>();
            foreach (StorageFile file in files)
            {
                string name = file.Name;
                if (name.EndsWith(extension))
                {
                    targetFiles.Add(file);
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

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            // case: stroke has started and ended
            // impossible so throw exception
            if (hasStarted && hasEnded)
            {
                throw new Exception("Cannot start and end stroke at the same time.");
            }

            // case: stroke has started
            // initialize the stroke
            if (hasStarted)
            {
                myTimes = new List<long>();
            }

            // calibrate recorded time
            long time = DateTime.Now.Ticks - MyDateTimeOffset;
            myTimes.Add(time);

            // case: stroke has ended
            // complete the stroke
            if (hasEnded)
            {
                myTimeCollection.Add(myTimes);
            }
        }


        #endregion

        #region Button Interactions

        private void MyImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (!MyImageButton.IsChecked.Value)
            {
                MyImage.Source = null;
            }

            else
            {
                InteractionTools.SetImage(MyImage, myImageFiles[MyImageIndex]);
                MySymbolsComboBox.SelectedIndex = MyImageIndex;
            }
        }

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            // clear the canvas
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();

            // clear the stroke and timing data
            myTimeCollection = new List<List<long>>();
            MyDateTimeOffset = 0;

            // clear the tracers
            MyCanvas.Children.Clear();
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            // get the strokes
            IReadOnlyList<InkStroke> strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            // reset the time offset and finish
            if (strokes.Count == 0)
            {
                MyDateTimeOffset = 0;
                return;
            }

            // select the last stroke and delete it
            strokes[strokes.Count - 1].Selected = true;
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();

            // remove the last time
            myTimeCollection.RemoveAt(myTimeCollection.Count - 1);

            // clear the canvas
            MyCanvas.Children.Clear();
        }

        private void MyPlayButton_Click(object sender, RoutedEventArgs e)
        {
            List<InkStroke> strokes = new List<InkStroke>();
            foreach (InkStroke stroke in MyInkStrokes.GetStrokes()) { strokes.Add(stroke); }

            if (strokes.Count > 0)
            {
                Sketch sketch = new Sketch("", strokes, myTimeCollection, 0, 0, MyBorderLength, MyBorderLength);
                Sketch input = SketchTools.Clone(sketch);
                List<Storyboard> inputStoryboards = Helper.Playback(MyCanvas, sketch.Strokes, sketch.Times, Colors.Red);
                foreach (Storyboard storyboard in inputStoryboards)
                {
                    storyboard.Begin();
                }
            }

            if (MyImageButton.IsChecked.Value)
            {
                Sketch template = SketchTools.Clone(myTemplates[MyImageIndex]);
                List<Storyboard> modelStoryboards = Helper.Trace(MyCanvas, template.Strokes, template.Times, Colors.Green, 30000);
                foreach (Storyboard storyboard in modelStoryboards)
                {
                    storyboard.Begin();
                }
            }
        }

        #endregion

        #region Combo Box Interactions

        private void MySymbolsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //
            if (!MyIsReady) { return; }

            //
            MyImage.Source = null;
            if (MyImageButton.IsChecked.Value)
            {
                MyImageIndex = MySymbolsComboBox.SelectedIndex;
                InteractionTools.SetImage(MyImage, myImageFiles[MyImageIndex]);
            }

            // TODO: delete later
            MyInkStrokes.Clear();
        }

        #endregion

        #region Properties

        private bool MyIsReady { get; set; }
        private int MyImageIndex { get; set; }
        public double MyBorderLength { get; private set; }
        private InkStrokeContainer MyInkStrokes { get; set; }
        private long MyDateTimeOffset { get; set; }

        #endregion

        #region Fields

        private List<long> myTimes;
        private List<List<long>> myTimeCollection;

        private List<StorageFile> myImageFiles;
        private List<StorageFile> myTemplateFiles;
        private List<Sketch> myTemplates;

        public InkDrawingAttributes StrokeVisuals = new InkDrawingAttributes() { Color = Colors.Red, IgnorePressure = true, PenTip = PenTipShape.Circle, Size = new Size(10, 10) };

        public readonly string IMAGES_PATH = @"Assets\Images";
        public readonly string TEMPLATES_PATH = @"Assets\Templates";

        #endregion
    }
}