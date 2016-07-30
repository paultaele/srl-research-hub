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

namespace PaulFeedbackViewer
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

            // set the screen size to full
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            // initialize the stroke event handlers
            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted; ;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued; ;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded; ;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            // initializer the feedback recognizers
            myStructureRecognizer = new StructureRecognizer();
            myTechniqueRecognizer = new TechniqueRecognizer();

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

                //
                MyCheckButton.IsEnabled = true;
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

        #region Commandbar Button Interactions

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

            //
            MyPlayButton.IsEnabled = true;
            MyCheckButton.IsEnabled = true;
            MyInkCanvas.InkPresenter.IsInputEnabled = true;

            //
            if (MyInkStrokes.GetStrokes().Count <= 0) { MyCheckButton.IsEnabled = false; }
        }

        private async void MyPlayButton_Click(object sender, RoutedEventArgs e)
        {
            //
            if (!MyImageButton.IsChecked.Value) { return; }

            //
            EnableAppBarButtons(false);
            MyInkCanvas.InkPresenter.IsInputEnabled = false;

            //
            Sketch model = myTemplates[MyCurrentIndex];
            model = SketchTools.Clone(model);
            double opacity = 0.8;
            Color color = Colors.Green;
            SolidColorBrush brush = new SolidColorBrush(color) { Opacity = opacity };

            // animate the expert's model strokes
            List<Storyboard> modelStoryboards = InteractionTools.Trace(MyCanvas, model.Strokes, model.Times, LARGE_DOT_SIZE, brush, POINT_DURATION);
            foreach (Storyboard storyboard in modelStoryboards)
            {
                storyboard.Begin();
            }

            //
            int numModelPoints = 0;
            foreach (InkStroke stroke in model.Strokes) { numModelPoints += stroke.GetInkPoints().Count; }
            int delay = numModelPoints * POINT_DURATION;
            await InteractionTools.Delay(delay);

            //
            EnableAppBarButtons(true);
            MyInkCanvas.InkPresenter.IsInputEnabled = true;
        }

        private void MyCheckButton_Click(object sender, RoutedEventArgs e)
        {
            // show the image if the image is not enabled
            if (!MyImageButton.IsChecked.Value)
            {
                InteractionTools.SetImage(MyImage, myImageFiles[MyCurrentIndex]);
                MySymbolsComboBox.SelectedIndex = MyCurrentIndex;
            }

            // hide the bottom command bar and shwo the top bar
            ShowAppBarButtons(true);
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            MyRightSide.Visibility = Visibility.Visible;

            // shift the ink canvas to the right
            MyGrid.ColumnDefinitions[0].Width = new GridLength(0);
            MyGrid.ColumnDefinitions[1].Width = new GridLength(5, GridUnitType.Star);
            MyGrid.ColumnDefinitions[2].Width = new GridLength(5, GridUnitType.Star);

            // get the model and input sketches
            Sketch model = myTemplates[MyCurrentIndex];
            Sketch input = Sketch.CreateStroke("", new List<InkStroke>(MyInkStrokes.GetStrokes()), myTimeCollection, 0, 0, BorderLength, BorderLength);

            // train and run the recognizers
            myStructureRecognizer.Train(model, myTemplates, input);
            myTechniqueRecognizer.Train(model, input);
            myStructureRecognizer.Run();
            myTechniqueRecognizer.Run();

            // check the visual structure
            CheckStructure(myStructureRecognizer);

            // check the written technique
            CheckTechnique(myTechniqueRecognizer);
        }

        private void MyRandomButton_Click(object sender, RoutedEventArgs e)
        {
            //
            Random random = new Random();
            int index = random.Next(0, MySymbolsComboBox.Items.Count);

            //
            UpdateSymbol(index);
        }

        private void MyReturnButton_Click(object sender, RoutedEventArgs e)
        {
            //
            ShowAppBarButtons(false);
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Touch;
            MyRightSide.Visibility = Visibility.Collapsed;

            //
            MyGrid.ColumnDefinitions[0].Width = new GridLength(2.5, GridUnitType.Star);
            MyGrid.ColumnDefinitions[1].Width = new GridLength(5, GridUnitType.Star);
            MyGrid.ColumnDefinitions[2].Width = new GridLength(2.5, GridUnitType.Star);

            //
            Clear();

            // reset to original image view defaults
            MyImageButton_Click(null, null);
        }

        #endregion

        #region Structure Feedback Button Interactions

        private async void MySymbolCorrectnessPlayButton_Click(object sender, RoutedEventArgs e)
        {
            //
            //EnableStructureButtons(false);
            MyImage.Visibility = Visibility.Collapsed;

            // get the model and input strokes
            Sketch model = SketchTools.Clone(myTemplates[MyCurrentIndex]);
            Sketch input = Sketch.CreateStroke("", new List<InkStroke>(MyInkStrokes.GetStrokes()), myTimeCollection, 0, 0, BorderLength, BorderLength);

            // store and remove the original strokes from the ink canvas
            List<InkStroke> originalStrokes = SketchTools.Clone(new List<InkStroke>(MyInkStrokes.GetStrokes()));
            foreach (InkStroke stroke in MyInkStrokes.GetStrokes()) { stroke.Selected = true; }
            MyInkStrokes.DeleteSelected();

            // set the animation colors
            SolidColorBrush modelBrush = new SolidColorBrush(Colors.Green) { Opacity = 1.0 };
            SolidColorBrush inputBrush = new SolidColorBrush(Colors.Red) { Opacity = 1.0 };

            // get the animations
            List<Storyboard> modelStoryboards = Helper.DisplaySymbol(MyCanvas, model.Strokes, modelBrush, SMALL_DOT_SIZE, STROKE_DURATION);
            List<Storyboard> inputStoryboards = Helper.DisplaySymbol(MyCanvas, input.Strokes, inputBrush, StrokeVisuals.Size.Width, STROKE_DURATION);

            // animate the feedback
            foreach (Storyboard storyboard in modelStoryboards) { storyboard.Begin(); }
            foreach (Storyboard storyboard in inputStoryboards) { storyboard.Begin(); }

            // re-add the original strokes to the ink canvas and re-enable return button
            int delay = STROKE_DURATION;
            await InteractionTools.Delay(delay);
            MyInkStrokes.AddStrokes(originalStrokes);

            //
            //EnableStructureButtons(true);
            MyImage.Visibility = Visibility.Visible;
        }

        private void MyStrokeBoundsPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // get the model and input strokes
            Sketch model = SketchTools.Clone(myTemplates[MyCurrentIndex]);
            Sketch input = Sketch.CreateStroke("", new List<InkStroke>(MyInkStrokes.GetStrokes()), myTimeCollection, 0, 0, BorderLength, BorderLength);

            // TODO
        }

        #endregion

        #region Technique Feedback Button Interactions

        private async void MyStrokeCountPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // do not show feedback if canvas has no strokes
            if (MyInkStrokes.GetStrokes().Count <= 0) { return; }

            //
            EnableTechniqueButtons(false);

            // get the model and input strokes
            Sketch model = myTemplates[MyCurrentIndex];
            model = SketchTools.Clone(model);
            Sketch input = Sketch.CreateStroke("", new List<InkStroke>(MyInkStrokes.GetStrokes()), myTimeCollection, 0, 0, BorderLength, BorderLength);
            input = SketchTools.Clone(input);
            input = SketchTransformation.Resample(input, 128);

            // set the animation colors
            SolidColorBrush modelBrush = new SolidColorBrush(Colors.Green) { Opacity = 1.0 };
            SolidColorBrush inputBrush = new SolidColorBrush(Colors.Red) { Opacity = 1.0 };

            // get the animations
            List<Storyboard> modelStoryboards = InteractionTools.DisplayEndpoints(MyCanvas, model.Strokes, modelBrush, LARGE_DOT_SIZE, STROKE_DURATION);
            List<Storyboard> inputStoryboards = InteractionTools.DisplayEndpoints(MyCanvas, input.Strokes, inputBrush, SMALL_DOT_SIZE, STROKE_DURATION, model);

            // animate the feedback
            foreach (Storyboard storyboard in modelStoryboards) { storyboard.Begin(); }
            foreach (Storyboard storyboard in inputStoryboards) { storyboard.Begin(); }

            // re-add the original strokes to the ink canvas and re-enable return button
            int delay = STROKE_DURATION * model.Strokes.Count;
            await InteractionTools.Delay(delay);

            //
            EnableTechniqueButtons(true);
        }

        private async void MyStrokeOrderPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // do not show feedback if canvas has no strokes
            if (MyInkStrokes.GetStrokes().Count <= 0) { return; }
            if (!myTechniqueRecognizer.StrokeCountResult) { return; }

            //
            EnableTechniqueButtons(false);

            // get the model and input strokes
            Sketch model = myTemplates[MyCurrentIndex];
            model = SketchTools.Clone(model);
            Sketch input = Sketch.CreateStroke("", new List<InkStroke>(MyInkStrokes.GetStrokes()), myTimeCollection, 0, 0, BorderLength, BorderLength);
            input = SketchTools.Clone(input);
            input = SketchTransformation.Resample(input, 128);

            // store and remove the original strokes from the ink canvas
            List<InkStroke> originalStrokes = SketchTools.Clone(new List<InkStroke>(MyInkStrokes.GetStrokes()));
            foreach (InkStroke stroke in MyInkStrokes.GetStrokes()) { stroke.Selected = true; }
            MyInkStrokes.DeleteSelected();

            // set the animation colors
            SolidColorBrush modelBrush = new SolidColorBrush(Colors.Green) { Opacity = 1.0 };
            SolidColorBrush inputBrush = new SolidColorBrush(Colors.Red) { Opacity = 1.0 };

            // get the animations
            List<Storyboard> modelStoryboards = InteractionTools.DisplayPaths(MyCanvas, model.Strokes, modelBrush, LARGE_STROKE_SIZE, STROKE_DURATION);
            List<Storyboard> inputStoryboards = InteractionTools.DisplayPaths(MyCanvas, input.Strokes, inputBrush, SMALL_STROKE_SIZE, STROKE_DURATION);

            // animate the feedback
            foreach (Storyboard storyboard in modelStoryboards) { storyboard.Begin(); }
            foreach (Storyboard storyboard in inputStoryboards) { storyboard.Begin(); }

            // re-add the original strokes to the ink canvas and re-enable return button
            int delay = STROKE_DURATION * model.Strokes.Count;
            await InteractionTools.Delay(delay);
            MyInkStrokes.AddStrokes(originalStrokes);

            //
            EnableTechniqueButtons(true);
        }

        private async void MyStrokeDirectionPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // do not show feedback if canvas has no strokes or incorrect stroke count
            if (MyInkStrokes.GetStrokes().Count <= 0) { return; }
            if (!myTechniqueRecognizer.StrokeCountResult) { return; }

            //
            EnableTechniqueButtons(false);

            // get the input sketch and stroke directions
            Sketch input = Sketch.CreateStroke("", new List<InkStroke>(MyInkStrokes.GetStrokes()), myTimeCollection, 0, 0, BorderLength, BorderLength);
            input = SketchTools.Clone(input);
            input = SketchTransformation.Resample(input, 128);
            List<bool> strokeDirections = new List<bool>(myTechniqueRecognizer.StrokeDirections);

            // create solution
            InkStrokeBuilder builder = new InkStrokeBuilder();
            List<InkStroke> solutionStrokes = new List<InkStroke>();
            for (int i = 0; i < input.Strokes.Count; ++i)
            {
                // get the current input stroke and times
                InkStroke inputStroke = input.Strokes[i];
                List<long> inputTimes = input.Times[i];

                // get current input points and stroke direction status
                List<InkPoint> inputPoints = new List<InkPoint>(inputStroke.GetInkPoints());
                bool strokeDirection = strokeDirections[i];

                // create the solution stroke
                List<Point> solutionPoints = new List<Point>();
                foreach (InkPoint inputPoint in inputPoints) { solutionPoints.Add(new Point(inputPoint.Position.X, inputPoint.Position.Y)); }
                if (!strokeDirection) { solutionPoints.Reverse(); }
                InkStroke solutionStroke = builder.CreateStroke(solutionPoints);

                // add the solution stroke to the list
                solutionStrokes.Add(solutionStroke);
            }

            // display solution
            SolidColorBrush newInputBrush = new SolidColorBrush(Colors.Black) { Opacity = 1.0 };
            List<Storyboard> newInputStoryboards = InteractionTools.Trace(MyCanvas, solutionStrokes, input.Times, LARGE_DOT_SIZE, newInputBrush, POINT_DURATION);
            foreach (Storyboard storyboard in newInputStoryboards)
            {
                storyboard.Begin();
            }

            // display original
            SolidColorBrush inputBrush = new SolidColorBrush(Colors.Red) { Opacity = 1.0 };
            List<Storyboard> inputStoryboards = InteractionTools.Trace(MyCanvas, input.Strokes, input.Times, LARGE_DOT_SIZE, inputBrush, POINT_DURATION);
            foreach (Storyboard storyboard in inputStoryboards)
            {
                storyboard.Begin();
            }

            // re-enable return button
            int delay = 0;
            foreach (InkStroke inputStroke in input.Strokes) { delay += POINT_DURATION * inputStroke.GetInkPoints().Count; }
            await InteractionTools.Delay(delay);

            //
            EnableTechniqueButtons(true);
        }

        private async void MyStrokeSpeedTestPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // do not show feedback if canvas has no strokes or incorrect stroke count
            if (MyInkStrokes.GetStrokes().Count <= 0) { return; }
            if (!myTechniqueRecognizer.StrokeCountResult) { return; }

            //
            EnableTechniqueButtons(false);

            //
            bool hasInput = false;
            Sketch model = SketchTools.Clone(myTemplates[MyCurrentIndex]);
            Sketch input = null;
            if (MyInkStrokes.GetStrokes().Count > 0)
            {
                input = new Sketch("", new List<InkStroke>(MyInkStrokes.GetStrokes()), myTimeCollection, 0, 0, BorderLength, BorderLength);
                hasInput = true;
            }

            // get the converted model times
            long modelOffset = model.Times[0][0];
            long modelShift = 0;
            List<List<long>> modelTimesCollection = new List<List<long>>();
            long modelFactor = 2;
            for (int i = 0; i < model.Times.Count; ++i)
            {
                //
                List<long> newModelTimes = new List<long>();
                foreach (long modelTime in model.Times[i])
                {
                    long newModelTime = (modelTime - modelOffset - modelShift) / modelFactor;
                    newModelTimes.Add(newModelTime);
                }

                //
                if (i < model.Times.Count - 1)
                {
                    long nextStart = model.Times[i + 1][0];
                    long prevLast = model.Times[i][model.Times[i].Count - 1];
                    modelShift += nextStart - prevLast;
                }

                modelTimesCollection.Add(newModelTimes);
            }

            //
            model = new Sketch(model.Label, model.Strokes, modelTimesCollection, model.FrameMinX, model.FrameMinY, model.FrameMaxX, model.FrameMaxY);
            SolidColorBrush modelBrush = new SolidColorBrush(Colors.Black) { Opacity = 0.8 };
            List<Storyboard> modelStoryboards = InteractionTools.Playback(MyCanvas, model.Strokes, model.Times, LARGE_DOT_SIZE, modelBrush);
            foreach (Storyboard storyboard in modelStoryboards)
            {
                storyboard.Begin();
            }

            //
            if (!hasInput) { return; }
            input = SketchTools.Clone(input);
            SolidColorBrush inputBrush = new SolidColorBrush(Colors.Red) { Opacity = 1.0 };
            List<Storyboard> inputStoryboards = InteractionTools.Playback(MyCanvas, input.Strokes, input.Times, SMALL_DOT_SIZE, inputBrush);
            foreach (Storyboard storyboard in inputStoryboards)
            {
                storyboard.Begin();
            }

            // 
            int inputDelay = (int)((input.Times[input.Times.Count - 1])[input.Times[input.Times.Count - 1].Count - 1] - (input.Times[0])[0]);
            int modelDelay = (int)((model.Times[model.Times.Count - 1])[model.Times[model.Times.Count - 1].Count - 1] - (model.Times[0])[0]);
            int delay = inputDelay > modelDelay ? inputDelay : modelDelay;
            await InteractionTools.Delay(delay);


            //
            EnableTechniqueButtons(true);
        }

        # endregion

        #region Combo Box Interactions

        private void MySymbolsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //
            if (!MyIsReady) { return; }

            //
            UpdateSymbol(MySymbolsComboBox.SelectedIndex);
        }

        #endregion

        #region Helper Methods

        private void CheckStructure(StructureRecognizer recognizer)
        {
            // get the structure feedback results
            bool symbolCorrectnessResult = recognizer.SymbolCorrectnessResult;
            bool strokeMatchResult = recognizer.StrokeMatchResult;

            // display the structure feedback results
            MySymbolCorrectnessResultText.Text = symbolCorrectnessResult ? "CORRECT" : "INCORRECT";
            MySymbolCorrectnessResultText.Foreground = symbolCorrectnessResult ? CORRECT_BRUSH : INCORRECT_BRUSH;

            MyStrokeMatchResultText.Text = strokeMatchResult ? "CORRECT" : "INCORRECT";
            MyStrokeMatchResultText.Foreground = strokeMatchResult ? CORRECT_BRUSH : INCORRECT_BRUSH;
        }

        private void CheckTechnique(TechniqueRecognizer recognizer)
        {
            // get the technique feedback results
            bool strokeCountResult = recognizer.StrokeCountResult;
            bool strokeOrderResult = recognizer.StrokeOrderResult;
            bool strokeDirectionResult = recognizer.StrokeDirectionResult;
            bool strokeSpeedResult = recognizer.StrokeSpeedResult;

            // display the technique feedback results
            MyStrokeCountResultText.Text = strokeCountResult ? "CORRECT" : "INCORRECT";
            MyStrokeCountResultText.Foreground = strokeCountResult ? CORRECT_BRUSH : INCORRECT_BRUSH;

            MyStrokeOrderResultText.Text = strokeOrderResult ? "CORRECT" : "INCORRECT";
            MyStrokeOrderResultText.Foreground = strokeOrderResult ? CORRECT_BRUSH : INCORRECT_BRUSH;

            MyStrokeDirectionResultText.Text = strokeDirectionResult ? "CORRECT" : "INCORRECT";
            MyStrokeDirectionResultText.Foreground = strokeDirectionResult ? CORRECT_BRUSH : INCORRECT_BRUSH;

            MyStrokeSpeedResultText.Text = strokeSpeedResult ? "SUFFICIENT" : "INSUFFICIENT";
            MyStrokeSpeedResultText.Foreground = strokeSpeedResult ? CORRECT_BRUSH : INCORRECT_BRUSH;
        }

        private void UpdateSymbol(int index)
        {
            //
            MyImage.Source = null;
            MyCurrentIndex = index;
            if (MyImageButton.IsChecked.Value) { InteractionTools.SetImage(MyImage, myImageFiles[MyCurrentIndex]); }

            //
            MySymbolsComboBox.SelectedIndex = index;
            string promptedSymbol = MySymbolsComboBox.SelectedValue.ToString().ToUpper();
            MyPrompText.Text = PROMPT_TEXT + promptedSymbol;

            //
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

            //
            MyPlayButton.IsEnabled = true;
            MyCheckButton.IsEnabled = true;
            MyInkCanvas.InkPresenter.IsInputEnabled = true;

            //
            MyCheckButton.IsEnabled = false;
        }

        private void ShowAppBarButtons(bool isFeedbackMode)
        {
            Visibility hide = Visibility.Collapsed;
            Visibility show = Visibility.Visible;

            MyImageButton.Visibility = isFeedbackMode ? hide : show;
            MySeparator1.Visibility = isFeedbackMode ? hide : show;
            MyPlayButton.Visibility = isFeedbackMode ? hide : show;
            MySeparator2.Visibility = isFeedbackMode ? hide : show;
            MyClearButton.Visibility = isFeedbackMode ? hide : show;
            MyUndoButton.Visibility = isFeedbackMode ? hide : show;
            MySeparator3.Visibility = isFeedbackMode ? hide : show;
            MyCheckButton.Visibility = isFeedbackMode ? hide : show;
            MySeparator4.Visibility = isFeedbackMode ? hide : show;
            MyRandomButton.Visibility = isFeedbackMode ? hide : show;
            MySymbolsButton.Visibility = isFeedbackMode ? hide : show;

            MyReturnButton.Visibility = isFeedbackMode ? show : hide;
        }

        private void EnableAppBarButtons(bool isVisible)
        {
            MyImageButton.IsEnabled = isVisible;
            MyPlayButton.IsEnabled = isVisible;
            MyClearButton.IsEnabled = isVisible;
            MyUndoButton.IsEnabled = isVisible;
            MyCheckButton.IsEnabled = isVisible;
            MyRandomButton.IsEnabled = isVisible;
            MySymbolsButton.IsEnabled = isVisible;
        }

        private void EnableTechniqueButtons(bool isVisible)
        {
            MyReturnButton.IsEnabled = isVisible;

            MySymbolCorrectnessPlayButton.IsEnabled = isVisible;

            MyStrokeCountPlayButton.IsEnabled = isVisible;
            MyStrokeOrderPlayButton.IsEnabled = isVisible;
            MyStrokeDirectionPlayButton.IsEnabled = isVisible;
            MyStrokeSpeedTestPlayButton.IsEnabled = isVisible;
        }

        #endregion

        #region Text Tap Interactions

        private void MySummaryTitleText_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            MySummaryGrid.Visibility = Visibility.Visible;
            MyStructureGrid.Visibility = Visibility.Collapsed;
            MyTechniqueGrid.Visibility = Visibility.Collapsed;
        }

        private void MyTechniqueTitleText_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            MySummaryGrid.Visibility = Visibility.Collapsed;
            MyStructureGrid.Visibility = Visibility.Collapsed;
            MyTechniqueGrid.Visibility = Visibility.Visible;
        }

        private void MyStructureTitleText_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            MySummaryGrid.Visibility = Visibility.Collapsed;
            MyStructureGrid.Visibility = Visibility.Visible;
            MyTechniqueGrid.Visibility = Visibility.Collapsed;
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

        private StructureRecognizer myStructureRecognizer;
        private TechniqueRecognizer myTechniqueRecognizer;

        private List<StorageFile> myImageFiles;
        private List<StorageFile> myTemplateFiles;
        private List<Sketch> myTemplates;

        public InkDrawingAttributes StrokeVisuals = new InkDrawingAttributes() { Color = Colors.Red, IgnorePressure = true, PenTip = PenTipShape.Circle, Size = new Size(10, 10) };

        public readonly string IMAGES_PATH = @"Assets\Images";
        public readonly string TEMPLATES_PATH = @"Assets\Templates";

        public readonly string PROMPT_TEXT = "Please draw the following symbol: ";

        public readonly SolidColorBrush CORRECT_BRUSH = new SolidColorBrush(Colors.Green);
        public readonly SolidColorBrush INCORRECT_BRUSH = new SolidColorBrush(Colors.Red);
        //public readonly SolidColorBrush SOLUTION_BRUSH = new SolidColorBrush(Colors.Black) { Opacity = 0.8, };

        public readonly int POINT_DURATION = 300000;
        public readonly int STROKE_DURATION = 15000000;

        public readonly int LARGE_DOT_SIZE = 50;
        public readonly int SMALL_DOT_SIZE = 30;
        public readonly int LARGE_STROKE_SIZE = 30;
        public readonly int SMALL_STROKE_SIZE = 10;

        #endregion
    }
}
