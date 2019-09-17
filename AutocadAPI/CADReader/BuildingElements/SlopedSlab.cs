using CADReader.Helpers;
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
        #region Constructors
        public SlopedSlab(List<Point3D> vertices)
        {
            LstFacePt = vertices;
        }
        #endregion 

        #region Properties
        
        public List<Point3D> LstFacePt { get; set; }

        public double Thickness { get; set; } = DefaultValues.SlabThinkess;
        #endregion
         
    }
}
