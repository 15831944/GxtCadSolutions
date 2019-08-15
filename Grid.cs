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

			Length = l;
			this.GridObjCollection = new DBObjectCollection();
		}

		public void Draw()
		{
			//TODO get rid of this
			HorizontalLineCount = 30;

			Document doc = Application.DocumentManager.MdiActiveDocument;

			//prompt user for insertion point
			PromptPointOptions prompt = new PromptPointOptions("Select insertion point:");
			InsertionPoint = doc.Editor.GetPoint(prompt).Value;

			//Create ending point from Length
			Point3d endPoint = new Point3d(InsertionPoint.X + Length, InsertionPoint.Y, InsertionPoint.Z);

			//create inital line to draw grid
			Line line = new Line(InsertionPoint, endPoint);

			//Horizontal grid lines
			for (int i = 0; i <= HorizontalLineCount; i++)
			{
				if (i == 0)
					this.GridObjCollection.Add(line.GetOffsetCurves(0)[0]);

				if (i % 5 == 0)
				{
					line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 255, 255);
					this.GridObjCollection.Add(line.GetOffsetCurves(i * VerticalScale)[0]);
				}
				else
				{
					line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(80, 80, 80);
					this.GridObjCollection.Add(line.GetOffsetCurves(i * VerticalScale)[0]);
				}
			}

			//verticle grid lines every 100 feet
			Point3d vpointBottom = new Point3d(InsertionPoint.X, InsertionPoint.Y, InsertionPoint.Z);
			Line vline = new Line(vpointBottom, new Point3d(vpointBottom.X, vpointBottom.Y + (VerticalScale * HorizontalLineCount), vpointBottom.Z));

			for (int i = 0; i <= (int)(this.Length / 100); i++)
			{
				this.GridObjCollection.Add(vline.GetOffsetCurves(-(i * 100))[0]);
			}

			PlaceGridElevationText();
			PlaceProfileStationText();
		}

		private void PlaceProfileStationText()
		{
			MText mText = new MText()
			{
				Contents = FormatStation(0),
				TextHeight = 2.5,
				Location = new Point3d(InsertionPoint.X, InsertionPoint.Y + (VerticalScale * HorizontalLineCount), InsertionPoint.Z),
				Attachment = AttachmentPoint.BottomCenter
			};


			GridObjCollection.Add(mText);

			for (int i = 1; i <= (Length / 100); i++)
			{
				MText m = (MText)mText.Clone();
				m.Contents = FormatStation(i * 100);
				m.Location = new Point3d(mText.Location.X + (i * 100), mText.Location.Y, mText.Location.Z);
				GridObjCollection.Add(m);
			}
		}

		public void PlaceGridElevationText()
		{
			MText mText = new MText()
			{
				TextHeight = 2.5,
				Location = new Point3d(InsertionPoint.X - 5, InsertionPoint.Y, InsertionPoint.Y),
				//Contents = "-25"
			};

			for (int i = 5; i <= HorizontalLineCount / 5; i--)
			{
				MText mt = (MText)mText.Clone();
				mt.Contents = (i * -5).ToString();
				mt.Location = new Point3d(mText.Location.X, mText.Location.Y + (VerticalScale * i * 5), mText.Location.Z);
				GridObjCollection.Add(mt);
			}

			MText mTextEnd = (MText)mText.Clone();
			mTextEnd.Location = new Point3d(InsertionPoint.X + (Length + 5), InsertionPoint.Y, InsertionPoint.Z);

			for (int i = 0; i <= HorizontalLineCount / 5; i++)
			{
				MText mt = (MText)mTextEnd.Clone();
				mt.Contents = (i * 5).ToString();
				mt.Location = new Point3d(mTextEnd.Location.X, mTextEnd.Location.Y + (VerticalScale * i * 5), mTextEnd.Location.Z);
				GridObjCollection.Add(mt);
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