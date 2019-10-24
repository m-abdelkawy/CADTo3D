using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.ElectricalElements
{
    public class ElectricalConduit
    {
        public Entity CurvePath { get; set; }
        public double Diameter { get; set; } = DefaultValues.ConduitDiameter;
        public ElectricalConduit(Entity _curvePath)
        {
            this.CurvePath = _curvePath;
        }
    }
}
