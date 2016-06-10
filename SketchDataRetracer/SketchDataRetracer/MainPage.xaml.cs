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
            MyCanvas.Children.Remove(myEllipse);
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
            MyCanvas.Children.Remove(myEllipse);
        }

        private void MyPlayButton_Click(object sender, RoutedEventArgs e)
        {
            //
            var strokes = MyInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            var times = myTimeCollection[0];

            // get starting position
            var stroke = strokes[0];
            double startX = stroke.GetInkPoints()[0].Position.X;
            double startY = stroke.GetInkPoints()[0].Position.Y;

            // create traing dot
            myEllipse = new Ellipse();
            myEllipse.Width = 50;
            myEllipse.Height = 50;
            myEllipse.Fill = new SolidColorBrush(Colors.Red);
            Canvas.SetLeft(myEllipse, -20);
            Canvas.SetTop(myEllipse, -20);
            MyCanvas.Children.Add(myEllipse);

            //Temp();

            //
            long initialTime = times[0];

            //
            myEllipse.RenderTransform = new CompositeTransform();
            Storyboard storyboard = new Storyboard();

            // 
            DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();
            KeyTime keyTime;
            EasingDoubleKeyFrame frameX, frameY;
            double x, y;
            for (int i = 0; i < times.Count; ++i)
            {
                keyTime = new TimeSpan(times[i] - initialTime);
                x = stroke.GetInkPoints()[i].Position.X;
                y = stroke.GetInkPoints()[i].Position.Y;

                frameX = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = x };
                frameY = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = y };

                translateXAnimation.KeyFrames.Add(frameX);
                translateYAnimation.KeyFrames.Add(frameY);
            }

            //
            Storyboard.SetTarget(translateXAnimation, myEllipse);
            Storyboard.SetTarget(translateYAnimation, myEllipse);
            Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");

            storyboard.Children.Add(translateXAnimation);
            storyboard.Children.Add(translateYAnimation);

            storyboard.Begin();
        }

        private void Temp()
        {
            myEllipse.RenderTransform = new CompositeTransform();

            Storyboard storyboard = new Storyboard();

            DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();

            //
            KeyTime time1 = new TimeSpan(0, 0, 0); //new TimeSpan(long ticks);
            KeyTime time2 = new TimeSpan(0, 0, 1); //new TimeSpan(long ticks);
            KeyTime time3 = new TimeSpan(0, 0, 2); //new TimeSpan(long ticks);

            EasingDoubleKeyFrame frame1x = new EasingDoubleKeyFrame() { KeyTime = time1, Value = 550 };
            EasingDoubleKeyFrame frame1y = new EasingDoubleKeyFrame() { KeyTime = time1, Value = 50 };

            EasingDoubleKeyFrame frame2x = new EasingDoubleKeyFrame();
            frame2x.KeyTime = time2;
            frame2x.Value = 300;

            EasingDoubleKeyFrame frame2y = new EasingDoubleKeyFrame();
            frame2y.KeyTime = time2;
            frame2y.Value = 400;

            EasingDoubleKeyFrame frame3x = new EasingDoubleKeyFrame();
            frame3x.KeyTime = time3;
            frame3x.Value = 100;

            EasingDoubleKeyFrame frame3y = new EasingDoubleKeyFrame();
            frame3y.KeyTime = time3;
            frame3y.Value = 200;

            translateXAnimation.KeyFrames.Add(frame1x);
            translateXAnimation.KeyFrames.Add(frame2x);
            translateXAnimation.KeyFrames.Add(frame3x);

            translateYAnimation.KeyFrames.Add(frame1y);
            translateYAnimation.KeyFrames.Add(frame2y);
            translateYAnimation.KeyFrames.Add(frame3y);

            Storyboard.SetTarget(translateXAnimation, myEllipse);
            Storyboard.SetTarget(translateYAnimation, myEllipse);

            Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");

            storyboard.Children.Add(translateXAnimation);
            storyboard.Children.Add(translateYAnimation);

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
        private Ellipse myEllipse;

        #endregion
    }
}
