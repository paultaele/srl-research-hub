using Srl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // Stroke Direction Test

            // Stroke Speed Test
        }

        private bool StrokeCountTest(Sketch model, Sketch input)
        {
            int modelStrokeCount = model.Strokes.Count;
            int inputStrokeCount = input.Strokes.Count;
            return modelStrokeCount == inputStrokeCount;
        }

        #region

        public bool StrokeCountResult { get; private set; }

        #endregion

        #region Fields

        private Sketch myModel;
        private Sketch myInput;

        #endregion
    }
}
