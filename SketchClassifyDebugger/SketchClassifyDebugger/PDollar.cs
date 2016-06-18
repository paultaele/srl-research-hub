using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Input.Inking;

namespace SketchClassifyDebugger
{
    public class PDollar
    {
        public PDollar(int n, double size, Point k)
        {
            N = n;
            Size = size;
            K = k;
        }

        public void Train(List<StorageFile> files)
        {
            List<SketchPair> templates = new List<SketchPair>();
            foreach (StorageFile file in files)
            {
                Sketch original = null;
                Task task = Task.Run(async () => { original = await SketchProcessing.ReadXml(file); });
                task.Wait();

                Sketch transformed = Normalize(SketchProcessing.Clone(original));
                
                templates.Add(new SketchPair(original, transformed));
            }
            
            myTemplates = templates;
        }

        public void Run(Sketch original)
        {
            //
            Sketch input = SketchProcessing.Clone(original);
            input = Normalize(input);

            //
            myLabels = new List<string>();
            myScoreEntries = new Dictionary<SketchPair, double>();
            myResults = new List<SketchPair>();

            //
            List<Tuple<SketchPair, double>> pairs = new List<Tuple<SketchPair, double>>();
            foreach (SketchPair template in myTemplates)
            {
                // calculate the template's score
                double distance1 = Distance(input, template.Transformed);
                double distance2 = Distance(template.Transformed, input);
                double distance = Math.Min(distance1, distance2);
                double score = ToScore(distance);

                // add the label, template+score pair, and score dictionary
                pairs.Add(new Tuple<SketchPair, double>(template, score));
            }

            //
            pairs.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            foreach (var pair in pairs)
            {
                //
                SketchPair t = pair.Item1;
                double s = pair.Item2;

                //
                myResults.Add(t);
                myLabels.Add((string)t.Original.Label);
                myScoreEntries.Add(t, s);
            }
        }

        public List<SketchPair> Results() { return myResults; }

        #region Helper Methods

        public Sketch Normalize(Sketch input)
        {
            input = SketchTransformation.Resample(input, N);
            input = SketchTransformation.ScaleSquare(input, Size);
            input = SketchTransformation.TranslateCentroid(input, K);

            return input;
        }

        private double Distance(Sketch alphaSketch, Sketch betaSketch)
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

        protected double ToScore(double distance)
        {
            return 100.0 - (distance / (0.5 * (Math.Sqrt(Size * Size + Size * Size))));
        }

        #endregion

        #region Properties

        private int N { get; set; }
        private double Size { get; set; }
        private Point K { get; set; }

        #endregion

        #region Fields

        private List<SketchPair> myTemplates;

        private List<string> myLabels;
        private Dictionary<SketchPair, double> myScoreEntries;
        private List<SketchPair> myResults;

        #endregion
    }
}
