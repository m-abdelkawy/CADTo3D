using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Base
{
    public interface IRectangleBase
    {
        double Width { get; set; }
        double Length { get; set; }
        Point3D CenterPt{ get; set; }
        Point3D PtLengthDir { get; set; }
    }
}
