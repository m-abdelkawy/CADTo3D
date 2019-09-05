using Autodesk.AutoCAD.ApplicationServices;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.EditorInput;

using Autodesk.AutoCAD.Geometry;

using Autodesk.AutoCAD.Runtime;



namespace DynamicBlocks

{

    public class Commands

    {

        [CommandMethod("DBP")]

        static public void DynamicBlockProps()

        {

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;



            PromptStringOptions pso =

              new PromptStringOptions(

                "\nEnter dynamic block name or enter to select: "

              );



            pso.AllowSpaces = true;

            PromptResult pr = ed.GetString(pso);



            if (pr.Status != PromptStatus.OK)

                return;



            Transaction tr = db.TransactionManager.StartTransaction();

            using (tr)

            {

                BlockReference br = null;



                // If a null string was entered allow entity selection



                if (pr.StringResult == "")

                {

                    // Select a block reference



                    PromptEntityOptions peo =

                      new PromptEntityOptions(

                        "\nSelect dynamic block reference: "

                      );



                    peo.SetRejectMessage("\nEntity is not a block.");

                    peo.AddAllowedClass(typeof(BlockReference), false);



                    PromptEntityResult per =

                      ed.GetEntity(peo);



                    if (per.Status != PromptStatus.OK)

                        return;



                    // Access the selected block reference



                    br =

                      tr.GetObject(

                        per.ObjectId,

                        OpenMode.ForRead

                      ) as BlockReference;

                }

                else

                {

                    // Otherwise we look up the block by name



                    BlockTable bt =

                      tr.GetObject(

                        db.BlockTableId,

                        OpenMode.ForRead) as BlockTable;



                    if (!bt.Has(pr.StringResult))

                    {

                        ed.WriteMessage(

                          "\nBlock \"" + pr.StringResult + "\" does not exist."

                        );

                        return;

                    }



                    // Create a new block reference referring to the block



                    br =

                      new BlockReference(

                        new Point3d(),

                        bt[pr.StringResult]

                      );

                }



                BlockTableRecord btr =

                  (BlockTableRecord)tr.GetObject(

                    br.DynamicBlockTableRecord,

                    OpenMode.ForRead

                  );



                // Call our function to display the block properties



                DisplayDynBlockProperties(ed, br, btr.Name);



                // Committing is cheaper than aborting



                tr.Commit();

            }

        }



        private static void DisplayDynBlockProperties(

          Editor ed, BlockReference br, string name

        )

        {

            // Only continue is we have a valid dynamic block



            if (br != null && br.IsDynamicBlock)

            {

                ed.WriteMessage(

                  "\nDynamic properties for \"{0}\"\n",

                  name

                );



                // Get the dynamic block's property collection



                DynamicBlockReferencePropertyCollection pc =

                  br.DynamicBlockReferencePropertyCollection;



                // Loop through, getting the info for each property



                foreach (DynamicBlockReferenceProperty prop in pc)

                {

                    // Start with the property name, type and description



                    ed.WriteMessage(

                      "\nProperty: \"{0}\" : {1}",

                      prop.PropertyName,

                      prop.UnitsType

                    );



                    if (prop.Description != "")

                        ed.WriteMessage(

                          "\n  Description: {0}",

                          prop.Description

                        );



                    // Is it read-only?



                    if (prop.ReadOnly)

                        ed.WriteMessage(" (Read Only)");



                    // Get the allowed values, if it's constrained



                    bool first = true;



                    foreach (object value in prop.GetAllowedValues())

                    {

                        ed.WriteMessage(

                          (first ? "\n  Allowed values: [" : ", ")

                        );

                        ed.WriteMessage("\"{0}\"", value);



                        first = false;

                    }

                    if (!first)

                        ed.WriteMessage("]");



                    // And finally the current value



                    ed.WriteMessage(

                      "\n  Current value: \"{0}\"\n",

                      prop.Value

                    );

                }

            }

        }

    }

}