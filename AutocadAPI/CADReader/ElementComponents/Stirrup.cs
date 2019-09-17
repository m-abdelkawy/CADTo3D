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
        private double diameter = 0.018;
        public List<Line> lstBranch = new List<Line>();
        public List<Curve> lstCurve;
        private double curveDiameter = 0.03;

        public Stirrup(double _diameter, LinearPath stirrupPath)
        {
            diameter = _diameter;
            LstBranchPopulate(stirrupPath);
        }


        public double Diameter
        {
            get { return diameter; }
            set { diameter = value; }
        }

        public void LstBranchPopulate(LinearPath stirrupPath)
        {
            int nVertices = stirrupPath.Vertices.Length;
            for (int i = 0; i < nVertices - 1; i++)
            {
                Point3D ptSt = stirrupPath.Vertices[i];
                Point3D ptEnd = stirrupPath.Vertices[i + 1];
                lstBranch.Add(new Line(new Point3D(ptSt.X, ptSt.Y, ptSt.Z), new Point3D(ptEnd.X, ptEnd.Y, ptEnd.Z)));
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
