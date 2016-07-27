using Srl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

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

        #endregion

        #region Fields

        public bool SymbolCorrectnessResult { get; private set; }

        public Sketch CorrectSymbol { get; private set; }

        #endregion

        #region Fields

        private Sketch myModel;
        private List<Sketch> myTemplates;
        private Sketch myInput;

        #endregion
    }
}