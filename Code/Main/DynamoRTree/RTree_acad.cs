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
        public static RTree<ObjectId> CreateRTreeByRTreeRectangles (Dictionary<string, object> IdsAndRectangles)
        {
            RTree<ObjectId> new_tree = new RTree<ObjectId>();
            List<ObjectId> Objects_Id = (List<ObjectId>)IdsAndRectangles["objects_id"];
            List<RTree.Rectangle> Rectangle_Collection = (List<RTree.Rectangle>)IdsAndRectangles["objects_RtreeRectangle"];
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
        //[MultiReturn(new[] { "objects_id", "objects_RtreeRectangle" })]
        public static Dictionary<string,object> GetRTReeRectangleByObjects (List<ObjectId> object_id_list)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
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

    }
}
