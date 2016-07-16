﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace Srl
{
public class SketchTools
    {
        public static async Task<Sketch> XmlToSketch(StorageFile file)
        {
            return await XmlToSketch(file, new InkDrawingAttributes());
        }

        public static async Task<Sketch> XmlToSketch(StorageFile file, InkDrawingAttributes attributes)
        {
            // create a new XML document
            // get the text from the XML file
            // load the file's text into an XML document 
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            // 
            string label = document.Root.Attribute("label").Value;
            double frameMinX = Double.Parse(document.Root.Attribute("frameMinX").Value);
            double frameMinY = Double.Parse(document.Root.Attribute("frameMinY").Value);
            double frameMaxX = Double.Parse(document.Root.Attribute("frameMaxX").Value);
            double frameMaxY = Double.Parse(document.Root.Attribute("frameMaxY").Value);

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

            Sketch sketch = new Sketch(label, strokesCollection, timesCollection, frameMinX, frameMinY, frameMaxX, frameMaxY);
            return sketch;
        }

        public static async void SketchToXml(StorageFile file, string label, List<InkStroke> strokeCollection, List<List<long>> timeCollection, double frameMinX, double frameMinY, double frameMaxX, double frameMaxY)
        {
            // create the string writer as the streaming source of the XML data
            string output = "";
            using (StringWriter stringWriter = new StringWriter())
            {
                // set the string writer as the streaming source for the XML writer
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                {
                    // <xml>
                    xmlWriter.WriteStartDocument();

                    // <sketch>
                    xmlWriter.WriteStartElement("sketch");
                    xmlWriter.WriteAttributeString("label", label);
                    xmlWriter.WriteAttributeString("frameMinX", "" + frameMinX);
                    xmlWriter.WriteAttributeString("frameMinY", "" + frameMinY);
                    xmlWriter.WriteAttributeString("frameMaxX", "" + frameMaxX);
                    xmlWriter.WriteAttributeString("frameMaxY", "" + frameMaxY);

                    // set up the required variables
                    List<InkStroke> strokes = strokeCollection;
                    List<InkPoint> points;
                    List<long> times;
                    InkPoint point;
                    long time;

                    // iterate through each stroke
                    for (int i = 0; i < strokes.Count; ++i)
                    {
                        // <stroke>
                        xmlWriter.WriteStartElement("stroke");

                        // get the current stroke's points and times
                        points = strokes[i].GetInkPoints().ToList();
                        times = timeCollection[i];

                        //
                        while (points.Count != times.Count)
                        {
                            if (points.Count > times.Count) { points.RemoveAt(points.Count - 1); }
                            else if (times.Count > points.Count) { times.RemoveAt(times.Count - 1); }
                        }

                        //
                        for (int j = 0; j < points.Count; ++j)
                        {
                            point = points[j];
                            time = times[j];  // TODO: FIX!

                            // <point>
                            xmlWriter.WriteStartElement("point");

                            xmlWriter.WriteAttributeString("x", "" + point.Position.X);
                            xmlWriter.WriteAttributeString("y", "" + point.Position.Y);
                            xmlWriter.WriteAttributeString("time", "" + times[j]);

                            // </point>
                            xmlWriter.WriteEndElement();
                        }


                        // </stroke>
                        xmlWriter.WriteEndElement();
                    }

                    // </sketch>
                    xmlWriter.WriteEndElement();

                    // </xml>
                    xmlWriter.WriteEndDocument();
                }

                output = stringWriter.ToString();
            }

            await FileIO.WriteTextAsync(file, output);
        }

        public static Sketch Clone(Sketch sketch)
        {
            //
            string label = sketch.Label;

            //
            List<InkStroke> newStrokesCollection = Clone(sketch.Strokes);

            //
            List<List<long>> newTimesCollection = new List<List<long>>();
            foreach (List<long> times in sketch.Times)
            {
                List<long> newTimes = new List<long>();
                foreach (long time in times)
                {
                    newTimes.Add(time);
                }
                newTimesCollection.Add(newTimes);
            }

            //
            double frameMinX = sketch.FrameMinX;
            double frameMinY = sketch.FrameMinY;
            double frameMaxX = sketch.FrameMaxX;
            double frameMaxY = sketch.FrameMaxY;
            return new Sketch(label, newStrokesCollection, newTimesCollection, frameMinX, frameMinY, frameMaxX, frameMaxY);
        }

        public static InkStroke Clone(InkStroke stroke)
        {
            List<InkStroke> newStrokesCollection = new List<InkStroke>();
            InkStrokeBuilder builder = new InkStrokeBuilder();

            List<Point> newPoints = new List<Point>();
            foreach (InkPoint point in stroke.GetInkPoints())
            {
                Point newPoint = new Point(point.Position.X, point.Position.Y);
                newPoints.Add(newPoint);
            }
            InkStroke newStroke = builder.CreateStroke(newPoints);
            newStroke.DrawingAttributes = stroke.DrawingAttributes;

            return newStroke;
        }

        public static List<InkStroke> Clone(List<InkStroke> strokes)
        {
            List<InkStroke> newStrokesCollection = new List<InkStroke>();
            foreach (InkStroke stroke in strokes)
            {
                InkStroke newStroke = Clone(stroke);
                newStrokesCollection.Add(newStroke);
            }

            return newStrokesCollection;
        }

        public static double Distance(Sketch alphaSketch, Sketch betaSketch)
        {
            // initialize the distance
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

        public static InkStroke Reverse(InkStroke stroke)
        {
            // clone the original stroke
            InkStroke original = Clone(stroke);

            // get the raw points from the original ink stroke
            List<Point> points = new List<Point>();
            foreach (InkPoint originalPoint in original.GetInkPoints())
            {
                Point point = new Point(originalPoint.Position.X, originalPoint.Position.Y);
                points.Add(point);
            }

            // reverse the points and create the reverse stroke
            points.Reverse();
            InkStrokeBuilder builder = new InkStrokeBuilder();
            InkStroke reverse = builder.CreateStroke(points);

            return reverse;
        }

        public static double PairwiseDistance(InkStroke stroke, InkStroke other)
        {
            //
            List<InkPoint> strokePoints = new List<InkPoint>(stroke.GetInkPoints());
            List<InkPoint> otherPoints = new List<InkPoint>(other.GetInkPoints());

            //
            int strokeCount = strokePoints.Count;
            int otherCount = otherPoints.Count;
            int count = strokeCount < otherCount ? strokeCount : otherCount;

            //
            double distances = 0.0;
            for (int i = 0; i < count; ++i)
            {
                InkPoint strokePoint = strokePoints[i];
                InkPoint otherPoint = otherPoints[i];

                double distance = Distance(strokePoint, otherPoint);
                distances += distance;
            }

            return distances;
        }
    }
}