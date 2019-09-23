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
        public override string Type { get; set; } = "RC";

        public RCFooting(LinearPath _profilePath, double thickness)
        {
            this.ProfilePath = _profilePath;
            this.Thickness = thickness;
        }
    }
}
