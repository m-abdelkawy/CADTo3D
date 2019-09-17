using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Stair
    {
        public double Width { get; set; }
        public double Thickness { get; set; }
        public List<LinearPathEx> LstStep { get; set; }
        public bool IsUp { get; set; } = false;

        public Stair(double _width,List<Line> lstStairLines)
        {
            Width = _width;
            LstStepPopulate(lstStairLines);
        }

        public void LstStepPopulate(List<Line> lstLine)
        {
            if (lstLine[0].StartPoint.Z < lstLine[0].EndPoint.Z)
                IsUp = true;
            
            LstStep = new List<LinearPathEx>();

            double projectedLength = MathHelper.CalcDistanceBetweenTwoPoint2D(lstLine[0].StartPoint, lstLine[0].EndPoint);
            int nSteps = Convert.ToInt32(projectedLength / 0.30) + 1;

            double thata = Math.Atan2(Math.Abs(lstLine[0].StartPoint.Z - lstLine[0].EndPoint.Z), projectedLength);

            double stepWidth = projectedLength / nSteps;

            Vector3D uvStartToEndProjected = MathHelper.UnitVector3DProjectedFromPt1ToPt2(lstLine[0].StartPoint, lstLine[0].EndPoint);

            //projected Line
            Point3D stProjected = lstLine[0].StartPoint;
            Point3D endProjected = new Point3D(lstLine[0].EndPoint.X, lstLine[0].EndPoint.Y, stProjected.Z);
            Line lineProjected = new Line(stProjected, endProjected);


            Thickness = stepWidth * Math.Tan(thata);


            List<Point3D> lstPt = new List<Point3D>();
            for (int i = 0; i <= nSteps; i++)
            {
                Point3D ptOnProjectedLine10 = lstLine[0].StartPoint + uvStartToEndProjected * stepWidth * i;

                Vector3D uvWidthDir = null;


                double dist1 = MathHelper.CalcDistanceBetweenTwoPoint3D(lstLine[0].StartPoint, lstLine[1].StartPoint);
                double dist2 = MathHelper.CalcDistanceBetweenTwoPoint3D(lstLine[0].StartPoint, lstLine[1].EndPoint);
                if(dist1 > dist2)
                {
                    uvWidthDir = MathHelper.UnitVector3DFromPt1ToPt2(lstLine[0].StartPoint, lstLine[1].EndPoint);
                }
                else
                {
                    uvWidthDir = MathHelper.UnitVector3DFromPt1ToPt2(lstLine[0].StartPoint, lstLine[1].StartPoint);
                }
                Point3D ptOnProjectedLine20 = ptOnProjectedLine10 + Width * uvWidthDir;

                lstPt.Add(ptOnProjectedLine10);
                lstPt.Add(ptOnProjectedLine20);

                if(lstPt.Count >= 4)
                {
                    List<Point3D> lstStepPts = new List<Point3D>()
                    {
                        lstPt[lstPt.Count-1],
                        lstPt[lstPt.Count-2], 
                        lstPt[lstPt.Count-4],
                        lstPt[lstPt.Count-3],
                        lstPt[lstPt.Count-1]
                    };
                    LstStep.Add(new LinearPathEx(lstStepPts));
                }
            }

        }
    }
}
 