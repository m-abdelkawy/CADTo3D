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
    public class RectSlab: IRectangleBase
    {
        #region Properties
        public double Thickness { get; set; } = DefaultValues.SlabThinkess;
        public double Width { get; set; }
        public double Length { get; set; }
        public Point3D CenterPt { get; set; }
        public Point3D PtLengthDir { get; set; }
        #endregion


        #region Constructor
        public RectSlab(double _width, double _length, Point3D _cntrPt, Point3D _ptLngthDir)
        {
            Width = _width;
            Length = _length;
            CenterPt = _cntrPt;
            PtLengthDir = _ptLngthDir;
        } 
        #endregion


    }
}
