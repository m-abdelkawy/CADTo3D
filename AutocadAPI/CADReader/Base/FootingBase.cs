using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using devDept.Geometry;

namespace CADReader.Base
{
   public abstract class FootingBase : IRectangleBase
    {
        public abstract string Type { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public Point3D CenterPt { get; set; }
        public Point3D PtLengthDir { get; set; }
        public double Thickness { get; set; }

    }
}
