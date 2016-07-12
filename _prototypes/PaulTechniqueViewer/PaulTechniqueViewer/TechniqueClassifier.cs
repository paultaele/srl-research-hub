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
        public TechniqueClassifier()
        {

        }

        public void Train(Sketch model, Sketch input)
        {
            myModel = SketchTools.Clone(model);
            myInput = SketchTools.Clone(input);
        }

        public void Run()
        {
            // Stroke Count Test
            StrokeCountResult = StrokeCountTest(myModel, myInput);

            // Stroke Order Test
            StrokeOrderResult = StrokeOrderTest(myModel, myInput);

            // Stroke Direction Test
            StrokeDirectionResult = StrokeDirectionTest(myModel, myInput);

            // Stroke Speed Test
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

            // 
            myStrokeOrder = new List<int>();
            for (int i = 0; i < model.Strokes.Count; ++i)
            {
                //
                InkStroke modelStroke = model.Strokes[i];
                List<long> modelTimes = model.Times[i];
                int numModelPoints = modelStroke.GetInkPoints().Count;
                Sketch modelStrokeSketch = new Sketch("", new List<InkStroke>() { modelStroke }, new List<List<long>> { modelTimes }, 0, 0, 0, 0);

                //
                List<double> distances = new List<double>();
                for (int j = 0; j < input.Strokes.Count; ++j)
                {
                    InkStroke inputStroke = input.Strokes[j];
                    List<long> inputTimes = input.Times[j];
                    Sketch inputStrokeSketch = new Sketch("", new List<InkStroke>() { inputStroke }, new List<List<long>> { inputTimes }, 0, 0, 0, 0);
                    inputStrokeSketch = SketchTools.Clone(inputStrokeSketch);
                    inputStrokeSketch = SketchTransformation.Resample(inputStrokeSketch, numModelPoints);

                    double distance1 = SketchTools.Distance(modelStrokeSketch, inputStrokeSketch);
                    double distance2 = SketchTools.Distance(inputStrokeSketch, modelStrokeSketch);
                    double distance = Math.Min(distance1, distance2);

                    distances.Add(distance);
                    Debug.WriteLine($"[{distances.Count}]DISTANCE: " + distance);
                }

                //
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

                myStrokeOrder.Add(minIndex);
            }

            //
            for (int i = 1; i < myStrokeOrder.Count; ++i)
            {
                if (myStrokeOrder[i-1] >= myStrokeOrder[i]) { return false; }
            }
            return true;
        }

        private bool StrokeDirectionTest(Sketch model, Sketch input)
        {
            return false;
        }

        #region Properties

        public bool StrokeCountResult { get; private set; }
        public bool StrokeOrderResult { get; private set; }
        public bool StrokeDirectionResult { get; private set; }

        public IReadOnlyList<int> StrokeOrder { get { return new List<int>(myStrokeOrder); } }

        #endregion

        #region Fields

        private Sketch myModel;
        private Sketch myInput;

        private List<int> myStrokeOrder;

        #endregion
    }
}
