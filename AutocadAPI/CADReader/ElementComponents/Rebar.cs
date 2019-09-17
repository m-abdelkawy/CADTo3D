using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.ElementComponents
{
    public class Rebar
    {
        private double diameter = 0.018;
        private double area;
        private Point3D locationPt;

        public Rebar(Point3D location, double diameter)
        {
            this.locationPt = location;
            this.diameter = diameter;
        }
        public Point3D LocationPt
        {
            get { return locationPt; }
            set { locationPt = value; }
        }

        public double Area
        {
            get {
                this.area = Math.PI * Math.Pow(diameter / 2, 2);
                return area;
            }
            private set { area = value; }
        }


        public double Diameter
        {
            get { return diameter; }
            set { diameter = value; }
        }

    }
}
