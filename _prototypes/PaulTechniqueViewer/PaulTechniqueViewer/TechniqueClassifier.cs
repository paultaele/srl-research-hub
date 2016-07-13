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

            // Stroke Edits Test?
            // the number of times that the user hit clear and undo?
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

            // make copies of the model and input strokes
            model = SketchTools.Clone(model);
            input = SketchTools.Clone(input);

            // iterate through each model stroke
            myStrokeOrders = new List<int>();
            for (int i = 0; i < model.Strokes.Count; ++i)
            {
                // wrap a sketch around the current model stroke
                InkStroke modelStroke = model.Strokes[i];
                List<long> modelTimes = model.Times[i];
                Sketch modelStrokeSketch = new Sketch("", new List<InkStroke>() { modelStroke }, new List<List<long>> { modelTimes }, 0, 0, 0, 0);
                int numModelPoints = modelStroke.GetInkPoints().Count;

                // iterate through each input stroke
                List<double> distances = new List<double>();
                for (int j = 0; j < input.Strokes.Count; ++j)
                {
                    // wrap a sketch around the current input stroke
                    InkStroke inputStroke = input.Strokes[j];
                    List<long> inputTimes = input.Times[j];
                    Sketch inputStrokeSketch = new Sketch("", new List<InkStroke>() { inputStroke }, new List<List<long>> { inputTimes }, 0, 0, 0, 0);

                    // resample the input stroke's points to match the current model stroke's points
                    inputStrokeSketch = SketchTools.Clone(inputStrokeSketch);
                    inputStrokeSketch = SketchTransformation.Resample(inputStrokeSketch, numModelPoints);

                    // get the minimum distance between the current model and input stroke
                    double distance1 = SketchTools.Distance(modelStrokeSketch, inputStrokeSketch);
                    double distance2 = SketchTools.Distance(inputStrokeSketch, modelStrokeSketch);
                    double distance = Math.Min(distance1, distance2);

                    // add to the list of distances
                    distances.Add(distance);
                }

                // get the index of the input stroke with the closest distance to the current mdoel stroke
                double minValue = double.MaxValue;
                int minIndex = 0;
                for (int j = 0; j < distances.Count; ++j)
                {
                    if (distances[j] < minValue)
                    {
                        minValue = distances[j];
                        minIndex = j;
                    }
                }

                // add the index of the input stroke that matches to the current model stroke
                myStrokeOrders.Add(minIndex);
            }

            // determine stroke order correctness by the ordering of the stroke order
            // if the order is not in ascending order, then the stroke order is incorrect
            for (int i = 1; i < myStrokeOrders.Count; ++i)
            {
                if (myStrokeOrders[i-1] >= myStrokeOrders[i]) { return false; }
            }
            return true;
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
            bool isCorrect = true;
            for (int i = 0; i < model.Strokes.Count; ++i)
            {
                // wrap a sketch around the current model stroke
                InkStroke modelStroke = model.Strokes[i];
                List<long> modelTimes = model.Times[i];
                //Sketch modelStrokeSketch = new Sketch("", new List<InkStroke>() { modelStroke }, new List<List<long>> { modelTimes }, 0, 0, 0, 0);
                int numModelPoints = modelStroke.GetInkPoints().Count;

                // find and wrap the matching input stroke into a sketch
                InkStroke inputStroke = null;
                List<long> inputTimes = null;
                for (int j = 0; j < myStrokeOrders.Count; ++j)
                {
                    if (myStrokeOrders[j] == i)
                    {
                        inputStroke = input.Strokes[j];
                        inputTimes = input.Times[j];
                    }
                }
                Sketch inputStrokeSketch = new Sketch("", new List<InkStroke>() { inputStroke }, new List<List<long>> { inputTimes }, 0, 0, 0, 0);

                // resample the input stroke's points to match the current model stroke's points
                inputStrokeSketch = SketchTools.Clone(inputStrokeSketch);
                inputStrokeSketch = SketchTransformation.Resample(inputStrokeSketch, numModelPoints);
                inputStroke = inputStrokeSketch.Strokes[0];

                // get the number of points to iterate between the corresponding model and input stroke
                int numInputPoints = inputStroke.GetInkPoints().Count;
                int count = numModelPoints < numInputPoints ? numModelPoints : numInputPoints;

                // calculate the distances both forward and backwards between the model and input strokes
                double distance1 = 0.0;
                double distance2 = 0.0;
                for (int j = 0, k = count - 1; j < count; ++j, --k)
                {
                    InkPoint modelPoint = modelStroke.GetInkPoints()[j];
                    InkPoint inputPoint1 = inputStroke.GetInkPoints()[j];
                    InkPoint inputPoint2 = inputStroke.GetInkPoints()[k];

                    distance1 += SketchTransformation.Distance(modelPoint, inputPoint1);
                    distance2 += SketchTransformation.Distance(modelPoint, inputPoint2);
                }

                // get the stroke direction correctness for each corresponding model and input strokes
                bool result = distance1 < distance2 ? true : false;
                myStrokeDirections.Add(result);

                // flag the entire stroke direction as incorrect if one stroke direction is incorrect
                if (!result) { isCorrect = false; }
            }

            return isCorrect;
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
        public IReadOnlyList<bool> StrokeDirections { get { return new List<bool>(myStrokeDirections); } }

        public Sketch RealTimeModel { get; private set; }

        #endregion

        #region Fields

        private Sketch myModel;
        private Sketch myInput;

        private List<int> myStrokeOrders;
        private List<bool> myStrokeDirections;

        #endregion
    }
}
