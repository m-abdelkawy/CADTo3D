using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.ElementComponents
{
    public class Stirrup
    {

        #region Properties
        public List<Line> LstBranch { get; set; } = new List<Line>();
        public List<Curve> LstCurve { get; set; }
        private double CurveDiameter { get; set; }
        public double Diameter { get; set; } = DefaultValues.BarDiameter; 
        #endregion

        public Stirrup(LinearPath stirrupPath)
        {
            LstBranchPopulate(stirrupPath);
        }



        public void LstBranchPopulate(LinearPath stirrupPath)
        {
            int nVertices = stirrupPath.Vertices.Length;
            for (int i = 0; i < nVertices - 1; i++)
            {
                Point3D ptSt = stirrupPath.Vertices[i];
                Point3D ptEnd = stirrupPath.Vertices[i + 1];
                LstBranch.Add(new Line(new Point3D(ptSt.X, ptSt.Y, ptSt.Z), new Point3D(ptEnd.X, ptEnd.Y, ptEnd.Z)));
            }
        }

        public void LstCurvePopulate()
        {
            //for (int i = 0; i < lstBranch.Count; i++)
            //{
            //    if(i == lstBranch.Count - 1)
            //    {

            //    }
            //    lstCurve.Add(new Curve()
            //}
        }

    }
}
