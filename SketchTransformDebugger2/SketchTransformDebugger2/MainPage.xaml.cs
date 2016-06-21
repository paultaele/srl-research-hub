using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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

namespace SketchTransformDebugger2
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
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(PEN_VISUALS);
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            double width = MyRightBorder.ActualWidth;
            double height = MyRightBorder.ActualHeight;
            double length = width < height ? width : height;
            MyRightBorder.Height = MyRightBorder.Width = length;

            MyInkStrokes = MyInkCanvas.InkPresenter.StrokeContainer;
        }

        #endregion

        #region Button Interactions

        private async void MyLoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add("*");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) { return; }

            mySketch = await SketchTools.XmlToSketch(file, PEN_VISUALS);

            MyInkStrokes.Clear();
            MyInkStrokes.AddStrokes(mySketch.Strokes);
        }

        private void MyTransformDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (mySketch == null || MyInkStrokes.GetStrokes().Count == 0) { return; }

            Sketch sketch = null;
            MyInkStrokes.Clear();
            sketch = SketchTools.Clone(mySketch);

            if (!MyResampleToggle.IsOn && !MyScaleToggle.IsOn && !MyTranslateToggle.IsOn && !MyFrameToggle.IsOn)
            {
                MyInkStrokes.AddStrokes(sketch.Strokes);
                return;
            }

            if (MyResampleToggle.IsOn)
            {
                int n = int.Parse(MyResampleCountTextBox.Text);

                sketch = SketchTransformation.Resample(sketch, n);
            }

            if (MyScaleToggle.IsOn)
            {
                double size = double.Parse(MyScaleSizeTextBox.Text);

                if (MyScaleSquareRadio.IsChecked.Value) { sketch = SketchTransformation.ScaleSquare(sketch, size); }
                else if (MyScaleProportionalRadio.IsChecked.Value) { sketch = SketchTransformation.ScaleProportional(sketch, size); }
                else if (MyScaleFrameRadio.IsChecked.Value) { sketch = SketchTransformation.ScaleFrame(sketch, size); }
            }

            if (MyTranslateToggle.IsOn)
            {
                Point k = new Point(MyInkCanvas.ActualWidth / 2, MyInkCanvas.ActualHeight / 2);

                if (MyTranslateCentroidRadio.IsChecked.Value) { sketch = SketchTransformation.TranslateCentroid(sketch, k); }
                else if (MyTranslateMedianRadio.IsChecked.Value) { sketch = SketchTransformation.TranslateMedian(sketch, k); }
                else if (MyTranslateFrameRadio.IsChecked.Value) { sketch = SketchTransformation.TranslateFrame(sketch, k); }
            }

            if (MyFrameToggle.IsOn)
            {
                List<InkStroke> frameStrokes = GetFrameStrokes(sketch);
                sketch.Strokes.AddRange(frameStrokes);
            }

            MyInkStrokes.AddStrokes(sketch.Strokes);
        }

        private List<InkStroke> GetFrameStrokes(Sketch sketch)
        {
            List<InkStroke> frameStrokes = new List<InkStroke>();

            Point topLeft = new Point(sketch.FrameMinX, sketch.FrameMinY);
            Point topRight = new Point(sketch.FrameMaxX, sketch.FrameMinY);
            Point bottomLeft = new Point(sketch.FrameMinX, sketch.FrameMaxY);
            Point bottomRight = new Point(sketch.FrameMaxX, sketch.FrameMaxY);

            InkStrokeBuilder builder = new InkStrokeBuilder();
            InkStroke topStroke = builder.CreateStroke(new List<Point>() { topLeft, topRight });
            InkStroke leftStroke = builder.CreateStroke(new List<Point>() { topLeft, bottomLeft });
            InkStroke rightStroke = builder.CreateStroke(new List<Point>() { topRight, bottomRight });
            InkStroke bottomStroke = builder.CreateStroke(new List<Point>() { bottomLeft, bottomRight });

            InkDrawingAttributes attributes = sketch.Strokes[0].DrawingAttributes;
            topStroke.DrawingAttributes = attributes;
            leftStroke.DrawingAttributes = attributes;
            rightStroke.DrawingAttributes = attributes;
            bottomStroke.DrawingAttributes = attributes;

            frameStrokes.Add(topStroke);
            frameStrokes.Add(leftStroke);
            frameStrokes.Add(rightStroke);
            frameStrokes.Add(bottomStroke);

            return frameStrokes;
        }

        #endregion

        #region Toggle Behaviors

        private void MyResampleToggle_Toggled(object sender, RoutedEventArgs e)
        {
            MyResampleCountTextBox.IsEnabled = MyResampleToggle.IsOn;

            if (!MyResampleToggle.IsOn) { MyResampleCountTextBox.Text = ""; }
        }

        private void MyScaleToggle_Toggled(object sender, RoutedEventArgs e)
        {
            MyScaleSizeTextBox.IsEnabled = MyScaleToggle.IsOn;
            MyScaleSquareRadio.IsEnabled = MyScaleToggle.IsOn;
            MyScaleProportionalRadio.IsEnabled = MyScaleToggle.IsOn;
            MyScaleFrameRadio.IsEnabled = MyScaleToggle.IsOn;
        }


        private void MyTranslateToggle_Toggled(object sender, RoutedEventArgs e)
        {
            MyTranslateCentroidRadio.IsEnabled = MyTranslateToggle.IsOn;
            MyTranslateMedianRadio.IsEnabled = MyTranslateToggle.IsOn;
            MyTranslateFrameRadio.IsEnabled = MyTranslateToggle.IsOn;
        }

        #endregion

        #region Properties

        private InkStrokeContainer MyInkStrokes { get; set; }

        #endregion

        #region Fields

        private Sketch mySketch;
        

        public InkDrawingAttributes PEN_VISUALS = new InkDrawingAttributes() { Color = Colors.Black, IgnorePressure = true, PenTip = PenTipShape.Circle, Size = new Size(10, 10) };

        #endregion
    }
}
