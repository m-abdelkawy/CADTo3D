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

            List<Point3D> lstIntersectionPts = MathHelper.PointsIntersectOfLineWithPolygon(polygon, line);


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
    }
}
