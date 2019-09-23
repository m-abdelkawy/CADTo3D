using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class ShearWall
    {
        public LinearPath ProfilePath { get; set; }

        public ShearWall(LinearPath _profilePath)
        {
            this.ProfilePath = _profilePath;
        }
    }
}
