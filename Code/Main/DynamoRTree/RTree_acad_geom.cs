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
    public class RTree_acad_geom
    {
        private RTree_acad_geom() { }
        private static double GetLenByPoints(Point3d p1, Point3d p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
        public static ObjectId GetNearestEnityByObject (ObjectId current_line_id, ds_g.Point current_point, RTree <ObjectId> tree, double SearchRadius = 0.1)
        {
            RTree.Rectangle temp_rect = RTree_acad.GetRTReeRectangleByPoint(RTree_acad.GetCoordsByDynamoPoint(current_point), (float)SearchRadius);
            List<ObjectId> nearest_objects = RTree_acad.GetObgects_Intersects(tree, temp_rect);
            ObjectId nearest_line_id = ObjectId.Null;
            foreach (ObjectId obj_id in nearest_objects)
            {
                if (obj_id != current_line_id)
                {
                    nearest_line_id = obj_id;
                    break;
                }
            }
            return nearest_line_id;
        }
        [MultiReturn(new[] { "Text_id", "Text_Value", "EndLinePoint", "TextRotation"})]
        public static Dictionary<string, object> GetNearestTextByLine (Autodesk.AutoCAD.DynamoNodes.Document doc_dyn, ds_g.Point start_point, ObjectId current_line_id, RTree<ObjectId> tree_with_text, double SearchRadius = 2.0)
        {
            //Document doc = Application.DocumentManager.MdiActiveDocument;
            Document doc = doc_dyn.AcDocument;
            Database db = doc.Database;

            List<ObjectId> texts_id = new List<ObjectId>();
            string Text_data = null;
            ds_g.Point end_point = null;
            double TextRotationAngle = 0d;
            //
            if (current_line_id != ObjectId.Null)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Autodesk.AutoCAD.DatabaseServices.Line ad_obj = tr.GetObject(current_line_id, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.Line;
                    Point3d line_sp = ad_obj.StartPoint; Point3d line_ep = ad_obj.EndPoint;
                    double check1 = GetLenByPoints(line_sp, new Point3d(start_point.X, start_point.Y, 0.0));
                    if (check1 < 0.2) end_point = ds_g.Point.ByCoordinates(line_sp.X, line_sp.Y, 0.0);
                    else end_point = ds_g.Point.ByCoordinates(line_ep.X, line_ep.Y, 0.0);

                    Point3d center_point = new Point3d((line_sp.X + line_ep.X) / 2.0, (line_sp.Y + line_ep.Y) / 2.0, 0.0);
                    List<ObjectId> nearest_text = tree_with_text.Nearest(new RTree.Point((float)center_point.X, (float)center_point.Y, 0f), (float)SearchRadius);
                    if (nearest_text.Count != 0)
                    {
                        foreach (ObjectId OneText in nearest_text)
                        {
                            Autodesk.AutoCAD.DatabaseServices.DBText text = tr.GetObject(OneText, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.DBText;
                            TextRotationAngle = text.Rotation;
                            if (TextRotationAngle > 180) TextRotationAngle = 360 - TextRotationAngle;
                            Text_data += text.TextString + "\n";
                            texts_id.Add(OneText);
                        }
                    }
                    tr.Commit();
                }
            }
           
            return new Dictionary<string, object>{
                {"Text_id",texts_id },
                {"Text_Value",Text_data },
                {"EndLinePoint", end_point},
                {"TextRotation",TextRotationAngle }
            };
        }
    }
}
