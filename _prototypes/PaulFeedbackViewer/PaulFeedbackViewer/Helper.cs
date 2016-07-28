using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace PaulFeedbackViewer
{
    public class Helper
    {
        public static List<Storyboard> DisplaySymbol(Canvas canvas, List<InkStroke> strokesCollection, Brush brush, double size, int duration)
        {
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
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });               // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(duration), Value = 1 });   // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(duration), Value = 0 });   // inivisible

                    // assign the animations to the animator
                    Storyboard.SetTarget(fadeAnimation, dot);

                    // assign the animations to their behavior's properties
                    Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                    // add the animations to the storyboard
                    storyboard.Children.Add(fadeAnimation);

                    // add the storyboard to the collection
                    storyboards.Add(storyboard);
                }
            }

            return storyboards;
        }
    }
}
