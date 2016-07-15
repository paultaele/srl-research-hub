using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Inking;

namespace Srl
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
                newStroke.DrawingAttributes = stroke.DrawingAttributes;
                newStrokes.Add(newStroke);
            }

            // 
            List<List<long>> timesCollection = sketch.Times;
            List<List<long>> newTimesCollection = new List<List<long>>();
            for (int i = 0; i < newStrokes.Count; ++i)
            {
                List<long> times = sketch.Times[i];
                List<long> newTimes = new List<long>();
                int oldCount = times.Count;
                int newCount = newStrokes[i].GetInkPoints().Count;

                for (int j = 0; j < newCount; ++j)
                {
                    int index = (int)((double)j / newCount * oldCount);
                    newTimes.Add(times[index]);
                }

                newTimesCollection.Add(newTimes);
            }

            //
            Sketch newSketch = new Sketch(sketch.Label, newStrokes, newTimesCollection, sketch.FrameMinX, sketch.FrameMinY, sketch.FrameMaxX, sketch.FrameMaxY);
            return newSketch;
        }

        public static InkStroke Resample(InkStroke stroke, int n)
        {
            // create dummy times
            List<long> times = new List<long>();
            foreach (InkPoint point in stroke.GetInkPoints()) { times.Add(0); }

            // wrap stroke into sketch and resample
            Sketch sketch = new Sketch("", new List<InkStroke> { stroke }, new List<List<long>> { times }, 0, 0, 0, 0);
            sketch = Resample(sketch, n);

            // return the sole resampled stroke of the sketch
            return sketch.Strokes[0];
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
                newStroke.DrawingAttributes = stroke.DrawingAttributes;
                newStrokes.Add(newStroke);
            }

            //
            double frameMinX = sketch.FrameMinX * size / B.Width;
            double frameMinY = sketch.FrameMinY * size / B.Width;
            double frameMaxX = sketch.FrameMaxX * size / B.Width;
            double frameMaxY = sketch.FrameMaxY * size / B.Width;
            Sketch newSketch = new Sketch(sketch.Label, newStrokes, sketch.Times, frameMinX, frameMinY, frameMaxX, frameMaxY);
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
                newStroke.DrawingAttributes = stroke.DrawingAttributes;
                newStrokes.Add(newStroke);
            }

            //
            double frameMinX = ((sketch.FrameMinX - xoffset) * scale) + xoffset;
            double frameMinY = ((sketch.FrameMinY - yoffset) * scale) + yoffset;
            double frameMaxX = ((sketch.FrameMaxX - xoffset) * scale) + xoffset;
            double frameMaxY = ((sketch.FrameMaxY - yoffset) * scale) + yoffset;
            Sketch newSketch = new Sketch(sketch.Label, newStrokes, sketch.Times, frameMinX, frameMinY, frameMaxX, frameMaxY);
            return newSketch;
        }

        public static Sketch ScaleFrame(Sketch sketch, double size)
        {
            //
            List<InkStroke> frameStrokes = FrameStrokes(sketch.FrameMinX, sketch.FrameMinY, sketch.FrameMaxX, sketch.FrameMaxY);
            List<long> dummyTime = new List<long>();

            //
            for (int i = 0; i < frameStrokes.Count; ++i)
            {
                InkStroke frameStroke = frameStrokes[i];
                sketch.Strokes.Add(frameStroke);
                sketch.Times.Add(dummyTime);
            }

            //
            sketch = ScaleProportional(sketch, size);

            //
            for (int i = 0; i < frameStrokes.Count; ++i)
            {
                sketch.Strokes.RemoveAt(sketch.Strokes.Count - 1);
                sketch.Times.RemoveAt(sketch.Times.Count - 1);
            }

            return sketch;
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

        public static Sketch TranslateFrame(Sketch sketch, Point k)
        {
            //
            List<InkStroke> frameStrokes = FrameStrokes(sketch.FrameMinX, sketch.FrameMinY, sketch.FrameMaxX, sketch.FrameMaxY);
            List<long> dummyTime = new List<long>();

            //
            for (int i = 0; i < frameStrokes.Count; ++i)
            {
                InkStroke frameStroke = frameStrokes[i];
                sketch.Strokes.Add(frameStroke);
                sketch.Times.Add(dummyTime);
            }

            //
            sketch = TranslateMedian(sketch, k);

            //
            for (int i = 0; i < frameStrokes.Count; ++i)
            {
                sketch.Strokes.RemoveAt(sketch.Strokes.Count - 1);
                sketch.Times.RemoveAt(sketch.Times.Count - 1);
            }

            return sketch;
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
                newStroke.DrawingAttributes = stroke.DrawingAttributes;
                newStrokes.Add(newStroke);
            }

            //
            double frameMinX = sketch.FrameMinX + k.X - c.X;
            double frameMinY = sketch.FrameMinY + k.Y - c.Y;
            double frameMaxX = sketch.FrameMaxX + k.X - c.X;
            double frameMaxY = sketch.FrameMaxY + k.Y - c.Y;
            Sketch newSketch = new Sketch(sketch.Label, newStrokes, sketch.Times, frameMinX, frameMinY, frameMaxX, frameMaxY);
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

        private static List<InkStroke> FrameStrokes(double minX, double minY, double maxX, double maxY)
        {
            Point topLeft = new Point(minX, minY);
            Point topRight = new Point(maxX, minY);
            Point bottomLeft = new Point(minX, maxY);
            Point bottomRight = new Point(maxX, maxY);

            InkStrokeBuilder builder = new InkStrokeBuilder();
            InkStroke topStroke = builder.CreateStroke(new List<Point>() { topLeft, topRight });
            InkStroke leftStroke = builder.CreateStroke(new List<Point>() { topLeft, bottomLeft });
            InkStroke rightStroke = builder.CreateStroke(new List<Point>() { topRight, bottomRight });
            InkStroke bottomStroke = builder.CreateStroke(new List<Point>() { bottomLeft, bottomRight });

            List<InkStroke> strokes = new List<InkStroke>();
            strokes.Add(topStroke);
            strokes.Add(leftStroke);
            strokes.Add(rightStroke);
            strokes.Add(bottomStroke);

            return strokes;
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
        public Sketch(string label, List<InkStroke> strokes, List<List<long>> times, double minX, double minY, double maxX, double maxY)
        {
            Label = label;
            Strokes = strokes;
            Times = times;
            FrameMinX = minX;
            FrameMinY = minY;
            FrameMaxX = maxX;
            FrameMaxY = maxY;
        }

        public string Label { get; set; }
        public List<InkStroke> Strokes { get; set; }
        public List<List<long>> Times { get; set; }
        public double FrameMinX { get; set; }
        public double FrameMinY { get; set; }
        public double FrameMaxX { get; set; }
        public double FrameMaxY { get; set; }
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
        public string Label { get { return Original.Label; } }
    }
}
