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
using AutocadAPI.BuildingElements;
using Xbim.Ifc4.StructuralElementsDomain;

namespace AutocadAPI
{
    class XbimCreateBuilding
    {

        IfcSlab slab = null;
        public XbimCreateBuilding(List<Wall> walls, List<RectColumn> columns, List<RectFooting> lstFooting, List<Slab> lstSlab, List<Opening> lstOpning)
        {
            using (var model = CreateandInitModel("Demo1"))
            {
                if (model != null)
                {
                    IfcBuilding building = CreateBuilding(model, "Default Building");

                    foreach (var item in walls)
                    {
                        IfcWallStandardCase wall = CreateIfcWall(model, item, 3000);

                        if (wall != null) AddPropertiesToWall(model, wall);
                        using (var txn = model.BeginTransaction("Add Wall"))
                        {
                            building.AddElement(wall);
                            txn.Commit();
                        }
                    }
                    foreach (var item in columns)
                    {

                        IfcColumn column = CreateIfcColumn(model, item, 3000);

                        using (var txn = model.BeginTransaction("Add column"))
                        {
                            building.AddElement(column);
                            txn.Commit();
                        }
                    }

                    foreach (var item in lstFooting)
                    {

                        IfcFooting footing = CreateIfcFooting(model, item, 500);

                        using (var txn = model.BeginTransaction("Add Footing"))
                        {
                            building.AddElement(footing);
                            txn.Commit();
                        }
                    }

                    foreach (var item in lstSlab)
                    {
                        slab = CreateSlab(model, item, 0.30);
                        using (var trans = model.BeginTransaction("Add Slab"))
                        {
                            building.AddElement(slab);
                            trans.Commit();
                        }
                    }

                    IfcOpeningElement opening = null;
                    foreach (var item in lstOpning)
                    {
                        opening = CreateOpening(model, item, 0.30);
                        using (var trans = model.BeginTransaction("Add Opening"))
                        {
                            building.AddElement(opening);
                            trans.Commit();
                        }
                    }
                    //attach opening
                    slab.AttchOpening(model, opening);

                    try
                    {
                        Console.WriteLine("Standard Wall successfully created....");
                        //write the Ifc File
                        model.SaveAs(@"F:\Work\AutocadAPI-masterWallIfc4.ifc", IfcStorageType.Ifc);
                        Console.WriteLine("WallIfc4.ifc has been successfully written");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to save!");
                        Console.WriteLine(e.Message);
                    }

                }

            }
        }

        private static IfcBuilding CreateBuilding(IfcStore model, string name)
        {
            using (var txn = model.BeginTransaction("Create Building"))
            {
                var building = model.Instances.New<IfcBuilding>();
                building.Name = name;

                building.CompositionType = IfcElementCompositionEnum.ELEMENT;
                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                building.ObjectPlacement = localPlacement;
                var placement = model.Instances.New<IfcAxis2Placement3D>();
                localPlacement.RelativePlacement = placement;
                placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                //get the project there should only be one and it should exist
                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project?.AddBuilding(building);
                txn.Commit();
                return building;
            }
        }

        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        private static IfcStore CreateandInitModel(string projectName)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xBimTeam",
                ApplicationFullName = "Hello Wall Application",
                ApplicationIdentifier = "HelloWall.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Team",
                EditorsGivenName = "xBIM",
                EditorsOrganisationName = "xBimTeam"
            };
            //now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            //database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required

            var model = IfcStore.Create(credentials, IfcSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {

                //create a project
                var project = model.Instances.New<IfcProject>();
                //set the units to SI (mm and metres)
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = projectName;
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;

        }
        /// <summary>
        /// This creates a wall and it's geometry, many geometric representations are possible and extruded rectangular footprint is chosen as this is commonly used for standard case walls
        /// </summary>
        /// <param name="model"></param>
        /// <param name="length">Length of the rectangular footprint</param>
        /// <param name="width">Width of the rectangular footprint (width of the wall)</param>
        /// <param name="height">Height to extrude the wall, extrusion is vertical</param>
        /// <returns></returns>
        static private IfcWallStandardCase CreateIfcWall(IfcStore model, Wall cadWall, double height)
        {
            //dimensions of the new IFC Wall we want to create
            double length = Math.Abs(cadWall.EndPt.X - cadWall.StPt.X) > 0 ? Math.Abs(cadWall.EndPt.X - cadWall.StPt.X) * 1000 : Math.Abs(cadWall.EndPt.Y - cadWall.StPt.Y) * 1000;
            double width = cadWall.Thickness * 1000;

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {
                IfcWallStandardCase wallToCreate = model.Instances.New<IfcWallStandardCase>();
                wallToCreate.Name = "A Standard rectangular wall";

                //represent wall as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);

                //Profile Insertion Point
                rectProf.ProfileInsertionPointSet(model, 0, 0);

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);


                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);

                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, cadWall.StPt.Z * 1000 - 3000);

