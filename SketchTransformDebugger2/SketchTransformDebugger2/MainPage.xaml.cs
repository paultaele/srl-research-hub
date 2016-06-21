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

            Sketch sketch = await SketchTools.XmlToSketch(file, PEN_VISUALS);

            MyInkStrokes.Clear();
            MyInkStrokes.AddStrokes(sketch.Strokes);
        }

        private void MyTransformDataButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Toggle Behaviors

        private void MyResampleToggle_Toggled(object sender, RoutedEventArgs e)
        {
            MyResampleCountTextBox.IsEnabled = MyResampleToggle.IsOn;

            if (!MyResampleToggle.IsOn) { MyResampleCountTextBox.Text = ""; }
        }

        #endregion

        #region Properties

        private InkStrokeContainer MyInkStrokes { get; set; }

        #endregion

        #region Fields

        public InkDrawingAttributes PEN_VISUALS = new InkDrawingAttributes() { Color = Colors.Black, IgnorePressure = true, PenTip = PenTipShape.Circle, Size = new Size(10, 10) };

        #endregion
    }
}
