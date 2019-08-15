using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace GxtCadSolutions
{
	public class Commands
	{
		Document document = Application.DocumentManager.MdiActiveDocument;
		Database database = Application.DocumentManager.MdiActiveDocument.Database;

		[CommandMethod("EPD")]
		public void ProfileCreator()
		{
			//prompt the user for running line
			PromptEntityOptions promptEntity = new PromptEntityOptions("\nPlease Select Running Line: ");
			promptEntity.SetRejectMessage("Line selected is not a polyline!!");
			promptEntity.AddAllowedClass(typeof(Polyline), true);

			PromptEntityResult entityResult = document.Editor.GetEntity(promptEntity);

			if (entityResult.Status != PromptStatus.OK)
			{
				document.Editor.WriteMessage("Error: Please select a Polyline.");
				return;
			}

			Transaction tr = database.TransactionManager.StartTransaction();
			//Save the polyline
			Polyline runningLine = null;
			using (tr)
			{
				runningLine =tr.GetObject(entityResult.ObjectId, OpenMode.ForRead) as Polyline;
			}

			//create the grid for the profile
			Grid grid = null;
			try
			{
				grid = new Grid(4, runningLine.Length);
				grid.Draw();
			}
			catch (Autodesk.AutoCAD.Runtime.Exception ex)
			{
				document.Editor.WriteMessage("error creating grid;" + ex.Message);
			}

			if (grid != null)
			{
				grid.SaveGrid();
			}

			//create the Ground Line
			Polyline groundLine = new Polyline();
			Transaction trans = database.TransactionManager.StartTransaction();

			Point3d pt = grid.InsertionPoint;
			Vector3d vector = pt.GetVectorTo(new Point3d(25, 80, 0));
			pt.TransformBy(Matrix3d.Displacement(vector));

			using (trans)
			{
				BlockTable bt = trans.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
				BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

				Polyline gradeLine = new Polyline();
				gradeLine.AddVertexAt(0, new Point2d((pt.X + 300), pt.Y), 0, 1, 1);
				gradeLine.AddVertexAt(1, new Point2d((pt.X + 300), pt.Y), 0, 1, 1);

				btr.AppendEntity(gradeLine);
				trans.AddNewlyCreatedDBObject(gradeLine, true);

				trans.Commit();
			}
		}

	}
}
