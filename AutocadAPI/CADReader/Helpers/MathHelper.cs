using CADReader.Helpers;
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
        public static double LineGetSlope(Point3D pt1, Point3D pt2)
        {//needs reconsideration (more tight code)
            double yDiff = pt2.Y - pt1.Y;
            double xDiff = pt2.X - pt1.X;
            if (xDiff < 0.001)
            {
                return double.NaN;
            }
            return yDiff / xDiff;
        }
        public static double LineGetSlope(Line line)
        {//needs reconsideration
            double yDiff = Math.Abs(line.EndPoint.Y - line.StartPoint.Y);
            double xDiff = Math.Abs(line.EndPoint.X - line.StartPoint.X);
            if (Math.Abs(xDiff) < 0.001)
            {
                return double.NaN;
            }
            return yDiff / xDiff;
        }
        public static double LineGetEqnConst(double slope, Point3D pt)
        {
            return (pt.Y - (slope * pt.X));
        }
        public static double LineGetEqnConst(double slope, Line line)
        {
            return (line.StartPoint.Y - (slope * line.StartPoint.X));
        }
        public static double DistanceBetweenTwoParallels(Line l1, Line l2)
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
        public static Point3D MidPoint3D(Point3D pt1, Point3D pt2)
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

        public static double LineSlope(Line line)
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

        //for convex polygon only (interior angels are all less than 180)
        public static bool IsInPolygon(LinearPath inner, LinearPath outer)
        {

            for (int j = 0; j < inner.Vertices.Length; j++)
            {
                var coef = outer.Vertices.Skip(1).Select((p, i) =>
                                           (inner.Vertices[j].Y - outer.Vertices[i].Y) * (p.X - outer.Vertices[i].X)
                                         - (inner.Vertices[j].X - outer.Vertices[i].X) * (p.Y - outer.Vertices[i].Y))
                                   .ToList();

                if (coef.Any(p => p == 0))
                    return true;

                for (int i = 1; i < coef.Count(); i++)
                {
                    if (coef[i] * coef[i - 1] < 0)
                        return false;
                }
            }
            return true;
        }

        public static bool IsInsidePolygon(LinearPath inner, LinearPath outer)

        {
            Line[] outerLines = outer.ConvertToLines();

            for (int i = 0; i < inner.Vertices.Length; i++)
            {
                Line l = new Line(inner.Vertices[i],
                    new Point3D(CADConfig.Units == linearUnitsType.Meters ? inner.Vertices[i].X + 1000
                    : inner.Vertices[i].X + 100000, inner.Vertices[i].Y, inner.Vertices[i].Z));
                bool colinear = false;


                int countIntersect = 0;
                for (int j = 0; j < outerLines.Length; j++)
                {
                    if (IsLineSegmentsIntersected(l, outerLines[j], ref colinear))
                    {
                        countIntersect++;
                    }
                }

                if (!colinear && countIntersect % 2 == 0)
                    return false;
            }
            return true;
        }

        public static bool IsInsidePolygon(Point3D pt, LinearPath outer)
        {
            Line[] outerLines = outer.ConvertToLines();


            Line l = new Line(pt,
                new Point3D(CADConfig.Units == linearUnitsType.Meters ? pt.X + 1000 : pt.X + 100000, pt.Y, pt.Z));


            int countIntersect = 0;

            for (int j = 0; j < outerLines.Length; j++)
            {
                if (IsLineSegmentsIntersected(l, outerLines[j]))
                    countIntersect++;
            }
            if (countIntersect % 2 == 0)
                return false;

            return true;
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
        public static double CalcDistanceBetweenTwoPoint2D(Point3D pt1, Point3D pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2));
        }


        public static bool IsRectangle(LinearPath linPath, double tolerance)
        {

            int nVertices = linPath.Vertices.Count();
            if (nVertices > 5)
                return false;

            for (int i = 0; i < nVertices; i++)
            {
                linPath.Vertices[i].X = Math.Round(linPath.Vertices[i].X, 1);
                linPath.Vertices[i].Y = Math.Round(linPath.Vertices[i].Y, 1);
            }

            double dist1;
            double dist2;
            double dist3;
            double dist4;
            double distDiagonal;

            dist1 = CalcDistanceBetweenTwoPoint3D(linPath.Vertices[0], linPath.Vertices[1]);
            dist2 = CalcDistanceBetweenTwoPoint3D(linPath.Vertices[1], linPath.Vertices[2]);
            dist3 = CalcDistanceBetweenTwoPoint3D(linPath.Vertices[2], linPath.Vertices[3]);
            dist4 = CalcDistanceBetweenTwoPoint3D(linPath.Vertices[3], linPath.Vertices[4]);

            distDiagonal = CalcDistanceBetweenTwoPoint3D(linPath.Vertices[1], linPath.Vertices[3]);

            if ((Math.Pow(dist1, 2) + Math.Pow(dist2, 2) - Math.Pow(distDiagonal, 2) < tolerance)
                &&
                (Math.Pow(dist3, 2) + Math.Pow(dist4, 2) - Math.Pow(distDiagonal, 2) < tolerance))
                return true;

            return false;
        }
        #endregion


        #region UnitVectors
        public static Vector3D UnitVector3DFromPt1ToPt2(Point3D pt1, Point3D pt2)
        {//check
            Vector3D vector = (new Vector3D(pt2.X, pt2.Y, pt2.Z) - new Vector3D(pt1.X, pt1.Y, pt1.Z));

            vector.Normalize();
            //return new Point3D(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return vector;
        }

        public static Vector3D UnitVector3DProjectedFromPt1ToPt2(Point3D pt1, Point3D pt2)
        {//check
            Vector3D vector = (new Vector3D(pt2.X, pt2.Y, pt1.Z) - new Vector3D(pt1.X, pt1.Y, pt1.Z));

            vector.Normalize();
            //return new Point3D(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return vector;
        }

        public static Vector2D UnitVector2DFromPt1ToPt2(Point2D pt1, Point2D pt2)
        {//check
            Vector2D vector = (new Vector2D(pt2.X, pt2.Y) - new Vector2D(pt1.X, pt1.Y));

            vector.Normalize();
            //return new Point3D(uv.X, uv.Y, (pt1.Z + pt2.Z) / 2);
            return vector;
        }

        public static Vector3D UVPerpendicularToLine2DFromPt(Line line, Point3D pt)
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


        #region Intesections
        //بتشتغل على امتداد الخطوط
        internal static bool IsLinesIntersected(Line line1, Line line2)
        {
            double a1 = line1.EndPoint.Y - line1.StartPoint.Y;
            double b1 = line1.StartPoint.X - line1.EndPoint.X;
            double c1 = a1 * line1.StartPoint.X + b1 * line1.StartPoint.Y;

            double a2 = line2.EndPoint.Y - line2.StartPoint.Y;
            double b2 = line2.StartPoint.X - line2.EndPoint.X;
            double c2 = a2 * line2.StartPoint.X + b2 * line2.StartPoint.Y;

            double delta = a1 * b2 - a2 * b1;
            return Math.Abs(delta) < 0.0001 ? false : true;
        }

        internal static Point3D IntersectionOfTwoLines(Line line1, Line line2)
        {
            double a1 = line1.EndPoint.Y - line1.StartPoint.Y;
            double b1 = line1.StartPoint.X - line1.EndPoint.X;
            double c1 = a1 * line1.StartPoint.X + b1 * line1.StartPoint.Y;

            double a2 = line2.EndPoint.Y - line2.StartPoint.Y;
            double b2 = line2.StartPoint.X - line2.EndPoint.X;
            double c2 = a2 * line2.StartPoint.X + b2 * line2.StartPoint.Y;

            double delta = a1 * b2 - a2 * b1;
            return Math.Abs(delta) < 0.001 ? new Point3D(double.NaN, double.NaN, double.NaN) :
                new Point3D((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta, line2.StartPoint.Z);
        }


        internal static bool IsLineSegmentsIntersected(Line line1, Line line2)
        {
            double a1 = line1.EndPoint.Y - line1.StartPoint.Y;
            double b1 = line1.StartPoint.X - line1.EndPoint.X;
            double c1 = a1 * line1.StartPoint.X + b1 * line1.StartPoint.Y;

            double a2 = line2.EndPoint.Y - line2.StartPoint.Y;
            double b2 = line2.StartPoint.X - line2.EndPoint.X;
            double c2 = a2 * line2.StartPoint.X + b2 * line2.StartPoint.Y;

            double delta = a1 * b2 - a2 * b1;
            if (Math.Abs(delta) > 0.0001) // !=0
            {
                int o1 = orientation(line1.StartPoint, line1.EndPoint, line2.StartPoint);
                int o2 = orientation(line1.StartPoint, line1.EndPoint, line2.EndPoint);
                int o3 = orientation(line2.StartPoint, line2.EndPoint, line1.StartPoint);
                int o4 = orientation(line2.StartPoint, line2.EndPoint, line1.EndPoint);

                if (o1 != o2 && o3 != o4)
                    return true;

                // Special Cases 
                // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
                if (o1 == 0 && onSegment(line1.StartPoint, line2.StartPoint, line1.EndPoint)) return true;

                // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
                if (o2 == 0 && onSegment(line1.StartPoint, line2.EndPoint, line1.EndPoint)) return true;

                // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
                if (o3 == 0 && onSegment(line2.StartPoint, line1.StartPoint, line2.EndPoint)) return true;

                // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
                if (o4 == 0 && onSegment(line2.StartPoint, line1.EndPoint, line2.EndPoint)) return true;
            }


            return false; // Doesn't fall in any of the above cases
        }

        internal static bool IsLineSegmentsIntersected(Line line1, Line line2, ref bool colinear)
        {
            //colinear = false;

            int o1 = orientation(line1.StartPoint, line1.EndPoint, line2.StartPoint);
            int o2 = orientation(line1.StartPoint, line1.EndPoint, line2.EndPoint);
            int o3 = orientation(line2.StartPoint, line2.EndPoint, line1.StartPoint);
            int o4 = orientation(line2.StartPoint, line2.EndPoint, line1.EndPoint);

            if (o1 == 0 || o2 == 0 || o3 == 0 || o4 == 0)
            {
                colinear = true;
            }
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases 
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
            if (o1 == 0 && onSegment(line1.StartPoint, line2.StartPoint, line1.EndPoint)) return true;

            // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
            if (o2 == 0 && onSegment(line1.StartPoint, line2.EndPoint, line1.EndPoint)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
            if (o3 == 0 && onSegment(line2.StartPoint, line1.StartPoint, line2.EndPoint)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
            if (o4 == 0 && onSegment(line2.StartPoint, line1.EndPoint, line2.EndPoint)) return true;



            return false; // Doesn't fall in any of the above cases
        }


        internal static bool IsLineSegmentIntersectingPolygon(LinearPath linPath, Line line2)
        {
            foreach (Line line1 in linPath.ConvertToLines())
            {
                //double a1 = line1.EndPoint.Y - line1.StartPoint.Y;
                //double b1 = line1.StartPoint.X - line1.EndPoint.X;
                //double c1 = a1 * line1.StartPoint.X + b1 * line1.StartPoint.Y;

                //double a2 = line2.EndPoint.Y - line2.StartPoint.Y;
                //double b2 = line2.StartPoint.X - line2.EndPoint.X;
                //double c2 = a2 * line2.StartPoint.X + b2 * line2.StartPoint.Y;

                //double delta = a1 * b2 - a2 * b1;
                //if (Math.Abs(delta) > 0.0001) // !=0
                //{
                int o1 = orientation(line1.StartPoint, line1.EndPoint, line2.StartPoint);
                int o2 = orientation(line1.StartPoint, line1.EndPoint, line2.EndPoint);
                int o3 = orientation(line2.StartPoint, line2.EndPoint, line1.StartPoint);
                int o4 = orientation(line2.StartPoint, line2.EndPoint, line1.EndPoint);

                if (o1 != o2 && o3 != o4)
                    return true;

                // Special Cases 
                // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
                if (o1 == 0 && onSegment(line1.StartPoint, line2.StartPoint, line1.EndPoint)) return true;

                // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
                if (o2 == 0 && onSegment(line1.StartPoint, line2.EndPoint, line1.EndPoint)) return true;

                // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
                if (o3 == 0 && onSegment(line2.StartPoint, line1.StartPoint, line2.EndPoint)) return true;

                // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
                if (o4 == 0 && onSegment(line2.StartPoint, line1.EndPoint, line2.EndPoint)) return true;
                //}
            }



            return false; // Doesn't fall in any of the above cases
        }

        internal static List<Point3D> PointsIntersectOfLineSegmentWithPolygon(LinearPath linPath, Line line2)
        {
            List<Point3D> lstIntersecionPts = new List<Point3D>();
            foreach (Line line1 in linPath.ConvertToLines())
            {
                double a1 = line1.EndPoint.Y - line1.StartPoint.Y;
                double b1 = line1.StartPoint.X - line1.EndPoint.X;
                double c1 = a1 * line1.StartPoint.X + b1 * line1.StartPoint.Y;

                double a2 = line2.EndPoint.Y - line2.StartPoint.Y;
                double b2 = line2.StartPoint.X - line2.EndPoint.X;
                double c2 = a2 * line2.StartPoint.X + b2 * line2.StartPoint.Y;

                double delta = a1 * b2 - a2 * b1;
                if (Math.Abs(delta) > 0.0001) // !=0
                {
                    int o1 = orientation(line1.StartPoint, line1.EndPoint, line2.StartPoint);
                    int o2 = orientation(line1.StartPoint, line1.EndPoint, line2.EndPoint);
                    int o3 = orientation(line2.StartPoint, line2.EndPoint, line1.StartPoint);
                    int o4 = orientation(line2.StartPoint, line2.EndPoint, line1.EndPoint);

                    if ((o1 != o2 && o3 != o4)
                        ||
                        (o1 == 0 && onSegment(line1.StartPoint, line2.StartPoint, line1.EndPoint))
                        ||
                        (o2 == 0 && onSegment(line1.StartPoint, line2.EndPoint, line1.EndPoint))
                        ||
                        (o3 == 0 && onSegment(line2.StartPoint, line1.StartPoint, line2.EndPoint))
                        ||
                        (o4 == 0 && onSegment(line2.StartPoint, line1.EndPoint, line2.EndPoint))
                        )
                    {
                        lstIntersecionPts.Add(new Point3D((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta, line2.StartPoint.Z));
                    }
                }
            }



            return lstIntersecionPts; // Doesn't fall in any of the above cases
        }

        internal static List<Point3D> PointsIntersectOfLineWithPolygon(LinearPath linPath, Line line2)
        {
            List<Point3D> lstIntersectionPts = new List<Point3D>();
            Line[] arrLine = linPath.ConvertToLines();
            for (int i = 0; i < arrLine.Length; i++)
            {
                try
                {
                    Point3D intersectionPt = IntersectionOfTwoLines(arrLine[i], line2);
                    if (Double.IsNaN(intersectionPt.X)) throw new Exception();
                    lstIntersectionPts.Add(intersectionPt);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return lstIntersectionPts;
        }


        // Given three colinear points p, q, r, the function checks if 
        // point q lies on line segment 'pr' 
        static Boolean onSegment(Point3D p, Point3D q, Point3D r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;

            return false;
        }

        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are colinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        static int orientation(Point3D p, Point3D q, Point3D r)
        {
            // for details of below formula. 
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);

            if (Math.Abs(val) < 0.001) return 0; // colinear 

            return (val > 0) ? 1 : 2; // clock or counterclock wise 
        }
        #endregion



    }
}
