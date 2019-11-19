using CADReader.Base;
using CADReader.ElementComponents;
using CADReader.Helpers;
using CADReader.Reinforced_Elements;
using devDept.Eyeshot;
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

        public List<SlopedSlab> LstRamp { get; set; }
        public List<ReinforcedCadColumn> LstRcColumn { get; set; }

        public List<PCFooting> LstPCFooting { get; set; }
        public List<ReinforcedCadSemelle> LstRCSemelle { get; set; }
        public List<ReinforcedCadFooting> LstRCCadFooting { get; set; }

        public List<ReinforcedCadWall> LstRCCadWall { get; set; }
        public List<ReinforcedCadShearWall> LstRCShearWall { get; set; }
        public List<Axis> LstAxis { get; set; }

        #endregion

        #region Constructor
        public Foundation(ReadAutodesk cadReader, double level)
        {
            Level = level;

            //Ramps
            LstRamp = GetRamps(cadReader);

            //Reinforced Columns
            LstRcColumn = base.GetRCColumns(cadReader);

            //PC Footingds
            GetPCFooting(cadReader);

            //RC Footings
            GetRCFooting(cadReader);

            //RC Shear Walls
            LstRCShearWall = GetRCShearWalls(cadReader);

            //RC Semelles
            LstRCSemelle= GetReinforcedSemelles(cadReader);

            //RC Footing
            LstRCCadFooting = GetReinforcedFootings(cadReader);

            //RC Retaining Wall
            LstRCCadWall = base.GetRCWalls(cadReader);

        }

        public Foundation(ReadAutodesk cadReader, double height, double level)
        {
            Level = level;
            Height = height;

            //Ramps
            LstRamp = GetRamps(cadReader);

            //Reinforced Columns
            LstRcColumn = base.GetRCColumns(cadReader);

            //PC Footingds
            GetPCFooting(cadReader);

            //RC Footings
            GetRCFooting(cadReader);

            //RC Shear Walls
            LstRCShearWall = GetRCShearWalls(cadReader);

            //RC Semelles
            LstRCSemelle = GetReinforcedSemelles(cadReader);

            //RC Footing
            LstRCCadFooting = GetReinforcedFootings(cadReader);

            //RC Retaining Wall
            LstRCCadWall = base.GetRCWalls(cadReader);

            //floor axes
            LstAxis = GetAxes(cadReader);
        }

        #endregion


        #region Class Method
        private void GetPCFooting(ReadAutodesk cadFileReader)
        {
            this.LstPCFooting = new List<PCFooting>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.PCFooting, true);

            for (int i = 0; i < lstPLine.Count; i++)
            {
                for (int j = 0; j < lstPLine[i].Vertices.Length; j++)
                {
                    lstPLine[i].Vertices[j].Z = Level;
                }
            }

            List<PCFooting> lstPCFooting = lstPLine.Select(s => new PCFooting(s, PcThickness)).ToList();

            LstPCFooting.AddRange(lstPCFooting);

        }

        private List<RCFooting> GetRCFooting(ReadAutodesk cadFileReader)
        {
            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.RCFooting, true);

            for (int i = 0; i < lstPLine.Count; i++)
            {
                for (int j = 0; j < lstPLine[i].Vertices.Length; j++)
                {
                    lstPLine[i].Vertices[j].Z = Level + DefaultValues.PCFootingThinkess;
                }
            }

            List<RCFooting> lstRCFooting = lstPLine.Select(s => new RCFooting(s, RcThickness)).ToList();


            return lstRCFooting;
        }

        public List<ReinforcedCadFooting> GetReinforcedFootings(ReadAutodesk cadFileReader)
        {
            List<RCFooting> LstRcFootings = GetRCFooting(cadFileReader);

            List<ReinforcedCadFooting> RcFootings = new List<ReinforcedCadFooting>();
            ReinforcedCadFooting RcFooting = null;
            foreach (var footing in LstRcFootings)
            {
                RcFooting = new ReinforcedCadFooting(footing);
                RcFootings.Add(RcFooting);
            }

            return RcFootings;
        }
        

        private List<Semelle> GetSmelles(ReadAutodesk cadReader)
        {
            List<Semelle> LstSemelle = new List<Semelle>();
            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.Semelle);
            for (int i = 0; i < lstPLine.Count; i++)
            {
                for (int j = 0; j < lstPLine[i].Vertices.Length; j++)
                {
                    lstPLine[i].Vertices[j].Z = Level + DefaultValues.PCFootingThinkess;
                }
            }

            for (int i = 0; i < lstPLine.Count; i++)
            {
                Line shortestLine = CadHelper.ShortestLineGet(lstPLine[i]);


                List<LinearPath> lstCol = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.Column);
                //double thickness = CadHelper.IsIntersectingWithElmCategory(shortestLine, lstCol) ? DefaultValues.SmellesWithColumnThickness : DefaultValues.SmellesWithFootingThickness;

                Semelle semelle = new Semelle(lstPLine[i], DefaultValues.SmellesWithFootingThickness);

                LstSemelle.Add(semelle);
            }

            return LstSemelle;
        }

        private List<ReinforcedCadSemelle> GetReinforcedSemelles(ReadAutodesk cadReader)
        {
            List<Semelle> lstSemelle = GetSmelles(cadReader);

            List<ReinforcedCadSemelle> lstRCSemelle = new List<ReinforcedCadSemelle>();

            for (int i = 0; i < lstSemelle.Count(); i++)
            {
                lstRCSemelle.Add(new ReinforcedCadSemelle(cadReader, lstSemelle[i]));
            }

            return lstRCSemelle;
        }

        private List<Axis> GetAxes(ReadAutodesk cadReader)
        {
            //get lines and plines of axes
            List<Entity> lstEntAxes = CadHelper.EntitiesGetByLayerName(cadReader, CadLayerName.Axes);

            List<Axis> lstAxis = new List<Axis>();

            //get axis block
            Block blkCircle = cadReader.Blocks.Where(b => b.Name == CadBlockName.GridCircle).FirstOrDefault();

            //get lines and polylines from axis block
            List<LinearPath> lstLinPathAxes = new List<LinearPath>();
            for (int i = 0; i < lstEntAxes.Count; i++)
            {
                if (lstEntAxes[i] is LinearPath)
                {
                    lstLinPathAxes.Add(lstEntAxes[i] as LinearPath);
                }
                else if (lstEntAxes[i] is Line)
                {
                    LinearPath linPath = new LinearPath((lstEntAxes[i] as Line).StartPoint, (lstEntAxes[i] as Line).EndPoint);
                    lstLinPathAxes.Add(linPath);
                }
            }

            //get circle grid blocks
            List<BlockReferenceEx> lstGridCircleBlkRef = cadReader.Entities.Where(b => b is BlockReferenceEx).Cast<BlockReferenceEx>().ToList();

            //get circles from circle grid block


            //for each line get the nearest two circles to its end points and their attribute
            double circleRadius = (blkCircle.Entities.FirstOrDefault(e => e is Circle) as Circle).Radius;

            for (int i = 0; i < lstLinPathAxes.Count(); i++)
            {
                List<Circle> lstCircleAddToAxis = new List<Circle>();

                double minDist1 = double.MaxValue;
                double minDist2 = double.MaxValue;

                Point3D insertionPt1 = new Point3D(0, 0, 0);
                Point3D insertionPt2 = new Point3D(0, 0, 0);

                string textAttr1 = "";
                string textAttr2 = "";


                for (int j = 0; j < lstGridCircleBlkRef.Count(); j++)
                {
                    //first end point
                    double dist1 = MathHelper.CalcDistanceBetweenTwoPoint3D(lstLinPathAxes[i].StartPoint, lstGridCircleBlkRef[j].InsertionPoint);
                    if (dist1 < minDist1)
                    {
                        minDist1 = dist1;
                        insertionPt1 = lstGridCircleBlkRef[j].InsertionPoint;
                        textAttr1 = lstGridCircleBlkRef[j].Attributes["GRID-CIRC"].Value.ToString();
                    }

                    //second end point
                    double dist2 = MathHelper.CalcDistanceBetweenTwoPoint3D(lstLinPathAxes[i].EndPoint, lstGridCircleBlkRef[j].InsertionPoint);
                    if (dist2 < minDist2)
                    {
                        minDist2 = dist2;
                        insertionPt2 = lstGridCircleBlkRef[j].InsertionPoint;
                        textAttr2 = lstGridCircleBlkRef[j].Attributes["GRID-CIRC"].Value.ToString();
                    }
                }
                Circle c1 = new Circle(insertionPt1, circleRadius);
                Circle c2 = new Circle(insertionPt2, circleRadius);

                lstCircleAddToAxis.Add(c1);
                lstCircleAddToAxis.Add(c2);

                Axis axis = new Axis(new LinearPath(lstLinPathAxes[i].Vertices), lstCircleAddToAxis, textAttr1.Equals(textAttr2) ? textAttr1 : "ccc");
                lstAxis.Add(axis);
            }

            return lstAxis;
        }


        #endregion

        #region Old Code to delete
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



        #region todo
        //slab on grade
        #endregion
    }
}
