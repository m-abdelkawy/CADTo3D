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
            building.AddNewFloor(@"D:\Coding Trials\AEC Development\New Demo 2019.09.06\03.2nd slab.dwg", 3000);
            building.AddNewFloor(@"D:\Coding Trials\AEC Development\New Demo 2019.09.06\02.1st slab.dwg", 0);
            building.AddBuildingFoundation(@"D:\Coding Trials\AEC Development\New Demo 2019.09.06\01.Foundation.dwg", -3000);
            XbimCreateBuilding newBuilding = new XbimCreateBuilding(building);
            devDept.Eyeshot.Translators.ReadAutodesk.OnApplicationExit(null, null);
        }
    }
}
