
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using OdaLibTrials;
using OdaLibTrials.BuildingElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdaLibTrials
{
    public class Class1
    {
        private List<List<Point3D>> lstMidPoints = new List<List<Point3D>>();
        List<double> lstThickness = new List<double>();
        List<Wall> lstWalls = new List<Wall>();
        List<RectColumn> lstColumns = new List<RectColumn>();
        List<RectFooting> lstFooting = new List<RectFooting>();


        ReadAutodesk ra = new ReadAutodesk(@"D:\Coding Trials\AEC Development\CadWallColFootingTrial.dwg");


        public void GetBlocks()
        {
            ra.DoWork();
            foreach (Block blk in ra.Blocks)
            {
                Console.WriteLine(blk.Name);
            }
        }

        public void WallGetEndPoints()
        {
            

            this.lstWalls = new List<Wall>();
           

            List<List<Point3D>> lstMidPoints = new List<List<Point3D>>();
            List<double> lstThickness = new List<double>();


            List<Line> lstWallLines = new List<Line>();
            foreach (Entity item in ra.Entities)
            {
                Line line = item as Line;
                if(line == null)
                {
                    continue;
                }
                if(line.LayerName == "Wall")
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

                if (stPt1.DistanceTo(stPt2) < stPt1.DistanceTo(endPt2))
                {
                    lstMidPoints.Add(new List<Point3D> { MathHelper.MidPoint(stPt1, stPt2), MathHelper.MidPoint(endPt1, endPt2) });
                }
                else
                {
                    lstMidPoints.Add(new List<Point3D> { MathHelper.MidPoint(stPt1, endPt2), MathHelper.MidPoint(stPt2, endPt1) });
                }
            }

            this.lstMidPoints = lstMidPoints;
            this.lstThickness = lstThickness;

            for (int i = 0; i < lstMidPoints.Count; i++)
            {
                this.lstWalls.Add(new Wall(lstThickness[i], lstMidPoints[i][0], lstMidPoints[i][1]));
            }
        }

        public void GetColumns()
        {
            WallGetEndPoints();

            this.lstColumns = new List<RectColumn>();
            

            List<LinearPath> lstPolyLine = new List<LinearPath>();

            foreach (Entity entity in ra.Entities)
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
                            widthMidPt = MathHelper.MidPoint(polyLinPath.Vertices[i], polyLinPath.Vertices[i+1]);
                        }
                        length = Math.Max(length, dist);
                    }

                    //Point3D minPt = polyLinPath.Vertices.Min();
                    //Point3D maxPt = polyLinPath.Vertices.Max();

                    //polyLinPath.

                    Point3D center = (polyLinPath.Vertices[0] + polyLinPath.Vertices[2])/ 2.0;

                    RectColumn col = new RectColumn(width, length, center, widthMidPt);
                    lstColumns.Add(col);
                }
            }



            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls,this.lstColumns);
            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls);
        }

        public void GetBuilding()
        {
            ra.DoWork();

            GetColumns();



            foreach (Entity entity in ra.Entities)
            {
                LinearPath polyLinPath = entity as LinearPath;
                if (polyLinPath == null)
                    continue;
                if (polyLinPath.LayerName == "Footing" && polyLinPath.IsClosed == true)
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
                        double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(polyLinPath.Vertices[i], polyLinPath.Vertices[i+1]);
                        width = Math.Min(width, dist);
                        if (width == dist)
                        {
                            widthMidPt = MathHelper.MidPoint(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        }
                        length = Math.Max(length, dist);
                    }

                    //Point3D center = polyLinPath.BoxMin + (polyLinPath.BoxMax - polyLinPath.BoxMin) / 2.0;

                    Point3D center = (polyLinPath.Vertices[0] + polyLinPath.Vertices[2]) / 2.0;

                    RectFooting footing = new RectFooting(width, length, center, widthMidPt);
                    lstFooting.Add(footing);
                }
            }




            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls, this.lstColumns, lstFooting);
            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls);
        }

    }
}
