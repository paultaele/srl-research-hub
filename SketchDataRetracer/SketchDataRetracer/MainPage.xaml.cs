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

namespace SketchDataRetracer
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
            InitializeInkCanvas();

            //
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            //
            MyInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            MyInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
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
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            myTimeCollection = new List<List<long>>();
            DateTimeOffset = 0;
            MyCanvas.Children.Clear();
        }

        private void MyUndoButton_Click(object sender, RoutedEventArgs e)
        {
            //
            IReadOnlyList<InkStroke> strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            //
            if (strokes.Count == 0)
            {
                DateTimeOffset = 0;
                return;
            }

            //
            strokes[strokes.Count - 1].Selected = true;
            MyInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();

            //
            myTimeCollection.RemoveAt(myTimeCollection.Count - 1);

            //
            MyCanvas.Children.Clear();
        }

        private void MyPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count == 0) { return; }

            Play2();
        }

        private void Play2()
        {
            // clear the canvas
            MyCanvas.Children.Clear();

            //
            List<InkStroke> strokesCollection = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList();
            List<List<long>> timesCollection = myTimeCollection;

            //
            long offsetTime = timesCollection[0][0];

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

                // create the stroke's current tracer
                Ellipse ellipse = new Ellipse()
                {
                    Width = 30,
                    Height = 30,
                    Fill = new SolidColorBrush(Colors.Red)
                };

                // add the tracer to the canvas
                Canvas.SetLeft(ellipse, -ellipse.Width / 2);
                Canvas.SetTop(ellipse, -ellipse.Height / 2);
                MyCanvas.Children.Add(ellipse);

                // initialize the storyboard and animation
                ellipse.RenderTransform = new CompositeTransform();
                Storyboard storyboard = new Storyboard();
                DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                // create the tracer's translation animation
                KeyTime keyTime;
                EasingDoubleKeyFrame frameX, frameY;
                double x, y;
                for (int j = 0; j < count; ++j)
                {
                    keyTime = new TimeSpan(times[j] - offsetTime);
                    x = stroke.GetInkPoints()[j].Position.X;
                    y = stroke.GetInkPoints()[j].Position.Y;

                    frameX = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = x };
                    frameY = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = y };

                    translateXAnimation.KeyFrames.Add(frameX);
                    translateYAnimation.KeyFrames.Add(frameY);
                }

                // update offset time to offset drawing break between current and previous stroke
                long firstTime = times[0] - offsetTime;
                long lastTime = times[times.Count - 1] - offsetTime;

                //
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(firstTime), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(firstTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 0 });

                // assign the animations to their components
                Storyboard.SetTarget(translateXAnimation, ellipse);
                Storyboard.SetTarget(translateYAnimation, ellipse);
                Storyboard.SetTarget(fadeAnimation, ellipse);

                // assign the animation to their behavior
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

        private void Play()
        {
            // clear the canvas
            MyCanvas.Children.Clear();

            //
            var strokesCollection = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes().ToList();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {

            }

            // create the tracer
            Ellipse ellipse = new Ellipse()
            {
                Width = 30,
                Height = 30,
                Fill = new SolidColorBrush(Colors.Red)
            };
            Canvas.SetLeft(ellipse, -ellipse.Width / 2);
            Canvas.SetTop(ellipse, -ellipse.Height / 2);
            MyCanvas.Children.Add(ellipse);

            //
            var stroke = strokesCollection[0];
            var times = myTimeCollection[0];

            //
            int pointsCount = stroke.GetInkPoints().Count;
            int count = pointsCount < times.Count ? pointsCount : times.Count;

            //
            double startX = stroke.GetInkPoints()[0].Position.X;
            double startY = stroke.GetInkPoints()[0].Position.Y;

            //
            long initialTime = times[0];

            //
            ellipse.RenderTransform = new CompositeTransform();
            Storyboard storyboard = new Storyboard();

            // 
            DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames fadeoutAnimation = new DoubleAnimationUsingKeyFrames();

            //
            KeyTime keyTime;
            EasingDoubleKeyFrame frameX, frameY;
            double x, y;
            for (int j = 0; j < count; ++j)
            {
                keyTime = new TimeSpan(times[j] - initialTime);
                x = stroke.GetInkPoints()[j].Position.X;
                y = stroke.GetInkPoints()[j].Position.Y;

                frameX = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = x };
                frameY = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = y };

                translateXAnimation.KeyFrames.Add(frameX);
                translateYAnimation.KeyFrames.Add(frameY);
            }

            //
            long lastTime = times[times.Count - 1] - initialTime;
            KeyTime visibleTime = new TimeSpan(lastTime);
            KeyTime invisibleTime = new TimeSpan(lastTime + 100);
            EasingDoubleKeyFrame visible = new EasingDoubleKeyFrame() { KeyTime = visibleTime, Value = 1 };
            EasingDoubleKeyFrame invisible = new EasingDoubleKeyFrame() { KeyTime = invisibleTime, Value = 0 };
            fadeoutAnimation.KeyFrames.Add(visible);
            fadeoutAnimation.KeyFrames.Add(invisible);

            //
            Storyboard.SetTarget(translateXAnimation, ellipse);
            Storyboard.SetTarget(translateYAnimation, ellipse);
            Storyboard.SetTarget(fadeoutAnimation, ellipse);

            //
            Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
            Storyboard.SetTargetProperty(fadeoutAnimation, "(UIElement.Opacity)");

            //
            storyboard.Children.Add(translateXAnimation);
            storyboard.Children.Add(translateYAnimation);
            storyboard.Children.Add(fadeoutAnimation);

            storyboard.Begin();
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
