using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutocadAPI.BuildingElements
{
    public class Opening
    {
        private double width;
        private double length;
        private Point3d centerPt;
        private Point3d ptLengthDir;

        private double zLevel;


        public Opening(double _width, double _length, Point3d _cntrPt, Point3d _ptLngthDir, double _zLevel)
        {
            width = _width;
            length = _length;
            centerPt = _cntrPt;
            ptLengthDir = _ptLngthDir;
            zLevel = _zLevel;
        }

        public double ZLevel
        {
            get { return zLevel; }
        }

        public Point3d PtLengthDir
        {
            get { return ptLengthDir; }
            set { ptLengthDir = value; }
        }


        public Point3d CenterPt
        {
            get { return centerPt; }
            private set { centerPt = value; }
        }

        public double Length
        {
            get { return length; }
            private set { length = value; }
        }

        public double Width
        {
            get { return width; }
            private set { width = value; }
        }
    }
}
