using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Xbim.COBieLite;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc4.Kernel;

using Xbim.ModelGeometry.Scene.Extensions;
using BIMWebViewer.Classes;

namespace BIMWebViewer
{
    public static class IFCConverter
    {
        private static List<IIfcProduct> _products = null;
        public static List<IIfcProduct> Products
        {
            get
            {
                if (_products == null)
                    _products = GetProducts();
                return _products;
            }
            set { _products = value; }
        }
        public static List<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
        public static List<IIfcRelDefinesByType> ModelTypes { get; set; }
        public static Dictionary<int, List<Xbim.Ifc2x3.Kernel.IfcPropertySet>> propertySets2x3 { get; set; } = new Dictionary<int, List<Xbim.Ifc2x3.Kernel.IfcPropertySet>>();
        public static Dictionary<int, List<IIfcPropertySet>> propertySetsx4 { get; set; } = new Dictionary<int, List<IIfcPropertySet>>();


        public static IfcStore Model { get; set; }
        public static string ToWexBIM(string path)
        {
            var wexBimFilename = "";
            string fileName = path;

            try
            {
                using (var model = IfcStore.Open(fileName))
                {
                    var context = new Xbim3DModelContext(model);
                    context.CreateContext();
                    Model = model;
                    //Products = GetProducts();
                    //ModelTypes = Model.Instances.OfType<IIfcRelDefinesByType>().ToList();
                    //Categories = GetCategories();
                    //if (model.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc4)
                    //    propertySetsx4 = GetPropertySets();
                    //else
                    //    propertySets2x3 = GetPropertySets2x3();
                    wexBimFilename = Path.ChangeExtension(fileName, "wexBIM");
                    using (var wexBiMfile = File.Create(wexBimFilename))
                    {
                        using (var wexBimBinaryWriter = new BinaryWriter(wexBiMfile))
                        {
                            model.SaveAsWexBim(wexBimBinaryWriter);
                            wexBimBinaryWriter.Close();
                        }
                        wexBiMfile.Close();
                    }


                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
            return wexBimFilename;
        }
        private static Dictionary<int, List<IIfcPropertySet>> GetPropertySets()
        {
            Dictionary<int, List<IIfcPropertySet>> propSets = new Dictionary<int, List<IIfcPropertySet>>();
            Xbim.Ifc4.Kernel.IfcProduct productIFC4 = null;
            foreach (var product in Products)
            {
                if (product is Xbim.Ifc4.Kernel.IfcProduct)
                {
                    productIFC4 = product as Xbim.Ifc4.Kernel.IfcProduct;
                    propSets.Add(product.EntityLabel, productIFC4.PropertySets.ToList());
                }
            }
            return propSets;
        }
        private static Dictionary<int, List<Xbim.Ifc2x3.Kernel.IfcPropertySet>> GetPropertySets2x3()
        {
            Dictionary<int, List<Xbim.Ifc2x3.Kernel.IfcPropertySet>> propSets = new Dictionary<int, List<Xbim.Ifc2x3.Kernel.IfcPropertySet>>();
            Xbim.Ifc2x3.Kernel.IfcProduct productIFC2x3 = null;

            foreach (var product in Products)
            {

                productIFC2x3 = product as Xbim.Ifc2x3.Kernel.IfcProduct;
                propSets.Add(product.EntityLabel, productIFC2x3.PropertySets.ToList());

            }
            return propSets;
        }
        private static List<IIfcProduct> GetProducts()
        {
            var Products = new List<IIfcProduct>();
            foreach (var product in Model.Instances.OfType<IIfcProduct>(true).Where(p => p.Representation != null))
            {

                if (product.Representation != null)
                {
                    if (product.Representation.Representations == null) continue;
                    var rep =
                        product.Representation.Representations.FirstOrDefault(
                            r => r.IsBodyRepresentation());
                    if (rep != null)
                        Products.Add(product);

                }
            }
            return Products;
        }
        private static List<ProductCategory> GetCategories()
        {
            List<IIfcRelDefinesByType> ifcRelDefinesByTypes = Model.Instances.OfType<IIfcRelDefinesByType>().ToList();
            List<ProductCategory> Categories = new List<ProductCategory>();

            List<string> list = Products.Select(a => a.GetType().Name).ToList();
            List<string> newlist = Products.Select(a => a.GetType().Name).ToList().Distinct().ToList();
            newlist.ForEach(a => Categories.Add(new ProductCategory() { Name = a }));


            foreach (var item in Categories)
            {
                List<IIfcProduct> products = Products.Where(p => p.GetType().Name == item.Name).ToList();
                foreach (var product in products)
                {
                    List<string> lst = new List<string>();
                    if (product.Name != null)
                        lst = product.Name.Value.Value.ToString().Split(':').ToList();
                    if (lst.Count >= 3)
                    {
                        IIfcRelDefinesByType relType = ifcRelDefinesByTypes.Where(a => a == product.IsTypedBy.FirstOrDefault()).FirstOrDefault();
                        if (item.Families.Where(c => c.Name == lst[0]).Count() == 0)
                        {
                            ProductFamily productFamily = new ProductFamily() { Name = lst[0] };
                            ProductType productType = new ProductType() { Name = lst[1], };
                            if (relType != null)
                            {
                                relType.RelatedObjects.ToList().ForEach(a => productType.Products.Add((IIfcProduct)a));
                                productType.Id = relType.RelatingType.EntityLabel;
                            }
                            else
                            {
                                productType.Products.Add(product);
                                if (product.IsTypedBy.Count() >= 1)
                                    productType.Id = product.IsTypedBy.FirstOrDefault().EntityLabel;
                            }
                            productFamily.Types.Add(productType);
                            item.Families.Add(productFamily);
                        }
                        else
                        {
                            ProductFamily existingFamily = item.Families.Where(c => c.Name == lst[0]).FirstOrDefault();
                            if (existingFamily.Types.Where(c => c.Name == lst[1]).Count() == 0)
                            {
                                ProductType productType = new ProductType() { Name = lst[1] };
                                if (relType != null)
                                {
                                    relType.RelatedObjects.ToList().ForEach(a => productType.Products.Add((IIfcProduct)a));
                                    productType.Id = relType.RelatingType.EntityLabel;
                                }
                                else
                                {
                                    productType.Products.Add(product);
                                    if (product.IsTypedBy.Count() >= 1)
                                        productType.Id = product.IsTypedBy.FirstOrDefault().EntityLabel;
                                }
                                item.Families.Where(c => c.Name == lst[0]).FirstOrDefault().Types.Add(productType);
                            }
                            else if (existingFamily.Types.Where(c => c.Name == lst[1]).FirstOrDefault().Products.Where(p => p.EntityLabel == product.EntityLabel).Count() == 0)
                            {
                                existingFamily.Types.Where(c => c.Name == lst[1]).FirstOrDefault().Products.Add(product);
                            }
                        }


                    }
                }
            }
            return Categories;
        }

        public static void CreateTree(string path)
        {
            string fileName = path;
            using (var model = IfcStore.Open(fileName))
            {
                Model = model;
                Products = GetProducts();
                //ModelTypes = Model.Instances.OfType<IIfcRelDefinesByType>().ToList();
                //Categories = GetCategories();
                if (model.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc4)
                    propertySetsx4 = GetPropertySets();
                else
                    propertySets2x3 = GetPropertySets2x3();
            }
        }


    }
}
