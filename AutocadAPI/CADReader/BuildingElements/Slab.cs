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
    public class Slab
    {
        #region Properties

        public LinearPath linPathSlab { get; set; }
        public List<Opening> Openings { get; set; }

        public double Thickness { get; set; } = DefaultValues.SlabThinkess;
        #endregion

        #region Constructors
        public Slab(ReadAutodesk cadFileReader, LinearPath path, double level)
        {
            linPathSlab = new LinearPath( path.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z + level)).ToArray());
            GetOpenings(cadFileReader, path, level);
        }
        #endregion

        #region Methods
        private void GetOpenings(ReadAutodesk cadFileReader,LinearPath path, double level)
        {
            Openings = new List<Opening>();

            List<LinearPath> lstPolyLine = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Opening, true);
            List<LinearPath> PolyLines = new List<LinearPath>();
            for (int i = 0; i < lstPolyLine.Count; i++)
            {
                if(MathHelper.IsInsidePolygon(lstPolyLine[i], path))
                {
                    PolyLines.Add(lstPolyLine[i]);
                }
            }

            for (int i = 0; i < PolyLines.Count; i++)
            {
                double width = double.MaxValue;
                double length = 0;
                List<Point2D> lstVertices = new List<Point2D>();

                Point3D widthMidPt = Point3D.Origin;
                int nVertices = PolyLines[i].Vertices.Length;

                for (int j = 0; j < nVertices - 1; j++)
                {
                    double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(PolyLines[i].Vertices[j], PolyLines[i].Vertices[j + 1]);
                    width = Math.Min(width, dist);
                    if (width == dist)
                    {
                        widthMidPt = MathHelper.MidPoint3D(PolyLines[i].Vertices[j], PolyLines[i].Vertices[j + 1]);
                    }
                    length = Math.Max(length, dist);
                }


                Point3D center = MathHelper.MidPoint3D(PolyLines[i].Vertices[0], PolyLines[i].Vertices[2]);

                center.Z = level;
                widthMidPt.Z = level;

                for (int j = 0; j < PolyLines[i].Vertices.Length; j++)
                {
                    PolyLines[i].Vertices[j].Z = level;
                }

                Openings.Add(new Opening(PolyLines[i],width, length, center, widthMidPt));
                // 
            }
        }


        #endregion
    }
}
