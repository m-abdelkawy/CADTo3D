using devDept.Geometry;

namespace CADReader
{
    internal class WrapperPoint : Point3D
    {
        public Point3D point;
        public double angle;

        public WrapperPoint(Point3D point, double angle)
        {
            this.point = point;
            this.angle = angle;
        }
    }
}