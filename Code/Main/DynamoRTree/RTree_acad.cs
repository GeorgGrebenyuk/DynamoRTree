using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.DataShortcuts;
using Autodesk.DesignScript.Runtime;
using ds_g = Autodesk.DesignScript.Geometry;
using System.Globalization;
using RTree;

namespace DynamoRTree
{
    public class RTree_acad
    {

        private RTree_acad() { }
        //public static RTree<ObjectId> GlobalTree;
        /// <summary>
        /// Implementation of RTree object (by drawing's object's if and theit BoundingBoxes)
        /// </summary>
        /// <param name="Rectangle_Collection"></param>
        /// <returns>Rtree instance></returns>
        public static RTree<ObjectId> CreateRTreeByRTreeRectangles (List<ObjectId> Objects_Id, List<RTree.Rectangle> Rectangle_Collection)
        {
            RTree<ObjectId> new_tree = new RTree<ObjectId>();
            //List<ObjectId> Objects_Id = (List<ObjectId>)IdsAndRectangles["objects_id"];
            //List<RTree.Rectangle> Rectangle_Collection = (List<RTree.Rectangle>)IdsAndRectangles["objects_RtreeRectangle"];
            for (int i1 = 0; i1 < Objects_Id.Count; i1++) 
            {
                new_tree.Add(Rectangle_Collection[i1], Objects_Id[i1]);
            }
            //GlobalTree = new_tree;
            //new_tree = null;
            return new_tree;
        }
        /// <summary>
        /// Getting RTree.Rectangle by objects's geometry (Autodesk.AutoCAD.DatabaseServices.DBObject) if them contains non null Bounds
        /// </summary>
        /// <param name="object_id_list">List with Autocad's DBObject id</param>
        /// <returns>List with RTree.Rectangle</returns>
        [MultiReturn(new[] { "objects_id", "objects_RtreeRectangle" })]
        public static Dictionary<string,object> GetRTReeRectangleByObjects (Autodesk.AutoCAD.DynamoNodes.Document doc_dyn, List<ObjectId> object_id_list)
        {
            //Document doc = Application.DocumentManager.MdiActiveDocument;
            Document doc = doc_dyn.AcDocument;
            Database db = doc.Database;
            //Dictionary<string, RTree.Rectangle> rect_list = new Dictionary<string, Rectangle>();
            List<ObjectId> objects_id_temp = new List<ObjectId>();
            List<RTree.Rectangle> objects_RtreeRectangle_temp = new List<RTree.Rectangle>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId object_id in object_id_list)
                {
                    Autodesk.AutoCAD.DatabaseServices.DBObject ad_obj = tr.GetObject(object_id, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.DBObject;
                    if (ad_obj.Bounds != null) //Редкая но возможная ситуация
                    {
                        Rectangle line_bbox = new Rectangle(new float[3] { (float)ad_obj.Bounds.Value.MinPoint.X, (float)ad_obj.Bounds.Value.MinPoint.Y, 0f },
                        new float[3] { (float)ad_obj.Bounds.Value.MaxPoint.X, (float)ad_obj.Bounds.Value.MaxPoint.Y, 0f });
                        if (!objects_id_temp.Contains(object_id)) 
                        {
                            objects_id_temp.Add(object_id);
                            objects_RtreeRectangle_temp.Add(line_bbox);
                        }
                    }
                }
                tr.Commit();
            }
            return new Dictionary<string, object>{
                {"objects_id",objects_id_temp },
                {"objects_RtreeRectangle",objects_RtreeRectangle_temp }
            };
            
        }
        /// <summary>
        /// Get RTree.Point by Autodesk Dynamo (Autodesk.DesignScript.Geometry.Point) for internal methods
        /// </summary>
        /// <param name="DynamoPoint">Dynamo's point</param>
        /// <returns>RTree.Point</returns>
        public static RTree.Point GetRTreePointByDynamoPoint (ds_g.Point DynamoPoint)
        {
            return new RTree.Point((float)DynamoPoint.X, (float)DynamoPoint.Y, (float)DynamoPoint.Z);
        }
        /// <summary>
        /// Create RTree.Rectangle by Dynamo's point with same sides (size = buffer's value)
        /// </summary>
        /// <param name="OnePoint">Double's array with coords</param>
        /// <param name="buffer">Size of each side of RTree.Rectangle</param>
        /// <returns>RTree.Rectangle</returns>
        public static RTree.Rectangle GetRTReeRectangleByPoint (double [] OnePoint, float buffer = 1.0f)
        {
            Rectangle line_bbox = new Rectangle(new float[3] { (float)OnePoint[0] - buffer, (float)OnePoint[1] - buffer, 0f },
            new float[3] { (float)OnePoint[0] + buffer, (float)OnePoint[1] + buffer, 0f });
            return line_bbox;
        }
        /// <summary>
        /// Get double's array with coords by Dynamo's point
        /// </summary>
        /// <param name="DynamoPoint">Dynamo's point</param>
        /// <returns>Double's array with coords</returns>
        public static double [] GetCoordsByDynamoPoint (ds_g.Point DynamoPoint)
        {
            return new double[3] { DynamoPoint.X, DynamoPoint.Y, DynamoPoint.Z };
        }
        /// <summary>
        /// Getting List with ObjectId's for objects, that placed near input's object
        /// </summary>
        /// <param name="tree">RTree of input object</param>
        /// <param name="object_point">RTree.Point point in object's place (get RTree.Point by node GetRTreePointByDynamoPoint)</param>
        /// <param name="search_radius">Search distance to find object</param>
        /// <returns>List with ObjectId</returns>
        public static List<ObjectId> GetObgects_Nearest (RTree<ObjectId> tree, RTree.Point object_point, float search_radius)
        {
            return tree.Nearest(object_point, search_radius);
        }
        /// <summary>
        /// Getting List with ObjectId's for objects, which Bounding Boxes are intersected by current object's RTree.Rectangle (it's Rectangle)
        /// </summary>
        /// <param name="tree">RTree</param>
        /// <param name="object_rectangle">RTree.Rectangle of input object</param>
        /// <returns>List with ObjectId</returns>
        public static List<ObjectId> GetObgects_Intersects (RTree<ObjectId> tree, RTree.Rectangle object_rectangle)
        {
            return tree.Intersects(object_rectangle);
        }
        /// <summary>
        /// Getting List with ObjectId's for objects, which Bounding Boxes are fit into objects's RTree.Rectangle
        /// </summary>
        /// <param name="tree">RTree</param>
        /// <param name="object_rectangle">RTree.Rectangle of input object</param>
        /// <returns>List with ObjectId</returns>
        public static List<ObjectId> GetObgects_Contains(RTree<ObjectId> tree, RTree.Rectangle object_rectangle)
        {
            return tree.Contains(object_rectangle);
        }

