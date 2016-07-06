using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace PaulAnimationViewer
{
    public class Helper
    {
        public static void Trace(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection)
        {
            // offset and shift the times
            // that is, offset all the times so that the first time is zeroed
            // and shift the times so that the delay between each point is one tick
            List<List<long>> newTimesCollection = new List<List<long>>();
            //int tick = 200000;
            int tick = 20000;
            long offset = timesCollection[0][0];
            int time = 0;
            for (int i = 0; i < timesCollection.Count; ++i)
            {
                List<long> times = new List<long>();
                for (int j = 0; j < timesCollection[i].Count; ++j)
                {
                    time += tick;
                    times.Add(time);
                }

                newTimesCollection.Add(times);
            }

            // iterate through each stroke
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                // get the current stroke and times
                InkStroke stroke = strokesCollection[i];
                List<long> times = newTimesCollection[i];
                int pointsCount = stroke.GetInkPoints().Count;
                int count = pointsCount < times.Count ? pointsCount : times.Count;

                // get the tracer's starting position
                double startX = stroke.GetInkPoints()[0].Position.X;
                double startY = stroke.GetInkPoints()[0].Position.Y;

                // create the stroke's corresponding tracer
                Ellipse tracer = new Ellipse()
                {
                    Width = 50,
                    Height = 50,
                    Fill = new SolidColorBrush(Colors.Green)
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
    }
}
