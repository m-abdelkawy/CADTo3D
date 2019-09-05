﻿using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Floor : IFloor
    {
        #region Properties
        public List<Wall> Walls { get; set; }
        public List<RectColumn> Columns { get; set; }
        public double Level { get; set; }
        public List<Opening> Openings { get; set; }
        public Slab Slab { get; set; }

        private ReadAutodesk FileReader { get; set; } 
        #endregion

        #region Constructors
        public Floor(string drawingFilePath,double level)
        {
            Level = level;
            FileReader = new ReadAutodesk(drawingFilePath);
            FileReader.DoWork();

            GetSlab();
            GetOpening();
            GetColumns();
            GetWalls();
        } 
        #endregion

        #region Private Methods
        private void GetWalls()
        {

            Walls = new List<Wall>();
            List<List<Point3D>> lstMidPoints = new List<List<Point3D>>();
            List<double> lstThickness = new List<double>();



            List<Line> lstWallLines = new List<Line>();
            foreach (Entity item in FileReader.Entities)
            {
                Line line = item as Line;
                if (line == null)
                {
                    continue;
                }
                if (line.LayerName == "Wall")
                {
                    lstWallLines.Add(line);

                }

            }
            List<List<Line>> lstWalls = new List<List<Line>>();


            for (int i = 0; i < lstWallLines.Count; i++)
            {
                Line parallel = lstWallLines[i].LineGetNearestParallel(lstWallLines.ToArray());
                if (parallel != null && (lstWallLines[i].Length() > parallel.Length()))
                {
                    lstWalls.Add(new List<Line> { lstWallLines[i], parallel });
                    lstThickness.Add(MathHelper.DistanceBetweenTwoParallels(lstWallLines[i], parallel));
                }
            }


            foreach (List<Line> lstParallels in lstWalls)
            {
                Point3D stPt1 = lstParallels[0].StartPoint;
                Point3D stPt2 = lstParallels[1].StartPoint;

               

                Point3D endPt1 = lstParallels[0].EndPoint;
                Point3D endPt2 = lstParallels[1].EndPoint;


                Point3D midStart = MathHelper.MidPoint(stPt1, stPt2);
                midStart.Z = Level - Slab.Thickness*1000;

                Point3D midEnd = MathHelper.MidPoint(endPt1, endPt2);
                midEnd.Z = Level - Slab.Thickness*1000;

                if (stPt1.DistanceTo(stPt2) < stPt1.DistanceTo(endPt2))
                {
                    lstMidPoints.Add(new List<Point3D> { midStart, midEnd});
                }
                else
                {
                    lstMidPoints.Add(new List<Point3D> { midEnd, midStart });
                }
            }


            for (int i = 0; i < lstMidPoints.Count; i++)
            {
                Walls.Add(new Wall(lstThickness[i], lstMidPoints[i][0], lstMidPoints[i][1]));
            }
        }

        private void GetColumns()
        {

            Columns = new List<RectColumn>();


            List<LinearPath> lstPolyLine = new List<LinearPath>();

            foreach (Entity entity in FileReader.Entities)
            {
                LinearPath polyLinPath = entity as LinearPath;

                if (polyLinPath == null)
                    continue;
                if (polyLinPath.LayerName == "Column" && polyLinPath.IsClosed == true)
                {

                    double width = double.MaxValue;
                    double length = 0;
                    Point3D widthMidPt = Point3D.Origin;

                    int verticesCount = polyLinPath.Vertices.Length;
                    for (int i = 0; i < verticesCount; i++)
                    {
                        if (i + 1 == verticesCount)
                            break;
                        double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        width = Math.Min(width, dist);
                        if (width == dist)
                        {
                            widthMidPt = MathHelper.MidPoint(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        }
                        length = Math.Max(length, dist);
                    }


                    Point3D center = (polyLinPath.Vertices[0] + polyLinPath.Vertices[2]) / 2.0;

                    center.Z = Level;
                    widthMidPt.Z = Level;

                    RectColumn col = new RectColumn(width, length, center, widthMidPt);
                    Columns.Add(col);
                }
            }

        }

        private void GetOpening()
        {
            Openings = new List<Opening>();
            foreach (Entity entity in FileReader.Entities)
            {
                LinearPath polyLinPath = entity as LinearPath;
                if (polyLinPath == null)
                    continue;
                if (polyLinPath.LayerName == "Opening" && polyLinPath.IsClosed == true)
                {
                    double width = double.MaxValue;
                    double length = 0;
                    List<Point2D> lstVertices = new List<Point2D>();

                    Point3D widthMidPt = Point3D.Origin;
                    int nVertices = polyLinPath.Vertices.Length;

                    for (int i = 0; i < nVertices; i++)
                    {
                        if (i + 1 == nVertices)
                            break;
                        double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        width = Math.Min(width, dist);
                        if (width == dist)
                        {
                            widthMidPt = MathHelper.MidPoint(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        }
                        length = Math.Max(length, dist);
                    }


                    Point3D center = (polyLinPath.Vertices[0] + polyLinPath.Vertices[2]) / 2.0;

                    center.Z = Level;
                    widthMidPt.Z = Level;

                    Openings.Add(new Opening(width, length, center, widthMidPt));
                }
            }

        }
        private void GetSlab()
        {
            foreach (Entity entity in FileReader.Entities)
            {
                LinearPath polyLinPath = entity as LinearPath;
                if (polyLinPath == null)
                    continue;
                if (polyLinPath.LayerName == "Slab" && polyLinPath.IsClosed == true)
                {
                    double width = double.MaxValue;
                    double length = 0;
                    List<Point2D> lstVertices = new List<Point2D>();

                    Point3D widthMidPt = Point3D.Origin;
                    int nVertices = polyLinPath.Vertices.Length;

                    for (int i = 0; i < nVertices; i++)
                    {
                        if (i + 1 == nVertices)
                            break;
                        double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        width = Math.Min(width, dist);
                        if (width == dist)
                        {
                            widthMidPt = MathHelper.MidPoint(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        }
                        length = Math.Max(length, dist);
                    }


                    Point3D center = (polyLinPath.Vertices[0] + polyLinPath.Vertices[2]) / 2.0;
                    center.Z = Level;
                    widthMidPt.Z = Level;
                    Slab = new Slab(width, length, center, widthMidPt);
                }
            }

        } 
        #endregion

    }
}