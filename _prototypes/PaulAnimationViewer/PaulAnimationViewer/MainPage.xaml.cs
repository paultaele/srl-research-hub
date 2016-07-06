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
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            //
            double width = MyBorder.ActualWidth;
            double height = MyBorder.ActualHeight;
            BorderLength = width < height ? width : height;
            MyBorder.Width = MyBorder.Height = BorderLength;

            //
            InkStrokes = MyInkCanvas.InkPresenter.StrokeContainer;
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;
            MyInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);

            //
            LoadContents(IMAGES_PATH, out myImageFiles, ".png");
            LoadContents(TEMPLATES_PATH, out myTemplateFiles, ".xml");

            //
            foreach (StorageFile file in myImageFiles) { MySymbolsComboBox.Items.Add(Path.GetFileNameWithoutExtension(file.Path)); }
            InteractionTools.SetImage(MyImage, myImageFiles[0]);
            MySymbolsComboBox.SelectedIndex = 0;

            //
            myTemplates = new List<Sketch>();
            foreach (StorageFile file in myTemplateFiles)
            {
                Sketch template = null;
                Task task = Task.Run(async () => template = await SketchTools.XmlToSketch(file));
                task.Wait();

                //template = SketchTransformation.Resample(template, 128);
                template = SketchTransformation.ScaleFrame(template, BorderLength);
                template = SketchTransformation.TranslateFrame(template, new Point(BorderLength / 2 - MyBorder.BorderThickness.Left, BorderLength / 2 - MyBorder.BorderThickness.Top));
                myTemplates.Add(template);
                foreach (InkStroke stroke in template.Strokes)
                {
                    stroke.DrawingAttributes = StrokeVisuals;
                }
            }

            //
            IsReady = true;
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

        #region Button Interactions

        private void MySymbolsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MyPlayButton_Click(object sender, RoutedEventArgs e)
        {
            Sketch template = SketchTools.Clone(myTemplates[ImageIndex]);
            Helper.Trace(MyCanvas, template.Strokes, template.Times);
        }

        #endregion

        #region Combo Box Interactions

        private void MySymbolsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //
            if (!IsReady) { return; }

            //
            ImageIndex = MySymbolsComboBox.SelectedIndex;
            InteractionTools.SetImage(MyImage, myImageFiles[ImageIndex]);
        }

        #endregion

        #region Properties

        private bool IsReady { get; set; }
        private int ImageIndex { get; set; }
        public double BorderLength { get; private set; }
        private InkStrokeContainer InkStrokes { get; set; }

        #endregion

        #region Fields

        private List<StorageFile> myImageFiles;
        private List<StorageFile> myTemplateFiles;
        private List<Sketch> myTemplates;

        public InkDrawingAttributes StrokeVisuals = new InkDrawingAttributes()
        {
            Color = Colors.Red,
            IgnorePressure = true,
            PenTip = PenTipShape.Circle,
            Size = new Size(10, 10)
        };

        public readonly string IMAGES_PATH = @"Assets\Images";
        public readonly string TEMPLATES_PATH = @"Assets\Templates";

        #endregion
    }
}
