using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.ElementComponents
{
    public class Stirrup
    {

        #region Properties 
        public LinearPath StirrupPath { get; set; }
        public double Diameter { get; set; } = DefaultValues.BarDiameter;
        #endregion

        #region Constructor
        public Stirrup(LinearPath stirrupPath)
        {
            StirrupPath = stirrupPath; 
        } 
        #endregion
 
         

    }
}
