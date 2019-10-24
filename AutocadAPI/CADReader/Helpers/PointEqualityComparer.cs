using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Helpers
{
    class PointEqualityComparer : IEqualityComparer<Point3D>
    {
        public bool Equals(Point3D pt1, Point3D pt2)
        {
            if(Math.Abs(pt1.X - pt2.X) < (CADConfig.Units == linearUnitsType.Meters ? 0.01 : 5)
                &&
                Math.Abs(pt1.Y - pt2.Y) < (CADConfig.Units == linearUnitsType.Meters ? 0.01 : 5)
                &&
                Math.Abs(pt1.Z - pt2.Z) < (CADConfig.Units == linearUnitsType.Meters ? 0.01 : 5)
                )
            {
                return true;
            }
            return false;
        }

        public int GetHashCode(Point3D obj)
        {
            return (int)DateTime.Now.Ticks;
        }
    }
}
