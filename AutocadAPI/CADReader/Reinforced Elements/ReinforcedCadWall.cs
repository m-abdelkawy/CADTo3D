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
    public class ReinforcedCadWall : ReinforcedElements
    {
        #region Properties
        public Wall CadWall { get; set; }
        public List<Rebar> LstRebar { get; set; } = new List<Rebar>();
        public Stirrup Stirrup { get; set; } 
        #endregion

        #region Constructor
        public ReinforcedCadWall(Wall _cadWall)
        {
            this.CadWall = _cadWall;
            ReinforcementPopulate();
        } 
        #endregion

        #region Methods
        public void LstRebarPopulate()
        {
            LinearPath rftPath = CadWall.LinPathWall.Offset(DefaultValues.WallCover * -1) as LinearPath;

            Line[] rftPathLines = rftPath.ConvertToLines();

            Line longDirLine = null;
            Line shortDirLine = null;

            double longDirLength = double.MinValue;
            double shortDirLength = double.MaxValue;

            for (int i = 0; i < rftPathLines.Length; i++)
            {
                if (rftPathLines[i].Length() > longDirLength)
                {
                    longDirLine = rftPathLines[i];
                    longDirLength = rftPathLines[i].Length();
                }

                if (rftPathLines[i].Length() < shortDirLength)
                {
                    shortDirLine = rftPathLines[i];
                    shortDirLength = rftPathLines[i].Length();
                }
            }

            

            Vector3D uvLength = MathHelper.UnitVector3DFromPt1ToPt2(longDirLine.StartPoint, longDirLine.EndPoint);

            double dia = DefaultValues.BarDiameter;

            Vector3D uvWidth;
            if (longDirLine.Vertices.Contains(shortDirLine.StartPoint))
            {
                uvWidth = MathHelper.UnitVector3DFromPt1ToPt2(shortDirLine.StartPoint, shortDirLine.EndPoint);
            }
            else
            {
                uvWidth = MathHelper.UnitVector3DFromPt1ToPt2(shortDirLine.EndPoint, shortDirLine.StartPoint);
            }

            //placement
            int nRebar = Convert.ToInt32(longDirLine.Length() / DefaultValues.LongBarSpacing);
            for (int i = 0; i < nRebar; i++)
            {
                Point3D locationPt1 = longDirLine.StartPoint + uvLength * DefaultValues.LongBarSpacing * i;
                Point3D locationPt2 = locationPt1 + uvWidth * shortDirLength;
                Rebar rebar1 = new Rebar(locationPt1);
                Rebar rebar2 = new Rebar(locationPt2);
                LstRebar.Add(rebar1);
                LstRebar.Add(rebar2);
            }


        }

        public void StirrupPopulate()
        {
            LinearPath stirrupLp = (LinearPath)CadWall.LinPathWall.Offset(-DefaultValues.WallCover * 1.2);

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
