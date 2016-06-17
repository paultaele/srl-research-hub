using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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

        private void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyDotButton_Click(object sender, RoutedEventArgs e)
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