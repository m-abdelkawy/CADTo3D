using CADReader.Helpers;
using CADReader.Reinforced_Elements;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CADReader.BuildingElements
{
    public class FloorBase
    {
        #region Properties
        public double Level { get; set; }
        #endregion
        
        #region Class Methods
        private List<Wall> GetWalls(ReadAutodesk CadReader)
        {
            List<Wall> Walls = new List<Wall>();

            List<LinearPath> lstLinPathWall = CadHelper.PLinesGetByLayerName(CadReader, CadLayerName.Wall);

            for (int i = 0; i < lstLinPathWall.Count; i++)
            {
                for (int j = 0; j < lstLinPathWall[i].Vertices.Length; j++)
                {
                    lstLinPathWall[i].Vertices[j].Z = Level;
                }
                Wall wall = new Wall(lstLinPathWall[i]);
                Walls.Add(wall);
            }



            return Walls;
        }
      
        public List<Column> GetColumns(ReadAutodesk cadFileReader)
        {

            List<Column> Columns = new List<Column>();


            List<LinearPath> lstPolyLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Column, true);
            for (int i = 0; i < lstPolyLine.Count; i++)
            {
                #region Old Code with Rectangule
                //double width = double.MaxValue;
                //double length = 0;
                //Point3D widthMidPt = Point3D.Origin;

                //int verticesCount = lstPolyLine[i].Vertices.Length;
                //for (int j = 0; j < verticesCount - 1; j++)
                //{
                //    double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(lstPolyLine[i].Vertices[j], lstPolyLine[i].Vertices[j + 1]);
                //    width = Math.Min(width, dist);
                //    if (width == dist)
                //    {
                //        widthMidPt = MathHelper.MidPoint3D(lstPolyLine[i].Vertices[j], lstPolyLine[i].Vertices[j + 1]);
                //    }
                //    length = Math.Max(length, dist);
                //}


                //Point3D center = MathHelper.MidPoint3D(lstPolyLine[i].Vertices[0], lstPolyLine[i].Vertices[2]);

                //center.Z = Level;
                //widthMidPt.Z = Level; 
                #endregion

                for (int j = 0; j < lstPolyLine[i].Vertices.Length; j++)
                {
                    lstPolyLine[i].Vertices[j].Z = Level;
                }

                Column col = new Column(lstPolyLine[i]);
                Columns.Add(col);
            }
            return Columns;
        }
        public List<ReinforcedCadColumn> GetRCColumns(ReadAutodesk cadReader)
        {
            List<RectColumn> Columns = GetColumns(cadReader);
            List<ReinforcedCadColumn> RcColumns = new List<ReinforcedCadColumn>();
            ReinforcedCadColumn RcCol = null;
            foreach (var col in Columns)
            {
                RcCol = new ReinforcedCadColumn(col, Level);
                RcColumns.Add(RcCol);
            }

            return RcColumns;
        }

        private List<Slab> GetSlabs(ReadAutodesk cadFileReader)
        {
            List<Slab> lstSlab = new List<Slab>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Slab);

            for (int i = 0; i < lstPLine.Count; i++)
            {
                Slab slab = new Slab(cadFileReader, lstPLine[i], Level);
                lstSlab.Add(slab);
            }

            return lstSlab;
        }

        internal List<ReinforcedCadSlab> GetRcSLabs(ReadAutodesk cadReader)
        {
            List<Slab> LstSlab = GetSlabs(cadReader);

            List<ReinforcedCadSlab> lstRcSlab = new List<ReinforcedCadSlab>();
            for (int i = 0; i < LstSlab.Count; i++)
            {
                lstRcSlab.Add(new ReinforcedCadSlab(LstSlab[i]));
            }

            return lstRcSlab;
        }

        private List<ShearWall> GetShearWalls(ReadAutodesk cadFileReader)
        {
            List<ShearWall> ShearWalls = new List<ShearWall>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.ShearWall, true);

            for (int i = 0; i < lstPLine.Count; i++)
            {
                for (int j = 0; j < lstPLine[i].Vertices.Length; j++)
                {
                    lstPLine[i].Vertices[j].Z += Level;
                }
            }

            List<ShearWall> lstShearWall = lstPLine.Select(s => new LinearPath(s.Vertices)).Select(s => new ShearWall(s)).ToList();

            ShearWalls.AddRange(lstShearWall);
            return ShearWalls;

        }

        internal List<ReinforcedCadShearWall> GetRCShearWalls(ReadAutodesk cadReader)
        {
            List<ShearWall> shearWalls = GetShearWalls(cadReader);

            List<ReinforcedCadShearWall> RcShearWalls = new List<ReinforcedCadShearWall>();
            ReinforcedCadShearWall RcShearWall = null;
            foreach (var shearWall in shearWalls)
            {
                RcShearWall = new ReinforcedCadShearWall(shearWall);
                RcShearWalls.Add(RcShearWall);
            }

            return RcShearWalls;
        }

        public List<SlopedSlab> GetRamps(ReadAutodesk cadReader)
        {
            List<SlopedSlab> Ramps = new List<SlopedSlab>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.Ramp);

            List<SlopedSlab> lstSlopedSlab = lstPLine.Where(e => e is LinearPathEx).Select(s => new SlopedSlab(s.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z + Level)).ToList())).ToList();

            Ramps.AddRange(lstSlopedSlab);

            return Ramps;
        }

        public List<ReinforcedCadWall> GetRCWalls(ReadAutodesk cadReader)
        {
            List<Wall> lstWall = GetWalls(cadReader);
            List<ReinforcedCadWall> lstRcWall = new List<ReinforcedCadWall>();
            ReinforcedCadWall rcWall = null;
            foreach (var wall in lstWall)
            {
                rcWall = new ReinforcedCadWall(wall);
                lstRcWall.Add(rcWall);
            }

            return lstRcWall;
        } 
        #endregion
    }
}