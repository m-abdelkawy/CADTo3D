using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADToIFC.BuildingElements
{
    public class Wall
    {
        private double thickness;
        private Point3D stPt;
        private Point3D endPt;


        public Wall(double _thick, Point3D _stPt, Point3D _endPt)
        {
            thickness = _thick;
            stPt = _stPt;
            endPt = _endPt;
        }


        public double Thickness
        {
            get { return thickness; }
            set { thickness = value; }
        }
        public Point3D EndPt
        {
            get { return endPt; }
            set { endPt = value; }
        }
        public Point3D StPt
        {
            get { return stPt; }
            set { stPt = value; }
        }
    }
}
