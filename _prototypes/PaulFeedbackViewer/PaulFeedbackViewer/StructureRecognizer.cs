using Srl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Inking;

namespace PaulFeedbackViewer
{
    public class StructureRecognizer
    {
        #region Core Methods

        public void Train(Sketch model, List<Sketch> templates, Sketch input)
        {
            // set the model and input sketches
            myModel = SketchTools.Clone(model);
            myInput = SketchTools.Clone(input);

            // set the template sketches
            myTemplates = new List<Sketch>();
            foreach (Sketch template in templates)
            {
                myTemplates.Add(SketchTools.Clone(template));
            }
        }

        public void Run()
        {
            // Symbol Correctness Test
            SymbolCorrectnessResult = SymbolCorrectnessTest(myModel, myTemplates, myInput);

            // Symbol Bounds Test
            // note: mySymbolMatches is set here
            StrokeBoundsResult = StrokeBoundsTest(myModel, myInput);
        }

        #endregion

        #region Test Methods

        private bool SymbolCorrectnessTest(Sketch model, List<Sketch> templates, Sketch input)
        {
            PDollar dollar = new PDollar(128, 500, new Point(0, 0));
            dollar.Train(templates);
            dollar.Run(input);

            string inputLabel = dollar.Labels[0];
            string modelLabel = model.Label;

            bool isCorrect = inputLabel.Equals(modelLabel);
            foreach (Sketch template in templates)
            {
                if (modelLabel.Equals(template.Label))
                {
                    CorrectSymbol = SketchTools.Clone(template);
                    break;
                }
            }         

            return isCorrect;
        }

        private bool StrokeBoundsTest(Sketch model, Sketch input)
        {
            // end test if there is a stroke count mismatch;
            // can't compare stroke bounds if there are no corresponding strokes
            if (!CheckStrokeCount(model, input)) { return false; }

            // get the cloned input and model sketches;
            // necessary for not affecting use of input and model sketches in other methods
            input = SketchTools.Clone(input);
            model = SketchTools.Clone(model);

            // get the matching input and model strokes
            myStrokeMatches = GetStrokeMatches(input, model); ;

            // get the corresponding strokes and whether their bounds are proportionally correct
            var triples = new List<Tuple<InkStroke, InkStroke, bool>>();
            for (int i = 0; i < myStrokeMatches.Count; ++i)
            {
                // get the current corresponding strokes
                var pair = myStrokeMatches[i];
                InkStroke inputStroke = pair.Item1;
                InkStroke modelStroke = pair.Item2;

                // get the corresponding strokes' bounding boxes
                Rect inputRect = inputStroke.BoundingRect;
                Rect modelRect = modelStroke.BoundingRect;

                // get the angles of the bounding boxes' diagonals
                double inputAngle = Math.Atan(inputRect.Height / inputRect.Width) * 180 / Math.PI;
                double modelAngle = Math.Atan(modelRect.Height / modelRect.Width) * 180 / Math.PI;

                // case: the input or the model sketch has a vertical-line bounding box
                bool isCorrect = false;
                if (inputAngle < 10.0 || modelAngle < 10.0)
                {
                    if (inputAngle >= 10.0 || modelAngle >= 10.0) { isCorrect = false; }
                    else
                    {
                        double proportionRatio = 1.0 - Math.Abs(1.0 - (inputRect.Width / modelRect.Width));
                        isCorrect = proportionRatio > 0.85;
                    }
                }

                // case: the input or the model sketch as a horizontal-line bounding box
                else if (inputAngle > 80.0 || modelAngle > 80.0)
                {
                    if (inputAngle <= 80.0 || modelAngle <= 80.0) { isCorrect = false; }
                    else
                    {
                        double proportionRatio = 1.0 - Math.Abs(1.0 - (inputRect.Height / modelRect.Height));
                        isCorrect = proportionRatio > 0.85;
                    }
                }

                // case: the input and model sketch both have regular bounding boxes
                else
                {
                    double inputProportion = inputRect.Width / inputRect.Height;
                    double modelProportion = modelRect.Width / modelRect.Height;
                    double proportionRatio = inputProportion / modelProportion;
                    isCorrect = proportionRatio < 1.3;
                }

                // collect the corresponding strokes' and the bounds correctness
                var triple = new Tuple<InkStroke, InkStroke, bool>(inputStroke, modelStroke, isCorrect);
                triples.Add(triple);
            }

            // check the bounds correctness for each corresponding strokes
            foreach (var triple in triples)
            {
                bool isCorrect = triple.Item3;

                if (!isCorrect) { return false; }
            }

            return true;
        }
      
