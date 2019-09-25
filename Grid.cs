using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace GxtCadSolutions
{
	public class Grid
	{
		public int VerticalScale { get; set;}
		public double Length { get; set; }
		public Point3d LeftBottomPt { get; set; }
		public int HorizontalLineCount { get; set; }
		public DBObjectCollection GridObjCollection { get; set; }
		public Point3d InsertionPoint { get; set; }


		public Grid(int vScale, double l)
		{
			if (vScale == 0)
			{
				VerticalScale = 4;
			}
			else
			{
				VerticalScale = vScale;
			}

			this.Length = l + (50 - (l % 25));
			this.GridObjCollection = new DBObjectCollection();
		}

		public void Draw()
		{
			//TODO get rid of this
			HorizontalLineCount = 30;

			Document doc = Application.DocumentManager.MdiActiveDocument;

			//prompt user for insertion point
			PromptPointOptions prompt = new PromptPointOptions("\nSelect insertion point:");
			InsertionPoint = doc.Editor.GetPoint(prompt).Value;

			//Left bottom grid corner
			LeftBottomPt = new Point3d(InsertionPoint.X - 25, InsertionPoint.Y, InsertionPoint.Z);

			//Create ending point from Length
			Point3d endPoint = new Point3d(LeftBottomPt.X + this.Length, LeftBottomPt.Y, LeftBottomPt.Z);

			//create inital line to draw grid
			Line line = new Line(LeftBottomPt, endPoint);

			//Horizontal grid lines
			for (int i = 0; i <= HorizontalLineCount; i++)
			{
				if (i == 0)
					this.GridObjCollection.Add(line.GetOffsetCurves(0)[0]);

				if (i % 5 == 0)
				{
					line.Layer = "BORDER4";
				}
				else line.Layer = "BORDER2";
				
				this.GridObjCollection.Add(line.GetOffsetCurves(i * VerticalScale)[0]);
			}

			//verticle grid lines every 100 feet
			Point3d vpointBottom = new Point3d(LeftBottomPt.X, LeftBottomPt.Y, LeftBottomPt.Z);
			Line vline = new Line(vpointBottom, new Point3d(vpointBottom.X, vpointBottom.Y + (VerticalScale * HorizontalLineCount), vpointBottom.Z));
			
			for (int i = 0; i*5 <= (int)(this.Length); i++)
			{
				//every line at 25 intervals
				if ((i * 5) % 25 == 0)
				{
					vline.Layer = "BORDER4";
				}
				//every line at 5 intervals
				else vline.Layer = "BORDER2"; 
				
				this.GridObjCollection.Add(vline.GetOffsetCurves(-(i * 5))[0]);
			}

			PlaceGridElevationText();
			PlaceProfileStationText();
		}

		private void PlaceProfileStationText()
		{
			
			for (int i = 0; (i * 100) <= (Length - 50); i++)
			{
				MText m = new MText()
				{
					Attachment = AttachmentPoint.BottomCenter,
					Contents = FormatStation(i * 100),
					TextHeight = 2.5,
					Location = new Point3d(InsertionPoint.X + (i * 100), InsertionPoint.Y + (VerticalScale * HorizontalLineCount + 2), InsertionPoint.Z),
				};
				GridObjCollection.Add(m);
			}
		}

		public void PlaceGridElevationText()
		{
			MText mText = new MText()
			{
				TextHeight = 3.5,
				Location = new Point3d(LeftBottomPt.X - 5, LeftBottomPt.Y, LeftBottomPt.Z),
				Layer = "TEXT-2",
				Attachment = AttachmentPoint.MiddleRight
			};

			Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
			using (tr)
			{
				TextStyleTable txtBlockTable = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
				mText.TextStyleId = txtBlockTable["B"];
			}

			int i = -30;
			int x = 0;
			while (i < 5 )
			{
				MText mt = (MText)mText.Clone();
				mt.Contents = FormatGridElevataion(i + 5);
				mt.Location = new Point3d(mText.Location.X, mText.Location.Y + (VerticalScale * x * 5), mText.Location.Z);
				GridObjCollection.Add(mt);
				i += 5;
				x++;
			}

			i = -30;
			x = 0;
			MText mTextEnd = (MText)mText.Clone();
			mTextEnd.Location = new Point3d(LeftBottomPt.X + (Length + 5), LeftBottomPt.Y, LeftBottomPt.Z);

			while (i < 5)
			{
				MText mt = (MText)mTextEnd.Clone();
				mt.Contents = FormatGridElevataion(i + 5);
				mt.Location = new Point3d(mTextEnd.Location.X, mTextEnd.Location.Y + (VerticalScale * x * 5), mTextEnd.Location.Z);
				mt.Attachment = AttachmentPoint.MiddleLeft;
				GridObjCollection.Add(mt);
				i += 5;
				x++;
			}
		}

		public string FormatStation(int v)
		{
			string formattedStation = "";

			if (v.ToString().Length == 1)
			{
				formattedStation = "0+0" + v.ToString();
			}
			else if (v.ToString().Length == 2)
			{
				formattedStation = "0+" + v.ToString();
			}
			else
			{
				char[] array = v.ToString().ToCharArray();

				for (int i = 0; i < array.Length; i++)
				{
					if (array.Length - i == 2)
					{
						formattedStation += "+";
					}

					formattedStation += array[i];
				}
			}
			return formattedStation;
		}

		public string FormatGridElevataion(int e)
		{

			if (e == -5)
				return "-05";

			if (e.ToString().Length == 1)
				return "0" + e.ToString();

			return e.ToString();
		}

		public void SaveGrid()
		{
			Database db = Application.DocumentManager.MdiActiveDocument.Database;

			Transaction trans = db.TransactionManager.StartTransaction();

			using (trans)
			{
				BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
				BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

				foreach (Entity obj in this.GridObjCollection)
				{
					btr.AppendEntity(obj);
					trans.AddNewlyCreatedDBObject(obj, true);
				}

				trans.Commit();
			}
		}

	}
}