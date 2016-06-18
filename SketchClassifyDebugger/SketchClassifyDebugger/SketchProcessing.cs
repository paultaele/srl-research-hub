using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace SketchClassifyDebugger
{
    public class SketchProcessing
    {
        public static async Task<Sketch> ReadXml(StorageFile file)
        {
            return await ReadXml(file, new InkDrawingAttributes());
        }

        public static async Task<Sketch> ReadXml(StorageFile file, InkDrawingAttributes attributes)
        {
            // create a new XML document
            // get the text from the XML file
            // load the file's text into an XML document 
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            // 
            string label = document.Root.Attribute("label").Value;

            // itereate through each stroke element
            InkStrokeBuilder builder = new InkStrokeBuilder();
            InkStroke stroke;
            List<InkStroke> strokesCollection = new List<InkStroke>();
            List<List<long>> timesCollection = new List<List<long>>();
            foreach (XElement element in document.Root.Elements())
            {
                // initialize the point and time lists
                List<Point> points = new List<Point>();
                List<long> times = new List<long>();

                // iterate through each point element
                double x, y;
                Point point;
                long time;
                foreach (XElement pointElement in element.Elements())
                {
                    x = Double.Parse(pointElement.Attribute("x").Value);
                    y = Double.Parse(pointElement.Attribute("y").Value);
                    point = new Point(x, y);
                    time = Int64.Parse(pointElement.Attribute("time").Value);

                    points.Add(point);
                    times.Add(time);
                }

                //
                stroke = builder.CreateStroke(points);
                stroke.DrawingAttributes = attributes;

                //
                strokesCollection.Add(stroke);
                timesCollection.Add(times);
            }

            Sketch sketch = new Sketch(label, strokesCollection, timesCollection);
            return sketch;
        }

        public static Sketch Clone(Sketch mySketch)
        {
            //
            string label = mySketch.Label;

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

            return new Sketch(label, newStrokesCollection, newTimesCollection);
        }
    }
}
