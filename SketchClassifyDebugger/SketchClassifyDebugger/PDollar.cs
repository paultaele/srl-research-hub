using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SketchClassifyDebugger
{
    public class PDollar
    {
        #region Initializers

        public PDollar(int n, double size, Point k)
        {
            N = n;
            Size = size;
            K = k;
        }

        #endregion

        #region Training

        public void Train(List<Sketch> originalSketches)
        {
            // iterate through each original sketch
            List<Sketch> transformedSketches = new List<Sketch>();
            foreach (Sketch originalSketch in originalSketches)
            {
                // 
                Sketch transformedSketch = Normalize(originalSketch);
                transformedSketches.Add(transformedSketch);
            }

            Train(originalSketches, transformedSketches);
        }

        public void Train(List<Sketch> originalSketches, List<Sketch> transformedSketches)
        {
            myTemplates = new List<SketchPair>();

            for (int i = 0; i < originalSketches.Count; ++i)
            {
                SketchPair template = new SketchPair(originalSketches[i], transformedSketches[i]);
                myTemplates.Add(template);
            }
        }

        #endregion

        #region Running

        public void Run(Sketch originalSketch)
        {
            //
            Sketch transformedSketch = Normalize(originalSketch);
            SketchPair input = new SketchPair(originalSketch, transformedSketch);

            //
            List<Tuple<string, double>> results = new List<Tuple<string, double>>();
            foreach (SketchPair template in myTemplates)
            {
                double distance1 = SketchTools.Distance(input.Transformed, template.Transformed);
                double distance2 = SketchTools.Distance(input.Transformed, template.Transformed);
                double distance = Math.Min(distance1, distance2);

                results.Add(new Tuple<string, double>(template.Transformed.Label, distance));
            }
            results.Sort((x, y) => x.Item2.CompareTo(y.Item2));

            //
            myLabels = new List<string>();
            myScores = new List<double>();
            for (int i = 0; i < results.Count; ++i)
            {
                string label = results[i].Item1;
                double score = results[i].Item2;

                myLabels.Add(label);
                myScores.Add(score);
            }
        }

        #endregion

        #region Helper Methdos

        private Sketch Normalize(Sketch original)
        {
            Sketch sketch = SketchTools.Clone(original);

            sketch = SketchTransformation.Resample(sketch, N);
            sketch = SketchTransformation.ScaleSquare(sketch, Size);
            sketch = SketchTransformation.TranslateCentroid(sketch, K);

            return sketch;
        }

        #endregion

        #region Properties

        public int N { get; private set; }
        public double Size { get; private set; }
        public Point K { get; private set; }

        public List<string> Labels { get { return myLabels; } }
        public List<double> Scores { get { return myScores; } }

        #endregion

        #region Fields

        private List<SketchPair> myTemplates;
        private List<string> myLabels;
        private List<double> myScores;

        #endregion
    }
}
