using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.StructuralElementsDomain;
using CADReader.BuildingElements;
using CADReader;
using devDept.Eyeshot.Entities;
using CADReader.Helpers;
using CADReader.Base;
using IfcFileCreator.Helpers;
using CADReader.Reinforced_Elements;
using CADReader.ElementComponents;
using devDept.Geometry;
using Xbim.Ifc4.SharedComponentElements;
using Xbim.Ifc4.ElectricalDomain;
using CADReader.ElectricalElements;

namespace IfcFileCreator
{
    public class XbimCreateBuilding
    {
        public Submissions BuildingSubmissions { get; set; }

        private Random random = new Random(1000);
        public XbimCreateBuilding(/*List<Building> lstBuilding, */Building cadBuilding, string pathToSave)
        {
            BuildingSubmissions = new Submissions();

            using (var model = IFCHelper.CreateandInitModel("Demo1"))
            {
                IfcSlab slab = null;

                //for (int m = 0; m < lstBuilding.Count; m++)

                List<FloorBase> lstSortedFloors = cadBuilding.Floors.OrderBy(f => f.Level).ToList();

                if (model != null)
                {
                    IfcBuilding building = IFCHelper.CreateBuilding(model, "Default Building", cadBuilding.Location);
                    //rel.
                    //double lvlDifference = 0;

                    for (int i = 0; i < lstSortedFloors.Count; i++)
                    {
                        IfcBuildingStorey storey;
                        using (var txn = model.BeginTransaction("Add Storey"))
                        {

                            storey = model.Instances.New<IfcBuildingStorey>();
                            IfcRelAggregates rel = model.Instances.New<IfcRelAggregates>();
                            rel.RelatingObject = building;
                            rel.RelatedObjects.Add(storey);

                            txn.Commit();
                        }


                        //if (i - 1 >= lstSortedFloors.Count)
                        //    lvlDifference = Math.Abs(lstSortedFloors[i].Level - lstSortedFloors[i - 1].Level);
                        //else
                        //    lvlDifference = lstSortedFloors[i].Level;


                        Floor floor = lstSortedFloors[i] as Floor;
                        if (floor != null)
                        {
                            List<IfcProduct> lstCols = new List<IfcProduct>();
                            List<IfcProduct> lstColFormWork = new List<IfcProduct>();

                            List<IfcProduct> lstShearWall = new List<IfcProduct>();
                            List<IfcProduct> lstShearWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstSlabFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstSlab = new List<IfcProduct>();
                            List<IfcProduct> lstWall = new List<IfcProduct>();
                            List<IfcProduct> lstWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstStair = new List<IfcProduct>();
                            List<IfcProduct> lstRebar = new List<IfcProduct>();
                            List<IfcProduct> lstSlabRebar = new List<IfcProduct>();


                            double wallHeight = floor.Height - DefaultValues.SlabThinkess;
                            if (i == 1)//basement
                                wallHeight = floor.Height - DefaultValues.SlabThinkess - DefaultValues.RCFootingThinkess - DefaultValues.PCFootingThinkess;

                            foreach (ReinforcedCadWall rcWall in floor.LstRcCadRetainingWall)
                            {
                                IfcWallStandardCase wall = CreateIfcWall(model, rcWall.CadWall, wallHeight, building);

                                if (wall != null) AddPropertiesToWall(model, wall);
                                using (var txn = model.BeginTransaction("Add RetainingWall"))
                                {
                                    storey.AddElement(wall);
                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, rcWall.CadWall.LinPathWall, DefaultValues.FormWorkThickness, wallHeight, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add wall to Submission
                                    lstWall.Add(wall);
                                    lstWallFormWork.Add(formWork);
                                    lstWallFormWork.Add(opening);

                                    ////Wall Rft
                                    for (int j = 0; j < rcWall.LstRebar.Count; j++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rcWall.LstRebar[j], wallHeight);
                                        storey.AddElement(bar);
                                    }
                                    int nStirrups = Convert.ToInt32(floor.Height / (DefaultValues.StirrupsSpacing));
                                    for (int j = 0; j < nStirrups; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcWall.Stirrup, DefaultValues.StirrupsSpacing);
                                        storey.AddElement(stirrup);
                                        //lstColRebar.Add(stirrup);
                                    }

                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadColumn rcCol in floor.LstRcColumn)
                            {
                                IfcColumn column = CreateIfcColumn(model, rcCol, floor.Height);
                                using (var txn = model.BeginTransaction("Add RcColumn"))
                                {
                                    storey.AddElement(column);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, rcCol.CadColumn.ColPath, DefaultValues.FormWorkThickness, floor.Height
                                        , out opening, false);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add columns to Submission
                                    lstCols.Add(column);

                                    lstColFormWork.Add(opening);
                                    lstColFormWork.Add(formWork);


                                    //rebarStopAndOperate
                                    //if (i == lstSortedFloors.Count - 1)
                                    //{
                                    foreach (var rebar in rcCol.LstRebar)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rebar, floor.Height);
                                        storey.AddElement(bar);
                                        lstRebar.Add(bar);
                                    }
                                    int nstirrups = Convert.ToInt32((floor.Height + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / DefaultValues.StirrupsSpacing);
                                    for (int j = 0; j < nstirrups - 1; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.Stirrup, DefaultValues.StirrupsSpacing);
                                        storey.AddElement(stirrup);
                                        lstRebar.Add(stirrup);

                                    }
                                    //}
                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadSlab cadRCSlab in floor.LstRcSlab)
                            {
                                slab = CreateIfcSlab(model, cadRCSlab.Slab);
                                using (var trans = model.BeginTransaction("Add Slab"))
                                {
                                    storey.AddElement(slab);

                                    IfcOpeningElement openingFormWork;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadRCSlab.Slab.LinPathSlab, DefaultValues.FormWorkThickness,
                                        cadRCSlab.Slab.Thickness, out openingFormWork, true);
                                    storey.AddElement(openingFormWork);
                                    storey.AddElement(formWork);

                                    //add slab to Submission
                                    lstSlab.Add(slab);

                                    lstSlabFormWork.Add(openingFormWork);
                                    lstSlabFormWork.Add(formWork);
                                    List<IfcOpeningElement> lstOpening = new List<IfcOpeningElement>();
                                    IfcOpeningElement opening = null;
                                    for (int n = 0; n < cadRCSlab.Slab.Openings.Count; n++)
                                    {
                                        var cadOpening = cadRCSlab.Slab.Openings[n];
                                        opening = CreateIfcOpening(model, cadOpening, DefaultValues.SlabThinkess);

                                        lstOpening.Add(opening);

                                        storey.AddElement(opening);

                                        lstSlab.Add(opening);

                                        //attach opening
                                        slab.AttchOpening(model, opening);
                                        formWork.AttchOpening(model, opening);

                                    }


                                    for (int k = 0; k < cadRCSlab.OpeningsRFT.Count; k++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, cadRCSlab.OpeningsRFT[k], 0);
                                        for (int j = 0; j < lstOpening.Count; j++)
                                        {
                                            bar.AttchOpening(model, lstOpening[j]);

                                            lstSlabRebar.Add(bar);
                                        }
                                        storey.AddElement(bar);
                                    }

                                    for (int j = 0; j < cadRCSlab.RFT.Count; j++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, cadRCSlab.RFT[j], 0);
                                        storey.AddElement(bar);
                                        lstSlabRebar.Add(bar);

                                    }
                                    trans.Commit();
                                }
                            }

