using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input.Inking;

namespace SketchTransformDebugger
{
    public class Sketch
    {
        public Sketch(List<InkStroke> strokes, List<List<long>> times)
        {
            Strokes = strokes;
            Times = times;
        }

        public List<InkStroke> Strokes { get; set; }
        public List<List<long>> Times { get; set; }
    }
}
