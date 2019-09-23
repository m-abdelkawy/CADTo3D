using CADReader.BuildingElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcFileCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Building building = new Building("Building A");
            building.AddNewFloor(@"E:\Work\CAD Template\03.Ground Roof Slab.dwg", 3);
            building.AddNewFloor(@"E:\Work\CAD Template\02.Basement Roof SLab.dwg", 0);
            building.AddBuildingFoundation(@"E:\Work\CAD Template\01.Foundation.dwg", -4);

            XbimCreateBuilding newBuilding = new XbimCreateBuilding(building, @"D:\Coding Trials\GIT\CADTo3DDesktop\CADTo3DDesktop\IfcFileCreator\bin");
            devDept.Eyeshot.Translators.ReadAutodesk.OnApplicationExit(null, null);
        }
    }
}
