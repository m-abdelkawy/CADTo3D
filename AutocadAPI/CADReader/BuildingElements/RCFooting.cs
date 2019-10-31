using CADReader.Base;
using devDept.Eyeshot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class RCFooting:FootingBase
    {
         
        public RCFooting(LinearPath _profilePath, double thickness)
        {
            ProfilePath = _profilePath;
            Thickness = thickness;
            Type = "RC";
        }
    }
}
