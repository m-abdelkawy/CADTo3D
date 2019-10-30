﻿using CADReader.ElectricalElements;
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
        public List<Wall> RetainingWalls { get; set; }
        public List<RectColumn> Columns { get; set; }
        public List<ReinforcedCadColumn> RcColumns { get; set; }
        public List<ReinforcedCadSlab> RcSlab { get; set; }
        public List<Slab> Slabs { get; set; }
        public List<Stair> Stairs { get; set; }
        public List<LinearPath> Landings { get; set; }

        public List<ShearWall> ShearWalls { get; set; }
        public List<SlopedSlab> Ramps { get; set; }

        public List<ReinforcedCadWall> ReinforcedCadWalls { get; set; }
        public List<ElectricalConduit> ElecConduits { get; set; }
        public List<ReinforcedCadShearWall> ReinforcedShearWalls { get; private set; }





        #endregion

        #region Constructors
        public Floor(ReadAutodesk cadReader, double level)
        {
            Level = level;

            GetSlabs(cadReader);
            Columns = base.GetColumns(cadReader);
            this.RetainingWalls = base.GetWalls(cadReader);
            GetStairs(cadReader);
            RcColumns = base.GetRCColumns(this.Columns);
            ShearWalls = GetShearWalls(cadReader);
            Ramps = GetRamps(cadReader);
            GetRcSLabs();
            this.ReinforcedCadWalls = base.GetRCWalls(this.RetainingWalls);
            ReinforcedShearWalls = GetRCShearWalls(ShearWalls);

            GetElectricalConduit(cadReader);
        }

        private void GetElectricalConduit(ReadAutodesk cadReader)
        {
            ElecConduits = new List<ElectricalConduit>();
            double elecLevel = Level - DefaultValues.SlabThinkess + DefaultValues.FootingCover + DefaultValues.BarDiameter;
            List<Entity> lstElecConduit = CadHelper.EntitiesGetByLayerName(cadReader, CadLayerName.ElecConduit);
            for (int i = 0; i < lstElecConduit.Count; i++)
            {

                if(lstElecConduit[i] is LinearPath)
                {
                    for (int j = 0; j < lstElecConduit[i].Vertices.Length; j++)
                    {
                        lstElecConduit[i].Vertices[j].Z = elecLevel;
                    }
                }
                else if(lstElecConduit[i] is CompositeCurve)
                {
                    CompositeCurve compCurve = lstElecConduit[i] as CompositeCurve;
                    for (int j = 0; j < compCurve.CurveList.Count; j++)
                    {
                       Line line = compCurve.CurveList[j] as Line;
                        if (line!=null)
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
                

                ElecConduits.Add(new ElectricalConduit(lstElecConduit[i]));
            }
        }
        #endregion

        #region Private Methods

        private void GetSlabs(ReadAutodesk cadFileReader)
        {
            this.Slabs = new List<Slab>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Slab);

            //  List<Slab> lstSlab = lstPLine.Where(e => e is LinearPath).Select((s,i) => new Slab(cadFileReader, lstPLine[i], Level)).ToList();

            for (int i = 0; i < lstPLine.Count; i++)
            {
                Slab slab = new Slab(cadFileReader, lstPLine[i], Level);
                Slabs.Add(slab);
            }

        }

        private void GetRcSLabs()
        {
            this.RcSlab = new List<ReinforcedCadSlab>();
            for (int i = 0; i < Slabs.Count; i++)
            {
                RcSlab.Add(new ReinforcedCadSlab(Slabs[i]));
            }
        }

        private void GetStairs(ReadAutodesk cadFileReader)
        {
            Stairs = new List<Stair>();
            List<LinearPathEx> lstStairFlightPath = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair).Where(p => p is LinearPathEx).Cast<LinearPathEx>().ToList();
            Landings = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair).Except<LinearPath>(lstStairFlightPath).ToList();

            for (int i = 0; i < Landings.Count; i++)
            {
                for (int j = 0; j < Landings[i].Vertices.Length; j++)
                {
                    Landings[i].Vertices[j].Z += Level;
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
                Stairs.Add(new Stair(stairWidth, slopedLines));
            }

        }


        #endregion

    }
}
