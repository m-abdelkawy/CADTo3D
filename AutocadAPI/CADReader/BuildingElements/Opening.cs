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
    public class Opening: IRectangleBase
    {
        #region Properties
        public double Width { get; set; }
        public double Length { get; set; }
        public Point3D CenterPt { get; set; }
        public Point3D PtLengthDir { get; set; }

        public LinearPath LinPathOpening { get; set; }
        #endregion

        public Opening(LinearPath _linPath,double _width, double _length, Point3D _cntrPt, Point3D _ptLngthDir)
        {//Todo: Draw openings using linearPath vertices
            Width = _width;
            Length = _length;
            CenterPt = _cntrPt;
            PtLengthDir = _ptLngthDir;
            LinPathOpening = _linPath;
        }
    }
}
