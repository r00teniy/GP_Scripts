// (C) Copyright 2022 by r00teniy 
//
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(P_Volumes.MyCommands))]

namespace P_Volumes
{
    public class MyCommands
    {
        [CommandMethod("Plabel")]

        public void Plabel()
        {
            // Getting list of Xrefs and starting form
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PlabelsForm pf = new PlabelsForm();
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 1; i < XrGraph.NumNodes; i++)
            {
                XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                pf.Xrefselect.Items.Add(XrNode.Name);
            }
            pf.Show();

        }
        public void DoPlabel(string X, string[] a)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            XrefGraphNode OSNOVA;
            string selectedOSN = X;
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            RXClass rxClassHatch = RXClass.GetClass(typeof(Hatch));
            string[] laylistH = { "|41_Покр_Проезд", "|49_Покр_Щебеночный_проезд", "|42_Покр_Тротуар", "|42_Покр_Тротуар_Пожарный", "|43_Покр_Отмостка", "|44_Покр_Детская_площадка", "|45_Покр_Спортивная_площадка", "|46_Покр_Площадка_отдыха", "|47_Покр_Хоз_площадка", "|48_Покр_Площадка_для_собак" };
            string PlabelLayer = "32_Подписи_покрытий";

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                OSNOVA = XrGraph.GetXrefNode(0);
                for (int i = 1; i < XrGraph.NumNodes; i++)
                {
                    XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                    if (XrNode.Name == selectedOSN)
                    {
                        OSNOVA = XrNode;
                    }
                }
                using (DocumentLock acLckDoc = doc.LockDocument())
                { 
                    var blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                    var blocktableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                    var blocktableRecordOSN = trans.GetObject(OSNOVA.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    foreach (ObjectId objectId in blocktableRecordOSN)
                    {
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = trans.GetObject(objectId, OpenMode.ForRead) as Hatch;
                            for (int i = 0; i < laylistH.Length; i++)
                            {
                                if ((hat.Layer == selectedOSN + laylistH[i]) && hat != null)
                                {
                                    try
                                    {
                                        Extents3d extents = hat.GeometricExtents;
                                        Point3d center = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2.0;
                                        MLeader leader = new MLeader();

                                        leader.SetDatabaseDefaults();
                                        leader.ContentType = ContentType.MTextContent;
                                        leader.Layer = PlabelLayer;
                                        
                                        MText mText = new MText();
                                        mText.SetDatabaseDefaults();
                                        mText.Width = 0.675;
                                        mText.Height = 1.25;
                                        mText.TextHeight = 1.25;
                                        mText.SetContentsRtf(a[i]);
                                        mText.Location = new Point3d(center.X + 5, center.Y + 5, center.Z);
                                        mText.Rotation = 0;
                                        mText.BackgroundFill = true;
                                        mText.BackgroundScaleFactor = 1.1;

                                        leader.MText = mText;
                                                                                
                                        int idx = leader.AddLeaderLine(center);
                                        blocktableRecord.AppendEntity(leader);
                                        trans.AddNewlyCreatedDBObject(leader, true);

                                    }
                                    catch 
                                    {
                                        ed.WriteMessage("\n" + "Some error, please check hatches");
                                    }
                                    
                                }

                            }

                        }
                    }
                    trans.Commit();
                }

            }
        }
        [CommandMethod("CheckHatch")]

        public void CheckHatch()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            RXClass rxClassHatch = RXClass.GetClass(typeof(Hatch));
            List<ObjectId> errors = new List<ObjectId>(); //list of objectId
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                using (DocumentLock acLckDoc = doc.LockDocument())
                { 
                    var blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                    var blocktableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) as BlockTableRecord;
                    //Finding self-intersecting Hatches
                    int i = 0;
                    foreach (ObjectId objectId in blocktableRecord)
                    {
                    
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = trans.GetObject(objectId, OpenMode.ForRead) as Hatch;

                            try
                            {
                                var test = hat.Area;
                            }
                            catch
                            {
                                errors.Add(objectId); // Adding hatches that don't have Area
                                i++;
                            }

                        }
                    }
                    // selecting objects
                    if (i == 0)
                    {
                        ed.WriteMessage("Самопересечений не найдено"); // if all hatches have Area
                    }
                    else
                    {
                        ed.WriteMessage("Выделено"+i+"самопересекающихся штриховок");
                        ed.SetImpliedSelection(errors.ToArray()); // selecting bad hatches
                        ed.SelectImplied();
                    }
                    trans.Commit();
                }
            }
        }

        [CommandMethod("CountVol")]
        public void CountVol()
        {
            // Getting list of Xrefs and starting form
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            MainForm mf = new MainForm();
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 1; i < XrGraph.NumNodes; i++)
            {
                XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                mf.Xrefselect.Items.Add(XrNode.Name);
            }
            mf.Show();
        }
            //Counting Volumes
        public void RealCount(string X, int a, int b, int c)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            XrefGraphNode OSNOVA;
            string selectedOSN = X;
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            long ln = Convert.ToInt64("774A", 16); //Table handle
            Handle hn = new Handle(ln);
            ObjectId id = db.GetObjectId(false, hn, 0); // Getting table object
            RXClass rxClassPoly = RXClass.GetClass(typeof(Polyline));
            RXClass rxClassHatch = RXClass.GetClass(typeof(Hatch));
            RXClass rxClassBlockRef = RXClass.GetClass(typeof(BlockReference));
            double[] TableValues = new double[19];
            string[] ErrorValues = new string[19];
            string[] laylistPL = { "|31_Борт_100.30.15", "|32_Борт_100.20.8", "|33_Борт_100.45.18", "|34_Борт_Металл", "|35_Борт_Пластик" };
            int[] PL_count = { a, b, c, 1, 1 };
            string[] laylistH = { "|41_Покр_Проезд", "|49_Покр_Щебеночный_проезд", "|42_Покр_Тротуар", "|42_Покр_Тротуар_Пожарный", "|43_Покр_Отмостка", "|44_Покр_Детская_площадка", "|45_Покр_Спортивная_площадка", "|46_Покр_Площадка_отдыха", "|47_Покр_Хоз_площадка", "|48_Покр_Площадка_для_собак", "|51_Газон", "|52_Газон_пожарный" };
            string[] laylistBL = { "52_Деревья", "51_Кустарники" };

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                    OSNOVA = XrGraph.GetXrefNode(0);
                for (int i = 1; i < XrGraph.NumNodes; i++)
                {
                    XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                    if (XrNode.Name == selectedOSN)
                    {
                        OSNOVA = XrNode;
                    }
                }
                using (DocumentLock acLckDoc = doc.LockDocument())
                {
                    var blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                    var blocktableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead, false) as BlockTableRecord;
                    var blocktableRecordOSN = trans.GetObject(OSNOVA.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    var Tabl = trans.GetObject(id, OpenMode.ForWrite) as Table;
                    foreach (ObjectId objectId in blocktableRecordOSN)
                    {
                        if (objectId.ObjectClass == rxClassPoly)
                        {
                            var poly = trans.GetObject(objectId, OpenMode.ForRead) as Polyline;
                            for (int i = 0; i < laylistPL.Length; i++)
                            {
                                if ((poly.Layer == selectedOSN + laylistPL[i]) && poly != null)
                                {
                                    TableValues[14 + i] += poly.GetDistanceAtParameter(poly.EndParam) / PL_count[i];
                                }

                            }

                        }
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = trans.GetObject(objectId, OpenMode.ForRead) as Hatch;
                            for (int i = 0; i < laylistH.Length; i++)
                            {
                                if ((hat.Layer == selectedOSN + laylistH[i]) && hat != null)
                                {
                                    try
                                    {
                                        TableValues[i] += hat.Area;
                                    }
                                    catch
                                    {
                                        ErrorValues[i] = "Самопересечение";
                                    }
                                }

                            }

                        }
                    }
                    foreach (ObjectId objectId in blocktableRecord)
                    {
                        if (objectId.ObjectClass == rxClassBlockRef)
                        {
                            var blr = trans.GetObject(objectId, OpenMode.ForRead) as BlockReference;
                            for (int i = 0; i < laylistBL.Length; i++)
                            {
                                if ((blr.Layer == laylistBL[i]) && blr != null)
                                {
                                    if (!AssocArray.IsAssociativeArray(objectId))
                                    {
                                        TableValues[12 + i] += 1;
                                    }

                                }

                            }

                        }
                        //attempt ot make it count blocks in array
                        if (AssocArray.IsAssociativeArray(objectId))
                        {
                            using (BlockReference br = (BlockReference)objectId.Open(OpenMode.ForRead))
                            {
                                using (BlockTableRecord btr = br.DynamicBlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
                                {
                                    foreach (ObjectId ids in btr)
                                    {
                                        if (ids.ObjectClass.Name == "AcDbBlockReference")
                                        {
                                            using (BlockReference bRef = (BlockReference)ids.Open(OpenMode.ForRead))
                                            {
                                                for (int j = 0; j < laylistBL.Length; j++)
                                                {
                                                    if (bRef.Layer == laylistBL[j])
                                                    {
                                                        TableValues[12 + j] += 1;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }
                            }
                        }
                    }
                    for (int k = 0; k < ErrorValues.Length; k++)
                    {
                        try
                        {
                            Tabl.SetTextString(2 + k, 2, ErrorValues?[k]);
                            if (ErrorValues[k] == "Самопересечение")
                            {
                                TableValues[k] = -1;
                            }
                        }
                        catch { }
                    }
                    for (int j = 0; j < TableValues.Length; j++)
                    {
                        if (TableValues[j] == 0)
                        {
                            ErrorValues[j] = "Нет Элементов";
                        }
                        if (TableValues[j] > 0)
                        {
                            ErrorValues[j] = "Всё в порядке";
                        }
                    }
                    for (int m = 14; m < 19; m++)
                    {
                        TableValues[m] = Math.Ceiling(TableValues[m]);
                    }
                    for (int i = 0; i < TableValues.Length; i++)
                    {
                        double mem = TableValues[i];
                        Tabl.SetTextString(2 + i, 1, mem.ToString("0.##"));
                        Tabl.SetTextString(2 + i, 2, ErrorValues[i]);
                    }
                    trans.Commit();
                }
            }
        }
                
    }

}