                //Create a Definition shape to hold the geometry of the wall 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                wallToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the wall
                var midPt = MathHelper.MidPoint(cadWall.StPt, cadWall.EndPt);

                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(midPt.X * 1000, midPt.Y * 1000, midPt.Z * 1000);

                var uvWallLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadWall.StPt, cadWall.EndPt);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvWallLongDir.X, uvWallLongDir.Y, uvWallLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);

                wallToCreate.ObjectPlacement = lp;


                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);

                trans.Commit();
                return wallToCreate;
            }

        }

        static private IfcColumn CreateIfcColumn(IfcStore model, RectColumn cadCol, double height)
        {
            //
            double length = cadCol.Length * 1000;
            double width = cadCol.Width * 1000;
            //begin a transaction
            using (var trans = model.BeginTransaction("Create column"))
            {
                IfcColumn colToCreate = model.Instances.New<IfcColumn>();
                colToCreate.Name = "A Standard rectangular column";

                //represent column as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);


                //Profile insertion point
                rectProf.ProfileInsertionPointSet(model, 0, 0);

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, -height);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                colToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadCol.CenterPt.X * 1000, cadCol.CenterPt.Y * 1000, cadCol.CenterPt.Z * 1000);

                var uvColLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadCol.CenterPt, cadCol.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvColLongDir.X, uvColLongDir.Y, uvColLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                colToCreate.ObjectPlacement = lp;

                trans.Commit();
                return colToCreate;
            }

        }


        static private IfcFooting CreateIfcFooting(IfcStore model, RectFooting cadFooting, double thickness)
        {
            //
            double length = cadFooting.Length * 1000;
            double width = cadFooting.Width * 1000;
            //begin a transaction
            using (var trans = model.BeginTransaction("Create Footing"))
            {
                IfcFooting footingToCreate = model.Instances.New<IfcFooting>();
                footingToCreate.Name = "A Standard rectangular Footing";

                //represent footing as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);

                //Profile Insertion Point
                rectProf.ProfileInsertionPointSet(model, 0, 0);


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness * 1000, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, -thickness - 3000);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                footingToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the wall

                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadFooting.CenterPt.X * 1000, cadFooting.CenterPt.Y * 1000, cadFooting.CenterPt.Z * 1000);

                var uvFootingLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadFooting.CenterPt, cadFooting.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvFootingLongDir.X, uvFootingLongDir.Y, uvFootingLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);


                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                footingToCreate.ObjectPlacement = lp;


                trans.Commit();
                return footingToCreate;
            }

        }

        static private IfcSlab CreateSlab(IfcStore model, Slab cadSlab, double thickness)
        {
            //
            double length = cadSlab.Length * 1000;
            double width = cadSlab.Width * 1000;
            //begin a transaction
            using (ITransaction trans = model.BeginTransaction("Create Slab"))
            {
                IfcSlab slabToCreate = model.Instances.New<IfcSlab>();
                slabToCreate.Name = "A Standard rectangular slab";

                //represent Element as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);

                //Profile insertion point
                rectProf.ProfileInsertionPointSet(model, 0, 0, cadSlab.ZLevel * 1000 - 3000);


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness * 1000, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, -thickness * 1000 - 3000);


                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);



                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                slabToCreate.Representation = prDefRep;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadSlab.CenterPt.X * 1000, cadSlab.CenterPt.Y * 1000, cadSlab.CenterPt.Z * 1000);

                var uvColLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvColLongDir.X, uvColLongDir.Y, uvColLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the slab into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                slabToCreate.ObjectPlacement = lp;


                trans.Commit();
                return slabToCreate;
            }

        }

        private IfcOpeningElement CreateOpening(IfcStore model, Opening cadOpening, double thickness)
        {
            //
            double length = cadOpening.Length * 1000;
            double width = cadOpening.Width * 1000;
            //begin a transaction
            using (var trans = model.BeginTransaction("Create Opening"))
            {
                IfcOpeningElement openingToCreate = model.Instances.New<IfcOpeningElement>();
                openingToCreate.Name = "A rectangular opening";

                //represent wall as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);

                //Profile insertion point
                rectProf.ProfileInsertionPointSet(model, 0, 0);

                var insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXYZ(0, 0, cadOpening.ZLevel * 1000); //insert at arbitrary position
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness * 1000, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, -thickness * 1000 - 3000);


                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the opening
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                openingToCreate.Representation = prDefRep;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadOpening.CenterPt.X * 1000, cadOpening.CenterPt.Y * 1000, cadOpening.CenterPt.Z * 1000);

                var uvColLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadOpening.CenterPt, cadOpening.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvColLongDir.X, uvColLongDir.Y, uvColLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                openingToCreate.ObjectPlacement = lp;
                

                //commit transaction
                trans.Commit();

                return openingToCreate;
            }

        }


        /// <summary>
        /// Add some properties to the wall,
        /// </summary>
        /// <param name="model">XbimModel</param>
        /// <param name="wall"></param>
        static private void AddPropertiesToWall(IfcStore model, IfcWallStandardCase wall)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                CreateSimpleProperty(model, wall);
                txn.Commit();
            }
        }

        private static void CreateSimpleProperty(IfcStore model, IfcWallStandardCase wall)
        {
            var ifcPropertySingleValue = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "IfcPropertySingleValue:Time";
                psv.Description = "";
                psv.NominalValue = new IfcTimeMeasure(150.0);
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.TIMEUNIT;
                    siu.Name = IfcSIUnitName.SECOND;
                });
            });
            var ifcPropertyEnumeratedValue = model.Instances.New<IfcPropertyEnumeratedValue>(pev =>
            {
                pev.Name = "IfcPropertyEnumeratedValue:Music";
                pev.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                {
                    pe.Name = "Notes";
                    pe.EnumerationValues.Add(new IfcLabel("Do"));
                    pe.EnumerationValues.Add(new IfcLabel("Re"));
                    pe.EnumerationValues.Add(new IfcLabel("Mi"));
                    pe.EnumerationValues.Add(new IfcLabel("Fa"));
                    pe.EnumerationValues.Add(new IfcLabel("So"));
                    pe.EnumerationValues.Add(new IfcLabel("La"));
                    pe.EnumerationValues.Add(new IfcLabel("Ti"));
                });
                pev.EnumerationValues.Add(new IfcLabel("Do"));
                pev.EnumerationValues.Add(new IfcLabel("Re"));
                pev.EnumerationValues.Add(new IfcLabel("Mi"));

            });
            var ifcPropertyBoundedValue = model.Instances.New<IfcPropertyBoundedValue>(pbv =>
            {
                pbv.Name = "IfcPropertyBoundedValue:Mass";
                pbv.Description = "";
                pbv.UpperBoundValue = new IfcMassMeasure(5000.0);
                pbv.LowerBoundValue = new IfcMassMeasure(1000.0);
                pbv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.MASSUNIT;
                    siu.Name = IfcSIUnitName.GRAM;
                    siu.Prefix = IfcSIPrefix.KILO;
                });
            });

            var definingValues = new List<IfcReal> { new IfcReal(100.0), new IfcReal(200.0), new IfcReal(400.0), new IfcReal(800.0), new IfcReal(1600.0), new IfcReal(3200.0), };
            var definedValues = new List<IfcReal> { new IfcReal(20.0), new IfcReal(42.0), new IfcReal(46.0), new IfcReal(56.0), new IfcReal(60.0), new IfcReal(65.0), };
            var ifcPropertyTableValue = model.Instances.New<IfcPropertyTableValue>(ptv =>
            {
                ptv.Name = "IfcPropertyTableValue:Sound";
                foreach (var item in definingValues)
                {
                    ptv.DefiningValues.Add(item);
                }
                foreach (var item in definedValues)
                {
                    ptv.DefinedValues.Add(item);
                }
                ptv.DefinedUnit = model.Instances.New<IfcContextDependentUnit>(cd =>
                {
                    cd.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 0;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                    cd.UnitType = IfcUnitEnum.FREQUENCYUNIT;
                    cd.Name = "dB";
                });


            });

            var listValues = new List<IfcLabel> { new IfcLabel("Red"), new IfcLabel("Green"), new IfcLabel("Blue"), new IfcLabel("Pink"), new IfcLabel("White"), new IfcLabel("Black"), };
            var ifcPropertyListValue = model.Instances.New<IfcPropertyListValue>(plv =>
            {
                plv.Name = "IfcPropertyListValue:Colours";
                foreach (var item in listValues)
                {
                    plv.ListValues.Add(item);
                }
            });

            var ifcMaterial = model.Instances.New<IfcMaterial>(m =>
            {
                m.Name = "Brick";
            });
            var ifcPrValueMaterial = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Material";
                prv.PropertyReference = ifcMaterial;
            });


            var ifcMaterialList = model.Instances.New<IfcMaterialList>(ml =>
            {
                ml.Materials.Add(ifcMaterial);
                ml.Materials.Add(model.Instances.New<IfcMaterial>(m => { m.Name = "Cavity"; }));
                ml.Materials.Add(model.Instances.New<IfcMaterial>(m => { m.Name = "Block"; }));
            });


            var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>(ml =>
            {
                ml.Material = ifcMaterial;
                ml.LayerThickness = 100.0;
            });
            var ifcPrValueMatLayer = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:MaterialLayer";
                prv.PropertyReference = ifcMaterialLayer;
            });

            var ifcDocumentReference = model.Instances.New<IfcDocumentReference>(dr =>
            {
                dr.Name = "Document";
                dr.Location = "c://Documents//TheDoc.Txt";
            });
            var ifcPrValueRef = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Document";
                prv.PropertyReference = ifcDocumentReference;
            });

            var ifcTimeSeries = model.Instances.New<IfcRegularTimeSeries>(ts =>
            {
                ts.Name = "Regular Time Series";
                ts.Description = "Time series of events";
                ts.StartTime = new IfcDateTime("2015-02-14T12:01:01");
                ts.EndTime = new IfcDateTime("2015-05-15T12:01:01");
                ts.TimeSeriesDataType = IfcTimeSeriesDataTypeEnum.CONTINUOUS;
                ts.DataOrigin = IfcDataOriginEnum.MEASURED;
                ts.TimeStep = 604800; //7 days in secs
            });

            var ifcPrValueTimeSeries = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:TimeSeries";
                prv.PropertyReference = ifcTimeSeries;
            });

            var ifcAddress = model.Instances.New<IfcPostalAddress>(a =>
            {
                a.InternalLocation = "Room 101";
                a.AddressLines.AddRange(new[] { new IfcLabel("12 New road"), new IfcLabel("DoxField") });
                a.Town = "Sunderland";
                a.PostalCode = "DL01 6SX";
            });
            var ifcPrValueAddress = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Address";
                prv.PropertyReference = ifcAddress;
            });
            var ifcTelecomAddress = model.Instances.New<IfcTelecomAddress>(a =>
            {
                a.TelephoneNumbers.Add(new IfcLabel("01325 6589965"));
                a.ElectronicMailAddresses.Add(new IfcLabel("bob@bobsworks.com"));
            });
            var ifcPrValueTelecom = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Telecom";
                prv.PropertyReference = ifcTelecomAddress;
            });



            //lets create the IfcElementQuantity
            var ifcPropertySet = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.Name = "Test:IfcPropertySet";
                ps.Description = "Property Set";
                ps.HasProperties.Add(ifcPropertySingleValue);
                ps.HasProperties.Add(ifcPropertyEnumeratedValue);
                ps.HasProperties.Add(ifcPropertyBoundedValue);
                ps.HasProperties.Add(ifcPropertyTableValue);
                ps.HasProperties.Add(ifcPropertyListValue);
                ps.HasProperties.Add(ifcPrValueMaterial);
                ps.HasProperties.Add(ifcPrValueMatLayer);
                ps.HasProperties.Add(ifcPrValueRef);
                ps.HasProperties.Add(ifcPrValueTimeSeries);
                ps.HasProperties.Add(ifcPrValueAddress);
                ps.HasProperties.Add(ifcPrValueTelecom);
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


    }
}
