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
        public double Level { get; set; }


        //public List<Wall> GetWalls(ReadAutodesk CadReader)
        //{

        //    List<Wall> Walls = new List<Wall>();
        //    List<List<Point3D>> lstMidPoints = new List<List<Point3D>>();
        //    List<double> lstThickness = new List<double>();

        //    List<Line> lstWallLines = CadHelper.LinesGetByLayerName(CadReader, CadLayerName.Wall);


        //    List<List<Line>> lstWalls = new List<List<Line>>();

        //    for (int i = 0; i < lstWallLines.Count; i++)
        //    {
        //        if (lstWallLines[i] == null)
        //            continue;
        //        Line parallel = lstWallLines[i].LineGetNearestParallelByNormalVector(lstWallLines.ToArray());


        //        if (parallel != null)
        //        {//Exclude Lines from list
        //            lstWalls.Add(new List<Line> { lstWallLines[i], parallel });

        //            //exclude line and parallel from lstWallLines
        //            lstThickness.Add(MathHelper.DistanceBetweenTwoParallels(lstWallLines[i], parallel));
        //            lstWallLines[lstWallLines.IndexOf(parallel)] = null;
        //            lstWallLines[i] = null;
        //        }

        //    }

        //    foreach (List<Line> lstParallels in lstWalls)
        //    {
        //        Point3D stPt1 = lstParallels[0].StartPoint;
        //        Point3D stPt2 = lstParallels[1].StartPoint;

        //        Point3D endPt1 = lstParallels[0].EndPoint;
        //        Point3D endPt2 = lstParallels[1].EndPoint;


        //        Point3D midStart = MathHelper.MidPoint3D(stPt1, stPt2);
        //        midStart.Z = Level /*- DefaultValues.SlabThinkess*/;

        //        Point3D midEnd = MathHelper.MidPoint3D(endPt1, endPt2);
        //        midEnd.Z = Level /*- DefaultValues.SlabThinkess*/;

        //        if (stPt1.DistanceTo(stPt2) < stPt1.DistanceTo(endPt2))
        //        {
        //            lstMidPoints.Add(new List<Point3D> { midStart, midEnd });
        //        }
        //        else
        //        {
        //            midStart = MathHelper.MidPoint3D(stPt1, endPt2);
        //            midStart.Z = Level /*- DefaultValues.SlabThinkess*/;

        //            midEnd = MathHelper.MidPoint3D(endPt1, stPt2);
        //            midEnd.Z = Level /*- DefaultValues.SlabThinkess*/;

        //            lstMidPoints.Add(new List<Point3D> { midEnd, midStart });
        //        }
        //    }


        //    for (int i = 0; i < lstMidPoints.Count; i++)
        //    {
        //        Walls.Add(new Wall(lstThickness[i], lstMidPoints[i][0], lstMidPoints[i][1], ));
        //    }

        //    return Walls;
        //}

        public List<Wall> GetWalls(ReadAutodesk CadReader)
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
      
        public List<RectColumn> GetColumns(ReadAutodesk cadFileReader)
        {

            List<RectColumn> Columns = new List<RectColumn>();


            List<LinearPath> lstPolyLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Column, true);
            for (int i = 0; i < lstPolyLine.Count; i++)
            {
                double width = double.MaxValue;
                double length = 0;
                Point3D widthMidPt = Point3D.Origin;

                int verticesCount = lstPolyLine[i].Vertices.Length;
                for (int j = 0; j < verticesCount - 1; j++)
                {
                    double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(lstPolyLine[i].Vertices[j], lstPolyLine[i].Vertices[j + 1]);
                    width = Math.Min(width, dist);
                    if (width == dist)
                    {
                        widthMidPt = MathHelper.MidPoint3D(lstPolyLine[i].Vertices[j], lstPolyLine[i].Vertices[j + 1]);
                    }
                    length = Math.Max(length, dist);
                }


                Point3D center = MathHelper.MidPoint3D(lstPolyLine[i].Vertices[0], lstPolyLine[i].Vertices[2]);

                center.Z = Level;
                widthMidPt.Z = Level;

                for (int j = 0; j < lstPolyLine[i].Vertices.Length; j++)
                {
                    lstPolyLine[i].Vertices[j].Z = Level;
                }

                RectColumn col = new RectColumn(width, length, center, widthMidPt, lstPolyLine[i]);
                Columns.Add(col);
            }
            return Columns;
        }
        public List<ReinforcedCadColumn> GetRCColumns(List<RectColumn> Columns)
        {
            List<ReinforcedCadColumn> RcColumns = new List<ReinforcedCadColumn>();
            ReinforcedCadColumn RcCol = null;
            foreach (var col in Columns)
            {
                RcCol = new ReinforcedCadColumn(col, Level);
                RcColumns.Add(RcCol);
            }

            return RcColumns;
        }



        public List<ShearWall> GetShearWalls(ReadAutodesk cadFileReader)
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

        public List<ReinforcedCadShearWall> GetRCShearWalls(List<ShearWall> shearWalls)
        {
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

        public List<ReinforcedCadWall> GetRCWalls(List<Wall> lstWall)
        {
            List<ReinforcedCadWall> lstRcWall = new List<ReinforcedCadWall>();
            ReinforcedCadWall rcWall = null;
            foreach (var wall in lstWall)
            {
                rcWall = new ReinforcedCadWall(wall);
                lstRcWall.Add(rcWall);
            }

            return lstRcWall;
        }

    }
}