using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SketchPlayer
{
    /// <summary>
    /// A page for drawing, editing, and playing back strokes on a sketching interface.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Initializers

        public MainPage()
        {
            // initialize the components and ink canvas
            InitializeComponent();
            InitializeInkCanvas();

            // maximize the interface window
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            // initialize the stroke event handlers
            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            // initialize the external timing data structure and offset
            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;
        }

        private void InitializeInkCanvas()
        {
            // enable pen, mouse, and touch input
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

        #region Button Interactions

        private void MyClearButton_Click(object sender, RoutedEventArgs e)
        {
            // clear the canvas
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();

            // clear the stroke and timing data
            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;

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
                DateTimeOffset = 0;
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
            // do nothing if there are no strokes on the ink canvas
            if (MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count == 0) { return; }

            // clear the canvas
            MyCanvas.Children.Clear();

            // retrace the strokes
            Canvas canvas = MyCanvas;
            List<InkStroke> strokesCollection = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList();
            Play(MyCanvas, strokesCollection);
        }

        private void MyPlaybackButton_Click(object sender, RoutedEventArgs e)
        {
            // do nothing if there are no strokes on the ink canvas
            if (MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count == 0) { return; }

            // clear the canvas
            MyCanvas.Children.Clear();

            // retrace the strokes
            Canvas canvas = MyCanvas;
            List<InkStroke> strokesCollection = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList();
            Playback(MyCanvas, strokesCollection, myTimeCollection);
        }

        private void Play(Canvas canvas, List<InkStroke> strokesCollection)
        {
            List<List<long>> timesCollection = new List<List<long>>();
            long interval = 0;
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                List<long> times = new List<long>();
                for (int j = 0; j < strokesCollection[i].GetInkPoints().Count; ++j)
                {
                    interval += 125000;
                    times.Add(interval);
                }

                timesCollection.Add(times);
                interval += 1000000;
            }

            Trace(canvas, strokesCollection, timesCollection, false);
        }

        private void Playback(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection)
        {
            Trace(canvas, strokesCollection, timesCollection, true);
        }

        private void Trace(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, bool hasTime)
        {
            if (hasTime)
            {
                // offset and shift the times
                // that is, offset all the times so that the first time is zeroed
                // and shift the times so that the delay between each stroke is one second
                List<List<long>> originalTimesCollection = timesCollection;
                timesCollection = new List<List<long>>();
                long shift = 0;
                long offset = myTimeCollection[0][0];
                for (int i = 0; i < myTimeCollection.Count; ++i)
                {
                    // begin shifting after the first stroke
                    if (i > 0)
                    {
                        long previousLastTime = myTimeCollection[i - 1][myTimeCollection[i - 1].Count - 1];
                        long currentFirstTime = myTimeCollection[i][0];
                        shift += currentFirstTime - previousLastTime - 1000000;
                    }

                    // include the shift and offset values
                    List<long> times = new List<long>();
                    for (int j = 0; j < myTimeCollection[i].Count; ++j)
                    {
                        long time = myTimeCollection[i][j] - shift - offset;
                        times.Add(time);
                    }
                    timesCollection.Add(times);
                }
            }

            // iterate through each stroke
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                // get the current stroke and times
                InkStroke stroke = strokesCollection[i];
                List<long> times = timesCollection[i];
                int pointsCount = stroke.GetInkPoints().Count;
                int count = pointsCount < times.Count ? pointsCount : times.Count;

                // get the tracer's starting position
                double startX = stroke.GetInkPoints()[0].Position.X;
                double startY = stroke.GetInkPoints()[0].Position.Y;

                // create the stroke's corresponding tracer
                Ellipse tracer = new Ellipse()
                {
                    Width = 30,
                    Height = 30,
                    Fill = new SolidColorBrush(Colors.Red)
                };

                // add the tracer to the canvas
                // note: the tracer is moved up and left its radius to center
                Canvas.SetLeft(tracer, -tracer.Width / 2);
                Canvas.SetTop(tracer, -tracer.Height / 2);
                canvas.Children.Add(tracer);

                // initialize the storyboard and animations
                tracer.RenderTransform = new CompositeTransform();
                Storyboard storyboard = new Storyboard();
                DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                // create the tracer's translation animations
                KeyTime keyTime;
                EasingDoubleKeyFrame frameX, frameY;
                double x, y;
                for (int j = 0; j < count; ++j)
                {
                    keyTime = new TimeSpan(times[j]);
                    x = stroke.GetInkPoints()[j].Position.X;
                    y = stroke.GetInkPoints()[j].Position.Y;

                    frameX = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = x };
                    frameY = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = y };

                    translateXAnimation.KeyFrames.Add(frameX);
                    translateYAnimation.KeyFrames.Add(frameY);
                }

                // create the tracer's fade animations
                long firstTime = times[0];
                long lastTime = times[times.Count - 1];
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(firstTime), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(firstTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 0 });

                // assign the animations to the tracer
                Storyboard.SetTarget(translateXAnimation, tracer);
                Storyboard.SetTarget(translateYAnimation, tracer);
                Storyboard.SetTarget(fadeAnimation, tracer);

                // assign the animations to their behavior's properties
                Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
                Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
                Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                // add the animations to the storyboard
                storyboard.Children.Add(translateXAnimation);
                storyboard.Children.Add(translateYAnimation);
                storyboard.Children.Add(fadeAnimation);

                // begin the storyboard
                storyboard.Begin();
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

        #endregion

        #region Helper Methods

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
            long time = DateTime.Now.Ticks - DateTimeOffset;
            myTimes.Add(time);

            // case: stroke has ended
            // complete the stroke
            if (hasEnded)
            {
                myTimeCollection.Add(myTimes);
            }
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