        #endregion

        #region Helper Methods

        private bool CheckStrokeCount(Sketch model, Sketch input)
        {
            // get the model and input stroke counts
            int modelStrokeCount = model.Strokes.Count;
            int inputStrokeCount = input.Strokes.Count;

            // return the test result
            return modelStrokeCount == inputStrokeCount;
        }

        private double GetResampledPairwiseDistance(Sketch input, Sketch model, int i, int j)
        {
            // get the corresponding model stroke, points, and point count
            InkStroke modelStroke = model.Strokes[j];
            List<InkPoint> modelPoints = new List<InkPoint>(modelStroke.GetInkPoints());
            int numModelPoints = modelPoints.Count;

            // wrap the current input stroke into a sketch, then resample to match model stroke
            InkStroke inputStroke = input.Strokes[i];
            List<long> inputTimes = input.Times[i];
            Sketch inputSketch = new Sketch("", new List<InkStroke>() { inputStroke }, new List<List<long>> { inputTimes }, 0, 0, 0, 0);

            // resample the input stroke's points to match the current model stroke's points
            inputSketch = SketchTools.Clone(inputSketch);
            inputSketch = SketchTransformation.Resample(inputSketch, modelPoints.Count);
            inputStroke = inputSketch.Strokes[0];

            // get the number of points to iterate between the corresponding model and input stroke
            int numInputPoints = inputStroke.GetInkPoints().Count;
            int count = numModelPoints < numInputPoints ? numModelPoints : numInputPoints;

            // calculate the distances both forward and backwards between the model and input strokes
            double distance1 = 0.0;
            double distance2 = 0.0;
            for (int a = 0, b = count - 1; a < count; ++a, --b)
            {
                InkPoint modelPoint = modelStroke.GetInkPoints()[a];
                InkPoint inputPoint1 = inputStroke.GetInkPoints()[a];
                InkPoint inputPoint2 = inputStroke.GetInkPoints()[b];

                distance1 += SketchTransformation.Distance(modelPoint, inputPoint1);
                distance2 += SketchTransformation.Distance(modelPoint, inputPoint2);
            }
            double distance = distance1 < distance2 ? distance1 : distance2;

            return distance;
        }

        private double GetRectLength(Sketch sketch)
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
                    if (x > maxX) { maxX = x; }
                    if (y < minY) { minY = y; }
                    if (y > maxY) { maxY = y; }
                }
            }

            //
            double width = maxX - minX;
            double height = maxY - minY;
            return width > height ? width : height;
        }

        private List<Tuple<InkStroke, InkStroke>> GetStrokeMatches(Sketch input, Sketch model)
        {
            // get the cloned input and model sketches;
            // necessary for not affecting use of input and model sketches in other methods
            input = SketchTools.Clone(input);
            model = SketchTools.Clone(model);

            // get the matching input and model strokes
            var pairs = new List<Tuple<InkStroke, InkStroke>>();
            for (int i = 0; i < input.Strokes.Count; ++i)
            {
                // initialize the minimal values
                double minDistance = double.MaxValue;
                int minIndex = -1;

                // iterate through each model stroke
                for (int j = 0; j < model.Strokes.Count; ++j)
                {
                    // get the current model stroke
                    InkStroke modelStroke = model.Strokes[j];

                    // get the pairwise distance of the input and model
                    double distance = GetResampledPairwiseDistance(input, model, i, j);

                    // case: minimum distance found
                    // set the minimum distance and index
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minIndex = j;
                    }
                }

                // set the matching input and model strokes
                InkStroke inputStroke = input.Strokes[i];
                InkStroke minModelStroke = model.Strokes[minIndex];
                pairs.Add(new Tuple<InkStroke, InkStroke>(inputStroke, minModelStroke));

                // remove the current model stroke and times candidate for the next iteration
                model.Strokes.RemoveAt(minIndex);
                model.Times.RemoveAt(minIndex);
            }

            return pairs;
        }

        #endregion

        #region Properties

        public bool SymbolCorrectnessResult { get; private set; }
        public bool StrokeBoundsResult { get; private set; }

        public Sketch CorrectSymbol { get; private set; }
        public List<Tuple<InkStroke, InkStroke>> StrokeMatches { get { return new List<Tuple<InkStroke, InkStroke>>(myStrokeMatches); } }

        #endregion

        #region Fields

        private Sketch myModel;
        private List<Sketch> myTemplates;
        private Sketch myInput;

        private List<Tuple<InkStroke, InkStroke>> myStrokeMatches;

        #endregion
    }
}