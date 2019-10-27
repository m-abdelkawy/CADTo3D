using CADReader.Base;
using CADReader.Helpers;
using CADReader.Reinforced_Elements;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Foundation : FloorBase
    {
        #region Properties
        private double PcThickness { get; set; } = DefaultValues.PCFootingThinkess;
        private double RcThickness { get; set; } = DefaultValues.RCFootingThinkess;
       
        public List<SlopedSlab> Ramps { get; set; }
        public List<Wall> RetainingWalls { get; set; }
        public List<ReinforcedCadColumn> RcColumns { get; set; }
        public List<ShearWall> ShearWalls { get; set; }

        public List<PCFooting> PCFooting { get; set; }
        public List<RCFooting> RCFooting { get; set; }
        public List<Semelle> Semelles { get; set; }
        public List<ReinforcedCadSemelle> ReinforcedSemelles { get; set; }
        public List<ReinforcedCadFooting> ReinforcedCadFootings{ get; set; }

        public List<ReinforcedCadWall> ReinforcedCadWalls { get; set; }

        #endregion

        #region Constructor
        public Foundation(ReadAutodesk cadReader, double level)
        {
            Level = level;
            //GetPCRectFootings(cadReader);
            //GetRCRectFootings(cadReader);
            Ramps = GetRamps(cadReader);
            this.RetainingWalls = base.GetWalls(cadReader);
            this.RcColumns = base.GetRCColumns(base.GetColumns(cadReader));

            GetPCFooting(cadReader);
            GetRCFooting(cadReader);
            GetSmelles(cadReader);
            ShearWalls = GetShearWalls(cadReader);

            GetReinforcedSemelles(cadReader);

            ReinforcedCadFootings = GetReinforcedFootings(RCFooting);

            ReinforcedCadWalls = base.GetRCWalls(this.RetainingWalls);
        }

        private void GetReinforcedSemelles(ReadAutodesk cadReader)
        {
            this.ReinforcedSemelles = new List<ReinforcedCadSemelle>();
            for (int i = 0; i < this.Semelles.Count(); i++)
            {
                this.ReinforcedSemelles.Add(new ReinforcedCadSemelle(cadReader, Semelles[i]));
            }
        }

        private void GetSmelles(ReadAutodesk cadReader)
        {
            Semelles = new List<Semelle>();
            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.Semelle);
            for (int i = 0; i < lstPLine.Count; i++)
            {
                for (int j = 0; j < lstPLine[i].Vertices.Length; j++)
                {
                    lstPLine[i].Vertices[j].Z = Level+DefaultValues.PCFootingThinkess;
                }
            }

            for (int i = 0; i < lstPLine.Count; i++)
            {
                Line shortestLine = CadHelper.ShortestLineGet(lstPLine[i]);
                

                List<LinearPath> lstCol = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.Column);
                //double thickness = CadHelper.IsIntersectingWithElmCategory(shortestLine, lstCol) ? DefaultValues.SmellesWithColumnThickness : DefaultValues.SmellesWithFootingThickness;

                Semelle semelle = new Semelle(lstPLine[i], DefaultValues.SmellesWithFootingThickness);

                Semelles.Add(semelle);
            }
        }
        #endregion

        private void GetPCFooting(ReadAutodesk cadFileReader)
        {
            this.PCFooting = new List<PCFooting>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.PCFooting, true);

            for (int i = 0; i < lstPLine.Count; i++)
            {
                for (int j = 0; j < lstPLine[i].Vertices.Length; j++)
                {
                    lstPLine[i].Vertices[j].Z = Level;
                }
            }

            List<PCFooting> lstPCFooting = lstPLine.Select(s => new PCFooting(s, PcThickness)).ToList();

            PCFooting.AddRange(lstPCFooting);

        }

        private void GetRCFooting(ReadAutodesk cadFileReader)
        {
            this.RCFooting = new List<RCFooting>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.RCFooting, true);

            for (int i = 0; i < lstPLine.Count; i++)
            {
                for (int j = 0; j < lstPLine[i].Vertices.Length; j++)
                {
                    lstPLine[i].Vertices[j].Z = Level + DefaultValues.PCFootingThinkess;
                }
            }

            //if (cadFooting.Type == "RC")
            //{ //From IFC class
            //    for (int i = 0; i < cadFooting.ProfilePath.Vertices.Length; i++)
            //    {
            //        cadFooting.ProfilePath.Vertices[i].Z += DefaultValues.PCFootingThinkess;
            //    }
            //}

            List<RCFooting> lstRCFooting = lstPLine.Select(s => new RCFooting(s, RcThickness)).ToList();

            RCFooting.AddRange(lstRCFooting);

        }


        #region Old Code
        //public List<PCRectFooting> PCRectFooting { get; set; }
        //public List<RCRectFooting> RCRectFooting { get; set; }

        //private void GetPCRectFootings(ReadAutodesk cadReader)
        //{
        //    PCRectFooting = new List<PCRectFooting>();

        //    List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.PCFooting).Where(pl => pl.IsClosed).ToList();

        //    for (int i = 0; i < lstPLine.Count; i++)
        //    {
        //        PCRectFooting footing = RectFootingCreate(lstPLine[i], PcThickness, "PC") as PCRectFooting;
        //        PCRectFooting.Add(footing);
        //    }

        //}

        //private void GetRCRectFootings(ReadAutodesk cadReader)
        //{
        //    RCRectFooting = new List<RCRectFooting>();

        //    List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.RCFooting).Where(pl => pl.IsClosed).ToList();

        //    for (int i = 0; i < lstPLine.Count; i++)
        //    {
        //        RCRectFooting footing = RectFootingCreate(lstPLine[i], RcThickness, "RC") as RCRectFooting;
        //        RCRectFooting.Add(footing);
        //    }

        //}

        //private FootingBase RectFootingCreate(LinearPath pLine, double thickness, string type)
        //{
        //    double width = double.MaxValue;
        //    double length = 0;

        //    Point3D widthMidPt = Point3D.Origin;
        //    int nVertices = pLine.Vertices.Length;

        //    for (int j = 0; j < nVertices - 1; j++)
        //    {
        //        double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(pLine.Vertices[j], pLine.Vertices[j + 1]);
        //        width = Math.Min(width, dist);
        //        if (width == dist)
        //        {
        //            widthMidPt = MathHelper.MidPoint3D(pLine.Vertices[j], pLine.Vertices[j + 1]);
        //        }
        //        length = Math.Max(length, dist);
        //    }


        //    Point3D center = (pLine.Vertices[0] + pLine.Vertices[2]) / 2.0;
        //    center.Z = Level;
        //    widthMidPt.Z = Level;

        //    if (type == "RC")
        //        return new RCRectFooting(width, length, thickness, center, widthMidPt);
        //    else
        //        return new PCRectFooting(width, length, thickness, center, widthMidPt);
        //}

        //private FootingBase RandomProfiletFootingCreate(LinearPath pLine, double thickness, string type)
        //{
        //    if (type == FoundationType.RC)
        //        return new RCFooting(pLine, thickness);
        //    else if (type == FoundationType.PC)
        //        return new PCFooting(pLine, thickness);

        //    return null;
        //} 
        #endregion


        public List<ReinforcedCadFooting> GetReinforcedFootings(List<RCFooting> LstRcFootings)
        {
            List<ReinforcedCadFooting> RcFootings = new List<ReinforcedCadFooting>();
            ReinforcedCadFooting RcFooting = null;
            foreach (var footing in LstRcFootings)
            {
                RcFooting = new ReinforcedCadFooting(footing);
                RcFootings.Add(RcFooting);
            }

            return RcFootings;
        }


    }
}
