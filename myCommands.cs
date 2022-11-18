// (C) Copyright 2022 by r00teniy 
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.AutoCAD.Colors;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(P_Volumes.MyCommands))]

namespace P_Volumes
{
    public class MyCommands
    {
        public Document doc = Application.DocumentManager.MdiActiveDocument;
        public Database db = Application.DocumentManager.MdiActiveDocument.Database;
        public Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        public RXClass rxClassHatch = RXClass.GetClass(typeof(Hatch));
        public RXClass rxClassPoly = RXClass.GetClass(typeof(Polyline));
        public RXClass rxClassRegion = RXClass.GetClass(typeof(Region));
        public XrefGraphNode OSNOVA;
        //layers for Hatches
        public string[] laylistHatch = { "41_Покр_Проезд", "49_Покр_Щебеночный_проезд", "42_Покр_Тротуар", "42_Покр_Тротуар_Пожарный", "43_Покр_Отмостка", "44_Покр_Детская_площадка", "45_Покр_Спортивная_площадка", "46_Покр_Площадка_отдыха", "47_Покр_Хоз_площадка", "48_Покр_Площадка_для_собак", "51_Газон", "52_Газон_пожарный", "15_Здание_заливка" };
        //Layers for polylines
        public string[] laylistPL = { "09_Граница_благоустройства", "16_Здание_контур_площадь_застройки", "31_Борт_100.30.15", "32_Борт_100.20.8", "33_Борт_100.45.18", "34_Борт_Металл", "35_Борт_Пластик" };
        //Layers for blocks
        public string[] laylistBlock = { "51_Деревья", "52_Кустарники" };
        //Layer for pavement labels
        public string pLabelLayer = "32_Подписи_покрытий";
        //Layer for greenery labels
        public string oLabelLayer = "50_Озеленение_подписи";
        //Temporary layer data
        public string tempLayer = "80_Временная_геометрия"; // layer for temporary geometry
        public Color tempLayerColor = Color.FromColorIndex(ColorMethod.ByAci, 3);
        public double tempLayerLineWeight = 2.0;
        public bool tempLayerPrintable = false;
        //Table handles
        public long th = Convert.ToInt64("774A", 16); //Table handle for hatches
        public long tp = Convert.ToInt64("78E16", 16); //Table handle for polylines
        public long tb = Convert.ToInt64("78E73", 16); //Table handle for blocks

        //All commands first
        [CommandMethod("CountVol")]
        public void CountVol()
        {
            // Getting list of Xrefs and starting form
            MainForm mf = new MainForm();
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 1; i < XrGraph.NumNodes; i++)
            {
                XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                mf.Xrefselect.Items.Add(XrNode.Name);
            }
            mf.Show();
        }

        [CommandMethod("Olabel")]
        public void Olabel()
        {
            OlabelsForm of = new OlabelsForm();
            of.Show();
        }

