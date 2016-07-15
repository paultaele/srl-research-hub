using Srl;
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
        public static List<Storyboard> DisplayPaths(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration)
        {
            // set the initial time
            long time = 0;

            // iterate through each stroke
            List<Storyboard> storyboards = new List<Storyboard>();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                List<InkPoint> points = new List<InkPoint>(strokesCollection[i].GetInkPoints());
                for (int j = 0; j < points.Count; ++j)
                {
                    InkPoint point = points[j];

                    // set the visuals of the stroke's corresponding dot
                    Ellipse dot = new Ellipse()
                    {
                        Width = size,
                        Height = size,
                        Fill = brush,
                        Stroke = brush,
                        //StrokeThickness = 5,
                    };

                    // add the dot to the canvas
                    // note: the tracer is moved up and left its radius to center
                    Canvas.SetLeft(dot, (-dot.Width / 2) + point.Position.X);
                    Canvas.SetTop(dot, (-dot.Height / 2) + point.Position.Y);
                    canvas.Children.Add(dot);

                    // initialize the storyboard and animations
                    dot.RenderTransform = new CompositeTransform();
                    Storyboard storyboard = new Storyboard();
                    DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                    // --------------------------------------------------

                    // create the animator's fade animations
                    long appear = time;
                    long disappear = time + duration;
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });               // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });               // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 0 });            // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 1 });            // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 1 });   // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 0 });   // inivisible

                    // assign the animations to the animator
                    Storyboard.SetTarget(fadeAnimation, dot);

                    // assign the animations to their behavior's properties
                    Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                    // add the animations to the storyboard
                    storyboard.Children.Add(fadeAnimation);

                    // add the storyboard to the collection
                    storyboards.Add(storyboard);
                }

                // update the time for the next stroke
                time += duration;
            }

            return storyboards;
        }

        public static List<Storyboard> DisplayPaths(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration, Sketch sketch)
        {
            int newDuration = duration * sketch.Strokes.Count / strokesCollection.Count;

            return DisplayPaths(canvas, strokesCollection, brush, size, newDuration);
        }

        public static List<Storyboard> DisplayEndpoints(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration)
        {
            // set the initial time
            long time = 0;

            // iterate through each stroke
            List<Storyboard> storyboards = new List<Storyboard>();
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                List<InkPoint> points = new List<InkPoint>(strokesCollection[i].GetInkPoints());
                for (int j = 0; j < points.Count; j = j + points.Count - 1)
                {
                    InkPoint point = points[j];

                    // set the visuals of the stroke's corresponding dot
                    Ellipse dot = new Ellipse()
                    {
                        Width = size,
                        Height = size,
                        Fill = brush,
                        Stroke = new SolidColorBrush(Colors.DarkGray),
                        StrokeThickness = 5,
                    };

                    // add the dot to the canvas
                    // note: the tracer is moved up and left its radius to center
                    Canvas.SetLeft(dot, (-dot.Width / 2) + point.Position.X);
                    Canvas.SetTop(dot, (-dot.Height / 2) + point.Position.Y);
                    canvas.Children.Add(dot);

                    // initialize the storyboard and animations
                    dot.RenderTransform = new CompositeTransform();
                    Storyboard storyboard = new Storyboard();
                    DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                    // --------------------------------------------------

                    // create the animator's fade animations
                    long appear = time;
                    long disappear = time + duration;
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });               // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });               // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 0 });            // inivisible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(appear), Value = 1 });            // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 1 });   // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(disappear), Value = 0 });   // inivisible

                    // assign the animations to the animator
                    Storyboard.SetTarget(fadeAnimation, dot);

                    // assign the animations to their behavior's properties
                    Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                    // add the animations to the storyboard
                    storyboard.Children.Add(fadeAnimation);

                    // add the storyboard to the collection
                    storyboards.Add(storyboard);
                }

                // update the time for the next stroke
                time += duration;
            }

            return storyboards;
        }

        public static List<Storyboard> DisplayEndpoints(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration, Sketch sketch)
        {
            int newDuration = duration * sketch.Strokes.Count / strokesCollection.Count;

            return DisplayEndpoints(canvas, strokesCollection, brush, size, newDuration);
        }
    }
}
