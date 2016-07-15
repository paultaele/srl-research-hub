using Srl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;

namespace PaulTechniqueViewer
{
    public class TechniqueClassifier
    {
        #region Initializers

        public TechniqueClassifier()
        {

        }

        public void Train(Sketch model, Sketch input)
        {
            myModel = SketchTools.Clone(model);
            myInput = SketchTools.Clone(input);
        }

        #endregion

        public void Run()
        {
            // Stroke Count Test
            StrokeCountResult = StrokeCountTest(myModel, myInput);

            // Stroke Order Test
            StrokeOrderResult = StrokeOrderTest(myModel, myInput);

            // Stroke Direction Test
            StrokeDirectionResult = StrokeDirectionTest(myModel, myInput);

            // Stroke Speed Test
            StrokeSpeedResult = StrokeSpeedTest(myModel, myInput);
        }

        private bool StrokeCountTest(Sketch model, Sketch input)
        {
            // get the model and input stroke counts
            int modelStrokeCount = model.Strokes.Count;
            int inputStrokeCount = input.Strokes.Count;

            // return the test result
            return modelStrokeCount == inputStrokeCount;
        }

        private bool StrokeOrderTest(Sketch model, Sketch input)
        {
            // skip this test if the stroke counts do not match up
            if (!StrokeCountResult) { return false; }

            // clone the model and input strokes
            model = SketchTools.Clone(model);
            input = SketchTools.Clone(input);

            // iterate through each input stroke
            myStrokeOrders = new List<int>();
            for (int i = 0; i < input.Strokes.Count; ++i)
            {
                // get the current input stroke and times
                InkStroke inputStroke = input.Strokes[i];
                List<long> inputTimes = input.Times[i];

                // iterate through each model stroke
                double minValue = double.MaxValue;
                int minIndex = -1;
                for (int j = 0; j < model.Strokes.Count; ++j)
                {
                    // get the current model stroke and point count
                    InkStroke modelStroke = model.Strokes[j];
                    List<long> modelTimes = model.Times[j];
                    int numModelPoints = modelStroke.GetInkPoints().Count;

                    // clone the current input stroke and wrap into sketch
                    inputStroke = SketchTools.Clone(inputStroke);
                    Sketch inputSketch = new Sketch("", new List<InkStroke>() { inputStroke }, new List<List<long>>() { inputTimes }, input.FrameMinX, input.FrameMinY, input.FrameMaxX, input.FrameMaxY);
                    inputSketch = SketchTransformation.Resample(inputSketch, numModelPoints);
                    Sketch modelSketch = new Sketch("", new List<InkStroke>() { modelStroke }, new List<List<long>>() { modelTimes }, model.FrameMinX, model.FrameMinY, model.FrameMaxX, model.FrameMaxY);

                    // calculate the distance between the two sketches and add to the list
                    double distance1 = SketchTools.Distance(inputSketch, modelSketch);
                    double distance2 = SketchTools.Distance(modelSketch, inputSketch);
                    double distance = Math.Min(distance1, distance2);

                    // determine minimum property
                    if (distance < minValue)
                    {
                        minValue = distance;
                        minIndex = j;
                    }
                }

                // add the stroke index to the list
                myStrokeOrders.Add(minIndex);
            }

            // determine stroke order correctness
            bool isOrdered = true;
            for (int i = 1; i < myStrokeOrders.Count; ++i)
            {
                int prevIndex = myStrokeOrders[i-1];
                int currIndex = myStrokeOrders[i];

                if (prevIndex >= currIndex)
                {
                    isOrdered = false;
                    break;
                }
            }
            return isOrdered;
        }

        private bool StrokeDirectionTest(Sketch model, Sketch input)
        {
            // skip this test if the stroke counts do not match up
            if (!StrokeCountResult) { return false; }

            // make copies of the model and input strokes
            model = SketchTools.Clone(model);
            input = SketchTools.Clone(input);

            // iterate through each model stroke
            myStrokeDirections = new List<bool>();

            // iterate through each input stroke
            bool isDirected = true;
            for (int i = 0; i < input.Strokes.Count; ++i)
            {
                // get the corresponding model stroke, points, and point count
                int modelIndex = myStrokeOrders[i];
                InkStroke modelStroke = model.Strokes[modelIndex];
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
                double distances1 = 0.0;
                double distances2 = 0.0;
                for (int j = 0, k = count - 1; j < count; ++j, --k)
                {
                    InkPoint modelPoint = modelStroke.GetInkPoints()[j];
                    InkPoint inputPoint1 = inputStroke.GetInkPoints()[j];
                    InkPoint inputPoint2 = inputStroke.GetInkPoints()[k];

                    distances1 += SketchTransformation.Distance(modelPoint, inputPoint1);
                    distances2 += SketchTransformation.Distance(modelPoint, inputPoint2);
                }

                // get the stroke direction correctness for each corresponding model and input strokes
                bool result = distances1 < distances2 ? true : false;
                myStrokeDirections.Add(result);

                // flag the entire stroke direction as incorrect if one stroke direction is incorrect
                if (!result) { isDirected = false; }
            }

            return isDirected;
        }

        private bool StrokeSpeedTest(Sketch model, Sketch input)
        {
            // skip this test if the stroke counts do not match up
            if (!StrokeCountResult) { return false; }

            // make copies of the model and input strokes
            model = SketchTools.Clone(model);
            input = SketchTools.Clone(input);

            //
            long modelTimespans = 0;
            long factor = 2;
            foreach (List<long> modelTimes in model.Times)
            {
                int modelCount = modelTimes.Count;
                long modelFirst = modelTimes[0];
                long modelLast = modelTimes[modelCount - 1];
                long modelTimespan = modelLast - modelFirst;

                modelTimespans += modelTimespan;
            }
            modelTimespans /= factor;

            //
            List<long> inputFirstTimes = input.Times[0];
            List<long> inputLastTimes = input.Times[input.Times.Count - 1];
            long inputFirstTime = inputFirstTimes[0];
            long inputLastTime = inputLastTimes[inputLastTimes.Count - 1];
            long inputTimespans = inputLastTime - inputFirstTime;

            //
            //Debug.WriteLine($"Model Timespan: {modelTimespans}");
            //Debug.WriteLine($"Input Timespan: {inputTimespans}");
            //Debug.WriteLine($"Difference: {modelTimespans - inputTimespans}");
            //Debug.WriteLine("-----");

            return modelTimespans - inputTimespans > 0;
        }

        #region Properties

        public bool StrokeCountResult { get; private set; }
        public bool StrokeOrderResult { get; private set; }
        public bool StrokeDirectionResult { get; private set; }
        public bool StrokeSpeedResult { get; private set; }

        public IReadOnlyList<int> StrokeOrders { get { return new List<int>(myStrokeOrders); } }
        public IReadOnlyList<bool> StrokeDirections { get { return myStrokeDirections != null ? new List<bool>(myStrokeDirections) : null; } }

        #endregion

        #region Fields

        private Sketch myModel;
        private Sketch myInput;

        private List<int> myStrokeOrders;
        private List<bool> myStrokeDirections;

        #endregion
    }
}
