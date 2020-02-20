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
        public static List<LinearPath> PLinesGetByLayerName(ReadAutodesk cadReader, string layerName)
        {
            return cadReader.Entities.Where(e => e.LayerName == layerName && e is LinearPath).Cast<LinearPath>().ToList();
        }

        public static List<Entity> EntitiesGetByLayerName(ReadAutodesk cadReader, string layerName)
        {
            return cadReader.Entities.Where(e => e.LayerName == layerName).ToList();
        }

        public static List<LinearPath> PLinesGetByLayerName(ReadAutodesk cadReader, params string[] layers)
        {
            List<LinearPath> lstEntity = new List<LinearPath>();
            for (int i = 0; i < layers.Length; i++)
            {
                lstEntity.AddRange(cadReader.Entities.Where(e => e.LayerName == layers[i] && e is LinearPath).Cast<LinearPath>().ToList());
            }
            return lstEntity;
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

            //if (lineSegments.Count() == 0)
            //    lstLineSegment.Add(line);
            //else
            //{
                for (int i = 0; i < lineSegments.Length; i++)
                {
                    Line l = lineSegments[i] as Line;
                    if (l != null && MathHelper.IsInsidePolygon(l.MidPoint, polygon))
                    {
                        lstLineSegment.Add(l);
                    }
                }
            //}

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
     
        public static List<LinearPath> EntitiesIntersectingSemelleGet(LinearPath semelleLinPath, List<LinearPath> lstCadFooting, out List<Line> lstSemelleLongLine)
        {
            //intersected footings
            HashSet<LinearPath> lstIntersectingEntity = new HashSet<LinearPath>();
            Line[] lstSemelleLine = semelleLinPath.ConvertToLines();

            lstSemelleLongLine = new List<Line>();

            for (int i = 0; i < lstSemelleLine.Length; i++)
            {
                int counter = 0;
                for (int j = 0; j < lstCadFooting.Count; j++)
                {
                    if (MathHelper.IsLineSegmentIntersectingPolygon(lstCadFooting[j], lstSemelleLine[i]) /*&& (lstCadFooting[j] != semelleLinPath)*/)
                    {
                        counter++;
                        lstIntersectingEntity.Add(lstCadFooting[j]);
                    }
                }
                if (counter >= 2) lstSemelleLongLine.Add(lstSemelleLine[i]);
            }

            return lstIntersectingEntity.ToList();
        }

        public static Line CenterLineBetweenTwoParallelsGet(Line line1, Line line2)
        {
            if (Math.Abs(MathHelper.LineGetSlope(line1)) - Math.Abs(MathHelper.LineGetSlope(line2)) > 0.01)
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
        /// <param name="lstColLinPath">List of Columns, Walls and Shear walls in the drawing</param>
        /// <returns></returns>
        public static List<LinearPath> EntitiesInsideFootingGet(LinearPath linPathFooting, List<LinearPath> lstEntityLinPath)
        {
            List<LinearPath> lstEntityInsideFooting = new List<LinearPath>();
            for (int i = 0; i < lstEntityLinPath.Count(); i++)
            {
                if (MathHelper.IsInsidePolygon(lstEntityLinPath[i], linPathFooting))
                {
                    lstEntityInsideFooting.Add(lstEntityLinPath[i]);
                }
            }
            return lstEntityInsideFooting;
        }

        public static Point3D PointIntersectSemelleWithNearEntity(Line line, List<LinearPath> lstLinPathEntityInsideFooting)
        {
            //intersection points with the nearest polygon ionside footing
            List<Point3D> lstNearIntersectionPts = new List<Point3D>();

            #region Calculate Modified Line

            Line linemodified = LineModify(line, CADConfig.Units == linearUnitsType.Meters ? 15 : 15000
                , CADConfig.Units == linearUnitsType.Meters ? 15 : 15000);
            

            //Line linemod2 = new Line(line.StartPoint, line.EndPoint);
            //linemod2.Scale(100);

            //Line lineMod3 = new Line(line.StartPoint, line.EndPoint);
            //lineMod3.Scale(uv12 * 100);
            #endregion
            double dist = double.MaxValue;
            for (int i = 0; i < lstLinPathEntityInsideFooting.Count; i++)
            {//loop over entities inside footing
                //points of intersection of centerline with polygon inside footing
                List<Point3D> lstIntersectionPts = MathHelper.PointsIntersectOfLineSegmentWithPolygon(lstLinPathEntityInsideFooting[i], linemodified);


                //calculate distance from centerline start to intersection point of index "0"
                if (lstIntersectionPts.Count == 0)
                {
                    return null;
                }
                if (dist >= MathHelper.CalcDistanceBetweenTwoPoint3D(line.StartPoint, lstIntersectionPts[0]))
                {
                    dist = MathHelper.CalcDistanceBetweenTwoPoint3D(line.StartPoint, lstIntersectionPts[0]);
                    lstNearIntersectionPts = lstIntersectionPts;
                }

                //if (lstIntersectionPts.Count == 2) break;
            }
            if (lstNearIntersectionPts.Count == 0) return null;
            return MathHelper.MidPoint3D(lstNearIntersectionPts[0], lstNearIntersectionPts[1]);
        }

        public static Line LineModify(Line line, double scaleStPt, double scaleEndPt)
        {
            Vector3D uv12 = MathHelper.UnitVector3DFromPt1ToPt2(line.StartPoint, line.EndPoint);
            Vector3D uv21 = MathHelper.UnitVector3DFromPt1ToPt2(line.EndPoint, line.StartPoint);

            Point3D lineModifiedStPt = line.StartPoint + uv21 * scaleStPt;
            Point3D lineModifiedEndPt = line.EndPoint + uv12 * scaleEndPt;

            return new Line(lineModifiedStPt, lineModifiedEndPt);
        }

        public static Dictionary<int, LinearPath> SubmittedElementsGet(LinearPath linPathAxesBoundary, Dictionary<int, LinearPath> dicElement)
        {
            Dictionary<int, LinearPath> SubmittedElements = new Dictionary<int, LinearPath>();
            foreach (KeyValuePair<int, LinearPath> elem in dicElement)
            {
                for (int i = 0; i < elem.Value.Vertices.Length; i++)
                {
                   if(MathHelper.IsInsidePolygon(elem.Value.Vertices[i], linPathAxesBoundary))
                    {
                        SubmittedElements.Add(elem.Key, elem.Value);
                        break;
                    }
                }
            }
            return SubmittedElements;
        }

        public static List<Line> GetBbOrientedWithTallestLine(LinearPath linPath, double offset)
        {
            List<Line> lstLine = new List<Line>();

            Line[] lineArr = linPath.ConvertToLines();
            double maxLength = 0;
            Line tallestLine = null;
            
            for (int i = 0; i < lineArr.Length; i++)
            {
                if(lineArr[i].Length() > maxLength)
                {
                    tallestLine = lineArr[i];
                    maxLength = lineArr[i].Length();
                }
            }
            Line tallestOffset = tallestLine.Offset(-1 * offset, Vector3D.AxisZ) as Line;

            /*  pt4 *********l3********** pt3
                    *                   *
                    *                   *
                    *                   *
                    *                   *
               pt1  *********l1********** pt2 */

            Point3D stPt = tallestOffset.StartPoint;
            Point3D endPt = tallestOffset.EndPoint;

            Point3D pt1 = tallestOffset.StartPoint + MathHelper.UnitVectorFromPt1ToPt2(endPt, stPt) * offset;
            Point3D pt2 = tallestOffset.EndPoint + MathHelper.UnitVectorFromPt1ToPt2(stPt, endPt) * offset;
            Line l1 = new Line(pt1, pt2);
            Line l3 = l1.Offset(2 * offset, Vector3D.AxisZ) as Line;





            //Point3D pt1 = new Point3D(tallestLine.StartPoint.X - offset, tallestLine.StartPoint.Y - offset, tallestLine.StartPoint.Z);
            //Point3D pt2 = pt1 + MathHelper.UnitVectorFromPt1ToPt2(tallestLine.StartPoint, tallestLine.EndPoint) * 2 * offset;

            //Point3D pt3 = pt2 + MathHelper.UVPerpendicularToLine2DFromPt(tallestLine, new Point3D(pt1.X - 1, pt1.Y + offset, pt1.Z));
            //Point3D pt4 = pt3 + MathHelper.UVPerpendicularToLine2DFromPt(tallestLine, new Point3D(pt1.X - 1, pt1.Y + offset, pt1.Z));

            lstLine.Add(l1);
            lstLine.Add(new Line(l1.EndPoint, l3.EndPoint));
            lstLine.Add(l3);
            lstLine.Add(new Line(l3.StartPoint, l1.StartPoint));

            return lstLine;
        }
    }
}
