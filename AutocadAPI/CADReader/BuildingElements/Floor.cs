using CADReader.Helpers;
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
    public class Floor : IFloor
    {
        #region Properties
        public List<Wall> Walls { get; set; }
        public List<RectColumn> Columns { get; set; }
        public double Level { get; set; }
        public List<Opening> Openings { get; set; }
        public List<Slab> Slabs { get; set; }
        public List<Stair> Stairs { get; set; }
        public List<LinearPath> Landings { get; set; }

        #endregion

        #region Constructors
        public Floor(ReadAutodesk cadReader, double level)
        {
            Level = level;

            GetSlabs(cadReader);
            GetOpening(cadReader);
            GetColumns(cadReader);
            GetWalls(cadReader);
        }
        #endregion

        #region Private Methods
        private void GetWalls(ReadAutodesk CadReader)
        {

            Walls = new List<Wall>();
            List<List<Point3D>> lstMidPoints = new List<List<Point3D>>();
            List<double> lstThickness = new List<double>();

            List<Line> lstWallLines = CadHelper.LinesGetByLayerName(CadReader, CadLayerName.Wall);


            List<List<Line>> lstWalls = new List<List<Line>>();

            for (int i = 0; i < lstWallLines.Count; i++)
            {
                Line parallel = lstWallLines[i].LineGetNearestParallel(lstWallLines.ToArray());

                if (parallel != null && (lstWallLines[i].Length() > parallel.Length()))
                {//Exclude Lines from list
                    lstWalls.Add(new List<Line> { lstWallLines[i], parallel });
                    lstThickness.Add(MathHelper.DistanceBetweenTwoParallels(lstWallLines[i], parallel));
                    lstWallLines.IndexOf(parallel);
                }
            }

            foreach (List<Line> lstParallels in lstWalls)
            {
                Point3D stPt1 = lstParallels[0].StartPoint;
                Point3D stPt2 = lstParallels[1].StartPoint;

                Point3D endPt1 = lstParallels[0].EndPoint;
                Point3D endPt2 = lstParallels[1].EndPoint;


                Point3D midStart = MathHelper.MidPoint3D(stPt1, stPt2);
                midStart.Z = Level - Slabs[0].Thickness * 1000;

                Point3D midEnd = MathHelper.MidPoint3D(endPt1, endPt2);
                midEnd.Z = Level - Slabs[0].Thickness * 1000;

                if (stPt1.DistanceTo(stPt2) < stPt1.DistanceTo(endPt2))
                {
                    lstMidPoints.Add(new List<Point3D> { midStart, midEnd });
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
        private void GetColumns(ReadAutodesk cadFileReader)
        {

            Columns = new List<RectColumn>();


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

                RectColumn col = new RectColumn(width, length, center, widthMidPt, lstPolyLine[i]);
                Columns.Add(col);
            }

        }
        private void GetOpening(ReadAutodesk cadFileReader)
        {
            Openings = new List<Opening>();
            List<LinearPath> lstPolyLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Opening, true);
            for (int i = 0; i < lstPolyLine.Count; i++)
            {
                double width = double.MaxValue;
                double length = 0;
                List<Point2D> lstVertices = new List<Point2D>();

                Point3D widthMidPt = Point3D.Origin;
                int nVertices = lstPolyLine[i].Vertices.Length;

                for (int j = 0; j < nVertices - 1; j++)
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

                Openings.Add(new Opening(width, length, center, widthMidPt));
            }
        }
        private void GetSlabs(ReadAutodesk cadFileReader)
        {
            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Slab, true);

            for (int i = 0; i < lstPLine.Count; i++)
            {
                double width = double.MaxValue;
                double length = 0;
                List<Point2D> lstVertices = new List<Point2D>();

                Point3D widthMidPt = Point3D.Origin;
                int nVertices = lstPLine[i].Vertices.Length;

                for (int j = 0; j < nVertices; j++)
                {
                    if (j + 1 == nVertices)
                        break;
                    double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(lstPLine[i].Vertices[j], lstPLine[i].Vertices[j + 1]);
                    width = Math.Min(width, dist);
                    if (width == dist)
                    {
                        widthMidPt = MathHelper.MidPoint3D(lstPLine[i].Vertices[j], lstPLine[i].Vertices[j + 1]);
                    }
                    length = Math.Max(length, dist);
                }


                Point3D center = MathHelper.MidPoint3D(lstPLine[i].Vertices[0], lstPLine[i].Vertices[2]);
                center.Z = Level;
                widthMidPt.Z = Level;
                Slabs.Add(new Slab(width, length, center, widthMidPt));
            }

        }
        private void GetStairs(ReadAutodesk cadFileReader)
        {
            List<LinearPathEx> lstStairFlightPath = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair).Cast<LinearPathEx>().ToList();
            Landings = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair);


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
                    if (item.Value[i].StartPoint.Z != item.Value[i].EndPoint.Z)
                    {
                        slopedLines.Add(item.Value[i]);
                    }
                    else
                    {
                        hzLines.Add(item.Value[i]);
                    }
                }
                double stairWidth = hzLines[0].Length();
                Stairs.Add(new Stair(stairWidth, slopedLines));
            }

        }

        #endregion

    }
}
