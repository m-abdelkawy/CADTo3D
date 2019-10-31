using CADReader.ElectricalElements;
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
    public class Floor : FloorBase
    {
        #region Properties
        public List<ReinforcedCadColumn> LstRcColumn { get; set; }
        public List<ReinforcedCadSlab> LstRcSlab { get; set; }
        public List<Stair> LstStair { get; set; }
        public List<LinearPath> LstLandingLinPath { get; set; }

        public List<SlopedSlab> LstRamp { get; set; }

        public List<ReinforcedCadWall> LstRcCadRetainingWall { get; set; }
        public List<ElectricalConduit> LstElectConduit { get; set; }
        public List<ReinforcedCadShearWall> LstRcShearWall { get; private set; }
        #endregion

        #region Constructors
        public Floor(ReadAutodesk cadReader, double level)
        {
            Level = level;

            //Reinforced Slabs
            LstRcSlab = base.GetRcSLabs(cadReader);

            //RC Columns
            LstRcColumn = base.GetRCColumns(cadReader);

            //RC Retaining Walls
            this.LstRcCadRetainingWall = base.GetRCWalls(cadReader);

            //Stairs
            GetStairs(cadReader);

            //Reinforced Shear Walls
            LstRcShearWall = GetRCShearWalls(cadReader);

            //Ramps
            LstRamp = GetRamps(cadReader);

            //Electricity hoses
            GetElectricalConduit(cadReader);
        }
        #endregion

        #region class Methods
        private void GetStairs(ReadAutodesk cadFileReader)
        {
            LstStair = new List<Stair>();
            List<LinearPathEx> lstStairFlightPath = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair).Where(p => p is LinearPathEx).Cast<LinearPathEx>().ToList();
            LstLandingLinPath = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair).Except<LinearPath>(lstStairFlightPath).ToList();

            for (int i = 0; i < LstLandingLinPath.Count; i++)
            {
                for (int j = 0; j < LstLandingLinPath[i].Vertices.Length; j++)
                {
                    LstLandingLinPath[i].Vertices[j].Z += Level;
                }
            }

            Dictionary<int, List<Line>> dicStairLines = new Dictionary<int, List<Line>>();
            //Dictionary<double, List<Line>> dicSlopedStairLines = new Dictionary<double, List<Line>>();
            for (int i = 0; i < lstStairFlightPath.Count(); i++)
            {

                List<Line> lstLines = new List<Line>();
                for (int j = 0; j < lstStairFlightPath[i].Vertices.Count() - 1; j++)
                {
                    Line l = new Line(lstStairFlightPath[i].Vertices[j], lstStairFlightPath[i].Vertices[j + 1]);
                    lstLines.Add(l);
                }
                dicStairLines.Add(i, lstLines);
            }

            foreach (KeyValuePair<int, List<Line>> item in dicStairLines)
            {
                List<Line> slopedLines = new List<Line>();
                List<Line> hzLines = new List<Line>();

                for (int i = 0; i < item.Value.Count(); i++)
                {
                    if (Math.Abs(item.Value[i].StartPoint.Z - item.Value[i].EndPoint.Z) > 0.0001)
                    {
                        slopedLines.Add(new Line(new Point3D(item.Value[i].StartPoint.X, item.Value[i].StartPoint.Y, Level + item.Value[i].StartPoint.Z)
                            , new Point3D(item.Value[i].EndPoint.X, item.Value[i].EndPoint.Y, Level + item.Value[i].EndPoint.Z)));
                    }
                    else
                    {
                        hzLines.Add(new Line(new Point3D(item.Value[i].StartPoint.X, item.Value[i].StartPoint.Y, Level + item.Value[i].StartPoint.Z)
                            , new Point3D(item.Value[i].EndPoint.X, item.Value[i].EndPoint.Y, Level + item.Value[i].EndPoint.Z)));
                    }
                }
                double stairWidth = hzLines[0].Length();
                LstStair.Add(new Stair(stairWidth, slopedLines));
            }

        }

        private void GetElectricalConduit(ReadAutodesk cadReader)
        {
            LstElectConduit = new List<ElectricalConduit>();
            double elecLevel = Level - DefaultValues.SlabThinkess + DefaultValues.FootingCover + DefaultValues.BarDiameter;
            List<Entity> lstElecConduit = CadHelper.EntitiesGetByLayerName(cadReader, CadLayerName.ElecConduit);
            for (int i = 0; i < lstElecConduit.Count; i++)
            {

                if (lstElecConduit[i] is LinearPath)
                {
                    for (int j = 0; j < lstElecConduit[i].Vertices.Length; j++)
                    {
                        lstElecConduit[i].Vertices[j].Z = elecLevel;
                    }
                }
                else if (lstElecConduit[i] is CompositeCurve)
                {
                    CompositeCurve compCurve = lstElecConduit[i] as CompositeCurve;
                    for (int j = 0; j < compCurve.CurveList.Count; j++)
                    {
                        Line line = compCurve.CurveList[j] as Line;
                        if (line != null)
                        {
                            for (int k = 0; k < line.Vertices.Count(); k++)
                            {
                                line.Vertices[k].Z = elecLevel;
                            }
                        }
                        else
                        {
                            Arc arc = compCurve.CurveList[j] as Arc;
                            arc.StartPoint.Z = elecLevel;
                            arc.EndPoint.Z = elecLevel;
                            arc.Center.Z = elecLevel;
                        }
                    }
                }


                LstElectConduit.Add(new ElectricalConduit(lstElecConduit[i]));
            }
        }
        #endregion

    }
}
