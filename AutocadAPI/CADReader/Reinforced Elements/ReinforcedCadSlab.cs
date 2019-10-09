using CADReader.BuildingElements;
using CADReader.ElementComponents;
using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Reinforced_Elements
{
    public class ReinforcedCadSlab
    {
        public Slab Slab { get; set; }
        //public Dictionary<int, int> OpeningsRFT { get; set; } = new Dictionary<int, int>();
        public List<Rebar> OpeningsRFT { get; set; } = new List<Rebar>();
        public List<Rebar> RFT { get; set; } = new List<Rebar>();

        public ReinforcedCadSlab(Slab _slab)
        {
            Slab = _slab;
            PopulateRft();
        }

        public void PopulateRft()
        {
            LinearPath BoundaryPath = new LinearPath(Slab.linPathSlab.Vertices.Select(e => new Point3D(e.X, e.Y, e.Z - DefaultValues.SlabThinkess + DefaultValues.ColumnCover)).ToArray());
            List<Line> lstBoundaryLines = CadHelper.BoundaryLinesGet(BoundaryPath);

            int nLong = Convert.ToInt32(lstBoundaryLines[0].Length() / DefaultValues.LongBarSpacing);
            int nTransverse = Convert.ToInt32(lstBoundaryLines[1].Length() / DefaultValues.LongBarSpacing);

            //Vector3D uvLong = MathHelper.UnitVector3DFromPt1ToPt2(lstBoundaryLines[0].StartPoint, lstBoundaryLines[0].EndPoint);

            //Vector3D uvTransverse = MathHelper.UnitVector3DFromPt1ToPt2(lstBoundaryLines[1].StartPoint, lstBoundaryLines[1].EndPoint);

            for (int i = 0; i < nTransverse; i++)
            {
                Line rftTransverse = lstBoundaryLines[0].Offset(DefaultValues.LongBarSpacing * -i, Vector3D.AxisZ) as Line;

                List<Line> lstRftLines = CadHelper.LinesTrimWithPolygon(Slab.linPathSlab, rftTransverse);

                for (int j = 0; j < lstRftLines.Count; j++)
                {
                    Rebar rebar = new Rebar(new LinearPath(lstRftLines[j].Vertices));
                    RFT.Add(rebar);
                }


            }


            for (int i = 0; i < nLong; i++)
            {
                Line rftLong = lstBoundaryLines[1].Offset(DefaultValues.LongBarSpacing * -i, Vector3D.AxisZ) as Line;

                List<Line> lstRftLines = CadHelper.LinesTrimWithPolygon(Slab.linPathSlab, rftLong);


                for (int j = 0; j < lstRftLines.Count; j++)
                {
                    Rebar rebar = new Rebar(new LinearPath(lstRftLines[j].Vertices));
                    RFT.Add(rebar);

                }
            }

            for (int i = 0; i < Slab.Openings.Count; i++)
            {
                List<Rebar> lstRebar = new List<Rebar>();

                for (int k = 0; k < RFT.Count; k++)
                {
                    if (MathHelper.IntersectionOfLineWithPolygon(Slab.Openings[i].LinPathOpening, new Line(RFT[k].LinearPath.StartPoint, RFT[k].LinearPath.EndPoint)))
                    {
                        lstRebar.Add(RFT[k]);

                    }
                }
                OpeningsRFT.AddRange(lstRebar);
            }
            for (int i = 0; i < OpeningsRFT.Count; i++)
            {
                RFT.Remove(OpeningsRFT[i]);

            }

        }


    }
}
