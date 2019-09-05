using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutocadAPI.BuildingElements
{
    public class Wall
    {
        private double thickness;
        private Point3d stPt;
        private Point3d endPt;


        public Wall(double _thick, Point3d _stPt, Point3d _endPt)
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
        public Point3d EndPt
        {
            get { return endPt; }
            set { endPt = value; }
        }
        public Point3d StPt
        {
            get { return stPt; }
            set { stPt = value; }
        }
    }
}
