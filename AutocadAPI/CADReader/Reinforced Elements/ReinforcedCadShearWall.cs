using CADReader.BuildingElements;
using CADReader.ElementComponents;
using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Reinforced_Elements
{
    public class ReinforcedCadShearWall
    {
        public ShearWall ShearWall { get; set; }
        public List<Rebar> VlRebar { get; set; }
        public Stirrup Stirrup { get; set; }

        public ReinforcedCadShearWall(ShearWall _shearWall)
        {
            this.ShearWall = _shearWall;
        }

        #region Methods
        public void LstRebarPopulate()
        {

        }

        //public void StirrupPopulate()
        //{
        //    LinearPath stirrupLp = (LinearPath)CadWall.LinPathWall.Offset(-DefaultValues.WallCover * 1.2);

        //    Stirrup = new Stirrup(stirrupLp);
        //}

        //public override void ReinforcementPopulate()
        //{
        //    LstRebarPopulate();
        //    StirrupPopulate();
        //}

        #endregion
    }
}