                            //Create stairs
                            foreach (Stair cadStair in floor.LstStair)
                            {
                                IfcStairFlight flight;
                                IfcStair stair = CreateIfcStair(model, cadStair, out flight);

                                using (var txn = model.BeginTransaction("Add Stair"))
                                {
                                    storey.AddElement(stair);

                                    //add stair to Submission
                                    lstStair.Add(flight);

                                    txn.Commit();
                                }
                            }
                            foreach (LinearPath cadLanding in floor.LstLandingLinPath)
                            {

                                IfcSlab landing = CreateIfcLanding(model, cadLanding, DefaultValues.SlabThinkess);

                                using (var txn = model.BeginTransaction("Add Landing"))
                                {
                                    storey.AddElement(landing);

                                    lstStair.Add(landing);

                                    txn.Commit();
                                }
                            }
                            foreach (ReinforcedCadShearWall cadShearWall in floor.LstRcShearWall)
                            {

                                IfcColumn shearWall = CreateIfcShearWall(model, cadShearWall.ShearWall, floor.Height);

                                using (var txn = model.BeginTransaction("Add Shear Wall"))
                                {
                                    storey.AddElement(shearWall);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadShearWall.ShearWall.ProfilePath, DefaultValues.FormWorkThickness,
                                        floor.Height, out opening, false);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add shear wall to Submission
                                    lstShearWall.Add(shearWall);

                                    lstShearWallFormWork.Add(opening);
                                    lstShearWallFormWork.Add(formWork);

                                    foreach (var rebar in cadShearWall.VlRebar)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rebar, floor.Height);
                                        storey.AddElement(bar);
                                    }
                                    int nstirrups = Convert.ToInt32((floor.Height + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (DefaultValues.StirrupsSpacing));
                                    for (int j = 0; j < nstirrups - 1; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, cadShearWall.Stirrup, DefaultValues.StirrupsSpacing);
                                        storey.AddElement(stirrup);

                                    }

                                    txn.Commit();
                                }
                            }

                            foreach (SlopedSlab cadRamp in floor.LstRamp)
                            {

                                IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);



                                using (var txn = model.BeginTransaction("Add Ramp"))
                                {
                                    storey.AddElement(ramp);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadRamp.LinPathSlopedSlab, DefaultValues.FormWorkThickness,
                                        cadRamp.Thickness, out opening, true);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add ramp to Submission
                                    lstSlab.Add(ramp);

                                    lstSlabFormWork.Add(opening);
                                    lstSlabFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            foreach (ElectricalConduit cadConduit in floor.LstElectConduit)
                            {

                                using (var txn = model.BeginTransaction("Add conduit"))
                                {
                                    IfcCableCarrierSegment conduit = CreateIfcConduit(model, cadConduit);
                                    storey.AddElement(conduit);
                                    txn.Commit();
                                }
                            }


                            BuildingSubmissions.SubmittedElems.Add(lstStair);
                            BuildingSubmissions.SubmittedElems.Add(lstSlabFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstSlabRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstSlab);
                            BuildingSubmissions.SubmittedElems.Add(lstRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstColFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstCols);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWall);
                            BuildingSubmissions.SubmittedElems.Add(lstWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstWall);
                        }
                        else
                        {

                            List<IfcProduct> lstPCFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstPCFooting = new List<IfcProduct>();
                            List<IfcProduct> lstRCFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstRCFooting = new List<IfcProduct>();
                            List<IfcProduct> lstColFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstCol = new List<IfcProduct>();
                            List<IfcProduct> lstShearWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstShearWall = new List<IfcProduct>();
                            List<IfcProduct> lstSemelle = new List<IfcProduct>();
                            List<IfcProduct> lstWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstWall = new List<IfcProduct>();
                            List<IfcProduct> lstSlabFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstSlab = new List<IfcProduct>();
                            List<IfcProduct> lstRebar = new List<IfcProduct>();
                            List<IfcProduct> lstColRebar = new List<IfcProduct>();
                            List<IfcProduct> lstWallRebar = new List<IfcProduct>();
                            List<IfcProduct> lstShearWallRebar = new List<IfcProduct>();


                            Foundation foundation = lstSortedFloors[i] as Foundation;
                            foreach (PCFooting cadFooting in foundation.LstPCFooting)
                            {
                                IfcFooting footing = CreateIfcFooting(model, cadFooting);



                                using (var txn = model.BeginTransaction("Add Footing"))
                                {
                                    storey.AddElement(footing);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadFooting.ProfilePath, DefaultValues.FormWorkThickness, cadFooting.Thickness,
                                        out opening, false, true);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add pcfooting to Submission
                                    lstPCFooting.Add(footing);

                                    lstPCFormWork.Add(opening);
                                    lstPCFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadSemelle cadSemelle in foundation.LstRCSemelle)
                            {
                                IfcBeam semelle = CreateIfcBeam(model, cadSemelle.Semelle);

                                using (var txn = model.BeginTransaction("Add Semelle"))
                                {
                                    storey.AddElement(semelle);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadSemelle.Semelle.HzLinPath,
                                        DefaultValues.FormWorkThickness, cadSemelle.Semelle.Thickness, out opening, false, false, true);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);
                                    lstRCFooting.Add(semelle);
                                    //add pcfooting to Submission
                                    //lstPCFooting.Add(footing);

                                    //Steel
                                    //for (int l = 0; l < cadSemelle.Rebars.Count(); l++)
                                    //{
                                    //    IfcReinforcingBar bar = CreateIfcRebar(model, cadSemelle.Rebars[l], 0);
                                    //    storey.AddElement(bar);
                                    //    lstRebar.Add(bar);

                                    //}

                                    lstRCFormWork.Add(opening);
                                    lstRCFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadFooting cadFooting in foundation.LstRCCadFooting)
                            {

                                IfcFooting footing = CreateIfcFooting(model, cadFooting.RcFooting);



                                using (var txn = model.BeginTransaction("Add Footing"))
                                {
                                    storey.AddElement(footing);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadFooting.RcFooting.ProfilePath, DefaultValues.FormWorkThickness,
                                        cadFooting.RcFooting.Thickness, out opening, false, true);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);


                                    //add rcfooting to Submission
                                    lstRCFooting.Add(footing);

                                    lstRCFormWork.Add(opening);
                                    lstRCFormWork.Add(formWork);

                                    //foreach (var longBar in cadFooting.LongRft)
                                    //{
                                    //    IfcReinforcingBar barLong = CreateIfcRebar(model, longBar, 0);
                                    //    storey.AddElement(barLong);
                                    //    lstRebar.Add(barLong);
                                    //}

                                    //foreach (var transverseBar in cadFooting.TransverseRft)
                                    //{
                                    //    IfcReinforcingBar barLong = CreateIfcRebar(model, transverseBar, 0);
                                    //    storey.AddElement(barLong);
                                    //    lstRebar.Add(barLong);

                                    //}

                                    txn.Commit();
                                }
                            }

                            foreach (SlopedSlab cadRamp in foundation.LstRamp)
                            {

                                IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);

                                using (var txn = model.BeginTransaction("Add Ramp"))
                                {
                                    storey.AddElement(ramp);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadRamp.LinPathSlopedSlab, DefaultValues.FormWorkThickness,
                                        cadRamp.Thickness, out opening, true);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add rcfooting to Submission
                                    lstSlab.Add(ramp);

                                    lstSlabFormWork.Add(opening);
                                    lstSlabFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            #region old code walls in foundation level
                            //double wallHeight = lvlDifference - DefaultValues.SlabThinkess;

                            //foreach (ReinforcedCadWall rcWall in foundation.LstRCCadWall)
                            //{

                            //    IfcWallStandardCase wall = CreateIfcWall(model, rcWall.CadWall, wallHeight, building);
                            //    if (wall != null) AddPropertiesToWall(model, wall);

                            //    using (var txn = model.BeginTransaction("Add RetainingWall"))
                            //    {
                            //        storey.AddElement(wall);

                            //        IfcOpeningElement opening;
                            //        IfcBuildingElementPart formWork = CreateFormWork(model, rcWall.CadWall.LinPathWall, DefaultValues.FormWorkThickness, wallHeight, out opening);
                            //        storey.AddElement(opening);
                            //        storey.AddElement(formWork);

                            //        //add rcfooting to Submission
                            //        lstWall.Add(wall);

                            //        lstWallFormWork.Add(opening);
                            //        lstWallFormWork.Add(formWork);

                            //        //Wall Rft
                            //        //for (int j = 0; j < rcWall.LstRebar.Count; j++)
                            //        //{
                            //        //    IfcReinforcingBar bar = CreateIfcRebar(model, rcWall.LstRebar[j], wallHeight);
                            //        //    storey.AddElement(bar);
                            //        //    lstWallRebar.Add(bar);
                            //        //}
                            //        //int nStirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (DefaultValues.StirrupsSpacing));
                            //        //for (int j = 0; j < nStirrups; j++)
                            //        //{
                            //        //    IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcWall.Stirrup, DefaultValues.StirrupsSpacing);
                            //        //    storey.AddElement(stirrup);
                            //        //    //lstColRebar.Add(stirrup);
                            //        //    lstWallRebar.Add(stirrup);

                            //        //}

                            //        txn.Commit();
                            //    }
                            //}

                            #endregion
                            #region columns old code
                            /*foreach (ReinforcedCadColumn rcCol in foundation.LstRcColumn)
                            {
                                IfcColumn column = CreateIfcColumn(model, rcCol, lvlDifference);
                                using (var txn = model.BeginTransaction("Add column"))
                                {
                                    storey.AddElement(column);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, rcCol.CadColumn.ColPath, DefaultValues.FormWorkThickness, lvlDifference, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add rcfooting to Submission
                                    lstCol.Add(column);

                                    lstColFormWork.Add(opening);
                                    lstColFormWork.Add(formWork);



                                    //foreach (var rebar in rcCol.LstRebar)
                                    //{
                                    //    IfcReinforcingBar bar = CreateIfcRebar(model, rebar, lvlDifference);
                                    //    storey.AddElement(bar);
                                    //    lstColRebar.Add(bar);
                                    //}
                                    //int nStirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / DefaultValues.StirrupsSpacing);
                                    //for (int j = 0; j < nStirrups; j++)
                                    //{
                                    //    IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.Stirrup, DefaultValues.StirrupsSpacing);
                                    //    storey.AddElement(stirrup);
                                    //    lstColRebar.Add(stirrup);

                                    //}

                                    txn.Commit();
                                }
                            }*/
                            #endregion

                            #region old code shearwall in foundation level
                            //foreach (ReinforcedCadShearWall cadShearWall in foundation.LstRCShearWall)
                            //{

                            //    IfcColumn shearWall = CreateIfcShearWall(model, cadShearWall.ShearWall, lvlDifference);

                            //    using (var txn = model.BeginTransaction("Add Shear Wall"))
                            //    {
                            //        storey.AddElement(shearWall);

                            //        IfcOpeningElement opening;
                            //        IfcBuildingElementPart formWork = CreateFormWork(model, cadShearWall.ShearWall.ProfilePath, DefaultValues.FormWorkThickness, lvlDifference, out opening);
                            //        storey.AddElement(opening);
                            //        storey.AddElement(formWork);

                            //        //add shear wall to Submission
                            //        lstShearWall.Add(shearWall);

                            //        lstShearWallFormWork.Add(opening);
                            //        lstShearWallFormWork.Add(formWork);

                            //        //foreach (var rebar in cadShearWall.VlRebar)
                            //        //{
                            //        //    IfcReinforcingBar bar = CreateIfcRebar(model, rebar, lvlDifference);
                            //        //    storey.AddElement(bar);
                            //        //    lstShearWallRebar.Add(bar);
                            //        //}
                            //        //int nstirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (DefaultValues.StirrupsSpacing));
                            //        //for (int j = 0; j < nstirrups - 1; j++)
                            //        //{
                            //        //    IfcReinforcingBar stirrup = CreateIfcStirrup(model, cadShearWall.Stirrup, DefaultValues.StirrupsSpacing);
                            //        //    storey.AddElement(stirrup);
                            //        //    lstShearWallRebar.Add(stirrup);

                            //        //}

                            //        txn.Commit();
                            //    }
                            //}
                            #endregion

                            BuildingSubmissions.SubmittedElems.Add(lstPCFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstPCFooting);
                            BuildingSubmissions.SubmittedElems.Add(lstRCFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstRCFooting);
                            BuildingSubmissions.SubmittedElems.Add(lstColRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstColFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstCol);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWallRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWall);
                            BuildingSubmissions.SubmittedElems.Add(lstWallRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstWall);

                        }
                    }



                }

                try
                {

                    model.SaveAs(pathToSave + @"\Demo1.ifc", IfcStorageType.Ifc);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public XbimCreateBuilding(Building cadBuilding, string pathToSave, bool createEachFloorIFC)
        {
            List<FloorBase> lstFloorSorted = cadBuilding.Floors.OrderBy(f => f.Level).ToList();

            for (int i = 0; i < lstFloorSorted.Count; i++)
            {
                using (IfcStore model = IFCHelper.CreateandInitModel("Demo2"))
                {
                    if (model != null)
                    {
                        IfcBuilding building = IFCHelper.CreateBuilding(model, $"BuildingB-{i}", cadBuilding.Location);
                        IfcBuildingStorey storey = IFCHelper.CreateStorey(model, building);

                        Floor floor = lstFloorSorted[i] as Floor;

                        if (floor != null)
                        {
                            CreateStoreyRetainingWalls(model, building, storey, floor);

                            CreateStoreyColumns(model, storey, floor);

                            CreateStoreyRoofSlabs(model, storey, floor);

                            CreateStoreyShearWalls(model, storey, floor);

                            CreateRamps(model, storey, floor);

                            CreateStoreyElectricalConduit(model, storey, floor);

                            //create stairs
                            CreateStoreyStairs(model, storey, floor);

                            using (var txn = model.BeginTransaction("Add Axes"))
                            {
                                CreateAxesFloor(model, storey, floor);
                                txn.Commit();
                            }
                        }

                        else //floor is foundation floor
                        {
                            Foundation foundation = lstFloorSorted[i] as Foundation;

                            CreateBuildingPCFootings(model, storey, foundation);
                            CreateBuildingSemelles(model, storey, foundation);
                            CreateRCFootings(model, storey, foundation);
                            CreateFoundationRamps(model, storey, foundation);
                            //CreateFoundationRetainingWalls(model, building, storey, foundation);
                            //CreateFoundationColumns(model, storey, foundation);
                            //CreateFoundationShearWalls(model, storey, foundation);

                            using (var txn = model.BeginTransaction("Add Axes"))
                            {
                                CreateAxesFoundation(model, storey, foundation);
                                txn.Commit();
                            }
                        }

                        try
                        {
                            model.SaveAs(pathToSave + @"\Demo" + i + ".ifc", IfcStorageType.Ifc);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
        }

        private void CreateStoreyStairs(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {
            foreach (Stair cadStair in floor.LstStair)
            {
                IfcStairFlight flight;
                IfcStair stair = CreateIfcStair(model, cadStair, out flight);

                using (var txn = model.BeginTransaction("Add Stair"))
                {
                    storey.AddElement(stair);

                    txn.Commit();
                }
            }
            foreach (LinearPath cadLanding in floor.LstLandingLinPath)
            {

                IfcSlab landing = CreateIfcLanding(model, cadLanding, DefaultValues.SlabThinkess);

                using (var txn = model.BeginTransaction("Add Landing"))
                {
                    storey.AddElement(landing);

                    txn.Commit();
                }
            }
        }

        private void CreateAxesFloorOld(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {

            foreach (Axis axis in floor.LstAxis)
            {
                Rebar bar = new Rebar(axis.AxisLinPath);
                IfcReinforcingBar ifcBar = CreateIfcRebar(model, bar, 0);


                storey.AddElement(ifcBar);
            }
        }

        private void CreateAxesFloor(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {

            foreach (Axis axis in floor.LstAxis)
            {
                IfcSlabStandardCase ifcAxis = CreateIfcAxis(model, axis);

                storey.AddElement(ifcAxis);
            }
        }

        private IfcSlabStandardCase CreateIfcAxis(IfcStore model, Axis axis)
        {
            IfcSlabStandardCase ifcAxisToCreate = model.Instances.New<IfcSlabStandardCase>();
            ifcAxisToCreate.Name = "Axis-" + random.Next(100000);
            ifcAxisToCreate.Tag = axis.AxisText;

            //Create Local axes system and assign it to the column
            

            IfcDirection localXDir = model.Instances.New<IfcDirection>();
            localXDir.SetXYZ(1, 0, 0);

            IfcDirection localZDir = model.Instances.New<IfcDirection>();
            localZDir.SetXYZ(0, 0, 1);


            //now place the wall into the model

            IfcCircleProfileDef profile = IFCHelper.CircleProfileCreate(model, CADConfig.Units == linearUnitsType.Meters ? 0.080 : 80);

            IfcSurfaceCurveSweptAreaSolid bodyLine = IFCHelper.ProfileSurfaceSweptSolidCreate(model, profile, axis.AxisLinPath);

            IfcCircleProfileDef circProf1 = IFCHelper.CircleProfileCreate(model, axis.LstCircle[0].Radius*2);
            IfcExtrudedAreaSolid circleAxis1 = IFCHelper.ProfileSweptSolidCreate(model, CADConfig.Units == linearUnitsType.Meters ? 0.080 : 80, circProf1);
            circleAxis1.BodyPlacementSet(model, axis.LstCircle[0].Center.X, axis.LstCircle[0].Center.Y, axis.LstCircle[0].Center.Z);

            IfcCircleProfileDef circProf2 = IFCHelper.CircleProfileCreate(model, axis.LstCircle[1].Radius*2);
            IfcExtrudedAreaSolid circleAxis2 = IFCHelper.ProfileSweptSolidCreate(model, CADConfig.Units == linearUnitsType.Meters ? 0.080 : 80, circProf2);
            circleAxis2.BodyPlacementSet(model, axis.LstCircle[1].Center.X, axis.LstCircle[1].Center.Y, axis.LstCircle[1].Center.Z);


            //IfcSurfaceCurveSweptAreaSolid bodyCircle2 = IFCHelper.ProfileSurfaceSweptSolidCreate(model, profile, axis.LstCircle[1]);

            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "AdvancedSweptSolid", "Body");
            shape.Items.Add(bodyLine);
            shape.Items.Add(circleAxis1);
            shape.Items.Add(circleAxis2);

            //Create a Product Definition and add the model geometry to the Axis
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            ifcAxisToCreate.Representation = prDefShape;

            return ifcAxisToCreate;
        }

        private void CreateAxesFoundation(IfcStore model, IfcBuildingStorey storey, Foundation foundation)
        {

            foreach (Axis axis in foundation.LstAxis)
            {
                IfcSlabStandardCase ifcAxis = CreateIfcAxis(model, axis);

                storey.AddElement(ifcAxis);
            }

        }

        private void CreateFoundationShearWalls(IfcStore model, IfcBuildingStorey storey, Foundation foundation)
        {
            foreach (ReinforcedCadShearWall cadShearWall in foundation.LstRCShearWall)
            {
                double shearWallHeight = foundation.Height - DefaultValues.SlabThinkess;
                IfcColumn shearWall = CreateIfcShearWall(model, cadShearWall.ShearWall, shearWallHeight);

                using (var txn = model.BeginTransaction("Add Shear Wall"))
                {
                    storey.AddElement(shearWall);

                    IfcOpeningElement formworkVoid;
                    IfcBuildingElementPart formWork = CreateFormWork(model, cadShearWall.ShearWall.ProfilePath, DefaultValues.FormWorkThickness, shearWallHeight, out formworkVoid);
                    storey.AddElement(formworkVoid);
                    storey.AddElement(formWork);

                    txn.Commit();
                }
            }
        }

        private void CreateFoundationColumns(IfcStore model, IfcBuildingStorey storey, Foundation foundation)
        {
            foreach (ReinforcedCadColumn rcCol in foundation.LstRcColumn)
            {
                double colHeight = foundation.Height - DefaultValues.SlabThinkess;
                IfcColumn column = CreateIfcColumn(model, rcCol, colHeight);
                column.Tag = ElementType.Column;
                using (var txn = model.BeginTransaction("Add column"))
                {
                    storey.AddElement(column);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, rcCol.CadColumn.ColPath, DefaultValues.FormWorkThickness, colHeight, out formworkVoid);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formWork);

                    txn.Commit();
                }
            }
        }

        private void CreateFoundationRetainingWalls(IfcStore model, IfcBuilding building, IfcBuildingStorey storey, Foundation foundation)
        {
            foreach (ReinforcedCadWall rcWall in foundation.LstRCCadWall)
            {

                using (var txn = model.BeginTransaction("Add RetainingWall"))
                {
                    double wallHeight = foundation.Height - DefaultValues.SlabThinkess;
                    IfcWallStandardCase wall = CreateIfcWall(model, rcWall.CadWall, wallHeight, building);
                    wall.Tag = ElementType.RetainingWall;
                    if (wall != null) AddPropertiesToWall(model, wall);

                    storey.AddElement(wall);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, rcWall.CadWall.LinPathWall, DefaultValues.FormWorkThickness, wallHeight, out formworkVoid);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formWork);

                    txn.Commit();
                }
            }
        }

        private void CreateFoundationRamps(IfcStore model, IfcBuildingStorey storey, Foundation foundation)
        {
            foreach (SlopedSlab cadRamp in foundation.LstRamp)
            {

                using (var txn = model.BeginTransaction("Add Ramp"))
                {
                    IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);
                    ramp.Tag = ElementType.Slab;
                    storey.AddElement(ramp);

                    IfcOpeningElement formworkVoid;
                    IfcBuildingElementPart formWork = CreateFormWork(model, cadRamp.LinPathSlopedSlab, DefaultValues.FormWorkThickness, cadRamp.Thickness, out formworkVoid, true);
                    storey.AddElement(formworkVoid);
                    storey.AddElement(formWork);

                    txn.Commit();
                }
            }
        }

        private void CreateRCFootings(IfcStore model, IfcBuildingStorey storey, Foundation foundation)
        {
            foreach (ReinforcedCadFooting cadFooting in foundation.LstRCCadFooting)
            {

                IfcFooting footing = CreateIfcFooting(model, cadFooting.RcFooting);
                using (var txn = model.BeginTransaction("Add Footing"))
                {
                    footing.Tag = ElementType.RCFooting;
                    storey.AddElement(footing);

                    //IfcOpeningElement opening;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, cadFooting.RcFooting.ProfilePath, DefaultValues.FormWorkThickness, 
                    //    cadFooting.RcFooting.Thickness, out opening, false, true);
                    //storey.AddElement(opening);
                    //storey.AddElement(formWork);

                    txn.Commit();
                }
            }
        }

        private void CreateBuildingSemelles(IfcStore model, IfcBuildingStorey storey, Foundation foundation)
        {
            foreach (ReinforcedCadSemelle cadSemelle in foundation.LstRCSemelle)
            {

                IfcBeam semelle = CreateIfcBeam(model, cadSemelle.Semelle);
                using (var txn = model.BeginTransaction("Add Semelle"))
                {
                    semelle.Tag = ElementType.Semelle;
                    storey.AddElement(semelle);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, cadSemelle.Semelle.HzLinPath,
                    //    DefaultValues.FormWorkThickness, cadSemelle.Semelle.Thickness, out formworkVoid, false, false, true);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formWork);

                    txn.Commit();
                }
            }
        }

        private void CreateBuildingPCFootings(IfcStore model, IfcBuildingStorey storey, Foundation foundation)
        {
            foreach (PCFooting cadFooting in foundation.LstPCFooting)
            {

                IfcFooting footing = CreateIfcFooting(model, cadFooting);
                using (var txn = model.BeginTransaction("Add Footing"))
                {
                    footing.Tag = ElementType.PCFooting;
                    storey.AddElement(footing);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, cadFooting.ProfilePath, DefaultValues.FormWorkThickness, cadFooting.Thickness, out formworkVoid, false, true);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formWork);


                    txn.Commit();
                }
            }
        }

        private void CreateStoreyElectricalConduit(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {
            foreach (ElectricalConduit cadConduit in floor.LstElectConduit)
            {
                using (var txn = model.BeginTransaction("Add conduit"))
                {
                    IfcCableCarrierSegment conduit = CreateIfcConduit(model, cadConduit);
                    conduit.Tag = ElementType.ElectricalConduit;

                    storey.AddElement(conduit);
                    txn.Commit();
                }
            }
        }

        private void CreateRamps(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {
            foreach (SlopedSlab cadRamp in floor.LstRamp)
            {
                IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);

                using (var txn = model.BeginTransaction("Add Ramp"))
                {
                    ramp.Tag = ElementType.Slab;
                    storey.AddElement(ramp);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, cadRamp.LinPathSlopedSlab, DefaultValues.FormWorkThickness, cadRamp.Thickness, out formworkVoid, true);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formWork);

                    txn.Commit();
                }
            }
        }

        private void CreateStoreyShearWalls(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {
            foreach (ReinforcedCadShearWall cadShearWall in floor.LstRcShearWall)
            {
                double wallHeight = floor.Height/* - DefaultValues.SlabThinkess*/;
                IfcColumn shearWall = CreateIfcShearWall(model, cadShearWall.ShearWall, floor.Height);

                using (var txn = model.BeginTransaction("Add Shear Wall"))
                {
                    shearWall.Tag = ElementType.ShearWall;
                    storey.AddElement(shearWall);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, cadShearWall.ShearWall.ProfilePath, DefaultValues.FormWorkThickness, wallHeight, out formworkVoid);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formWork);

                    //create wall reinforcement
                    //CreateWallRft(model, storey, cadShearWall, wallHeight);

                    txn.Commit();
                }
            }
        }

        private void CreateStoreyRoofSlabs(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {
            foreach (ReinforcedCadSlab cadRCSlab in floor.LstRcSlab)
            {
                IfcSlab slab = CreateIfcSlab(model, cadRCSlab.Slab);

                using (var trans = model.BeginTransaction("Add Slab"))
                {
                    slab.Tag = ElementType.Slab;
                    storey.AddElement(slab);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formwork = CreateFormWork(model, cadRCSlab.Slab.LinPathSlab, DefaultValues.FormWorkThickness, cadRCSlab.Slab.Thickness, out formworkVoid, true);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formwork);


                    //List<IfcOpeningElement> lstOpening = ModelSlabOpenings(cadRCSlab, formwork, model, storey, slab);
                    List<IfcOpeningElement> lstOpening = ModelSlabOpenings(cadRCSlab, null, model, storey, slab);

                    //Create Slab Reinforcement
                    //CreateSlabRft(model, storey, cadRCSlab, lstOpening);

                    //create slab openingd
                    //List<IfcOpeningElement> lstOpening = CreateSlabOpenings(model, cadRCSlab);

                    trans.Commit();
                }
            }
        }

        private List<IfcOpeningElement> CreateSlabOpenings(IfcStore model, IfcBuildingStorey storey, ReinforcedCadSlab cadRCSlab)
        {
            List<IfcOpeningElement> lstIfcOpening = new List<IfcOpeningElement>();
            foreach (Opening cadOpening in cadRCSlab.Slab.Openings)
            {
                IfcOpeningElement ifcOpening = CreateIfcOpening(model, cadOpening, DefaultValues.SlabThinkess);

                lstIfcOpening.Add(ifcOpening);
            }
            return lstIfcOpening;
        }

        private void CreateStoreyColumns(IfcStore model, IfcBuildingStorey storey, Floor floor)
        {
            foreach (ReinforcedCadColumn rcCol in floor.LstRcColumn)
            {
                IfcColumn column = CreateIfcColumn(model, rcCol, floor.Height);

                using (var txn = model.BeginTransaction("Add RcColumn"))
                {
                    column.Tag = ElementType.Column;
                    storey.AddElement(column);

                    //IfcOpeningElement formworkVoid;
                    //IfcBuildingElementPart formWork = CreateFormWork(model, rcCol.CadColumn.ColPath, DefaultValues.FormWorkThickness, floor.Height, out formworkVoid);
                    //storey.AddElement(formworkVoid);
                    //storey.AddElement(formWork);

                    //create column reinforcement
                    //CreateColumnRft(rcCol, storey, model, floor.Height);

                    txn.Commit();
                }
            }
        }

        private void CreateStoreyRetainingWalls(IfcStore model, IfcBuilding building, IfcBuildingStorey storey, Floor floor)
        {
            foreach (ReinforcedCadWall rcWall in floor.LstRcCadRetainingWall)
            {
                double wallHeight = floor.Height - DefaultValues.SlabThinkess;
                IfcWallStandardCase wall = CreateIfcWall(model, rcWall.CadWall, wallHeight, building);

                using (var txn = model.BeginTransaction("Add RetainingWall"))
                {
                    wall.Tag = ElementType.RetainingWall;

                    //IfcOpeningElement formWorkVoid;
                    //IfcBuildingElementPart formwork = CreateFormWork(model, rcWall.CadWall.LinPathWall, DefaultValues.FormWorkThickness, wallHeight, out formWorkVoid);

                    storey.AddElement(wall);
                    //storey.AddElement(formwork);
                    //storey.AddElement(formWorkVoid);

                    //create wall reinforcement
                    //CreateWallRft(rcWall, storey, model, wallHeight);

                    txn.Commit();
                }

            }
        }

        private void CreateWallRft(IfcStore model, IfcBuildingStorey storey, ReinforcedCadShearWall cadShearWall, double wallHeight)
        {
            foreach (var rebar in cadShearWall.VlRebar)
            {
                IfcReinforcingBar bar = CreateIfcRebar(model, rebar, wallHeight);
                storey.AddElement(bar);
            }
            int nstirrups = Convert.ToInt32((wallHeight + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (DefaultValues.StirrupsSpacing));
            for (int j = 0; j < nstirrups - 1; j++)
            {
                IfcReinforcingBar stirrup = CreateIfcStirrup(model, cadShearWall.Stirrup, DefaultValues.StirrupsSpacing);
                storey.AddElement(stirrup);
            }
        }

        private void CreateSlabRft(IfcStore model, IfcBuildingStorey storey, ReinforcedCadSlab cadRCSlab, List<IfcOpeningElement> lstOpening)
        {
            for (int k = 0; k < cadRCSlab.OpeningsRFT.Count; k++)
            {
                IfcReinforcingBar bar = CreateIfcRebar(model, cadRCSlab.OpeningsRFT[k], 0);
                for (int j = 0; j < lstOpening.Count; j++)
                {
                    bar.AttchOpening(model, lstOpening[j]);
                }
                storey.AddElement(bar);
            }

            for (int j = 0; j < cadRCSlab.RFT.Count; j++)
            {
                IfcReinforcingBar bar = CreateIfcRebar(model, cadRCSlab.RFT[j], 0);
                storey.AddElement(bar);
            }
        }

        private List<IfcOpeningElement> ModelSlabOpenings(ReinforcedCadSlab cadRCSlab, IfcBuildingElementPart formWork, IfcStore model, IfcBuildingStorey storey, IfcSlab slab)
        {
            List<IfcOpeningElement> lstOpening = new List<IfcOpeningElement>();
            IfcOpeningElement opening = null;
            for (int n = 0; n < cadRCSlab.Slab.Openings.Count; n++)
            {
                var cadOpening = cadRCSlab.Slab.Openings[n];
                opening = CreateIfcOpening(model, cadOpening, DefaultValues.SlabThinkess);

                lstOpening.Add(opening);

                storey.AddElement(opening);

                //attach opening
                slab.AttchOpening(model, opening);
                //formWork.AttchOpening(model, opening);
            }

            return lstOpening;
        }


        private IfcWallStandardCase CreateIfcWall(IfcStore model, Wall cadWall, double height, IfcBuilding building)
        {
            for (int i = 0; i < cadWall.LinPathWall.Vertices.Length; i++)
            {
                cadWall.LinPathWall.Vertices[i].Z -= DefaultValues.SlabThinkess;
            }

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {
                IfcWallStandardCase wallToCreate = model.Instances.New<IfcWallStandardCase>();
                wallToCreate.Name = " Wall - Wall:UC305x305x97:" + random.Next(1000, 10000);

                //represent wall as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadWall.LinPathWall.Vertices.ToList());


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);


                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);


                //Placement To building
                var localPlacement = model.Instances.New<IfcLocalPlacement>();

                localPlacement.PlacementRelTo = building.ObjectPlacement;
                wallToCreate.ObjectPlacement = localPlacement;

                //Create a Definition shape to hold the geometry of the wall 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                wallToCreate.Representation = prDefShape;


                trans.Commit();
                return wallToCreate;
            }

        }

        private IfcBeam CreateIfcBeam(IfcStore model, Semelle semelle)
        {


            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {
                IfcBeam beamToCreate = model.Instances.New<IfcBeam>();
                beamToCreate.Name = " Wall - Wall:UC305x305x97:" + random.Next(1000, 10000);

                //represent wall as a rectangular profile
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, semelle.HzLinPath.Vertices.ToList());


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);


                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, semelle.Thickness, profile, extrusionDir);



                //Create a Definition shape to hold the geometry of the wall 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                beamToCreate.Representation = prDefShape;


                trans.Commit();
                return beamToCreate;
            }

        }


        private IfcFooting CreateIfcFooting(IfcStore model, FootingBase cadFooting)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Footing"))
            {
                IfcFooting footingToCreate = model.Instances.New<IfcFooting>();
                footingToCreate.Name = " Foundation - Footing:UC305x305x97: " + random.Next(1000, 10000);




                //represent footing as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadFooting.ProfilePath.Vertices.ToList());

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, cadFooting.Thickness, rectProf, extrusionDir);





                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                footingToCreate.Representation = prDefShape;



                trans.Commit();
                return footingToCreate;
            }

        }

        private IfcColumn CreateIfcShearWall(IfcStore model, ShearWall cadShearWall, double height)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Shear Wall"))
            {
                IfcColumn shearWallToCreate = model.Instances.New<IfcColumn>();
                shearWallToCreate.Name = " ShearWall - ShearWall:UC305x305x97: " + random.Next(1000, 10000);

                //represent footing as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadShearWall.ProfilePath.Vertices.ToList());



                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);

                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                shearWallToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the wall

                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(0, 0, 0);

                //var uvFootingLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadFooting.CenterPt, cadFooting.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);


                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                shearWallToCreate.ObjectPlacement = lp;


                trans.Commit();
                return shearWallToCreate;
            }

        }


        private IfcSlab CreateIfcSlopedSlab(IfcStore model, SlopedSlab cadSlab)
        {
            //for (int i = 0; i < cadSlab.LstFacePt.Count; i++)
            //{
            //    cadSlab.LstFacePt[i] *= 1000;
            //}
            //cadSlab.Thickness *= 1000;

            //begin a transaction
            using (ITransaction trans = model.BeginTransaction("Create Slab"))
            {
                IfcSlab slabToCreate = model.Instances.New<IfcSlab>();
                slabToCreate.Name = " Slab - Slab:UC305x305x97:" + random.Next(1000, 10000);

                //represent Element as a rectangular profile
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadSlab.LinPathSlopedSlab.Vertices.ToList());

                //Profile insertion point 


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, cadSlab.Thickness, profile, extrusionDir);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);



                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                slabToCreate.Representation = prDefRep;

                trans.Commit();
                return slabToCreate;
            }

        }

        private IfcSlab CreateIfcSlab(IfcStore model, Slab cadSlab)
        {

            using (ITransaction trans = model.BeginTransaction("Create Slab"))
            {
                IfcSlab slabToCreate = model.Instances.New<IfcSlab>();
                slabToCreate.Name = " Slab - Slab:UC305x305x97:" + random.Next(1000, 10000);

                //represent Element as a rectangular profile
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadSlab.LinPathSlab.Vertices.ToList());

                //Profile insertion point 


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, cadSlab.Thickness, profile, extrusionDir);




                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);



                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                slabToCreate.Representation = prDefRep;

                trans.Commit();
                return slabToCreate;
            }

        }

        private IfcOpeningElement CreateIfcOpening(IfcStore model, Opening cadOpening, double thickness)
        {

            IfcOpeningElement openingToCreate = model.Instances.New<IfcOpeningElement>();
            openingToCreate.Name = " Openings - Openings:UC305x305x97:" + random.Next(1000, 10000);

            IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadOpening.LinPathOpening.Vertices.ToList());


            IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
            extrusionDir.SetXYZ(0, 0, -1);

            IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness + DefaultValues.FormWorkThickness, rectProf, extrusionDir);

            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
            prDefRep.Representations.Add(shape);
            openingToCreate.Representation = prDefRep;


            return openingToCreate;


        }
        public static IfcOpeningElement CreateIfcOpening(IfcStore model, LinearPath cadOpeninlinPath, double thickness, bool isSlab = false, bool isFooting = false, bool isBeam = false)
        {


            IfcOpeningElement openingToCreate = model.Instances.New<IfcOpeningElement>();
            openingToCreate.Name = " Openings - Openings:UC305x305x97:";

            //represent wall as a rectangular profile
            IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadOpeninlinPath.Vertices.ToList());




            //model as a swept area solid
            IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
            extrusionDir.SetXYZ(0, 0, (isBeam || isFooting) ? 1 : -1);

            IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness, profile, extrusionDir);


            //parameters to insert the geometry in the model 


            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the opening
            IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
            prDefRep.Representations.Add(shape);
            openingToCreate.Representation = prDefRep;

            return openingToCreate;

        }

        private IfcStair CreateIfcStair(IfcStore model, Stair stair, out IfcStairFlight flight)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Stair"))
            {
                IfcStair stairToCreate = model.Instances.New<IfcStair>();
                stairToCreate.Name = " Stair :UC305x305x97:" + random.Next(10000);


                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                //Create a Definition shape to hold the geometry of the Stair 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");

                for (int i = 0; i < stair.LstStep.Count; i++)
                {
                    IfcArbitraryClosedProfileDef stepProfile = IFCHelper.ArbitraryClosedProfileCreate(model, (stair.LstStep[i].Vertices/*.Select(v => v * 1000)*/).ToList());

                    IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, stair.Thickness, stepProfile, extrusionDir);

                    body.BodyPlacementSet(model, 0, 0, stair.IsUp ? stair.Thickness * i : stair.Thickness * -i);

                    shape.Items.Add(body);
                }


                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);

                flight = model.Instances.New<IfcStairFlight>();
                flight.Name = " Stair :Flight:" + random.Next(10000);
                flight.Representation = prDefShape;

                IfcRelAggregates relAggregate = model.Instances.New<IfcRelAggregates>();
                relAggregate.RelatingObject = stairToCreate;
                relAggregate.RelatedObjects.Add(flight);

                ////Create Local axes system and assign it to the column
                //IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                //location3D.SetXYZ(0, 0, 0);

                ////var uvColLongDir = MathHelper.UnitVectorPtFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                //IfcDirection localXDir = model.Instances.New<IfcDirection>();
                //localXDir.SetXYZ(1, 0, 0);

                //IfcDirection localZDir = model.Instances.New<IfcDirection>();
                //localZDir.SetXYZ(0, 0, 1);

                //IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                ////now place the slab into the model
                //IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                //flight.ObjectPlacement = lp;

                trans.Commit();
                return stairToCreate;
            }

        }

        private IfcSlab CreateIfcLanding(IfcStore model, LinearPath landingPline, double landingThickness)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {

                IfcSlab landing = model.Instances.New<IfcSlab>();
                landing.Name = " Landing :UC305x305x97:" + random.Next(10000);

                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                //Create a Definition shape to hold the geometry of the Stair 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");


                IfcArbitraryClosedProfileDef stepProfile = IFCHelper.ArbitraryClosedProfileCreate(model, (landingPline.Vertices/*.Select(v => v * 1000)*/).ToList());

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, landingThickness, stepProfile, extrusionDir);


                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);

                landing.Representation = prDefShape;

                trans.Commit();
                return landing;
            }

        }

        private IfcColumn CreateIfcColumn(IfcStore model, ReinforcedCadColumn cadCol, double height)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create column"))
            {
                IfcColumn colToCreate = model.Instances.New<IfcColumn>();
                colToCreate.Name = "UC-Universal Columns-Column:UC305x305x97:" + random.Next(10000);

                //represent column as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadCol.CadColumn.ColPath.Vertices.ToList());




                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                colToCreate.Representation = prDefShape;



                trans.Commit();
                return colToCreate;
            }

        }

        public static IfcReinforcingBar CreateIfcRebar(IfcStore model, Rebar rebar, double height)
        {
            IfcReinforcingBar rebarToCreate = model.Instances.New<IfcReinforcingBar>();
            rebarToCreate.Name = "Rebar:UC305x305x97:" + 10000;

            //represent column as a rectangular profile
            IfcCircleProfileDef cirProf = IFCHelper.CircleProfileCreate(model, rebar.Diameter / 2);


            if (rebar.Type.ToLower() == "Vertical".ToLower())
            {
                double dowelHeight = (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000);
                height += dowelHeight;

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, cirProf, extrusionDir);

                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                rebarToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(rebar.LocationPt.X, rebar.LocationPt.Y, rebar.LocationPt.Z + dowelHeight);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                rebarToCreate.ObjectPlacement = lp;
            }

            else if (rebar.Type.ToLower() == "Horizontal".ToLower())
            {

                List<Point3D> lstVertices = new List<Point3D>();
                for (int i = 0; i < rebar.LinearPath.Vertices.Count(); i++)
                {
                    lstVertices.Add(rebar.LinearPath.Vertices[i]);
                }
                IfcSurfaceCurveSweptAreaSolid body;

                if (lstVertices.Count > 2)
                {
                    Vector3D uvPerpRebar = MathHelper.UVPerpendicularToLine2DFromPt(new Line(lstVertices[1], lstVertices[2]), lstVertices[1]);

                    IfcDirection planeZAxis = model.Instances.New<IfcDirection>();
                    planeZAxis.SetXYZ(uvPerpRebar.X, uvPerpRebar.Y, uvPerpRebar.Z);

                    Vector3D refDirUV = MathHelper.UnitVector3DFromPt1ToPt2(lstVertices[1], lstVertices[2]);

                    IfcDirection refDir = model.Instances.New<IfcDirection>();
                    refDir.SetXYZ(refDirUV.X, refDirUV.Y, refDirUV.Z);
                    body = IFCHelper.ProfileSurfaceSweptSolidCreate(model, cirProf, lstVertices, planeZAxis, refDir);
                }
                else
                    body = IFCHelper.ProfileSurfaceSweptSolidCreate(model, cirProf, lstVertices);


                //parameters to insert the geometry in the model

                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                rebarToCreate.Representation = prDefShape;
            }
            return rebarToCreate;
        }

        public static IfcReinforcingBar CreateIfcStirrup(IfcStore model, Stirrup stirrup, double zPosition)
        {

            for (int i = 0; i < stirrup.StirrupPath.Vertices.Length; i++)
                stirrup.StirrupPath.Vertices[i].Z -= zPosition;

            IfcReinforcingBar stirrupToCreate = model.Instances.New<IfcReinforcingBar>();
            stirrupToCreate.Name = "Rebar:UC305x305x97:" +1000;

            IfcCircleProfileDef cirProf = IFCHelper.CircleProfileCreate(model, stirrup.Diameter / 2);


            IfcSurfaceCurveSweptAreaSolid body = IFCHelper.ProfileSurfaceSweptSolidCreate(model, cirProf, stirrup.StirrupPath.Vertices.ToList());
            //parameters to insert the geometry in the model

            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            stirrupToCreate.Representation = prDefShape;


            return stirrupToCreate;

        }
        private IfcCableCarrierSegment CreateIfcConduit(IfcStore model, ElectricalConduit conduit)
        {
            IfcCableCarrierSegment conduitToCreate = model.Instances.New<IfcCableCarrierSegment>();
            conduitToCreate.Name = "Conduit:UC305x305x97:" + random.Next(100000);

            IfcSweptDiskSolid body = IFCHelper.ProfileSweptDiskSolidByCompositeCurve(model, conduit.CurvePath, conduit.Diameter);

            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "AdvancedSweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            conduitToCreate.Representation = prDefShape;

            return conduitToCreate;

        }



        public static IfcBuildingElementPart CreateFormWork(IfcStore model, LinearPath linPathElem, double formWorkThickness, double extrusionHeight
            , out IfcOpeningElement open, bool isSlab = false, bool isFooting = false, bool isBeam = false)
        {
            LinearPath outerLinPath = (LinearPath)linPathElem.Offset(formWorkThickness);

            if (isBeam)
            {//new
                for (int i = 0; i < outerLinPath.Vertices.Length; i++)
                {
                    outerLinPath.Vertices[i].Z -= formWorkThickness;
                }
            }

            IfcBuildingElementPart formWork = model.Instances.New<IfcBuildingElementPart>();
            formWork.Name = " Foundation - Footing:UC305x305x97: ";

            //represent footing as a rectangular profile
            IfcArbitraryClosedProfileDef outerRectProfile = IFCHelper.ArbitraryClosedProfileCreate(model, outerLinPath.Vertices.ToList());



            //model as a swept area solid
            IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
            extrusionDir.SetXYZ(0, 0, (isBeam || isFooting) ? 1 : -1);

            IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, (isSlab || isBeam) ? extrusionHeight + formWorkThickness : extrusionHeight, outerRectProfile, extrusionDir);



            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            formWork.Representation = prDefShape;



            open = CreateIfcOpening(model, linPathElem, extrusionHeight, isSlab, isFooting, isBeam);
            IfcRelVoidsElement relVoids = model.Instances.New<IfcRelVoidsElement>();

            relVoids.RelatedOpeningElement = open;
            relVoids.RelatingBuildingElement = formWork;

            return formWork;
        }

        #region For Wall
        private void AddPropertiesToWall(IfcStore model, IfcWallStandardCase wall)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                CreateSimpleProperty(model, wall);
                txn.Commit();
            }
        }

        private void CreateSimpleProperty(IfcStore model, IfcWallStandardCase wall)
        {
            var Material = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "Material";
                psv.Description = "";
                psv.NominalValue = new IfcLabel("Concrete");
            });
            var Area = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "Area";
                psv.Description = "";
                psv.NominalValue = new IfcLabel(random.Next(500) + " m2");
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.AREAUNIT;
                    siu.Name = IfcSIUnitName.METRE;
                });
            });
            var Volume = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "Volume";
                psv.Description = "";
                psv.NominalValue = new IfcLabel(random.Next(5000) + " m3");
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.VOLUMEUNIT;
                    siu.Name = IfcSIUnitName.METRE;
                });
            });

            var Building = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "Building";
                psv.Description = "";
                psv.NominalValue = new IfcLabel("Building A");

            });

            //lets create the IfcElementQuantity
            var ifcPropertySet = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.Name = "Element Properties";
                ps.Description = "Property Set";
                ps.HasProperties.Add(Material);
                ps.HasProperties.Add(Area);
                ps.HasProperties.Add(Volume);
                ps.HasProperties.Add(Building);
            });

            //need to create the relationship
            model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.Name = "Property Association";
                rdbp.Description = "IfcPropertySet associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcPropertySet;
            });
        }
        #endregion

        #region methods
        internal void CreateWallRft(ReinforcedCadWall rcWall, IfcBuildingStorey storey, IfcStore model, double wallHeight)
        {
            for (int i = 0; i < rcWall.LstRebar.Count; i++)
            {
                IfcReinforcingBar bar = CreateIfcRebar(model, rcWall.LstRebar[i], wallHeight);
                storey.AddElement(bar);
            }
            int nStirrups = Convert.ToInt32(wallHeight / (DefaultValues.StirrupsSpacing));
            for (int j = 0; j < nStirrups; j++)
            {
                IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcWall.Stirrup, DefaultValues.StirrupsSpacing);
                storey.AddElement(stirrup);
            }
        }

        public static void CreateColumnRft(ReinforcedCadColumn rcCol, IfcBuildingStorey storey, IfcStore model, double colHeight)
        {
            foreach (var rebar in rcCol.LstRebar)
            {
                IfcReinforcingBar bar = CreateIfcRebar(model, rebar, colHeight);
                storey.AddElement(bar);
            }
            int nstirrups = Convert.ToInt32((colHeight + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / DefaultValues.StirrupsSpacing);
            for (int j = 0; j < nstirrups - 1; j++)
            {
                IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.Stirrup, DefaultValues.StirrupsSpacing);
                storey.AddElement(stirrup);
            }
        }

        //internal void CreateReinforcement(ReinforcedElements elem, IfcBuildingStorey storey, IfcStore model, double wallHeight)

        #endregion
    }
}
