using CADReader.Base;
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
    public class ReinforcedCadShearWall: ReinforcedElements
    {
        #region Properties
        public ShearWall ShearWall { get; set; }
        public List<Rebar> VlRebar { get; set; }
        public Stirrup Stirrup { get; set; } 
        #endregion

        public ReinforcedCadShearWall(ShearWall _shearWall)
        {
            this.ShearWall = _shearWall;
            ReinforcementPopulate();
        }

        #region Methods
        public void LstRebarPopulate()
        {
            VlRebar = new List<Rebar>();
            LinearPath linPathRFT = (LinearPath)ShearWall.ProfilePath.Offset(-DefaultValues.ShearWallCover);
            Line[] RFTlines = linPathRFT.ConvertToLines();
            for (int i = 0; i < RFTlines.Length; i++)
            {
                Vector3D uv = MathHelper.UnitVector3DFromPt1ToPt2(RFTlines[i].StartPoint, RFTlines[i].EndPoint);
                int rebarCount = Convert.ToInt32(RFTlines[i].Length() / DefaultValues.LongBarSpacing);
                for (int j = 0; j < rebarCount; j++)
                {
                    Point3D location = RFTlines[i].StartPoint + uv * j * DefaultValues.LongBarSpacing;
                    Rebar rebar = new Rebar(location);
                    VlRebar.Add(rebar);
                }
            }
        }

        public void StirrupPopulate()
        {
            LinearPath stirrupLp = (LinearPath)ShearWall.ProfilePath.Offset(-DefaultValues.ShearWallCover * 1.2);

            //double dowelLength = (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000);
            //for (int i = 0; i < stirrupLp.Vertices.Length; i++)
            //{
            //    stirrupLp.Vertices[i].Z += dowelLength;
            //}

            Stirrup = new Stirrup(stirrupLp);
        }

        public override void ReinforcementPopulate()
        {
            LstRebarPopulate();
            StirrupPopulate();
        }

        #endregion
    }
}
