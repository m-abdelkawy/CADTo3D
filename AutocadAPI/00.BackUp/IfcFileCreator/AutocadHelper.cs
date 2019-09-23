using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutocadAPI
{
    public static class AutocadHelper
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


    }
}
