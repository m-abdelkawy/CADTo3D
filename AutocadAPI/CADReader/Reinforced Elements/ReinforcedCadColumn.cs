using CADReader.Base;
using CADReader.BuildingElements;
using CADReader.ElementComponents;
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
        private RectColumn rectColumn;
        public List<Rebar> lstRebar;
        public Stirrup stirrup;
        public double spacing = 0.15;

        public ReinforcedCadColumn(RectColumn column)
        {
            RectColumn = column;
            ReinforcementPopulate();
        }
        public RectColumn RectColumn
        {
            get { return rectColumn; }
            set { rectColumn = value; }
        }

        #region Methods
        public void LstRebarPopulate()
        {
            Vector3D uvLength = MathHelper.UnitVector3DFromPt1ToPt2(this.rectColumn.CenterPt, this.rectColumn.PtLengthDir);
            Line l = new Line(this.rectColumn.CenterPt, this.rectColumn.PtLengthDir);
            

            double dia = 0.025;
            Vector3D uvWidth = MathHelper.UVPerpendicularToLine2DFromPt(l, this.rectColumn.CenterPt);


            //middle rebars
            
            Point3D p1Mid = this.rectColumn.CenterPt + uvWidth * (rectColumn.Width/2 - rectColumn.Cover - dia / 2);
            Point3D p2Mid = this.rectColumn.CenterPt - uvWidth * (rectColumn.Width/2 - rectColumn.Cover - dia / 2);
            Rebar rebarMid1 = new Rebar(p1Mid, dia);
            Rebar rebarMid2 = new Rebar(p2Mid, dia);

            //Corner Rebar
            Point3D p3Mid = p1Mid + uvLength * (rectColumn.Length/2 - rectColumn.Cover - dia / 2);
            Point3D p4Mid = p1Mid - uvLength * (rectColumn.Length/2 - rectColumn.Cover - dia / 2);
            Rebar rebarCorner1 = new Rebar(p3Mid, dia);
            Rebar rebarCorner2 = new Rebar(p4Mid, dia);

            Point3D p5Mid = p2Mid + uvLength * (rectColumn.Length / 2 - rectColumn.Cover - dia / 2);
            Point3D p6Mid = p2Mid - uvLength * (rectColumn.Length / 2 - rectColumn.Cover - dia / 2);
            Rebar rebarCorner3 = new Rebar(p5Mid, dia);
            Rebar rebarCorner4 = new Rebar(p6Mid, dia);

            this.lstRebar = new List<Rebar>
            {
                rebarMid1,rebarMid2,rebarCorner1,rebarCorner2,rebarCorner3,rebarCorner4
            };
        }

        public void StirrupPopulate()
        {
            LinearPath stirrupLp = (LinearPath)rectColumn.ColPath.Offset(-rectColumn.Cover * 1.2);
            for (int i = 0; i < stirrupLp.Vertices.Length; i++)
            {
                stirrupLp.Vertices[i] *= 1000;
                stirrupLp.Vertices[i].Z += 1000;
            }

            stirrup = new Stirrup(12, stirrupLp);
        }

        public override void ReinforcementPopulate()
        {
            LstRebarPopulate();
            StirrupPopulate();
        }

        #endregion
    }
}
