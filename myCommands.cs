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
            PlabelsForm pf = new PlabelsForm();
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 1; i < XrGraph.NumNodes; i++)
            {
                XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                pf.Xrefselect.Items.Add(XrNode.Name);
            }
            pf.Show();
        }

        //Function that labels hatches based on layer
        public void DoPlabel(string X, string[] a)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            XrefGraphNode OSNOVA;
            string selectedOSN = X;
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            RXClass rxClassHatch = RXClass.GetClass(typeof(Hatch));
            string[] laylistH = { "|41_Покр_Проезд", "|49_Покр_Щебеночный_проезд", "|42_Покр_Тротуар", "|42_Покр_Тротуар_Пожарный", "|43_Покр_Отмостка", "|44_Покр_Детская_площадка", "|45_Покр_Спортивная_площадка", "|46_Покр_Площадка_отдыха", "|47_Покр_Хоз_площадка", "|48_Покр_Площадка_для_собак" };
            string PlabelLayer = "32_Подписи_покрытий"; //layer for Mleaders
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //selecting correct Xref
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
                        // getting all hatches on layers
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = trans.GetObject(objectId, OpenMode.ForRead) as Hatch;
                            for (int i = 0; i < laylistH.Length; i++)
                            {
                                if ((hat.Layer == selectedOSN + laylistH[i]) && hat != null)
                                {
                                    try
                                    {
                                        //getting center of each hatch
                                        Extents3d extents = hat.GeometricExtents;
                                        Point3d center = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2.0;
                                        //createing Mleader
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

        [CommandMethod("Olabel")]
        public void Olabel()
        {
            OlabelsForm of = new OlabelsForm();
            of.Show();
        }
        
        // Function to get all blocks on a Layer, including ones in array
        public List<Point3d> GetBlocksPosition (BlockTableRecord blocktablerecord, Transaction trans, string LayerName)
        {
            List<Point3d> pointsList = new List<Point3d>();
            foreach (ObjectId objectId in blocktablerecord)
            {
                if (objectId.ObjectClass == RXClass.GetClass(typeof(BlockReference)))
                {
                    var blr = trans.GetObject(objectId, OpenMode.ForRead) as BlockReference;
                    if (blr.Layer == LayerName && blr != null)
                    {
                        // checing for array in order to exclude doubles, becuase array is blockref
                        if (!AssocArray.IsAssociativeArray(objectId))
                        {
                            pointsList.Add(blr.Position);
                        }
                    }
                }
                //counting blocks in array
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
                                        if (bRef.Layer == LayerName)
                                        {
                                            pointsList.Add(bRef.Position);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return pointsList;
        }

        public List<List<Point3d>> GroupO (List<Point3d> Points, double Distance)
        {
            // calclulate distance between points
            List<List<double>> Dist = new List<List<double>>();
            for (int i = 0; i < Points.Count; i++)
            {
                Dist.Add(new List<double>());

                for (int j = 0; j < Points.Count; j++)
                {
                    Dist[i].Add(Points[i].DistanceTo(Points[j]));
                }
            }
            //making lists of close objects
            List<List<int>> Close = new List<List<int>>();
            for (int i = 0; i < Points.Count; i++)
            {
                Close.Add(new List<int>());
                for (int j = 0; j < Points.Count; j++)
                {
                    if (Dist[i][j] < Distance)
                    {
                        Close[i].Add(j);
                    }
                }
            }
            // make groups
            List<List<int>> Groups = new List<List<int>>();
            for (int i = 0; i < Points.Count; i++)
            {
                List<int> temp = new List<int>();
                List<int> temp2 = new List<int>();
                temp.Add(Close[i][0]);
                temp2.Add(Close[i][0]);
                int X = 0;
                while (X == 0)
                {
                    for (int k = 0; k < temp.Count; k++)
                    {
                        foreach (int l in Close[temp[k]])
                        {
                            if (!temp.Contains(l))
                            {
                                temp2.Add(l);
                            }
                        }
                    }
                    if (temp == temp2)
                    {
                        X = 1;
                    }
                    else
                    {
                        temp = temp2;
                    }
                }
                Groups.Add(temp);
            }
            // clean group
            List<List<int>> GroupsClean = new List<List<int>>();
            for (int i = 0; i < Groups.Count; i++)
            {
                if (Groups[i] != null && Groups[i].Count > 1)
                {
                    foreach (int j in Groups[i])
                    {
                        if (j != i)
                        {
                            Groups[j] = null;
                        }
                    }
                }
            }
            foreach (List<int> m in Groups)
            {
                if (m != null)
                {
                    GroupsClean.Add(m);
                }
            }
            // change back to coordinates
            List<List<Point3d>> GroupsCoordinates = new List<List<Point3d>>();
            for (int i = 0; i < GroupsClean.Count; i++)
            {
                GroupsCoordinates.Add(new List<Point3d>());
                foreach (int j in GroupsClean[i])
                {
                    GroupsCoordinates[i].Add(Points[j]);
                }
            }
            return GroupsCoordinates;
        }
        public void CreateMleaderO (List<List<Point3d>> coords, Transaction trans, BlockTable BT, BlockTableRecord BR, Database db, string Tname , double rotAngle)
        {
            MLeader leader;
            for (int i = 0; i < coords.Count; i++)
            {
                DBDictionary mlStyles = trans.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
                ObjectId mlStyleId = mlStyles.GetAt("Озеленение");
                leader = new MLeader();
                leader.SetDatabaseDefaults();
                leader.MLeaderStyle = mlStyleId;
                leader.ContentType = ContentType.BlockContent;
                leader.Layer = "50_Озеленение_подписи";
                leader.BlockContentId = BT["Выноска_озеленение"];
                leader.BlockPosition = new Point3d(coords[i][0].X + 5, coords[i][0].Y + 5, 0);
                leader.BlockRotation = rotAngle;
                int idx = leader.AddLeaderLine(coords[i][0]);
                // adding leader points for each element
                //temporary solution, need sorting algorithm for better performance.
                if (coords[i].Count > 1)
                {
                    foreach (Point3d m in coords[i])
                    {
                        leader.AddFirstVertex(idx, m);
                    }
                }
                //Handle Block Attributes
                BlockTableRecord blkLeader = trans.GetObject(leader.BlockContentId, OpenMode.ForRead) as BlockTableRecord;
                //Doesn't take in consideration oLeader.BlockRotation
                Matrix3d Transfo = Matrix3d.Displacement(leader.BlockPosition.GetAsVector());
                foreach (ObjectId blkEntId in blkLeader)
                {
                    AttributeDefinition AttributeDef = trans.GetObject(blkEntId, OpenMode.ForRead) as AttributeDefinition;
                    if (AttributeDef != null)
                    {
                        AttributeReference AttributeRef = new AttributeReference();
                        AttributeRef.SetAttributeFromBlock(AttributeDef, Transfo);
                        AttributeRef.Position = AttributeDef.Position.TransformBy(Transfo);
                        // setting attributes
                        if (AttributeRef.Tag == "НОМЕР")
                        {
                            AttributeRef.TextString = Tname;
                        }
                        if (AttributeRef.Tag == "КОЛ-ВО")
                        {
                            AttributeRef.TextString = coords[i].Count.ToString();
                        }
                        leader.SetBlockAttribute(blkEntId, AttributeRef);
                    }
                }
                // adding Mleader to blocktablerecord
                BR.AppendEntity(leader);
                trans.AddNewlyCreatedDBObject(leader, true);
            }
        }
        public void DoOlabel(string Tp, string Bp, float Td, float Bd)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Matrix3d UCS = ed.CurrentUserCoordinateSystem;
            CoordinateSystem3d cs = UCS.CoordinateSystem3d;
            double rotAngle = cs.Xaxis.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
            List<Point3d> TreePoints;
            List<Point3d> BushPoints;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                using (DocumentLock acLckDoc = doc.LockDocument())
                {
                    var blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                    var blocktableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                    //Getting insertion points from blocks on defined layers
                    TreePoints = GetBlocksPosition(blocktableRecord, trans, "52_Деревья");
                    BushPoints = GetBlocksPosition(blocktableRecord, trans, "51_Кустарники");
                    //Grouping elements by max distance
                    List<List<Point3d>> TreeGroupsCoordinates = GroupO(TreePoints, Td);
                    List<List<Point3d>> BushGroupsCoordinates = GroupO(BushPoints, Bd);
                    // Creating Mleaders with information
                    CreateMleaderO(TreeGroupsCoordinates, trans, blockTable, blocktableRecord, db, Tp, rotAngle);
                    CreateMleaderO(BushGroupsCoordinates, trans, blockTable, blocktableRecord, db, Bp, rotAngle);
                }
                trans.Commit();
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
            XrefGraphNode OSNOVA;
            string selectedOSN = X;
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            long ln = Convert.ToInt64("774A", 16); //Table handle
            Handle hn = new Handle(ln);
            ObjectId id = db.GetObjectId(false, hn, 0); // Getting table object
            RXClass rxClassPoly = RXClass.GetClass(typeof(Polyline));
            RXClass rxClassHatch = RXClass.GetClass(typeof(Hatch));
            double[] TableValues = new double[19];
            string[] ErrorValues = new string[19];
            string[] laylistPL = { "|31_Борт_100.30.15", "|32_Борт_100.20.8", "|33_Борт_100.45.18", "|34_Борт_Металл", "|35_Борт_Пластик" };
            int[] PL_count = { a, b, c, 1, 1 };
            string[] laylistH = { "|41_Покр_Проезд", "|49_Покр_Щебеночный_проезд", "|42_Покр_Тротуар", "|42_Покр_Тротуар_Пожарный", "|43_Покр_Отмостка", "|44_Покр_Детская_площадка", "|45_Покр_Спортивная_площадка", "|46_Покр_Площадка_отдыха", "|47_Покр_Хоз_площадка", "|48_Покр_Площадка_для_собак", "|51_Газон", "|52_Газон_пожарный" };

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
                    TableValues[12] = GetBlocksPosition(blocktableRecord, trans, "52_Деревья").Count;
                    TableValues[13] = GetBlocksPosition(blocktableRecord, trans, "51_Кустарники").Count;
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
