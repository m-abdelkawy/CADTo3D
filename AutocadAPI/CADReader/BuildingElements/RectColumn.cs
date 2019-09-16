using CADReader.Base;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class RectColumn: IRectangleBase
    {
        public double Width { get; set; }
        public double Length { get; set; }
        public Point3D CenterPt { get; set; }
        public Point3D PtLengthDir { get; set; }

        public LinearPath ColPath { get; set; }
        public double Cover { get; set; } = 0.03;
        



        public RectColumn(double _width, double _length, Point3D _cntrPt, Point3D _ptLngthDir, LinearPath _colLinPath)
        {
            Width = _width;
            Length = _length;
            CenterPt = _cntrPt;
            PtLengthDir = _ptLngthDir;
            ColPath = _colLinPath;
        }
        

        
        

        /*
        private double width;
        private double length;
        private Point3D centerPt;
        private Point3D ptLengthDir;

        public RectColumn(double _width, double _length, Point3D _cntrPt, Point3D _ptLngthDir)
        {
            width = _width;
            length = _length;
            centerPt = _cntrPt;
            ptLengthDir = _ptLngthDir;
        }

        public Point3D PtLengthDir
        {
            get { return ptLengthDir; }
            set { ptLengthDir = value; }
        }
        

        public Point3D CenterPt
        {
            get { return centerPt; }
            private set { centerPt = value; }
        }
        
        public double Length
        {
            get { return length; }
            set { length = value; }
        }
        
        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        public override void ReinforcementPopulate()
        {
            throw new NotImplementedException();
        }
        */
    }
}
