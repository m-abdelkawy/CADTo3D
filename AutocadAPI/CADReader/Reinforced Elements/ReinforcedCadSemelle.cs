using CADReader.Base;
using CADReader.BuildingElements;
using CADReader.ElementComponents;
using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Reinforced_Elements
{
    public class ReinforcedCadSemelle : ReinforcedElements
    {
        public Semelle Semelle { get; set; }
        public List<Rebar> Rebars { get; set; }
        public Stirrup Stirrup { get; set; }
        public Point3D StartPt { get; set; }
        public Point3D EndPt { get; set; }

        public ReinforcedCadSemelle(ReadAutodesk cadreader,Semelle semelle)
        {
            Semelle = semelle;
            

            LstRebarPopulate(cadreader);
        }



        //private override void ReinforcementPopulate()
        //{
        //    LstRebarPopulate(cadreader);
        //    StirrupPopulate();
        //}

        private void LstRebarPopulate(ReadAutodesk cadreader)
        {
            this.Rebars = new List<Rebar>();

            List<LinearPath> lstFootingLinPath = CadHelper.PLinesGetByLayerName(cadreader, CadLayerName.RCFooting);
            List<Line> lstSemelleLongLine;
            List<LinearPath> lstFootingWithSemelle = CadHelper.FootingsWithSemelles(Semelle.HzLinPath, lstFootingLinPath, out lstSemelleLongLine);

            if (lstSemelleLongLine.Count != 2) return;

            //center line
            Line centerLine = CadHelper.CenterLineBetweenTwoParallelsGet(lstSemelleLongLine[0], lstSemelleLongLine[1]);

            //Columns inside footings
            List<LinearPath> lstVlElements = CadHelper.PLinesGetByLayerName(cadreader, CadLayerName.Column);

            lstVlElements.AddRange(CadHelper.PLinesGetByLayerName(cadreader, CadLayerName.ShearWall));

            List<LinearPath> lstColFooting1 = CadHelper.ColumnsInsideFootingGet(lstFootingWithSemelle[0], lstVlElements);
            List<LinearPath> lstColFooting2 = CadHelper.ColumnsInsideFootingGet(lstFootingWithSemelle[1], lstVlElements);

            //Get Intersection points of centerline with vl elements inside footing
            
            Point3D pt1 = CadHelper.PointIntersectionSemelleWithColumn(centerLine, lstColFooting1);
            Point3D pt2 = CadHelper.PointIntersectionSemelleWithColumn(centerLine, lstColFooting2);

            //offset the two ling lines
            if(pt1 != null && pt2 != null)
            {
                LinearPath centerRebarBot = new LinearPath(pt1 + (Vector3D.AxisZ * DefaultValues.SemelleCover), pt2 + (Vector3D.AxisZ * DefaultValues.SemelleCover));
                double width = MathHelper.DistanceBetweenTwoParallels(lstSemelleLongLine[0], lstSemelleLongLine[1]);
                LinearPath l1RebarBot = (LinearPath)centerRebarBot.Offset(width / 2 - DefaultValues.SemelleCover);
                LinearPath l2RebarBot = (LinearPath)centerRebarBot.Offset((width / 2 - DefaultValues.SemelleCover) * -1);

                //offset to get the top rebar
                LinearPath centerRebarTop = new LinearPath(pt1 + (Vector3D.AxisZ * (Semelle.Thickness - DefaultValues.SemelleCover)), pt2 + (Vector3D.AxisZ * (Semelle.Thickness - DefaultValues.SemelleCover)));
                LinearPath l1RebarTop = (LinearPath)centerRebarTop.Offset(width / 2 - DefaultValues.SemelleCover);
                LinearPath l2RebarTop = (LinearPath)centerRebarTop.Offset((width / 2 - DefaultValues.SemelleCover) * -1);

                //create rebars
                Rebars.Add(new Rebar(centerRebarBot));
                Rebars.Add(new Rebar(l1RebarBot));
                Rebars.Add(new Rebar(l2RebarBot));

                Rebars.Add(new Rebar(centerRebarTop));
                Rebars.Add(new Rebar(l1RebarTop));
                Rebars.Add(new Rebar(l2RebarTop));
            }
            
        }

        //private void StirrupPopulate()
        //{
        //    LinearPath stirrupLp = (LinearPath)RectColumn.ColPath.Offset(-RectColumn.Cover * 1.2);
        //    //for (int i = 0; i < stirrupLp.Vertices.Length; i++)
        //    //{
        //    //    //stirrupLp.Vertices[i].Z += CADConfig.Units == linearUnitsType.Meters?lvl+1:lvl+1000;
        //    //    //stirrupLp.Vertices[i].Z += lvl;
        //    //}

        //    Stirrup = new Stirrup(stirrupLp);
        //}

        public override void ReinforcementPopulate()
        {
            throw new NotImplementedException();
        }
    }
}
