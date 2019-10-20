using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Semelle
    {
        public LinearPath HzLinPath { get; set; }
        public double Length { get; set; }
        public double Thickness { get; set; }

        public Semelle(LinearPath linPath,double thickness)
        {
            HzLinPath = linPath;
            Thickness = thickness;
        }

    }
}
