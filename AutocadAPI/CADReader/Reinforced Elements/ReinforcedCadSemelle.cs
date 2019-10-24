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

        public ReinforcedCadSemelle(ReadAutodesk cadreader, Semelle semelle)
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
            //instantiate rebar property
            Rebars = new List<Rebar>();

            // Get footings in the drawing
            List<LinearPath> lstFooting = CadHelper.PLinesGetByLayerName(cadreader, CadLayerName.RCFooting);

            //footings intersecting with semelles
            List<Line> lstSemelleLongLine;

            List<LinearPath> lstIntersectingFooting = CadHelper.EntitiesIntersectingSemelleGet(Semelle.HzLinPath, lstFooting, out lstSemelleLongLine);



            if (lstSemelleLongLine.Count == 2)
            {
                //Check type of intersecting entities
                Line centerLine = CadHelper.CenterLineBetweenTwoParallelsGet(lstSemelleLongLine[0], lstSemelleLongLine[1]);


                //for (int i = 0; i < lstIntersectingFooting.Count; i++)
                //{

                //(columns, shearwalls, walls) inside footings
                string[] layerArr = { CadLayerName.Column, CadLayerName.ShearWall, CadLayerName.Wall };



                // Get entities inside footing
                List<LinearPath> lstVlElements = CadHelper.PLinesGetByLayerName(cadreader, layerArr);
                List<LinearPath> lstEntityInsideFooting1 = CadHelper.EntitiesInsideFootingGet(lstIntersectingFooting[0], lstVlElements);

                //intersection point of Semelle Center Line with the nearest entity inside the footing polygon
                Point3D pt1 = CadHelper.PointIntersectSemelleWithNearEntity(centerLine, lstEntityInsideFooting1);
                if(pt1 == null)
                {
                    Line lineModified = CadHelper.LineModify(centerLine, CADConfig.Units == linearUnitsType.Meters ? 15 : 15000
                        , CADConfig.Units == linearUnitsType.Meters ? 15 : 15000);
                    List<Point3D> lstPtIntersection = MathHelper.PointsIntersectOfLineSegmentWithPolygon(lstIntersectingFooting[0], lineModified);

                    pt1 = MathHelper.MidPoint3D(lstPtIntersection[0], lstPtIntersection[1]);
                }

                //2 points of Center Line intersection with polygon
                List<LinearPath> lstEntityInsideFooting2 = CadHelper.EntitiesInsideFootingGet(lstIntersectingFooting[1], lstVlElements);


                //intersection point of Semelle Center Line with the nearest entity inside the footing polygon
                Point3D pt2 = CadHelper.PointIntersectSemelleWithNearEntity(centerLine, lstEntityInsideFooting2);
                if (pt2 == null)
                {
                    Line lineModified = CadHelper.LineModify(centerLine, CADConfig.Units == linearUnitsType.Meters ? 15 : 15000
                        , CADConfig.Units == linearUnitsType.Meters ? 15 : 15000);
                    List<Point3D> lstPtIntersection = MathHelper.PointsIntersectOfLineSegmentWithPolygon(lstIntersectingFooting[1], lineModified);

                    pt2 = MathHelper.MidPoint3D(lstPtIntersection[0], lstPtIntersection[1]);
                }





                //}

                //offset the two ling lines
                if (pt1 != null && pt2 != null)
                {
                    LinearPath centerRebarBot = new LinearPath(pt1 + (Vector3D.AxisZ * DefaultValues.SemelleCover), pt2 + (Vector3D.AxisZ * DefaultValues.SemelleCover));
                    double width = MathHelper.DistanceBetweenTwoParallels(lstSemelleLongLine[0], lstSemelleLongLine[1]);
                    LinearPath l1RebarBot = (LinearPath)centerRebarBot.Offset(width / 2 - DefaultValues.SemelleCover);
                    LinearPath l2RebarBot = (LinearPath)centerRebarBot.Offset((width / 2 - DefaultValues.SemelleCover) * -1);

                    //offset to get the top rebar
                    LinearPath centerRebarTop = new LinearPath(pt1 + (Vector3D.AxisZ * (Semelle.Thickness - DefaultValues.SemelleCover)),
                        pt2 + (Vector3D.AxisZ * (Semelle.Thickness - DefaultValues.SemelleCover)));
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
