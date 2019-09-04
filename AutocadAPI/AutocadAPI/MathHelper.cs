using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutocadAPI
{
    public static class MathHelper
    {
        #region Geometry
        internal static double LineGetSlope(Point3d pt1, Point3d pt2)
        {//needs reconsideration (more tight code)
            double yDiff = pt2.Y - pt1.Y;
            double xDiff = pt2.X - pt1.X;
            if (xDiff < 0.001)
            {
                return double.NaN;
            }
            return yDiff / xDiff;
        }
        internal static double LineGetSlope(Line line)
        {//needs reconsideration
            double yDiff = Math.Abs(line.EndPoint.Y - line.StartPoint.Y);
            double xDiff = Math.Abs(line.EndPoint.X - line.StartPoint.X);
            if (Math.Abs(xDiff) < 0.001)
            {
                return double.NaN;
            }
            return yDiff / xDiff;
        }
        internal static double LineGetEqnConst(double slope, Point3d pt)
        {
            return (pt.Y - (slope * pt.X));
        }
        internal static double LineGetEqnConst(double slope, Line line)
        {
            return (line.StartPoint.Y - (slope * line.StartPoint.X));
        }
        internal static double DistanceBetweenTwoParallels(Line l1, Line l2)
        {
            double m1 = LineGetSlope(l1);
            double m2 = LineGetSlope(l2);

            if(double.IsNaN(m1) && double.IsNaN(m2))
            {
                return Math.Abs(l1.StartPoint.X - l2.StartPoint.X);
            }

            double c1 = LineGetEqnConst(m1, l1);
            double c2 = LineGetEqnConst(m2, l2);

            return Math.Abs(c1 - c2) / Math.Sqrt(1 + Math.Pow(m1, 2));
        }


        internal static Point3d MidPoint(Point3d pt1, Point3d pt2)
        {
            double midX = 0.50 * (pt1.X + pt2.X);
            double midY = 0.50 * (pt1.Y + pt2.Y);
            double midZ = 0.50 * (pt1.Z + pt2.Z);

            return new Point3d(midX, midY, midZ);
        }
        internal static Point3d UnitVectorFromPt1ToPt2(Point3d pt1, Point3d pt2)
        {
            Vector3d uv = (pt2 - pt1).GetNormal();
            //return new Point3d(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return new Point3d(uv.X, uv.Y, uv.Z);
        }

        internal static double CalcDistanceBetweenTwoPoint3D(Point3d pt1, Point3d pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2) + Math.Pow(pt1.Z - pt2.Z, 2));
        }

        #endregion
    }
}
