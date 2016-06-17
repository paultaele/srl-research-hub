using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Inking;

namespace SketchTransformDebugger
{
    public class SketchTransformation
    {
        public static Sketch Resample(Sketch sketch, int n)
        {
            // set the variable for point spacing
            // initialize the variable for total distance
            // initialize list for new strokes
            double I = PathLength(sketch.Strokes) / (n - 1);
            double D = 0.0;
            List<InkStroke> newStrokes = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();

            // iterate through each stroke points in a list of strokes
            int pointCount = 0;
            foreach (InkStroke stroke in sketch.Strokes)
            {
                // initialize list of resampled stroke points
                // add the first stroke point
                List<InkPoint> points = stroke.GetInkPoints().ToList();
                List<Point> newPoints = new List<Point>();
                newPoints.Add(new Point(points[0].Position.X, points[0].Position.Y));
                ++pointCount;

                //
                bool isDone = false;
                for (int i = 1; i < points.Count(); ++i)
                {
                    double d = Distance(points[i - 1], points[i]);
                    if (D + d >= I)
                    {
                        double qx = points[i - 1].Position.X + ((I - D) / d) * (points[i].Position.X - points[i - 1].Position.X);
                        double qy = points[i - 1].Position.Y + ((I - D) / d) * (points[i].Position.Y - points[i - 1].Position.Y);
                        Point q = new Point(qx, qy);

                        if (pointCount < n - 1)
                        {
                            newPoints.Add(q);
                            ++pointCount;

                            points.Insert(i, new InkPoint(q, 0.5f));
                            D = 0.0;
                        }
                        else
                        {
                            isDone = true;
                        }
                    }
                    else
                    {
                        D += d;
                    }

                    if (isDone)
                    {
                        break;
                    }
                }
                D = 0.0;

                //
                InkStroke newStroke = builder.CreateStroke(newPoints);
                newStrokes.Add(newStroke);
            }

            //
            List<List<long>> newTimesCollection = new List<List<long>>();
            foreach (InkStroke stroke in newStrokes)
            {
                ;
            }

            //
            Sketch newSketch = new Sketch(newStrokes, sketch.Times);
            return newSketch;
        }

        public static Sketch ScaleSquare(Sketch sketch, double size)
        {
            //
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            foreach (InkStroke stroke in sketch.Strokes)
            {
                foreach (InkPoint point in stroke.GetInkPoints())
                {
                    double x = point.Position.X;
                    double y = point.Position.Y;

                    if (x < minX) { minX = x; }
                    if (y < minY) { minY = y; }
                    if (x > maxX) { maxX = x; }
                    if (y > maxY) { maxY = y; }
                }
            }
            Rect B = new Rect(new Point(minX, minY), new Point(maxX, maxY));

            // 
            List<InkStroke> newStrokes = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();
            foreach (InkStroke stroke in sketch.Strokes)
            {
                List<Point> newPoints = new List<Point>();
                foreach (InkPoint point in stroke.GetInkPoints())
                {
                    double qx = point.Position.X * size / B.Width;
                    double qy = point.Position.Y * size / B.Height;
                    Point q = new Point(qx, qy);
                    newPoints.Add(q);
                }

                //
                InkStroke newStroke = builder.CreateStroke(newPoints);
                newStrokes.Add(newStroke);
            }

            //
            Sketch newSketch = new Sketch(newStrokes, sketch.Times);
            return newSketch;
        }

        public static Sketch ScaleProportional(Sketch sketch, double size)
        {
            //
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            foreach (InkStroke stroke in sketch.Strokes)
            {
                foreach (InkPoint point in stroke.GetInkPoints())
                {
                    double x = point.Position.X;
                    double y = point.Position.Y;

                    if (x < minX) { minX = x; }
                    if (y < minY) { minY = y; }
                    if (x > maxX) { maxX = x; }
                    if (y > maxY) { maxY = y; }
                }
            }
            Rect B = new Rect(new Point(minX, minY), new Point(maxX, maxY));
            double scale = B.Height > B.Width ? size / B.Height : size / B.Width;

            // get the offset
            double xoffset = Double.MaxValue;
            double yoffset = Double.MaxValue;
            foreach (InkStroke stroke in sketch.Strokes)
            {
                foreach (InkPoint point in stroke.GetInkPoints())
                {
                    if (point.Position.X < xoffset) xoffset = point.Position.X;
                    if (point.Position.Y < yoffset) yoffset = point.Position.Y;
                }
            }

            // 
            List<InkStroke> newStrokes = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();
            foreach (InkStroke stroke in sketch.Strokes)
            {
                List<Point> newPoints = new List<Point>();
                foreach (InkPoint point in stroke.GetInkPoints())
                {
                    double x = ((point.Position.X - xoffset) * scale) + xoffset;
                    double y = ((point.Position.Y - yoffset) * scale) + yoffset;
                    newPoints.Add(new Point(x, y));
                }

                //
                InkStroke newStroke = builder.CreateStroke(newPoints);
                newStrokes.Add(newStroke);
            }

            //
            Sketch newSketch = new Sketch(newStrokes, sketch.Times);
            return newSketch;
        }

