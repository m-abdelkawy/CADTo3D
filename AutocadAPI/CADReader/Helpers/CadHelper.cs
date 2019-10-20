using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Helpers
{
    public static class CadHelper
    {
        //  public static ReadAutodesk CADReader { get; set; }
        public static List<LinearPath> PLinesGetByLayerName(ReadAutodesk cadReader, string layerName)
        {
            return cadReader.Entities.Where(e => e.LayerName == layerName && e is LinearPath).Cast<LinearPath>().ToList();
        }

        public static List<LinearPath> PLinesGetByLayerName(ReadAutodesk cadReader, string layerName, bool isClosed)
        {
            if (isClosed)
                return cadReader.Entities.Where(e => e.LayerName == layerName && e is LinearPath).Cast<LinearPath>()
                    .Where(l => l.IsClosed).ToList();

            return cadReader.Entities.Where(e => e.LayerName == layerName && e is LinearPath).Cast<LinearPath>().ToList();
        }

        public static List<LinearPathEx> PLinesXGetByLayerName(ReadAutodesk cadReader, string layerName)
        {
            return cadReader.Entities.Where(e => e.LayerName == layerName && e is LinearPathEx).Cast<LinearPathEx>().ToList();
        }

        public static List<Line> LinesGetByLayerName(ReadAutodesk cadReader, string layerName)
        {
            return cadReader.Entities.Where(e => e.LayerName == layerName && e is Line).Cast<Line>().ToList();
        }

        public static List<Line> BoundaryLinesGet(LinearPath linPath)
        {
            List<Line> lstLines = new List<Line>();

            Point3D minPt;
            Point3D maxPt;

            linPath.GetBb(out minPt, out maxPt);

            Point3D minPt2 = new Point3D(maxPt.X, minPt.Y, minPt.Z);
            Point3D maxPt2 = new Point3D(minPt.X, maxPt.Y, minPt.Z);

            Line line1 = new Line(minPt, minPt2);
            Line line2 = new Line(minPt2, maxPt);
            Line line3 = new Line(maxPt, maxPt2);
            Line line4 = new Line(maxPt2, minPt);

            lstLines.Add(line1);
            lstLines.Add(line2);
            lstLines.Add(line3);
            lstLines.Add(line4);

            return lstLines;
        }

        public static List<Line> LinesTrimWithPolygon(LinearPath polygon, Line line, bool isInside = true)
        {
            List<Line> lstLineSegment = new List<Line>();

            List<Point3D> lstIntersectionPts = MathHelper.PointsIntersectOfLineSegmentWithPolygon(polygon, line);


            ICurve[] lineSegments;
            line.SplitBy(lstIntersectionPts, out lineSegments);

            if (lineSegments.Count() == 0)
                lstLineSegment.Add(line);
            else
            {
                for (int i = 0; i < lineSegments.Length; i++)
                {
                    Line l = lineSegments[i] as Line;
                    if (l != null && MathHelper.IsInsidePolygon(l.MidPoint, polygon))
                    {
                        lstLineSegment.Add(l);
                    }
                }
            }

            return lstLineSegment;
        }


        public static Line ShortestLineGet(LinearPath linPath)
        {
            Line[] lineArr = linPath.ConvertToLines();
            Line shortestLine = null;
            double length = double.MaxValue;
            for (int j = 0; j < lineArr.Length; j++)
            {
                shortestLine = lineArr[j].Length() < length ? lineArr[j] : shortestLine;
            }

            return shortestLine;
        }

        #region Filters
        public static bool IsIntersectingWithElmCategory(Line line, List<LinearPath> lstPolygonByLayer)
        {
            for (int i = 0; i < lstPolygonByLayer.Count; i++)
            {
                if (MathHelper.IsLineSegmentIntersectingPolygon(lstPolygonByLayer[i], line))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        public static List<LinearPath> FootingsWithSemelles(LinearPath semelleLinPath, List<LinearPath> lstFootingLinPath, out List<Line> lstSemelleLongLine)
        {
            HashSet<LinearPath> lstFooting = new HashSet<LinearPath>();
            Line[] lstSemelleLine = semelleLinPath.ConvertToLines();

            lstSemelleLongLine = new List<Line>();

            for (int i = 0; i < lstSemelleLine.Length; i++)
            {
                int counter = 0;
                for (int j = 0; j < lstFootingLinPath.Count; j++)
                {

                    if (MathHelper.IsLineSegmentIntersectingPolygon(lstFootingLinPath[j], lstSemelleLine[i]))
                    {
                        counter++;
                        lstFooting.Add(lstFootingLinPath[j]);
                    }
                }
                if (counter == 2) lstSemelleLongLine.Add(lstSemelleLine[i]);
            }
            
            return lstFooting.ToList();
        }

        public static Line CenterLineBetweenTwoParallelsGet(Line line1, Line line2)
        {
            if (Math.Abs(MathHelper.LineGetSlope(line1)) - Math.Abs(MathHelper.LineGetSlope(line2)) > 0.001)
            {
                return null;
            }

            Point3D centerStPt;
            Point3D centerEndPt;
            if (MathHelper.CalcDistanceBetweenTwoPoint3D(line1.StartPoint, line2.StartPoint) < MathHelper.CalcDistanceBetweenTwoPoint3D(line1.StartPoint, line2.EndPoint))
            {
                centerStPt = MathHelper.MidPoint3D(line1.StartPoint, line2.StartPoint);
                centerEndPt = MathHelper.MidPoint3D(line1.EndPoint, line2.EndPoint);
            }

            else
            {
                centerStPt = MathHelper.MidPoint3D(line1.StartPoint, line2.EndPoint);
                centerEndPt = MathHelper.MidPoint3D(line1.EndPoint, line2.StartPoint);
            }

            return new Line(centerStPt, centerEndPt);
        }

        /// <summary>
        /// Returns List Of Columns and Shear Walls insidy Footing
        /// </summary>
        /// <param name="linPathFooting">Polygon of footing</param>
        /// <param name="lstColLinPath">List of Columns and Shear walls in the drawing</param>
        /// <returns></returns>
        public static List<LinearPath> ColumnsInsideFootingGet(LinearPath linPathFooting, List<LinearPath> lstColumnLinPath)
        {
            List<LinearPath> lstColInsideFooting = new List<LinearPath>();
            for (int i = 0; i < lstColumnLinPath.Count(); i++)
            {
                if(MathHelper.IsInsidePolygon(lstColumnLinPath[i], linPathFooting))
                {
                    lstColInsideFooting.Add(lstColumnLinPath[i]);
                }
            }
            return lstColInsideFooting;
        }

        public static Point3D PointIntersectionSemelleWithColumn(Line line, List<LinearPath> lstLinPathCol)
        {
            List<Point3D> lstIntersectionPts = new List<Point3D>();
            for (int i = 0; i < lstLinPathCol.Count; i++)
            {
                lstIntersectionPts =MathHelper.PointsIntersectOfLineWithPolygon(lstLinPathCol[i], line);
                if (lstIntersectionPts.Count == 2) break;
            }
            if (lstIntersectionPts.Count == 0) return null;
            return MathHelper.MidPoint3D(lstIntersectionPts[0], lstIntersectionPts[1]);
        }
    }
}
