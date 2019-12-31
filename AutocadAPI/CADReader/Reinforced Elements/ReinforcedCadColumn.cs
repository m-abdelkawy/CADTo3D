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
        public Column CadColumn { get; set; }
        public List<Rebar> LstRebar { get; set; }
        public Stirrup Stirrup { get; set; } 
        #endregion

        #region Constructors
        public ReinforcedCadColumn(Column column, double lvl)
        {
            CadColumn = column;
            ReinforcementPopulate();
        } 
        #endregion


        #region Methods
        public void LstRebarPopulate()
        {

            List<Point3D> points = new List<Point3D>();
            
            LstRebar = new List<Rebar>();
            LinearPath stirrupLinPath = (LinearPath)CadColumn.ColPath.Offset(-CadColumn.Cover * 1.2);
            Line[] stirrupBranches = stirrupLinPath.ConvertToLines();
            for (int i = 0; i < stirrupBranches.Length; i++)
            {
                Line branch = stirrupBranches[i];
                Rebar rebarCorner1, rebarCorner2, rebarMid;
            
                if(!points.Contains(branch.StartPoint))
                {
                    rebarCorner1 = new Rebar(branch.StartPoint);
                    LstRebar.Add(rebarCorner1);
                }
                if (!points.Contains(branch.EndPoint))
                {
                    rebarCorner2 = new Rebar(branch.EndPoint);
                    LstRebar.Add(rebarCorner2);
                }
                rebarMid = new Rebar(branch.MidPoint);
                LstRebar.Add(rebarMid);
                points.AddRange(branch.Vertices);
                

            } 
             
        }

        public void StirrupPopulate()
        { 
            LinearPath stirrupLp = (LinearPath)CadColumn.ColPath.Offset(-CadColumn.Cover);

            double dowelLength = (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000);
            for (int i = 0; i < stirrupLp.Vertices.Length; i++)
            {
                stirrupLp.Vertices[i].Z += dowelLength;
            }
             
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
