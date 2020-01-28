using CADReader.BuildingElements;
using CADReader.ElectricalElements;
using CADReader.ElementComponents;
using CADReader.Helpers;
using CADReader.Reinforced_Elements;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Base
{
    public class SuperStructure: FloorBase
    {
        #region Properties
        public List<ReinforcedCadColumn> LstRcColumn { get; set; }
        public List<ReinforcedCadSlab> LstRcSlab { get; set; }
        public List<Stair> LstStair { get; set; }
        public List<LinearPath> LstLandingLinPath { get; set; }

        public List<SlopedSlab> LstRamp { get; set; }

        public List<ElectricalConduit> LstElectConduit { get; set; }
        public List<ReinforcedCadShearWall> LstRcShearWall { get; private set; }
        public List<Axis> LstAxis { get; set; }
        #endregion

        #region Constructors
        public SuperStructure(ReadAutodesk cadReader, double level)
        {
            Level = level;

            //Reinforced Slabs
            LstRcSlab = base.GetRcSLabs(cadReader);

            //RC Columns
            LstRcColumn = base.GetRCColumns(cadReader);


            //Stairs
            GetStairs(cadReader);

            //Reinforced Shear Walls
            LstRcShearWall = GetRCShearWalls(cadReader);

            //Ramps
            LstRamp = GetRamps(cadReader);

            //Electricity hoses
            GetElectricalConduit(cadReader);

        }

        public SuperStructure(ReadAutodesk cadReader, double level, double height)
        {
            this.Level = level;
            this.Height = height;

            //Reinforced Slabs
            LstRcSlab = base.GetRcSLabs(cadReader);

            //RC Columns
            LstRcColumn = base.GetRCColumns(cadReader);


            //Stairs
            GetStairs(cadReader);

            //Reinforced Shear Walls
            LstRcShearWall = GetRCShearWalls(cadReader);

            //Ramps
            LstRamp = GetRamps(cadReader);

            //Electricity hoses
            GetElectricalConduit(cadReader);

            //floor axes
            LstAxis = GetAxes(cadReader);
        }

        #endregion

        #region class Methods
        private void GetStairs(ReadAutodesk cadFileReader)
        {
            LstStair = new List<Stair>();
            List<LinearPathEx> lstStairFlightPath = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair).Where(p => p is LinearPathEx).Cast<LinearPathEx>().ToList();
            LstLandingLinPath = CadHelper.PLinesGetByLayerName(cadFileReader, CadLayerName.Stair).Except<LinearPath>(lstStairFlightPath).ToList();

            for (int i = 0; i < LstLandingLinPath.Count; i++)
            {
                for (int j = 0; j < LstLandingLinPath[i].Vertices.Length; j++)
                {
                    LstLandingLinPath[i].Vertices[j].Z += Level;
                }
            }

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
                    if (Math.Abs(item.Value[i].StartPoint.Z - item.Value[i].EndPoint.Z) > 0.0001)
                    {
                        slopedLines.Add(new Line(new Point3D(item.Value[i].StartPoint.X, item.Value[i].StartPoint.Y, Level + item.Value[i].StartPoint.Z)
                            , new Point3D(item.Value[i].EndPoint.X, item.Value[i].EndPoint.Y, Level + item.Value[i].EndPoint.Z)));
                    }
                    else
                    {
                        hzLines.Add(new Line(new Point3D(item.Value[i].StartPoint.X, item.Value[i].StartPoint.Y, Level + item.Value[i].StartPoint.Z)
                            , new Point3D(item.Value[i].EndPoint.X, item.Value[i].EndPoint.Y, Level + item.Value[i].EndPoint.Z)));
                    }
                }
                double stairWidth = hzLines[0].Length();
                LstStair.Add(new Stair(stairWidth, slopedLines));
            }

        }

        private void GetElectricalConduit(ReadAutodesk cadReader)
        {
            LstElectConduit = new List<ElectricalConduit>();
            double elecLevel = Level - DefaultValues.SlabThinkess + DefaultValues.FootingCover + DefaultValues.BarDiameter;
            List<Entity> lstElecConduit = CadHelper.EntitiesGetByLayerName(cadReader, CadLayerName.ElecConduit);
            for (int i = 0; i < lstElecConduit.Count; i++)
            {

                if (lstElecConduit[i] is LinearPath)
                {
                    for (int j = 0; j < lstElecConduit[i].Vertices.Length; j++)
                    {
                        lstElecConduit[i].Vertices[j].Z = elecLevel;
                    }
                }
                else if (lstElecConduit[i] is CompositeCurve)
                {
                    CompositeCurve compCurve = lstElecConduit[i] as CompositeCurve;
                    for (int j = 0; j < compCurve.CurveList.Count; j++)
                    {
                        Line line = compCurve.CurveList[j] as Line;
                        if (line != null)
                        {
                            for (int k = 0; k < line.Vertices.Count(); k++)
                            {
                                line.Vertices[k].Z = elecLevel;
                            }
                        }
                        else
                        {
                            Arc arc = compCurve.CurveList[j] as Arc;
                            arc.StartPoint.Z = elecLevel;
                            arc.EndPoint.Z = elecLevel;
                            arc.Center.Z = elecLevel;
                        }
                    }
                }


                LstElectConduit.Add(new ElectricalConduit(lstElecConduit[i]));
            }
        }

        private List<Axis> GetAxes(ReadAutodesk cadReader)
        {
            //get lines and plines of axes
            List<Entity> lstEntAxes = CadHelper.EntitiesGetByLayerName(cadReader, CadLayerName.Axes);

            List<Axis> lstAxis = new List<Axis>();

            //get axis block
            Block blkCircle = cadReader.Blocks.Where(b => b.Name == CadBlockName.GridCircle).FirstOrDefault();

            //get lines and polylines from axis block
            List<LinearPath> lstLinPathAxes = new List<LinearPath>();
            for (int i = 0; i < lstEntAxes.Count; i++)
            {
                if (lstEntAxes[i] is LinearPath)
                {
                    lstLinPathAxes.Add(lstEntAxes[i] as LinearPath);
                }
                else if (lstEntAxes[i] is Line)
                {
                    LinearPath linPath = new LinearPath((lstEntAxes[i] as Line).StartPoint, (lstEntAxes[i] as Line).EndPoint);
                    lstLinPathAxes.Add(linPath);
                }
            }

            //get circle grid blocks
            List<BlockReferenceEx> lstGridCircleBlkRef = cadReader.Entities.Where(b => b is BlockReferenceEx).Cast<BlockReferenceEx>().Where(b => b.BlockName == CadBlockName.GridCircle).ToList();

            //get circles from circle grid block


            //for each line get the nearest two circles to its end points and their attribute
            double circleRadius = (blkCircle.Entities.FirstOrDefault(e => e is Circle) as Circle).Radius;

            for (int i = 0; i < lstLinPathAxes.Count(); i++)
            {
                List<Circle> lstCircleAddToAxis = new List<Circle>();

                double minDist1 = double.MaxValue;
                double minDist2 = double.MaxValue;

                Point3D insertionPt1 = new Point3D(0, 0, 0);
                Point3D insertionPt2 = new Point3D(0, 0, 0);

                string textAttr1 = "";
                string textAttr2 = "";


                for (int j = 0; j < lstGridCircleBlkRef.Count(); j++)
                {
                    //first end point
                    double dist1 = MathHelper.CalcDistanceBetweenTwoPoint3D(lstLinPathAxes[i].StartPoint, lstGridCircleBlkRef[j].InsertionPoint);
                    if (dist1 < minDist1)
                    {
                        minDist1 = dist1;
                        insertionPt1 = lstGridCircleBlkRef[j].InsertionPoint;
                        textAttr1 = lstGridCircleBlkRef[j].Attributes["GRID-CIRC"].Value.ToString();
                    }

                    //second end point
                    double dist2 = MathHelper.CalcDistanceBetweenTwoPoint3D(lstLinPathAxes[i].EndPoint, lstGridCircleBlkRef[j].InsertionPoint);
                    if (dist2 < minDist2)
                    {
                        minDist2 = dist2;
                        insertionPt2 = lstGridCircleBlkRef[j].InsertionPoint;
                        textAttr2 = lstGridCircleBlkRef[j].Attributes["GRID-CIRC"].Value.ToString();
                    }
                }

                insertionPt1.Z = Level;
                insertionPt2.Z = Level;
                Circle c1 = new Circle(insertionPt1, circleRadius);
                Circle c2 = new Circle(insertionPt2, circleRadius);


                lstCircleAddToAxis.Add(c1);
                lstCircleAddToAxis.Add(c2);
                for (int j = 0; j < lstLinPathAxes[i].Vertices.Length; j++)
                {
                    lstLinPathAxes[i].Vertices[j].Z = Level;
                }



                Axis axis = new Axis(new LinearPath(lstLinPathAxes[i].Vertices), lstCircleAddToAxis, textAttr1.Equals(textAttr2) ? textAttr1 : "ccc");

                lstAxis.Add(axis);
            }

            return lstAxis;
        }

        #endregion
    }
}
