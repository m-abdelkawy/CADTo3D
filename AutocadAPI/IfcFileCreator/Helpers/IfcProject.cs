using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.MeasureResource;
using Xbim.Common;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.RepresentationResource;
namespace Xbim.Ifc4.Kernel
{
   public partial class IfcProject
    {
        public void InitProject(linearUnitsType units)
        {
            var model = Model;
            if (units == ProjectUnits.SIUnitsUK)
            {
                var ua = model.Instances.New<IfcUnitAssignment>();
                ua.Units.Add(model.Instances.New<IfcSIUnit>(s =>
                {
                    s.UnitType = IfcUnitEnum.LENGTHUNIT;
                    s.Name = IfcSIUnitName.METRE;
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
                UnitsInContext = ua;
            }

        }
    }
}
