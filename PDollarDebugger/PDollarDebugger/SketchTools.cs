﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace PDollarDebugger
{
    public class SketchTools
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
                InkStroke newStroke = builder.CreateStroke(newPoints);
                newStroke.DrawingAttributes = stroke.DrawingAttributes;
                newStrokesCollection.Add(newStroke);
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

        public static double Distance(Sketch alphaSketch, Sketch betaSketch)
        {
            //
            double distances = 0.0;

            // get the alpha and beta points from their respective strokes
            List<InkPoint> alphaPoints = new List<InkPoint>();
            List<InkPoint> betaPoints = new List<InkPoint>();
            foreach (InkStroke stroke in alphaSketch.Strokes)
            {
                alphaPoints.AddRange(stroke.GetInkPoints().ToList());
            }
            foreach (var stroke in betaSketch.Strokes)
            {
                betaPoints.AddRange(stroke.GetInkPoints().ToList());
            }

            // iterate through each alpha point
            var pairs = new List<Tuple<InkPoint, InkPoint>>();
            double minDistance, weight, distance;
            int index;
            InkPoint minPoint = betaPoints[0];
            foreach (var alphaPoint in alphaPoints)
            {
                minDistance = Double.MaxValue;

                // iterate through each beta point to find the min beta point to the alpha point
                index = 1;
                foreach (var betaPoint in betaPoints)
                {
                    distance = Distance(alphaPoint, betaPoint);

                    // update the min distance and min point
                    if (minDistance > distance)
                    {
                        minDistance = distance;
                        minPoint = betaPoint;
                    }
                }

                // update distance between alpha and beta point lists
                weight = 1 - ((index - 1) / alphaPoints.Count);
                distances += minDistance * weight;

                // pair the alpha point to the min beta point and remove min beta point from list of beta points
                pairs.Add(new Tuple<InkPoint, InkPoint>(alphaPoint, minPoint));
                betaPoints.Remove(minPoint);
            }

            //
            return distances;
        }

        public static double Distance(InkPoint a, InkPoint b)
        {
            double distance = Math.Sqrt((b.Position.X - a.Position.X) * (b.Position.X - a.Position.X) + (b.Position.Y - a.Position.Y) * (b.Position.Y - a.Position.Y));

            return distance;
        }
    }
}
