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
        #region Data members
        private double thickness = 0.25;
        private List<Point3D> lstFacePt;
        #endregion



        #region Constructors
        public SlopedSlab(List<Point3D> vertices)
        {
            this.lstFacePt = vertices;
        }
        #endregion 

        #region Properties
        
        public List<Point3D> LstFacePt
        {
            get { return lstFacePt; }
            set { lstFacePt = value; }
        }

        public double Thickness
        {
            get { return thickness; }
            set { thickness = value; }
        }
        #endregion
         
    }
}