        [CommandMethod("hInt")]
        public void hInt()
        {
            hCheckForm hc = new hCheckForm();
            hc.Show();
        }
        [CommandMethod("Plabel")]
        public void Plabel()
        {
            // Getting list of Xrefs and starting form
            PlabelsForm pf = new PlabelsForm();
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            for (int i = 1; i < XrGraph.NumNodes; i++)
            {
                XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                pf.Xrefselect.Items.Add(XrNode.Name);
            }
            pf.Show();
        }
        //Function to check if layer exist and create if not
        public void LayerCheck(Transaction tr, string layer, Color color, double lw, Boolean isPlottable)
        {
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            if (!lt.Has(layer))
            {
                var newLayer = new LayerTableRecord
                {
                    Name = layer,
                    Color = color,
                    LineWeight = (LineWeight)(lw*100),
                    IsPlottable = isPlottable
                };

                lt.UpgradeOpen();
                lt.Add(newLayer);
                tr.AddNewlyCreatedDBObject(newLayer, true);
            }
        }
        //Function finding intersection region of 2 polylines
        public Region regInt(Polyline pl1, Polyline pl2)
        {
            DBObjectCollection c1 = new DBObjectCollection();
            DBObjectCollection c2 = new DBObjectCollection();
            c1.Add(pl1);
            c2.Add(pl2);
            var r1 = Region.CreateFromCurves(c1);
            var r2 = Region.CreateFromCurves(c2);
            if (r1 != null && r2 != null && r1.Count != 0 && r2.Count != 0)
            {
                Region r11 = r1.Cast<Region>().First(); //region from first polyline
                Region r21 = r2.Cast<Region>().First(); // region from second polyline
                r11.BooleanOperation(BooleanOperationType.BoolIntersect, r21); // Creating intersection in first one
                return r11;
            }
            return null;
        }
        //Function to check hatches intersections
        public void HatInt()
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                using (DocumentLock acLckDoc = doc.LockDocument())
                {
                    var blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                    var btr = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord; // FOrWrite, because we will add new objects
                    List<Polyline> plines = new List<Polyline>(); // list of hatch boundaries
                    //Finding intersecting Hatches
                    foreach (ObjectId objectId in btr)
                    {
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = tr.GetObject(objectId, OpenMode.ForRead) as Hatch;
                            for (int i = 0; i < laylistHatch.Length; i++)
                            {
                                if ((hat.Layer == laylistHatch[i]) && hat != null)
                                {
                                    // can't instersec hatches, get polyline and intersect them or just get curves for each loop and intersect them??
                                    Plane plane = hat.GetPlane();
                                    int nLoops = hat.NumberOfLoops;
                                    for (int k = 0; k < nLoops; k++)
                                    {
                                        HatchLoop loop = hat.GetLoopAt(k);
                                        var tmp = SelfIntArea(loop, plane); // getting polyline boundary of a hatch
                                        if (tmp != null)
                                        {
                                            plines.Add(tmp);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    int sCount = 1; // starting number for second for, so we won't repeat intersections we already did
                    for (int i = 0; i < plines.Count; i++)
                    {
                        for (int j = sCount; j < plines.Count - 1; j++) // we don't need to intersect last one
                        {
                            Region aReg = regInt(plines[i], plines[j]);
                            if (aReg != null && aReg.Area != 0)
                            {
                                // checking if temporary layer exist, if not - creating it.
                                LayerCheck(tr, tempLayer, tempLayerColor, tempLayerLineWeight, tempLayerPrintable);
                                aReg.Layer = tempLayer;
                                // Adding region to modelspace
                                btr.AppendEntity(aReg);
                                tr.AddNewlyCreatedDBObject(aReg, true);
                            }
                            else
                            {
                                aReg.Dispose();
                            }
                        }
                        sCount++; // adding one so it would start intersecting from next one
                    }
                    tr.Commit();
                }
            }
        }
        //Function to clear temporary geometry
        public void ClearTemp()
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                using (DocumentLock acLckDoc = doc.LockDocument())
                {
                    var blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                    var btr = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                    foreach (ObjectId objectId in btr)
                    {
                        if (objectId.ObjectClass == rxClassRegion) // Checking for regions only
                        {
                            var obj = tr.GetObject(objectId, OpenMode.ForWrite) as Region;
                            if (obj.Layer == tempLayer) // Checking for temporary layer
                            {
                                obj.Erase();
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }
        //Function to check for rare cases then it isn't self-intersecting, but won't let you make region
        public void hCheck()
        {
            List<ObjectId> errors = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                using (DocumentLock acLckDoc = doc.LockDocument())
                {
                    var blockTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead, false) as BlockTable;
                    var btr = tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                    int errCount = 0;
                    foreach (ObjectId objectId in btr)
                    {
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = tr.GetObject(objectId, OpenMode.ForRead) as Hatch;
                            for (int i = 0; i < laylistHatch.Length; i++)
                            {
                                if ((hat.Layer == laylistHatch[i]) && hat != null) //Getting all hatches on correct layer
                                {
                                    Plane plane = hat.GetPlane();
                                    int nLoops = hat.NumberOfLoops;
                                    for (int k = 0; k < nLoops; k++)
                                    {
                                        HatchLoop loop = hat.GetLoopAt(k);
                                        if (SelfIntArea(loop, plane) == null)
                                        {
                                            errors.Add(objectId);
                                            errCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (errCount == 0)
                    {
                        ed.WriteMessage("Проблемных штриховок не найдено \n "); // if all hatches give back polylines
                    }
                    else
                    {
                        ed.WriteMessage($"Выделено {errCount} проблемных штриховок \n ");
                        ed.SetImpliedSelection(errors.ToArray()); // selecting bad hatches
                        ed.SelectImplied();
                    }
                }
                tr.Commit();
            }
        }
        //Function that labels hatches based on layer
        public void DoPlabel(string X, string[] a)
        {
            string selectedOSN = X;
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
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
                    //Getting BlockTableRecord for main document, ForWritem because wwe will add labels
                    var blocktableRecord = trans.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false) as BlockTableRecord;
                    //Getting BlockTableRecord for xRef ForRead, we won't change it
                    var blocktableRecordOSN = trans.GetObject(OSNOVA.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    foreach (ObjectId objectId in blocktableRecordOSN)
                    {
                        // getting all hatches on layers
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = trans.GetObject(objectId, OpenMode.ForRead) as Hatch;
                            for (int i = 1; i < laylistHatch.Length; i++) // From one, because we don't need label for first one
                            {
                                if ((hat.Layer == selectedOSN + "|" + laylistHatch[i]) && hat != null) // xRef name + | + Layername to find correct object.
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
                                        leader.Layer = pLabelLayer;
                                        MText mText = new MText();
                                        mText.SetDatabaseDefaults();
                                        mText.Width = 0.675;
                                        mText.Height = 1.25;
                                        mText.TextHeight = 1.25;
                                        mText.SetContentsRtf(a[i-1]);
                                        mText.Location = new Point3d(center.X + 2, center.Y + 2, center.Z);
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
                                        ed.WriteMessage("Ошибки, проверьте штриховки \n");
                                    }
                                }
                            }

                        }
                    }
                    trans.Commit();
                }
            }
        }
        // Function to get all blocks on a Layer, including ones in array
        public List<Point3d> GetBlocksPosition(BlockTableRecord blocktablerecord, Transaction trans, string LayerName)
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
                    
                    var array = AssocArray.GetAssociativeArray(objectId);

                    var arrayParamsPa = array.GetParameters() as AssocArrayPathParameters;
                    var arrayParamsRe = array.GetParameters() as AssocArrayRectangularParameters;
                    var arrayParamsPo = array.GetParameters() as AssocArrayPolarParameters;


                    if (arrayParamsPa != null)
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
                                            string sourceLayer;
                                            using (BlockReference blist = (BlockReference)array.SourceEntities[0].Open(OpenMode.ForRead))
                                            {
                                                sourceLayer = blist.Layer;
                                            }
                                            if (sourceLayer == LayerName)
                                            {
                                                pointsList.Add(bRef.Position);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (arrayParamsRe != null)
                    {
                        using (BlockReference bRef = (BlockReference)objectId.Open(OpenMode.ForRead))
                        {
                            string sourceLayer;
                            using (BlockReference blist = (BlockReference)array.SourceEntities[0].Open(OpenMode.ForRead))
                            {
                                sourceLayer = blist.Layer;
                            }
                            if (sourceLayer == LayerName)
                            {
                                var basePt = bRef.Position;
                                var locators = array.getItems(true);
                                foreach (var loc in locators)
                                {
                                    
                                    var matrix = array.GetItemTransform(loc);
                                    pointsList.Add(basePt.TransformBy(matrix));
                                }
                            }
                        }
                    }
                    if (arrayParamsPo != null)
                    {
                        using (BlockReference bRef = (BlockReference)objectId.Open(OpenMode.ForRead))
                        {
                            string sourceLayer;
                            using (BlockReference blist = (BlockReference)array.SourceEntities[0].Open(OpenMode.ForRead))
                            {
                                sourceLayer = blist.Layer;
                            }
                            if (sourceLayer == LayerName)
                            {
                                var basePt = bRef.Position;
                                double[] mat1 = { 1.0, 0.0, 0.0, basePt.X, 0.0, 1.0, 0.0, basePt.Y, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };
                                double[] mat2 = { 1.0, 0.0, 0.0, -basePt.X , 0.0, 1.0, 0.0, -basePt.Y, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };
                                Matrix3d mat13d = new Matrix3d(mat1);
                                Matrix3d mat23d = new Matrix3d(mat2);
                                var basePtB = basePt.TransformBy(mat23d);
                                var locators = array.getItems(true);
                                foreach (var loc in locators)
                                {
                                    var matrix = array.GetItemTransform(loc);
                                    pointsList.Add(basePtB.TransformBy(matrix).TransformBy(mat13d));
                                }
                            }
                        }
                    }
                }
            }
            return pointsList;
        }

        //public double SelfIntArea(HatchLoop loop, Plane plane, Transaction tr, Editor ed, BlockTableRecord btr)
        public Polyline SelfIntArea(HatchLoop loop, Plane plane)
        {
            //Modified code from Rivilis Restore Hatch Boundary program
            Polyline looparea = new Polyline();
            if (loop.IsPolyline)
            {
                using (Polyline poly = new Polyline())
                {
                    int iVertex = 0;
                    foreach (BulgeVertex bv in loop.Polyline)
                    {
                        poly.AddVertexAt(iVertex++, bv.Vertex, bv.Bulge, 0.0, 0.0);
                    }
                    if (looparea != null)
                    {
                        looparea.JoinEntity(poly);
                    }
                    else
                    {
                        looparea = poly;
                    }
                }
            }
            else
            {
                foreach (Curve2d cv in loop.Curves)
                {
                    LineSegment2d line2d = cv as LineSegment2d;
                    CircularArc2d arc2d = cv as CircularArc2d;
                    EllipticalArc2d ellipse2d = cv as EllipticalArc2d;
                    NurbCurve2d spline2d = cv as NurbCurve2d;
                    if (line2d != null)
                    {
                        using (Line ent = new Line())
                        {
                            try
                            {
                                ent.StartPoint = new Point3d(plane, line2d.StartPoint);
                                ent.EndPoint = new Point3d(plane, line2d.EndPoint);
                                looparea.JoinEntity(ent);
                            }
                            catch
                            {
                                looparea.AddVertexAt(0, line2d.StartPoint, 0, 0, 0);
                                looparea.AddVertexAt(1, line2d.EndPoint, 0, 0, 0);
                            }

                        }
                    }
                    else if (arc2d != null)
                    {
                        try
                        {
                            if (arc2d.IsClosed() || Math.Abs(arc2d.EndAngle - arc2d.StartAngle) < 1e-5)
                            {
                                using (Circle ent = new Circle(new Point3d(plane, arc2d.Center), plane.Normal, arc2d.Radius))
                                {
                                    looparea.JoinEntity(ent);
                                }
                            }
                            else
                            {
                                if (arc2d.IsClockWise)
                                {
                                    arc2d = arc2d.GetReverseParameterCurve() as CircularArc2d;
                                }
                                double angle = new Vector3d(plane, arc2d.ReferenceVector).AngleOnPlane(plane);
                                double startAngle = arc2d.StartAngle + angle;
                                double endAngle = arc2d.EndAngle + angle;
                                using (Arc ent = new Arc(new Point3d(plane, arc2d.Center), plane.Normal, arc2d.Radius, startAngle, endAngle))
                                {
                                    looparea.JoinEntity(ent);
                                }
                            }
                        }
                        catch
                        {
                            // Calculating Bulge
                            double deltaAng = arc2d.EndAngle - arc2d.StartAngle;
                            if (deltaAng < 0)
                                deltaAng += 2 * Math.PI;
                            double GetArcBulge = Math.Tan(deltaAng * 0.25);
                            //Adding first arc to polyline
                            looparea.AddVertexAt(0, new Point2d(arc2d.StartPoint.X, arc2d.StartPoint.Y), GetArcBulge, 0, 0);
                            looparea.AddVertexAt(1, new Point2d(arc2d.EndPoint.X, arc2d.EndPoint.Y), 0, 0, 0);
                        }
                    }
                    else if (ellipse2d != null)
                    {
                        using (Ellipse ent = new Ellipse(new Point3d(plane, ellipse2d.Center), plane.Normal, new Vector3d(plane, ellipse2d.MajorAxis) * ellipse2d.MajorRadius, ellipse2d.MinorRadius / ellipse2d.MajorRadius, ellipse2d.StartAngle, ellipse2d.EndAngle))
                        {
                            ent.GetType().InvokeMember("StartParam", BindingFlags.SetProperty, null, ent, new object[] { ellipse2d.StartAngle });
                            ent.GetType().InvokeMember("EndParam", BindingFlags.SetProperty, null, ent, new object[] { ellipse2d.EndAngle });

                            looparea.JoinEntity(ent);
                        }
                    }
                    else if (spline2d != null)
                    {
                        if (spline2d.HasFitData)
                        {
                            NurbCurve2dFitData n2fd = spline2d.FitData;
                            using (Point3dCollection p3ds = new Point3dCollection())
                            {
                                foreach (Point2d p in n2fd.FitPoints) p3ds.Add(new Point3d(plane, p));
                                using (Spline ent = new Spline(p3ds, new Vector3d(plane, n2fd.StartTangent), new Vector3d(plane, n2fd.EndTangent), /* n2fd.KnotParam, */  n2fd.Degree, n2fd.FitTolerance.EqualPoint))
                                {
                                    looparea.JoinEntity(ent);
                                }
                            }
                        }
                        else
                        {
                            NurbCurve2dData n2fd = spline2d.DefinitionData;
                            using (Point3dCollection p3ds = new Point3dCollection())
                            {
                                DoubleCollection knots = new DoubleCollection(n2fd.Knots.Count);
                                foreach (Point2d p in n2fd.ControlPoints) p3ds.Add(new Point3d(plane, p));
                                foreach (double k in n2fd.Knots) knots.Add(k);
                                double period = 0;
                                using (Spline ent = new Spline(n2fd.Degree, n2fd.Rational, spline2d.IsClosed(), spline2d.IsPeriodic(out period), p3ds, knots, n2fd.Weights, n2fd.Knots.Tolerance, n2fd.Knots.Tolerance))
                                {
                                    looparea.JoinEntity(ent);
                                }
                            }
                        }
                    }
                }
            }

            return looparea;
        }
        //Function to group elements based on distance between 2 of them
        public List<List<Point3d>> GroupO(List<Point3d> Points, double Distance)
        {
            // Calclulate distance between points
            List<List<double>> Dist = new List<List<double>>();
            for (int i = 0; i < Points.Count; i++)
            {
                Dist.Add(new List<double>());

                for (int j = 0; j < Points.Count; j++)
                {
                    Dist[i].Add(Points[i].DistanceTo(Points[j]));
                }
            }
            // Making lists of close objects
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
            // Making groups
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
                    if (temp == temp2) // If we didn't add new objects we go to next one
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
            // Clean group
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
            // Change back to points
            List<List<Point3d>> GroupPoints = new List<List<Point3d>>();
            for (int i = 0; i < GroupsClean.Count; i++)
            {
                GroupPoints.Add(new List<Point3d>());
                foreach (int j in GroupsClean[i])
                {
                    GroupPoints[i].Add(Points[j]);
                }
            }
            return GroupPoints;
        }
        //Function that creates Mleaders for greenery
        public void CreateMleaderO(List<List<Point3d>> coords, Transaction trans, BlockTable BT, BlockTableRecord BR, Database db, string Tname, double rotAngle)
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
                leader.Layer = oLabelLayer;
                leader.BlockContentId = BT["Выноска_озеленение"];
                leader.BlockPosition = new Point3d(coords[i][0].X + 5, coords[i][0].Y + 5, 0);
                leader.BlockRotation = rotAngle;
                int idx = leader.AddLeaderLine(coords[i][0]);
                // adding leader points for each element
                // TODO: temporary solution, need sorting algorithm for better performance.
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
        // Function that adds label to greenery
        public void DoOlabel(string Tp, string Bp, float Td, float Bd)
        {
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
                    //TODO fix array coordinates
                    //Getting insertion points from blocks on defined layers
                    TreePoints = GetBlocksPosition(blocktableRecord, trans, laylistBlock[0]);
                    BushPoints = GetBlocksPosition(blocktableRecord, trans, laylistBlock[1]);
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
        // Function checks if hatch has Area and selects hatches that don't
        public void CheckHatch()
        {
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
                        ed.WriteMessage("Самопересечений не найдено \n "); // if all hatches have Area
                    }
                    else
                    {
                        ed.WriteMessage($"Выделено {i} самопересекающихся штриховок \n ");
                        ed.SetImpliedSelection(errors.ToArray()); 
                        ed.SelectImplied(); // selecting bad hatches
                    }
                    trans.Commit();
                }
            }
        }
        //Counting Volumes
        public void DoCount(string X, int a, int b, int c)
        {
            string selectedOSN = X;
            XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
            ObjectId idh = db.GetObjectId(false, new Handle(th), 0); // Getting table object 
            ObjectId idp = db.GetObjectId(false, new Handle(tp), 0);
            ObjectId idb = db.GetObjectId(false, new Handle(tb), 0);
            double[] hatchValues = new double[14];
            string[] hatchErrors = new string[14];
            double[] plineValues = new double[9];
            string[] plineErrors = new string[9];
            double[] blockValues = new double[10];
            string[] blockErrors = new string[10];
            int[] PL_count = { 1, 1, a, b, c, 1, 1 };

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
                        if (objectId.ObjectClass == rxClassPoly)
                        {
                            var poly = trans.GetObject(objectId, OpenMode.ForRead) as Polyline;
                            for (int i = 0; i < laylistPL.Length; i++)
                            {
                                if ((poly.Layer == selectedOSN + "|" + laylistPL[i]) && poly != null)
                                {
                                    if (i <= 1) // for area
                                    {
                                        // Getting from AcadObject for cases of selfintersection
                                        object pl = poly.AcadObject;
                                        plineValues[i] = (double)pl.GetType().InvokeMember("Area", BindingFlags.GetProperty, null, pl, null);
                                    }
                                    else
                                    {
                                        plineValues[i] += poly.GetDistanceAtParameter(poly.EndParam) / PL_count[i];
                                    }
                                }
                                // TODO: add check for values and fill errors
                            }

                        }
                        if (objectId.ObjectClass == rxClassHatch)
                        {
                            var hat = trans.GetObject(objectId, OpenMode.ForRead) as Hatch;
                            for (int i = 0; i < laylistHatch.Length-1; i++)
                            {
                                if ((hat.Layer == selectedOSN + "|" + laylistHatch[i]) && hat != null)
                                {
                                    try
                                    {
                                        hatchValues[i] += hat.Area;
                                    }
                                    catch
                                    {
                                        hatchErrors[i] = "Самопересечение";
                                        //changing to count self-intersecting hatches
                                        Plane plane = hat.GetPlane();
                                        int nLoops = hat.NumberOfLoops;
                                        double corArea = 0.0;
                                        for (int k = 0; k < nLoops; k++)
                                        {
                                            HatchLoop loop = hat.GetLoopAt(k);
                                            HatchLoopTypes hlt = hat.LoopTypeAt(k);
                                            Polyline looparea = SelfIntArea(loop, plane);
                                            // Can get correct value from AcadObject, but need to add in first
                                            blocktableRecord.AppendEntity(looparea);
                                            trans.AddNewlyCreatedDBObject(looparea, true);
                                            object pl = looparea.AcadObject;
                                            var corrval = (double)pl.GetType().InvokeMember("Area", BindingFlags.GetProperty, null, pl, null);
                                            looparea.Erase(); // Erasing polyline we just created
                                            if (hlt == HatchLoopTypes.External) // External loops with +
                                            {
                                                corArea += corrval;
                                            }
                                            else // Internal with -
                                            {
                                                corArea -= corrval;
                                            }
                                        }
                                        hatchValues[i] += corArea;
                                    }
                                }

                            }

                        }
                    }
                    // Rounding curbs
                    for (int i = 2; i < plineValues.Length; i++)
                    {
                        plineValues[i] = Math.Ceiling(plineValues[i]);
                    }
                    // Counting blocks
                    blockValues[0] = GetBlocksPosition(blocktableRecord, trans, laylistBlock[0]).Count;
                    blockValues[1] = GetBlocksPosition(blocktableRecord, trans, laylistBlock[1]).Count;
                    // Checking hatches
                    for (int i = 0; i < hatchValues.Length; i++)
                    {
                        if (hatchValues[i] == 0)
                        {
                            hatchErrors[i] = "Нет Элементов";
                        }
                        if (hatchValues[i] > 0 && hatchErrors[i] != "Самопересечение")
                        {
                            hatchErrors[i] = "Всё в порядке";
                        }
                    }
                    // Checking polylines
                    for (int i = 0; i < plineValues.Length; i++)
                    {
                        if (plineValues[i] == 0)
                        {
                            plineErrors[i] = "Нет Элементов";
                        }
                        if (plineValues[i] > 0)
                        {
                            plineErrors[i] = "Всё в порядке";
                        }
                    }
                    // Checking blocks
                    for (int i = 0; i < blockValues.Length; i++)
                    {
                        if (blockValues[i] == 0)
                        {
                            blockErrors[i] = "Нет Элементов";
                        }
                        if (blockValues[i] > 0)
                        {
                            blockErrors[i] = "Всё в порядке";
                        }
                    }
                    // Filling hatch table
                    var tablH = trans.GetObject(idh, OpenMode.ForWrite) as Table;
                    for (int i = 0; i < hatchValues.Length; i++)
                    {
                        tablH.SetTextString(2 + i, 1, hatchValues[i].ToString("0.##"));
                        tablH.SetTextString(2 + i, 2, hatchErrors[i]);
                    }
                    // Filling polyline table
                    var tablP = trans.GetObject(idp, OpenMode.ForWrite) as Table;
                    for (int i = 0; i < plineValues.Length; i++)
                    {
                        tablP.SetTextString(2 + i, 1, plineValues[i].ToString("0.##"));
                        tablP.SetTextString(2 + i, 2, plineErrors[i]);
                    }
                    // Filling block table
                    var tablB = trans.GetObject(idb, OpenMode.ForWrite) as Table;
                    for (int i = 0; i < blockValues.Length; i++)
                    {
                        tablB.SetTextString(2 + i, 1, blockValues[i].ToString("0.##"));
                        tablB.SetTextString(2 + i, 2, blockErrors[i]);
                    }
                    trans.Commit();
                }
            }
        }

    }

}
