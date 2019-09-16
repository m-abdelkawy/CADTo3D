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
    public class Foundation : IFloor
    {
        #region Properties
        private double PcThickness { get; set; } = DefaultValues.PCFootingThinkess;
        private double RcThickness { get; set; } = DefaultValues.RCFootingThinkess;
        public double Level { get; set; }
        public List<RCRectFooting> PCFooting { get; set; }
        public List<RCRectFooting> RCFooting { get; set; }
        public List<SlopedSlab> Ramps { get; set; }
        #endregion

        #region Constructor
        public Foundation(ReadAutodesk cadReader, double level)
        {
            Level = level; 
            GetPCFootings(cadReader);
            GetRCFootings(cadReader);
            GetRamps(cadReader);
        }
        #endregion

        private void GetPCFootings(ReadAutodesk cadReader)
        {
            PCFooting = new List<RCRectFooting>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.PCFooting).Where(pl => pl.IsClosed).ToList();

            for (int i = 0; i < lstPLine.Count; i++)
            {
                RCRectFooting footing = RectFootingCreate(lstPLine[i], PcThickness);
                PCFooting.Add(footing);
            }

        }

        private void GetRCFootings(ReadAutodesk cadReader)
        {
            RCFooting = new List<RCRectFooting>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader, CadLayerName.RCFooting).Where(pl => pl.IsClosed).ToList();

            for (int i = 0; i < lstPLine.Count; i++)
            {
                RCRectFooting footing = RectFootingCreate(lstPLine[i], RcThickness);
                RCFooting.Add(footing);
            }

        }

        private RCRectFooting RectFootingCreate(LinearPath pLine, double thickness)
        {
            double width = double.MaxValue;
            double length = 0;

            Point3D widthMidPt = Point3D.Origin;
            int nVertices = pLine.Vertices.Length;

            for (int j = 0; j < nVertices - 1; j++)
            {
                double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(pLine.Vertices[j], pLine.Vertices[j + 1]);
                width = Math.Min(width, dist);
                if (width == dist)
                {
                    widthMidPt = MathHelper.MidPoint(pLine.Vertices[j], pLine.Vertices[j + 1]);
                }
                length = Math.Max(length, dist);
            }


            Point3D center = (pLine.Vertices[0] + pLine.Vertices[2]) / 2.0;
            center.Z = Level;
            widthMidPt.Z = Level;
            RCRectFooting footing = new RCRectFooting(width, length, thickness, center, widthMidPt);
            return footing;
        }

        private void GetRamps(ReadAutodesk cadReader)
        {
            this.Ramps = new List<SlopedSlab>();

            List<LinearPath> lstPLine = CadHelper.PLinesGetByLayerName(cadReader,CadLayerName.Ramp);

            List<SlopedSlab> lstSlopedSlab = lstPLine.Where(e => e is LinearPathEx).Select(s => new SlopedSlab(s.Vertices.ToList())).ToList();

            Ramps.AddRange(lstSlopedSlab);
        }
    }
}
