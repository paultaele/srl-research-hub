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
            // initialize list of storyboards
            List<Storyboard> storyboards = new List<Storyboard>();

            // iterate through each stroke
            for (int i = 0; i < strokesCollection.Count; ++i)
            {
                // get the current stroke's points
                List<InkPoint> points = new List<InkPoint>(strokesCollection[i].GetInkPoints());

                // iterate through each point
                for (int j = 0; j < points.Count - 1; ++j)
                {
                    // get the start and end point
                    InkPoint startPoint = points[j];
                    InkPoint endPoint = points[j + 1];

                    Line segment = new Line()
                    {
                        StrokeThickness = size,
                        Fill = brush,
                        Stroke = brush,
                        X1 = startPoint.Position.X,
                        Y1 = startPoint.Position.Y,
                        X2 = endPoint.Position.X,
                        Y2 = endPoint.Position.Y,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                    };

                    // add the dot to the canvas
                    canvas.Children.Add(segment);

                    // initialize the storyboard and animations
                    segment.RenderTransform = new CompositeTransform();
                    Storyboard storyboard = new Storyboard();
                    DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                    // --------------------------------------------------

                    // create the animator's fade animations
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });               // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(duration), Value = 1 });   // visible
                    fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(duration), Value = 0 });   // inivisible

                    // assign the animations to the animator
                    Storyboard.SetTarget(fadeAnimation, segment);

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
