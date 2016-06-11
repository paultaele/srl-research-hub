using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

namespace SketchTransformDebugger
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

            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;
        }

        private void Mypage_Loaded(object sender, RoutedEventArgs e)
        {
            //double length = MyInkCanvas.ActualHeight < MyInkCanvas.ActualWidth ? MyInkCanvas.ActualHeight : MyInkCanvas.ActualWidth;
            //MyRightBorder.Width = MyRightBorder.Height = length;

            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;
        }

        private void InitializeInkCanvas()
        {
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;

            // define ink canvas' attributes
            StrokeVisuals = new InkDrawingAttributes();
            StrokeVisuals.Color = Colors.Black;
            StrokeVisuals.IgnorePressure = true;
            StrokeVisuals.PenTip = PenTipShape.Circle;
            StrokeVisuals.Size = new Size(10, 10);
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
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

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            if (hasStarted && hasEnded) { throw new Exception("Cannot start and end stroke at the same time."); }

            if (hasStarted) { myTimes = new List<long>(); }

            long time = DateTime.Now.Ticks - DateTimeOffset;
            myTimes.Add(time);

            if (hasEnded) { myTimeCollection.Add(myTimes); }
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
            if (strokes.Count == 0) { DateTimeOffset = 0; return; }

            //
            strokes[strokes.Count - 1].Selected = true;
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();

            //
            myTimeCollection.RemoveAt(myTimeCollection.Count - 1);
        }

        #endregion

        #region Toggle Handlers

        private void MyDrawToggle_Toggled(object sender, RoutedEventArgs e)
        {
            bool isEnabled = MyDrawToggle.IsOn;
            MyInkCanvas.InkPresenter.IsInputEnabled = isEnabled;

            // null check needed since this method is called before clear and undo buttons exist
            if (MyClearButton != null) { MyClearButton.IsEnabled = isEnabled; }
            if (MyUndoButton != null) { MyUndoButton.IsEnabled = isEnabled; }
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
