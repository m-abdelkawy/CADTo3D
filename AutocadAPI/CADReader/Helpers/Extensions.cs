using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Helpers
{
    public static class Extensions
    {
        internal static Line LineGetNearestParallel(this Line line, Line[] arrLines)
        {
            double tolerance = 0.001;
            Line nearestParallelLine = null;
            double minDist = double.MaxValue;
            double lineSlope = MathHelper.LineGetSlope(line);
            int n = arrLines.Length;
            List<Line> lstParallels = new List<Line>();
            for (int i = 0; i < n; i++)
            {
                try
                {
                    Line other = arrLines[i];
                    if (Math.Abs(lineSlope - MathHelper.LineGetSlope(other)) <= tolerance
                        ||
                        (double.IsNaN(lineSlope) && double.IsNaN(MathHelper.LineGetSlope(other)))
                        )
                    {
                        double distance = MathHelper.DistanceBetweenTwoParallels(line, other);
                        if (distance <= tolerance)
                            continue;
                        if (distance < minDist)
                        {
                            minDist = distance;
                            nearestParallelLine = arrLines[i];
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return nearestParallelLine;
        }

        internal static Line LineGetNearestParallelByNormalVector(this Line line, Line[] arrLines)
        {
            double tolerance = 0.001;
            Line nearestParallelLine = null;
            double minDist = double.MaxValue;
            double lineSlope = MathHelper.LineGetSlope(line);
            int n = arrLines.Length;
            
            Vector3D uvPerpLine = MathHelper.UVPerpendicularToLine2DFromPt(line, line.MidPoint);
            Point3D stPt = line.MidPoint - (CADConfig.Units == linearUnitsType.Meters ? 100 : 10000) * uvPerpLine;
            Point3D endPt = line.MidPoint + (CADConfig.Units == linearUnitsType.Meters ? 100 : 10000) * uvPerpLine;

            Line perpLine = new Line(stPt, endPt);

            for (int i = 0; i < n; i++)
            {
                Line other = arrLines[i];

                if (other == null)
                    continue;

                if (MathHelper.IsLineSegmentsIntersected(perpLine, other))
                {
                    try
                    {
                        if (Math.Abs(lineSlope - MathHelper.LineGetSlope(other)) <= tolerance
                            ||
                            (double.IsNaN(lineSlope) && double.IsNaN(MathHelper.LineGetSlope(other)))
                            )
                        {
                            double distance = MathHelper.DistanceBetweenTwoParallels(line, other);
                            if (distance <= tolerance)
                                continue;
                            if (distance < minDist)
                            {
                                minDist = distance;
                                nearestParallelLine = arrLines[i];
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                
            }
            return nearestParallelLine;
        }


        internal static List<Line> LineGetNearestParallels(this Line line, Line[] arrLines)
        {
            double lineSlope = MathHelper.LineGetSlope(line);

            double tolerance = 0.001;

            double minDist = double.MaxValue;

            List<Line> lstParallelsForLine = new List<Line>();
            Line nearestParallelForOneVector = null;
            //Dictionary<int, List<Vector3D>> dicPerpVector = new Dictionary<int, List<Vector3D>>();

            Vector3D uvLineStToEnd = MathHelper.UnitVector3DFromPt1ToPt2(line.StartPoint, line.EndPoint);

            double propsedSegLength = CADConfig.Units == linearUnitsType.Meters ? 1 : 1000;
            int nSegments = Convert.ToInt32(line.Length() / propsedSegLength) + 1;

            double realSegLength = line.Length() / nSegments;

            List<Vector3D> lstPerpVectors = new List<Vector3D>();
            for (int i = 0; i < nSegments - 1; i++)
            {

                Point3D stPt = line.StartPoint + uvLineStToEnd * i * realSegLength;
                //Point3D endPt = line.StartPoint + uvLineStToEnd * (i + 1) * realSegLength;

                Vector3D vPos = MathHelper.UVPerpendicularToLine2DFromPt(line, stPt) * 1000;
                Vector3D vNeg = MathHelper.UVPerpendicularToLine2DFromPt(line, stPt) * -1000;
                //lstPerpVectors.Add(vPos);
                //lstPerpVectors.Add(MathHelper.UVPerpendicularToLine2DFromPt(line, stPt) * -1000);

                Line lPos = new Line(stPt, stPt + vPos);
                Line lNeg = new Line(stPt, stPt + vNeg);

                for (int j = 0; j < arrLines.Length; j++)
                {
                    if (MathHelper.IsLinesIntersected(lPos, arrLines[j]))
                    {
                        try
                        {
                            Line other = arrLines[j];
                            if (Math.Abs(lineSlope - MathHelper.LineGetSlope(other)) <= tolerance
                                ||
                                (double.IsNaN(lineSlope) && double.IsNaN(MathHelper.LineGetSlope(other)))
                                )
                            {
                                double distance = MathHelper.DistanceBetweenTwoParallels(line, other);
                                if (distance <= tolerance)
                                    continue;
                                if (distance <= minDist)
                                {
                                    minDist = distance;
                                    nearestParallelForOneVector = other;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    if (MathHelper.IsLinesIntersected(lNeg, arrLines[j]))
                    {
                        try
                        {
                            Line other = arrLines[j];
                            if (Math.Abs(lineSlope - MathHelper.LineGetSlope(other)) <= tolerance
                                ||
                                (double.IsNaN(lineSlope) && double.IsNaN(MathHelper.LineGetSlope(other)))
                                )
                            {
                                double distance = MathHelper.DistanceBetweenTwoParallels(line, other);
                                if (distance <= tolerance)
                                    continue;
                                if (distance <= minDist)
                                {
                                    minDist = distance;
                                    nearestParallelForOneVector = other;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    
                }
                if (!(lstParallelsForLine.Contains(nearestParallelForOneVector)))
                {
                    lstParallelsForLine.Add(nearestParallelForOneVector);
                }
            }

            return lstParallelsForLine;
        }

        
    }
}
