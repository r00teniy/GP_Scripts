// (C) Copyright 2022 by r00teniy 
//
using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(P_Volumes.MyCommands))]

namespace P_Volumes
{
    public class MyCommands
    {
        [CommandMethod("CountVol")]
        public void CountVol()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            XrefGraphNode OSNOVA;
            RXClass rxClassPoly = RXClass.GetClass(typeof(Polyline));
            RXClass rxClassHatch = RXClass.GetClass(typeof(Hatch));
            RXClass rxClassBlockRef = RXClass.GetClass(typeof(BlockReference));
            double[] TableValues = new double[19];
            string[] ErrorValues = new string[19];
            string[] laylistPL = { "Основа|31_Борт_100.30.15", "Основа|32_Борт_100.20.8", "Основа|33_Борт_100.45.18", "Основа|34_Борт_Металл", "Основа|35_Борт_Пластик" };
            int[] PL_count = { 2, 1, 2, 1, 1 };
            string[] laylistH = { "Основа|41_Покр_Проезд", "Основа|49_Покр_Щебеночный_проезд", "Основа|42_Покр_Тротуар", "Основа|42_Покр_Тротуар_Пожарный", "Основа|43_Покр_Отмостка", "Основа|44_Покр_Детская_площадка", "Основа|45_Покр_Спортивная_площадка", "Основа|46_Покр_Площадка_отдыха", "Основа|47_Покр_Хоз_площадка", "Основа|48_Покр_Площадка_для_собак", "Основа|51_Газон", "Основа|52_Газон_пожарный" };
            string[] laylistBL = { "51_Кустарники", "52_Деревья" };
            long ln = Convert.ToInt64("774A",16);
            Handle hn = new Handle(ln);
            ObjectId id = db.GetObjectId(false, hn, 0);
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                XrefGraph XrGraph = db.GetHostDwgXrefGraph(false);
                OSNOVA = XrGraph.GetXrefNode(0);
                for (int i = 1; i < XrGraph.NumNodes; i++)
                {
                    XrefGraphNode XrNode = XrGraph.GetXrefNode(i);
                    if (XrNode.Name == "Основа")
                    {
                        OSNOVA = XrNode;
                    }
                }
                var blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForWrite, false) as BlockTable;
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
                            if ((poly.Layer == laylistPL[i]) && poly != null)
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
                            if ((hat.Layer == laylistH[i]) && hat != null)
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
                                TableValues[12+i] += 1;
                                
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
