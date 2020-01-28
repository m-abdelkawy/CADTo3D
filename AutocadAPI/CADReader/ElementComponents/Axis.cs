using devDept.Eyeshot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.ElementComponents
{
    public class Axis
    {
        public string AxisText { get; set; }
        public LinearPath AxisLinPath { get; set; }
        //public List<CircBlockObject> CircBlockObj { get; set; }
        public List<Circle> LstCircle { get; set; }
        public string Orientation { get; set; }


        public Axis(LinearPath _axisLinPath, List<Circle> _lstCircle, string _axisText)
        {
            AxisLinPath = _axisLinPath;
            LstCircle = _lstCircle;
            AxisText = _axisText;
            Orientation = CalcOrientation();
        }

        private string CalcOrientation()
        {
            Line[] linPathLineArr = AxisLinPath.ConvertToLines();

            double maxLength = double.MinValue;

            Line lineTallest = null;

            for (int i = 0; i < linPathLineArr.Length; i++)
            {
                if(linPathLineArr[i].Length() >= maxLength)
                {
                    maxLength = linPathLineArr[i].Length();
                    lineTallest = linPathLineArr[i];
                }
            }

            if(lineTallest != null)
            {
                double lineSlope = MathHelper.LineGetSlope(lineTallest);
                if (double.IsNaN(lineSlope))
                    return "Vertical";
                else if (Math.Abs(lineSlope) < 0.001)
                    return "Horizontal";
                else
                    return "Sloped";
            }
            return null;
        }
    }
}
