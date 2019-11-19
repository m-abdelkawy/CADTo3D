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
        public LinearPath AxisLine { get; set; }
        //public List<CircBlockObject> CircBlockObj { get; set; }
        public List<Circle> LstCircle { get; set; }

        public Axis(LinearPath _axisLine, List<Circle> _lstCircle, string _axisText)
        {
            this.AxisLine = _axisLine;
            this.LstCircle = _lstCircle;
            this.AxisText = _axisText;
        }
    }
}
