using CADReader.Base;
using CADReader.Helpers;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    class PCRectFooting:IRectangleBase
    {
        #region Properties
        public double Width { get; set; }
        public double Length { get; set; }
        public Point3D CenterPt { get; set; }
        public Point3D PtLengthDir { get; set; }

        public double Thickness { get; set; } = DefaultValues.RCFootingThinkess;
        #endregion

        public PCRectFooting(double _width, double _length, double _thickness, Point3D _cntrPt, Point3D _ptLngthDir)
        {
            Width = _width;
            Length = _length;
            CenterPt = _cntrPt;
            PtLengthDir = _ptLngthDir;
            Thickness = _thickness;
        }

    }
}
