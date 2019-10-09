using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Wall
    {

        #region Properties
        public LinearPath LinPathWall { get; set; }
        #endregion


        public Wall(LinearPath wallLinPath)
        {
            this.LinPathWall = wallLinPath;
        }


     
    }
}
