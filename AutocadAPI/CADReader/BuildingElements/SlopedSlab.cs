using CADReader.Helpers;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class SlopedSlab
    {
        #region Properties

        public LinearPathEx LinPathSlopedSlab { get; set; }

        public double Thickness { get; set; } = DefaultValues.SlabThinkess;
        #endregion

        #region Constructors
        public SlopedSlab(List<Point3D> vertices)
        {
            LinPathSlopedSlab = new LinearPathEx(vertices.ToArray());
        }
        #endregion



    }
}
