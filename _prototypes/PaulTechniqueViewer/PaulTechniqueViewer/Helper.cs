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

namespace PaulTechniqueViewer
{
    public class Helper
    {
        public static List<Storyboard> DisplayPaths(Canvas canvas, List<InkStroke> strokesCollection, List<List<long>> timesCollection, Brush brush, int duration)
        {
            long time = 0;

            // iterate through each stroke
            List<Storyboard> storyboards = new List<Storyboard>();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                // set the visuals of the stroke's corresponding tracer
                Ellipse animator = new Ellipse()
                {
                    Width = 50,
                    Height = 50,
                    Fill = brush,
                    Stroke = new SolidColorBrush(Colors.DarkGray),
                    StrokeThickness = 5,
                };

                // add the tracer to the canvas
                // note: the tracer is moved up and left its radius to center
                double x = strokesCollection[i].GetInkPoints()[0].Position.X;
                double y = strokesCollection[i].GetInkPoints()[0].Position.Y;
                Canvas.SetLeft(animator, (-animator.Width / 2) + x);
                Canvas.SetTop(animator, (-animator.Height / 2) + y);
                canvas.Children.Add(animator);

                // initialize the storyboard and animations
                animator.RenderTransform = new CompositeTransform();
                Storyboard storyboard = new Storyboard();
                DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                // --------------------------------------------------

                // get the current stroke and times
                InkStroke stroke = strokesCollection[i];
                List<long> times = timesCollection[i];
                int pointsCount = stroke.GetInkPoints().Count;
                int count = pointsCount < times.Count ? pointsCount : times.Count;

                // create the animator's fade animations
                //long firstTime = times[0];
                //long lastTime = times[times.Count - 1];
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });           // visible
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });           // inivisible
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(time), Value = 0 });   // inivisible
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(time), Value = 1 });   // visible
                //fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 1 });    // visible
                //fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(lastTime), Value = 0 });    // inivisible

                time += duration;

                // assign the animations to the animator
                Storyboard.SetTarget(fadeAnimation, animator);

                // assign the animations to their behavior's properties
                Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                // add the animations to the storyboard
                storyboard.Children.Add(fadeAnimation);

                // add the storyboard to the collection
                storyboards.Add(storyboard);
            }

            return storyboards;
        }
    }
}
