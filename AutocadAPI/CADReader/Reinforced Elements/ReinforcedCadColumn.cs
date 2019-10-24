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
    public class ReinforcedCadColumn:ReinforcedElements
    {
        #region Properties
        public RectColumn RectColumn { get; set; }
        public List<Rebar> LstRebar { get; set; }
        public Stirrup Stirrup { get; set; }
        public double Spacing { get; set; } = DefaultValues.StirrupsSpacing;
        #endregion

        #region Constructors
        public ReinforcedCadColumn(RectColumn column, double lvl)
        {
            RectColumn = column;
            ReinforcementPopulate();
        } 
        #endregion


        #region Methods
        public void LstRebarPopulate()
        {
            Vector3D uvLength = MathHelper.UnitVector3DFromPt1ToPt2(this.RectColumn.CenterPt, this.RectColumn.PtLengthDir);
            Line l = new Line(this.RectColumn.CenterPt, this.RectColumn.PtLengthDir);
            

            double dia = DefaultValues.BarDiameter;
            Vector3D uvWidth = MathHelper.UVPerpendicularToLine2DFromPt(l, this.RectColumn.CenterPt);


            //middle rebars
            
            Point3D p1Mid = this.RectColumn.CenterPt + uvWidth * (RectColumn.Width/2 - RectColumn.Cover - dia / 2);
            Point3D p2Mid = this.RectColumn.CenterPt - uvWidth * (RectColumn.Width/2 - RectColumn.Cover - dia / 2);
            Rebar rebarMid1 = new Rebar(p1Mid);
            Rebar rebarMid2 = new Rebar(p2Mid);

            //Corner Rebar
            Point3D p3Mid = p1Mid + uvLength * (RectColumn.Length/2 - RectColumn.Cover - dia / 2);
            Point3D p4Mid = p1Mid - uvLength * (RectColumn.Length/2 - RectColumn.Cover - dia / 2);
            Rebar rebarCorner1 = new Rebar(p3Mid);
            Rebar rebarCorner2 = new Rebar(p4Mid);

            Point3D p5Mid = p2Mid + uvLength * (RectColumn.Length / 2 - RectColumn.Cover - dia / 2);
            Point3D p6Mid = p2Mid - uvLength * (RectColumn.Length / 2 - RectColumn.Cover - dia / 2);
            Rebar rebarCorner3 = new Rebar(p5Mid);
            Rebar rebarCorner4 = new Rebar(p6Mid);

            this.LstRebar = new List<Rebar>
            {
                rebarMid1,rebarMid2,rebarCorner1,rebarCorner2,rebarCorner3,rebarCorner4
            };
        }

        public void StirrupPopulate()
        {
            LinearPath stirrupLp = (LinearPath)RectColumn.ColPath.Offset(-RectColumn.Cover * 1.2);
            //for (int i = 0; i < stirrupLp.Vertices.Length; i++)
            //{
            //    //stirrupLp.Vertices[i].Z += CADConfig.Units == linearUnitsType.Meters?lvl+1:lvl+1000;
            //    //stirrupLp.Vertices[i].Z += lvl;
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
