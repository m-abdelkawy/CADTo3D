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
    public class Foundation : IFloor
    {
        #region Properties
        public double Level { get; set; }
        public ReadAutodesk FileReader { get; set; }
        public List<RectFooting> Footings { get;  set; }
        #endregion

        #region Constructor
        public Foundation(string drawingFilePath)
        {
            FileReader = new ReadAutodesk(drawingFilePath);
            FileReader.DoWork();
            GetFootings();
        } 
        #endregion

        private void GetFootings()
        {
            Footings = new List<RectFooting>();
            foreach (Entity entity in FileReader.Entities)
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
                        double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        width = Math.Min(width, dist);
                        if (width == dist)
                        {
                            widthMidPt = MathHelper.MidPoint(polyLinPath.Vertices[i], polyLinPath.Vertices[i + 1]);
                        }
                        length = Math.Max(length, dist);
                    }
                     

                    Point3D center = (polyLinPath.Vertices[0] + polyLinPath.Vertices[2]) / 2.0;

                    RectFooting footing = new RectFooting(width, length, center, widthMidPt);
                    Footings.Add(footing);
                }
            }
             
        }
    }
}
