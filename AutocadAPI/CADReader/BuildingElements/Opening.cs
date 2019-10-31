using CADReader.Base;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Opening
    {
        #region Properties 

        public LinearPath LinPathOpening { get; set; }
        #endregion

        public Opening(LinearPath _linPath)
        {
             
            LinPathOpening = _linPath;
        } 
       
    }
}
