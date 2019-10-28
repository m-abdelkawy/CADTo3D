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
    public class ReinforcedCadFooting
    {
        #region Properties
        public RCFooting RcFooting { get; set; }
        public List<Rebar> LongRft { get; set; } = new List<Rebar>();
        public List<Rebar> TransverseRft { get; set; } = new List<Rebar>();
        #endregion



        public ReinforcedCadFooting(RCFooting _RcFooting)
        {
            this.RcFooting = _RcFooting;

            PopulateRft();
        }


        public void PopulateRft()
        {
            if (RcFooting == null)
                return;

            if (!MathHelper.IsRectangle(RcFooting.ProfilePath,0.02))
                PopulateRftStripFooting();
            else
            {

            LinearPath linPathFooting = (LinearPath)RcFooting.ProfilePath.Offset(-1 * DefaultValues.FootingCover);
            //Mesh mesg = linPathFooting.mesh;

            for (int i = 0; i < linPathFooting.Vertices.Count(); i++)
            {
                linPathFooting.Vertices[i].Z += DefaultValues.FootingCover/* + DefaultValues.PCFootingThinkess*/;
            }


        
            Line[] pathLines = linPathFooting.ConvertToLines();

            int numRebarLong = Convert.ToInt32(pathLines[0].Length() / DefaultValues.LongBarSpacing);
            int numRebarTransverse = Convert.ToInt32(pathLines[1].Length() / DefaultValues.LongBarSpacing);

            Vector3D uvLong = MathHelper.UnitVector3DFromPt1ToPt2(pathLines[1].StartPoint, pathLines[1].EndPoint);

            Vector3D uvTransverse = MathHelper.UnitVector3DFromPt1ToPt2(pathLines[0].EndPoint, pathLines[0].StartPoint);

            for (int i = 0; i < numRebarLong; i++)
            {
                //Transverse Rebars
                Point3D stPtTrans = pathLines[0].EndPoint + uvTransverse * DefaultValues.LongBarSpacing * i;
                Point3D endPtTrans = stPtTrans + pathLines[1].Length() * uvLong;

                Point3D stPtZTrans = stPtTrans + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);
                Point3D endPtZTrans = endPtTrans + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);

                LinearPath linePathTransverse = new LinearPath(stPtZTrans, stPtTrans, endPtTrans, endPtZTrans);
                //LinearPath linePathTransverse = new LinearPath(stPtZTrans,stPtTrans, endPtTrans);
                TransverseRft.Add(new Rebar(linePathTransverse));
                
            }

            for (int i = 0; i < numRebarTransverse; i++)
            {
                //Longitudinal Rebars
                Point3D stPtLong = pathLines[1].StartPoint + uvLong * DefaultValues.LongBarSpacing * i;
                Point3D endPtLong = stPtLong + pathLines[0].Length() * uvTransverse;

                Point3D stPZtLong = stPtLong + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);
                Point3D endPtZLong = endPtLong + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);

                LinearPath linePathLong = new LinearPath(stPZtLong, stPtLong, endPtLong, endPtZLong);
                //LinearPath linePathLong = new LinearPath(stPtLong, endPtLong);


                LongRft.Add(new Rebar(linePathLong));
            }
            }
        }

        private void PopulateRftStripFooting()
        {
            LinearPath boundaryPath =(LinearPath)RcFooting.ProfilePath.Offset(-1 * DefaultValues.FootingCover);
            //Mesh mesg = linPathFooting.mesh;

            for (int i = 0; i < boundaryPath.Vertices.Count(); i++)
            {
                boundaryPath.Vertices[i].Z += DefaultValues.FootingCover;
            }
            List<Line> lstBoundaryLines = CadHelper.BoundaryLinesGet(boundaryPath);

            int nLong = Convert.ToInt32(lstBoundaryLines[0].Length() / DefaultValues.LongBarSpacing);
            int nTransverse = Convert.ToInt32(lstBoundaryLines[1].Length() / DefaultValues.LongBarSpacing);
             

            for (int i = 0; i < nTransverse; i++)
            {
                Line rftTransverse = lstBoundaryLines[0].Offset(DefaultValues.LongBarSpacing * -i, Vector3D.AxisZ) as Line;

                List<Line> lstRftLines = CadHelper.LinesTrimWithPolygon(boundaryPath, rftTransverse);

                for (int j = 0; j < lstRftLines.Count; j++)
                {
                    Point3D stPZtLong = lstRftLines[j].Vertices[0] + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);
                    Point3D endPtZLong = lstRftLines[j].Vertices[1] + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);
                    Rebar rebar = new Rebar(new LinearPath(stPZtLong, lstRftLines[j].Vertices[0], lstRftLines[j].Vertices[1], endPtZLong));
                    TransverseRft.Add(rebar);
                }


            }


            for (int i = 0; i < nLong; i++)
            {
                Line rftLong = lstBoundaryLines[1].Offset(DefaultValues.LongBarSpacing * -i, Vector3D.AxisZ) as Line;

                List<Line> lstRftLines = CadHelper.LinesTrimWithPolygon(boundaryPath, rftLong);


                for (int j = 0; j < lstRftLines.Count; j++)
                {
                    Point3D stPZtLong = lstRftLines[j].Vertices[0] + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);
                    Point3D endPtZLong = lstRftLines[j].Vertices[1] + Vector3D.AxisZ * (DefaultValues.RCFootingThinkess - DefaultValues.FootingCover);
                    Rebar rebar = new Rebar(new LinearPath(stPZtLong, lstRftLines[j].Vertices[0], lstRftLines[j].Vertices[1], endPtZLong));
                    LongRft.Add(rebar);

                }
            }
        }
    }
}
