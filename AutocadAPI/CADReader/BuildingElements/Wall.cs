using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Wall
    {

        #region Properties
        public double Thickness { get; set; }
        public Point3D StPt { get; set; }
        public Point3D EndPt { get; set; } 
        #endregion


        public Wall(double _thick, Point3D _stPt, Point3D _endPt)
        {
            Thickness = _thick;
            StPt = _stPt;
            EndPt = _endPt;
        }


     
    }
}
