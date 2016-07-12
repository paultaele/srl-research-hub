using Srl;
using System;
using System.Collections.Generic;
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
            StrokeOrderReslt = StrokeOrderTest(myModel, myInput);

            // Stroke Direction Test

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
            //
            if (!StrokeCountResult) { return false; }

            //
            model = SketchTools.Clone(model);
            input = SketchTools.Clone(input);

            //
            //use: double Distance(Sketch alphaSketch, Sketch betaSketch)

            // return the test result (FIX)
            return false;
        }

        #region Properties

        public bool StrokeCountResult { get; private set; }
        public bool StrokeOrderReslt { get; private set; }

        #endregion

        #region Fields

        private Sketch myModel;
        private Sketch myInput;

        #endregion
    }
}
