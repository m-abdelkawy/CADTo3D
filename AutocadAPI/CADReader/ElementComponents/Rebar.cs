using CADReader.Helpers;
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

        public Rebar(Point3D location, double diameter)
        {
           LocationPt = location;
           Diameter = diameter;
        }
        public Point3D LocationPt { get; set; }

        public double Diameter { get; set; } = DefaultValues.BarDiameter;

    }
}