        /// <summary>
        /// Auxilary node that delete or choosing drawing's linear objects in selected area by each (Radius value) 
        /// non more than MaxLength's value and (if SearchMode =0) which length is equal at least one of value in LineLength's list or (if SearchMode =1)
        /// which length is more than LineLengt[0] and smaller than LineLengt[1]
        /// </summary>
        /// <param name="obj_group">List with object's id</param>
        /// <param name="tree">RTree</param>
        /// <param name="LineLength">Double array with at least two numbers</param>
        /// <param name="MaxLength">Maximum length of line</param>
        /// <param name="Radius">Value of searching's value</param>
        /// <param name="SearchMode">Mode for work with LineLength, read node's description</param>
        /// <param name="NeedDeleteObjects">Boolean, if true -- selected objects will be removed</param>
        public static List<ObjectId> GetObjectsByCirclesSearching(Autodesk.AutoCAD.DynamoNodes.Document doc_dyn, List<ObjectId> obj_group, RTree<ObjectId> tree, List<double> LineLength, double MaxLength, double Radius, int SearchMode = 0, bool NeedDeleteObjects = false)
        {
            //Document doc = Application.DocumentManager.MdiActiveDocument;
            Document doc = doc_dyn.AcDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            int debug_counter1 = 0;

            //Список для удаления объектов
            List<ObjectId> lines_for_deleting = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId line_id in obj_group)
                {
                    Autodesk.AutoCAD.DatabaseServices.Line OneLine = tr.GetObject(line_id, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Line;
                    bool IsObjectValid = false;
                    double LineLen = Math.Round(OneLine.Length, 8);
                    if (SearchMode == 0)
                    {
                        if (LineLength.Contains(LineLen)) IsObjectValid = true;
                    }
                    else if (SearchMode == 1)
                    {
                        if (LineLen >= LineLength[0] && LineLen <= LineLength[1]) IsObjectValid = true;
                    }
                    if (IsObjectValid)
                    {
                        Point3d line_start = OneLine.StartPoint; Point3d line_end = OneLine.EndPoint;
                        Point3d line_center = new Point3d(new double[3] { (line_start.X + line_end.X) / 2.0, (line_start.Y + line_end.Y) / 2.0, 0 });
                        double[] pnt = new double[3] { line_center.X, line_center.Y, 0 };

                        List<ObjectId> intersects_lines = RTree_acad.GetObgects_Intersects(tree, RTree_acad.GetRTReeRectangleByPoint(pnt, (float)Radius));
                        List<ObjectId> internal_lines = RTree_acad.GetObgects_Contains(tree, RTree_acad.GetRTReeRectangleByPoint(pnt, (float)Radius));
                        AddLines(intersects_lines); AddLines(internal_lines);
                        void AddLines(List<ObjectId> IndexedList)
                        {
                            foreach (ObjectId OneCode in IndexedList)
                            {
                                Autodesk.AutoCAD.DatabaseServices.Line OneLine2 = tr.GetObject(OneCode, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Line;
                                if (!lines_for_deleting.Contains(OneCode) && OneLine2.Length < 1) lines_for_deleting.Add(OneCode);
                            }
                        }

                        debug_counter1++;
                    }
                }

                tr.Commit();
            }
            if (NeedDeleteObjects == true && lines_for_deleting.Count > 0)
            {
                using (DocumentLock acDocLock = doc.LockDocument())
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        foreach (ObjectId line_id in lines_for_deleting)
                        {
                            Autodesk.AutoCAD.DatabaseServices.DBObject OneObject = tr.GetObject(line_id, OpenMode.ForWrite) as Autodesk.AutoCAD.DatabaseServices.DBObject;
                            OneObject.Erase(true);
                        }
                        tr.Commit();
                    }
                }

                return null;
            }
            else return lines_for_deleting;

        }

    }
}