        public static Sketch TranslateCentroid(Sketch sketch, Point k)
        {
            //
            Point c = Centroid(sketch.Strokes);
            return Translate(sketch, k, c);
        }

        public static Sketch TranslateMedian(Sketch sketch, Point k)
        {
            //
            Point c = Median(sketch.Strokes);
            return Translate(sketch, k, c);
        }

        private static Sketch Translate(Sketch sketch, Point k, Point c)
        {
            // 
            List<InkStroke> newStrokes = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();
            foreach (InkStroke stroke in sketch.Strokes)
            {
                List<Point> newPoints = new List<Point>();
                foreach (InkPoint point in stroke.GetInkPoints())
                {

                    double qx = point.Position.X + k.X - c.X;
                    double qy = point.Position.Y + k.Y - c.Y;
                    Point q = new Point(qx, qy);
                    newPoints.Add(q);
                }

                //
                InkStroke newStroke = builder.CreateStroke(newPoints);
                newStrokes.Add(newStroke);
            }

            //
            Sketch newSketch = new Sketch(newStrokes, sketch.Times);
            return newSketch;
        }

        private static Point Centroid(List<InkStroke> strokes)
        {
            List<InkPoint> points = new List<InkPoint>();
            foreach (InkStroke stroke in strokes)
            {
                points.AddRange(stroke.GetInkPoints().ToList());
            }

            double meanX = 0.0;
            double meanY = 0.0;
            foreach (InkPoint point in points)
            {
                meanX += point.Position.X;
                meanY += point.Position.Y;
            }
            meanX /= points.Count;
            meanY /= points.Count;

            return new Point(meanX, meanY);
        }

        private static Point Median(List<InkStroke> strokes)
        {
            List<InkPoint> points = new List<InkPoint>();
            foreach (InkStroke stroke in strokes)
            {
                points.AddRange(stroke.GetInkPoints().ToList());
            }

            double minX = Double.MaxValue;
            double minY = Double.MaxValue;
            double maxX = Double.MinValue;
            double maxY = Double.MinValue;
            foreach (InkPoint point in points)
            {
                if (point.Position.X < minX) minX = point.Position.X;
                if (point.Position.Y < minY) minY = point.Position.Y;

                if (point.Position.X > maxX) maxX = point.Position.X;
                if (point.Position.Y > maxY) maxY = point.Position.Y;
            }

            return new Point((minX + maxX) / 2.0, (minY + maxY) / 2.0);
        }

        public static Sketch Clone(Sketch mySketch)
        {
            //
            List<InkStroke> newStrokesCollection = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();
            foreach (InkStroke stroke in mySketch.Strokes)
            {
                List<Point> newPoints = new List<Point>();
                foreach (InkPoint point in stroke.GetInkPoints())
                {
                    Point newPoint = new Point(point.Position.X, point.Position.Y);
                    newPoints.Add(newPoint);
                }
                newStrokesCollection.Add(builder.CreateStroke(newPoints));
            }

            //
            List<List<long>> newTimesCollection = new List<List<long>>();
            foreach (List<long> times in mySketch.Times)
            {
                List<long> newTimes = new List<long>();
                foreach (long time in times)
                {
                    newTimes.Add(time);
                }
                newTimesCollection.Add(newTimes);
            }

            return new Sketch(newStrokesCollection, newTimesCollection);
        }

        public static double PathLength(List<InkStroke> strokes)
        {
            double d = 0.0;
            foreach (InkStroke stroke in strokes)
            {
                d += PathLength(stroke);
            }

            return d;
        }

        public static double PathLength(InkStroke stroke)
        {
            double d = 0.0;
            for (int i = 1; i < stroke.GetInkPoints().Count(); ++i)
            {
                d += Distance(stroke.GetInkPoints()[i - 1], stroke.GetInkPoints()[i]);
            }

            return d;
        }

        public static double Distance(InkPoint a, InkPoint b)
        {
            double distance = Math.Sqrt((b.Position.X - a.Position.X) * (b.Position.X - a.Position.X) + (b.Position.Y - a.Position.Y) * (b.Position.Y - a.Position.Y));

            return distance;
        }
    }

    public class Sketch
    {
        public Sketch(List<InkStroke> strokes, List<List<long>> times)
        {
            Strokes = strokes;
            Times = times;
        }

        public List<InkStroke> Strokes { get; set; }
        public List<List<long>> Times { get; set; }
    }

    public class SketchPair
    {
        public SketchPair(Sketch original, Sketch transformed)
        {
            Original = original;
            Transformed = transformed;
        }

        public SketchPair(Sketch original)
        {
            Original = original;
            Transformed = null;
        }

        public Sketch Original { get; set; }

        public Sketch Transformed { get; set; }
    }
}
