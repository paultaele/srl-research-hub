using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace BasicWhiteBoard
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
            myTimesCollection = new List<List<long>>();
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

            myTimes.Add(DateTime.Now.Ticks);

            if (hasEnded) { myTimesCollection.Add(myTimes); }
        }

        #endregion

        #region Button Handlers

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            myTimesCollection = new List<List<long>>();
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            //
            var strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count == 0) { return; }

            //
            strokes[strokes.Count - 1].Selected = true;
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();

            //
            myTimesCollection.RemoveAt(myTimesCollection.Count - 1);
        }

        private void MySaveButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void MyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".xml");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Debug.WriteLine("File was picked.");
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

        #endregion

        #region Properties

        private InkDrawingAttributes StrokeVisuals { get; set; }

        #endregion
        
        #region Fields

        private List<long> myTimes;
        private List<List<long>> myTimesCollection;

        #endregion
    }
}
