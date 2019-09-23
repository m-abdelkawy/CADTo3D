using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.RepresentationResource;

namespace IfcFileCreator.Helpers
{
    public static class IFCExtensions
    {
        public static void InitProject(this IfcProject project, linearUnitsType units)
        {
            var model = project.Model;
            
                var ua = model.Instances.New<IfcUnitAssignment>();
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.LENGTHUNIT;
                    s.Name = IfcSIUnitName.METRE;
                    if (units == linearUnitsType.Millimeters)
                        s.Prefix = IfcSIPrefix.MILLI;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.AREAUNIT;
                    s.Name = IfcSIUnitName.SQUARE_METRE;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.VOLUMEUNIT;
                    s.Name = IfcSIUnitName.CUBIC_METRE;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.SOLIDANGLEUNIT;
                    s.Name = IfcSIUnitName.STERADIAN;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.PLANEANGLEUNIT;
                    s.Name = IfcSIUnitName.RADIAN;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.MASSUNIT;
                    s.Name = IfcSIUnitName.GRAM;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.TIMEUNIT;
                    s.Name = IfcSIUnitName.SECOND;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType =
                        IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT;
                    s.Name = IfcSIUnitName.DEGREE_CELSIUS;
                }));
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.LUMINOUSINTENSITYUNIT;
                    s.Name = IfcSIUnitName.LUMEN;
                }));
                project.UnitsInContext = ua;
            

            if (project.ModelContext == null)
            {
                var origin = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                var axis3D = model.Instances.New<IfcAxis2Placement3D>(a => a.Location = origin);
                var gc = model.Instances.New<IfcGeometricRepresentationContext>(c =>
                {
                    c.
                        ContextType
                        =
                        "Model";
                    c.
                        ContextIdentifier
                        =
                        "Building Model";
                    c.
                        CoordinateSpaceDimension
                        = 3;
                    c.Precision
                        =
                        0.00001;
                    c.
                        WorldCoordinateSystem
                        = axis3D;
                }
                    );
               project.RepresentationContexts.Add(gc);

                var origin2D = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(0, 0));
                var axis2D = model.Instances.New<IfcAxis2Placement2D>(a => a.Location = origin2D);
                var pc = model.Instances.New<IfcGeometricRepresentationContext>(c =>
                {
                    c.
                        ContextType
                        =
                        "Plan";
                    c.
                        ContextIdentifier
                        =
                        "Building Plan View";
                    c.
                        CoordinateSpaceDimension
                        = 2;
                    c.Precision
                        =
                        0.00001;
                    c.
                        WorldCoordinateSystem
                        = axis2D;
                }
                    );
               project.RepresentationContexts.Add(pc);

            }
        }
    }
}
