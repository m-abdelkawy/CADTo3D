using CADReader.Base;
using devDept.Eyeshot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class PCFooting: FootingBase
    {
         

        public PCFooting(LinearPath _profilePath, double thickness)
        {
            this.ProfilePath = _profilePath;
            this.Thickness = thickness;
            Type = "PC";
        }
    }
}
