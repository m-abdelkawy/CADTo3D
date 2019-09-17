using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader
{
    public static class MathHelper
    {
        #region Geometry
        internal static double LineGetSlope(Point3D pt1, Point3D pt2)
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
        internal static double LineGetEqnConst(double slope, Point3D pt)
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

            if (double.IsNaN(m1) && double.IsNaN(m2))
            {
                return Math.Abs(l1.StartPoint.X - l2.StartPoint.X);
            }

            double c1 = LineGetEqnConst(m1, l1);
            double c2 = LineGetEqnConst(m2, l2);

            return Math.Abs(c1 - c2) / Math.Sqrt(1 + Math.Pow(m1, 2));
        }
        public static Point3D MidPoint(Point3D pt1, Point3D pt2)
        {
            double midX = 0.50 * (pt1.X + pt2.X);
            double midY = 0.50 * (pt1.Y + pt2.Y);
            double midZ = 0.50 * (pt1.Z + pt2.Z);

            return new Point3D(midX, midY, midZ);
        }
        public static Point3D UnitVectorFromPt1ToPt2(Point3D pt1, Point3D pt2)
        {//check
            Vector3D vector = (new Vector3D(pt2.X, pt2.Y, pt2.Z) - new Vector3D(pt1.X, pt1.Y, pt1.Z));

            vector.Normalize();
            //return new Point3D(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return new Point3D(vector.X, vector.Y, vector.Z);
        }

        internal static double LineSlope(Line line)
        {//needs reconsideration
            #region Old code
            double yDiff = line.EndPoint.Y - line.StartPoint.Y;
            double xDiff = line.EndPoint.X - line.StartPoint.X;
            if (Math.Abs(xDiff) < 0.001)
            {
                return 6000;
            }
            return yDiff / xDiff;
            #endregion
        }


        //public static Vector3D UnitVector3DFromPt1ToPt2(Point3D pt1, Point3D pt2)
        //{//check
        //    Vector3D vector = (new Vector3D(pt2.X, pt2.Y, pt2.Z) - new Vector3D(pt1.X, pt1.Y, pt1.Z));

        //    vector.Normalize();
        //    //return new vector normalized
        //    return vector;
        //}

        public static double CalcDistanceBetweenTwoPoint3D(Point3D pt1, Point3D pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2) + Math.Pow(pt1.Z - pt2.Z, 2));
        }
        internal static double CalcDistanceBetweenTwoPoint2D(Point3D pt1, Point3D pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2));
        }


        #region UnitVectors
        internal static Vector3D UnitVector3DFromPt1ToPt2(Point3D pt1, Point3D pt2)
        {//check
            Vector3D vector = (new Vector3D(pt2.X, pt2.Y, pt2.Z) - new Vector3D(pt1.X, pt1.Y, pt1.Z));

            vector.Normalize();
            //return new Point3D(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return vector;
        }

        internal static Vector3D UnitVector3DProjectedFromPt1ToPt2(Point3D pt1, Point3D pt2)
        {//check
            Vector3D vector = (new Vector3D(pt2.X, pt2.Y, pt1.Z) - new Vector3D(pt1.X, pt1.Y, pt1.Z));

            vector.Normalize();
            //return new Point3D(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return vector;
        }

        internal static Vector2D UnitVector2DFromPt1ToPt2(Point2D pt1, Point2D pt2)
        {//check
            Vector2D vector = (new Vector2D(pt2.X, pt2.Y) - new Vector2D(pt1.X, pt1.Y));

            vector.Normalize();
            //return new Point3D(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return vector;
        }

        internal static Vector3D UVPerpendicularToLine2DFromPt(Line line, Point3D pt)
        {
            //Vector3D pt = line.StartPoint;

            double yEnd, xEnd;
            double m1 = LineSlope(line);
            if (m1 > 0.001 || m1 < -0.001)
            {
                double m2 = -1 / m1;
                //Eqn: yEnd - pt.Y == m2 * (xEnd - pt.X)
                //set arbitrary x -> let x = 10
                yEnd = (m2 * (10 - pt.X)) + pt.Y;

                return UnitVector3DFromPt1ToPt2(new Point3D(10, yEnd, pt.Z), pt);
            }
            else
            {
                double m2 = -1 / m1;
                //Eqn: yEnd - pt.Y == m2 * (xEnd - pt.X)
                //set arbitrary y -> let y = 10
                xEnd = ((10 - pt.Y) / m2) + pt.X;
                return UnitVector3DFromPt1ToPt2(new Point3D(xEnd, 10, pt.Z), pt);
            }

            //Line perpLine = Line.CreateBound(new XYZ(10, yEnd, pt.Z), pt);
        }

        #endregion

        #endregion
    }
}
