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

namespace PaulTechniqueViewer
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
            // squarify the ink canvas' drawing area
            double width = MyBorder.ActualWidth;
            double height = MyBorder.ActualHeight;
            BorderLength = width < height ? width : height;
            MyBorder.Width = MyBorder.Height = BorderLength;

            // initialize the external timing data structure and offset
            myTimeCollection = new List<List<long>>();
            MyDateTimeOffset = 0;

            // enable writing and set the stroke visuals of the ink canvas
            MyInkStrokes = MyInkCanvas.InkPresenter.StrokeContainer;
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);

            // load the images and templates
            LoadContents(IMAGES_PATH, out myImageFiles, ".png");
            LoadContents(TEMPLATES_PATH, out myTemplateFiles, ".xml");

            // populate the symbols combo box
            foreach (StorageFile file in myImageFiles) { MySymbolsComboBox.Items.Add(Path.GetFileNameWithoutExtension(file.Path)); }
            MySymbolsComboBox.SelectedIndex = 0;

            // set the prompt text
            string promptedSymbol = MySymbolsComboBox.SelectedValue.ToString().ToUpper();
            MyPrompText.Text = PROMPT_TEXT + promptedSymbol;

            // modify the templates to fit the current ink canvas' drawing area
            myTemplates = new List<Sketch>();
            foreach (StorageFile file in myTemplateFiles)
            {
                Sketch template = null;
                Task task = Task.Run(async () => template = await SketchTools.XmlToSketch(file));
                task.Wait();

                template = SketchTransformation.ScaleFrame(template, BorderLength);
                template = SketchTransformation.TranslateFrame(template, new Point(BorderLength / 2 - MyBorder.BorderThickness.Left, BorderLength / 2 - MyBorder.BorderThickness.Top));
                myTemplates.Add(template);
                foreach (InkStroke stroke in template.Strokes)
                {
                    stroke.DrawingAttributes = StrokeVisuals;
                }
            }

            // set the flag to allow buttons to run code
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
                InteractionTools.SetImage(MyImage, myImageFiles[MyCurrentIndex]);
                MySymbolsComboBox.SelectedIndex = MyCurrentIndex;
            }
        }

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void Clear()
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

        private async void MyPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // get the input and model sketch, and set the duration
            List<InkStroke> strokes = new List<InkStroke>();
            foreach (InkStroke stroke in MyInkStrokes.GetStrokes()) { strokes.Add(stroke); }
            Sketch input = new Sketch("", strokes, myTimeCollection, 0, 0, BorderLength, BorderLength);
            Sketch model = myTemplates[MyCurrentIndex];
            int duration = 30000;

            // animate the expert's model strokes
            if (MyImageButton.IsChecked.Value)
            {
                Sketch sketch = SketchTools.Clone(model);
                double opacity = strokes.Count > 0 ? 0.8 : 1.0;
                Color color = strokes.Count > 0 ? Colors.Black : Colors.Green;
                SolidColorBrush brush = new SolidColorBrush(color) { Opacity = opacity };

                List<Storyboard> modelStoryboards = InteractionTools.Trace(MyCanvas, sketch.Strokes, sketch.Times, brush, duration);
                foreach (Storyboard storyboard in modelStoryboards)
                {
                    storyboard.Begin();
                }
            }

            // animate the user's input strokes
            if (strokes.Count > 0)
            {
                Sketch sketch = SketchTools.Clone(input);
                sketch = SketchTransformation.Resample(sketch, 128);
                double opacity = 1.0;
                Color color = Colors.Red;
                SolidColorBrush brush = new SolidColorBrush(color) { Opacity = opacity };

                List<Storyboard> inputStoryboards = InteractionTools.Trace(MyCanvas, sketch.Strokes, sketch.Times, brush, duration, model);
                foreach (Storyboard storyboard in inputStoryboards)
                {
                    storyboard.Begin();
                }
            }

            //
            if (strokes.Count == 0 && !MyImageButton.IsChecked.Value) { return; }

            //
            int numModelPoints = 0;
            foreach (InkStroke stroke in model.Strokes) { numModelPoints += stroke.GetInkPoints().Count; }
            int delay = (numModelPoints * duration) / 10000;
            MyPlayButton.IsEnabled = false;
            MyCheckButton.IsEnabled = false;
            MyInkCanvas.InkPresenter.IsInputEnabled = false;
            await Task.Delay(delay);
            MyPlayButton.IsEnabled = true;
            MyCheckButton.IsEnabled = true;
            MyInkCanvas.InkPresenter.IsInputEnabled = true;
        }

        private void MyCheckButton_Click(object sender, RoutedEventArgs e)
        {
            // hide the bottom command bar and shwo the top bar
            MyTopCommandBar.Visibility = Visibility.Visible;
            MyBottomCommandBar.Visibility = Visibility.Collapsed;

            // shift the ink canvas to the right
            MyGrid.ColumnDefinitions[0].Width = new GridLength(0);
            MyGrid.ColumnDefinitions[1].Width = new GridLength(5, GridUnitType.Star);
            MyGrid.ColumnDefinitions[2].Width = new GridLength(5, GridUnitType.Star);

            // get the model and input sketches
            Sketch model = myTemplates[MyCurrentIndex];
            Sketch input = ToSketch(MyInkStrokes.GetStrokes(), myTimeCollection, 0, 0, BorderLength, BorderLength);

            //
            TechniqueClassifier techniqueClassifier = new TechniqueClassifier();
            techniqueClassifier.Train(model, input);
            techniqueClassifier.Run();
            bool strokeCountResult = techniqueClassifier.StrokeCountResult;

            //
            string text = "";
            text += "Is stroke count correct? " + strokeCountResult;
            MyFeedbackText.Text = text;
        }

        private void MyReturnButton_Click(object sender, RoutedEventArgs e)
        {
            MyTopCommandBar.Visibility = Visibility.Collapsed;
            MyBottomCommandBar.Visibility = Visibility.Visible;

            MyGrid.ColumnDefinitions[0].Width = new GridLength(2.5, GridUnitType.Star);
            MyGrid.ColumnDefinitions[1].Width = new GridLength(5, GridUnitType.Star);
            MyGrid.ColumnDefinitions[2].Width = new GridLength(2.5, GridUnitType.Star);
        }

        #endregion

        #region Combo Box Interactions

        private void MySymbolsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //
            if (!MyIsReady) { return; }

            //
            MyImage.Source = null;
            MyCurrentIndex = MySymbolsComboBox.SelectedIndex;
            if (MyImageButton.IsChecked.Value)
            {
                InteractionTools.SetImage(MyImage, myImageFiles[MyCurrentIndex]);
            }

            //
            string promptedSymbol = MySymbolsComboBox.SelectedValue.ToString().ToUpper();
            MyPrompText.Text = PROMPT_TEXT + promptedSymbol;

            //
            Clear();
        }

        #endregion

        #region Helper Methods

        private Sketch ToSketch(IReadOnlyList<InkStroke> strokeCollection, List<List<long>> timeCollection, double minX, double minY, double maxX, double maxY)
        {
            //
            InkStrokeBuilder builder = new InkStrokeBuilder();
            List<InkStroke> newStrokeCollection = new List<InkStroke>();
            List<List<long>> newTimeCollection = new List<List<long>>();
            for (int i = 0; i < strokeCollection.Count; ++i)
            {
                IReadOnlyList<InkPoint> points = strokeCollection[i].GetInkPoints();
                List<long> times = timeCollection[i];
                int count = times.Count < points.Count ? times.Count : points.Count;

                List<Point> newPoints = new List<Point>();
                List<long> newTimes = new List<long>();
                for (int j = 0; j < count; ++j)
                {
                    InkPoint point = points[j];
                    long time = times[j];

                    newPoints.Add(new Point(point.Position.X, point.Position.Y));
                    newTimes.Add(time);
                }

                newStrokeCollection.Add(builder.CreateStroke(newPoints));
                newTimeCollection.Add(newTimes);
            }

            //
            Sketch sketch = new Sketch("", newStrokeCollection, newTimeCollection, minX, minY, maxX, maxY);
            return sketch;
        }


        #endregion

        #region Properties

        public double BorderLength { get; private set; }

        private bool MyIsReady { get; set; }
        private int MyCurrentIndex { get; set; }
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

        public readonly string PROMPT_TEXT = "Please draw the following symbol: ";

        #endregion
    }
}