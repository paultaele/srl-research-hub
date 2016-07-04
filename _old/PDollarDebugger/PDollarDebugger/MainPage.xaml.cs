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

namespace PDollarDebugger
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

            MyInputInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            MyTemplateInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            // set the transformation attributes
            N = 128;
            Size = MyInputInkCanvasBorder.ActualHeight * 0.75;
            K = new Point(MyInputInkCanvasBorder.ActualWidth / 2, MyInputInkCanvasBorder.ActualHeight / 2);

            // read the data
            Task task = Task.Run(async () => await ReadData());
            task.Wait();

            // initialize combo boxes
            foreach (string label in myInputLabels) { MyInputsComboBox.Items.Add(label); }
            foreach (string label in myTemplateLabels) { MyTemplatesComboBox.Items.Add(label); }
            MyInputsComboBox.SelectedIndex = 0;
            MyTemplatesComboBox.SelectedIndex = 0;

            // normalize data
            NormalizeData();

            //
            MyInputInkCanvas.InkPresenter.StrokeContainer.AddStrokes(myInputPairs[0].Transformed.Strokes);
            MyTemplateInkCanvas.InkPresenter.StrokeContainer.AddStrokes(myTemplatePairs[0].Transformed.Strokes);

            //
            IsLoaded = true;
        }

        #endregion

        #region Combo Box Interactions

        private void MyInputsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) { return; }

            MyInputInkCanvas.InkPresenter.StrokeContainer.Clear();

            int index = MyInputsComboBox.SelectedIndex;
            Sketch sketch = SketchTools.Clone(myInputPairs[index].Transformed);
            MyInputInkCanvas.InkPresenter.StrokeContainer.AddStrokes(sketch.Strokes);
        }

        private void MyTemplatesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) { return; }

            MyTemplateInkCanvas.InkPresenter.StrokeContainer.Clear();

            int index = MyTemplatesComboBox.SelectedIndex;
            Sketch sketch = SketchTools.Clone(myTemplatePairs[index].Transformed);
            MyTemplateInkCanvas.InkPresenter.StrokeContainer.AddStrokes(sketch.Strokes);
        }

        #endregion

        #region Button Interaction

        private void MyCalculateButton_Click(object sender, RoutedEventArgs e)
        {
            SketchPair input = myInputPairs[MyInputsComboBox.SelectedIndex];
            SketchPair template = myInputPairs[MyTemplatesComboBox.SelectedIndex];

            double distance1 = SketchTools.Distance(input.Transformed, template.Transformed);
            double distance2 = SketchTools.Distance(template.Transformed, input.Transformed);
            double minDistance = Math.Min(distance1, distance2);

            MyLrDistanceText.Text = "" + distance1;
            MyRlDistanceText.Text = "" + distance2;
            MyMinDistanceText.Text = "" + minDistance;
        }

        #endregion

        #region Helper Methods

        private async Task ReadData()
        {
            // get directory of model templates
            string root = Package.Current.InstalledLocation.Path;
            string inputsPath = root + @"\Assets\Inputs\";
            string templatesPath = root + @"\Assets\Models\";

            //
            StorageFolder inputsFolder = await StorageFolder.GetFolderFromPathAsync(inputsPath);
            StorageFolder templatesFolder = await StorageFolder.GetFolderFromPathAsync(templatesPath);

            //
            List<StorageFile> inputFiles = new List<StorageFile>();
            List<StorageFile> templateFiles = new List<StorageFile>();
            foreach (StorageFile file in await inputsFolder.GetFilesAsync()) { if (file.Name.EndsWith(".xml")) { inputFiles.Add(file); } }
            foreach (StorageFile file in await templatesFolder.GetFilesAsync()) { if (file.Name.EndsWith(".xml")) { templateFiles.Add(file); } }

            //
            myInputs = new List<Sketch>();
            myTemplates = new List<Sketch>();
            myInputLabels = new List<string>();
            myTemplateLabels = new List<string>();
            foreach (StorageFile file in inputFiles) { Sketch sketch = await SketchTools.ReadXml(file, PEN_DRAWING_ATTRIBUTES); myInputs.Add(sketch); myInputLabels.Add(sketch.Label); }
            foreach (StorageFile file in templateFiles) { Sketch sketch = await SketchTools.ReadXml(file, PEN_DRAWING_ATTRIBUTES); myTemplates.Add(sketch); myTemplateLabels.Add(sketch.Label); }
        }

        private void NormalizeData()
        {
            myInputPairs = new List<SketchPair>();
            myTemplatePairs = new List<SketchPair>();
            foreach (Sketch original in myInputs)
            {
                Sketch transformed = Normalize(original);
                myInputPairs.Add(new SketchPair(original, transformed));
            }
            foreach (Sketch original in myTemplates)
            {
                Sketch transformed = Normalize(original);
                myTemplatePairs.Add(new SketchPair(original, transformed));
            }
        }

        private Sketch Normalize(Sketch original)
        {
            Sketch sketch = SketchTools.Clone(original);
            sketch = SketchTransformation.Resample(sketch, N);
            sketch = SketchTransformation.ScaleSquare(sketch, Size);
            sketch = SketchTransformation.TranslateCentroid(sketch, K);

            return sketch;
        }

        #endregion

        #region Properties

        private int N { get; set; }
        private double Size { get; set; }
        private Point K { get; set; }

        private bool IsLoaded { get; set; }

        #endregion

        #region Fields

        private List<Sketch> myInputs;
        private List<Sketch> myTemplates;
        private List<string> myInputLabels;
        private List<string> myTemplateLabels;
        private List<SketchPair> myInputPairs;
        private List<SketchPair> myTemplatePairs;

        public InkDrawingAttributes PEN_DRAWING_ATTRIBUTES = new InkDrawingAttributes() { Color = Colors.Black, IgnorePressure = true, PenTip = PenTipShape.Circle, Size = new Size(10, 10), };

        #endregion
    }
}
