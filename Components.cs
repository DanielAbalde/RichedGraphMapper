using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RichedGraphMapper;
using RichedGraphMapper.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace RichedGraphMapper
{
	public class RichGraphMapper : GH_Component, IGH_StateAwareObject
	{
		public class RichGraphMapperAttributes : GH_ResizableAttributes<RichGraphMapper>
		{
			private int m_dragmode;

			private int inputWidth;

			private int outputWidth;

			public bool DrawDomainsTags
			{
				get;
				set;
			}

			public override string PathName => "RichGraphMapper";

			protected override Size MinimumSize => new Size(100 + inputWidth + outputWidth, 100);

			protected override Padding SizingBorders => new Padding(6);

			public RichGraphMapperAttributes(RichGraphMapper owner)
				: base(owner)
			{
				base.Bounds = new Rectangle(0, 0, 100, 100);
				inputWidth = (outputWidth = GH_FontServer.MeasureString("M", GH_FontServer.Standard).Width);
			}

			public override void AppendToAttributeTree(List<IGH_Attributes> attributes)
			{
				attributes.Add((IGH_Attributes)(object)this);
				foreach (IGH_Param item in Owner.Params.Input)
				{
					attributes.Add(item.Attributes);
				}
				foreach (IGH_Param item2 in Owner.Params.Output)
				{
					attributes.Add(item2.Attributes);
				}
			}

			protected override void Layout()
			{
				RectangleF bounds = base.Bounds;
				bounds.Location = base.Pivot;
				bounds = GH_Convert.ToRectangle(bounds);
				Bounds = bounds;
				GH_ComponentAttributes.LayoutInputParams(Owner, bounds);
				foreach (IGH_Param item in Owner.Params.Input)
				{
					RectangleF bounds2 = item.Attributes.Bounds;
					bounds2.X += bounds2.Width + 3f;
					item.Attributes.Bounds = bounds2;
				}
				inputWidth = (int)Owner.Params.Input[0].Attributes.Bounds.Width;
				bounds.Width -= inputWidth;
				bounds.X += inputWidth;
				GH_ComponentAttributes.LayoutOutputParams(Owner, bounds);
				outputWidth = (int)(Owner.Params.Output[0]).Attributes.Bounds.Width;
				foreach (IGH_Param item2 in Owner.Params.Output)
				{
					RectangleF bounds3 = item2.Attributes.Bounds;
					bounds3.X -= (float)outputWidth + 3f;
					bounds3.Width = outputWidth;
					bounds3.Inflate(0f, -30f);
					item2.Attributes.Bounds = bounds3;
				}
				bounds.Width -= outputWidth;
				if (bounds.Width < 96f)
				{
					bounds.Width = 96f;
				}
				if (bounds.Height < 100f)
				{
					bounds.Height = 100f;
				}
				bounds.Inflate(-6f, -6f);
				if (base.Owner.Container != null)
				{
					base.Owner.Container.Region = GH_Convert.ToRectangle(bounds);
				}
				Bounds = new Rectangle((int)Pivot.X, (int)Pivot.Y, (int)((float)inputWidth + bounds.Width + 12f + (float)outputWidth), (int)base.Bounds.Height);
			}

			protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
			{
				if ((int)channel == 10)
				{
					foreach (IGH_Param item in Owner.Params.Input)
					{
						item.Attributes.RenderToCanvas(canvas, (GH_CanvasChannel)10);
					}
				}
				else
				{
					if ((int)channel != 20)
					{
						return;
					}
					RectangleF bounds = base.Bounds;
					bool flag = canvas.Viewport.IsVisible(ref bounds, 10f);
					Bounds = bounds;
					if (flag)
					{
						GH_Palette impliedPalette = GH_CapsuleRenderEngine.GetImpliedPalette((IGH_ActiveObject)(object)base.Owner);
						GH_PaletteStyle impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(impliedPalette, Selected, Owner.Locked, Owner.Hidden);
						GH_Capsule val = GH_Capsule.CreateCapsule(base.Bounds, impliedPalette, 5, 30);
						foreach (IGH_Param item2 in Owner.Params.Input)
						{
							val.AddInputGrip(item2.Attributes.InputGrip.Y);
						}
						val.AddOutputGrip(OutputGrip.Y);
						if (Owner.Message != null)
						{
							val.RenderEngine.RenderMessage(graphics, Owner.Message, impliedStyle);
						}
						val.Render(graphics, Selected, Owner.Locked, true);
						val.Dispose();
						GH_ComponentAttributes.RenderComponentParameters(canvas, graphics, base.Owner, impliedStyle);
						if (base.Owner.Graph == null)
						{
							RectangleF bounds2 = base.Bounds;
							bounds2.X += inputWidth;
							bounds2.Width -= inputWidth + outputWidth;
							bounds2.Inflate(-6f, -6f);
							GH_GraphContainer.Render_GraphBackground(graphics, bounds2, false);
						}
						else
						{
							base.Owner.Container.Render(graphics, DrawDomainsTags, base.Owner.Samples);
						}
					}
				}
			}

			public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
			{ 
				m_dragmode = 0;
				if (e.Button== MouseButtons.Left && base.Owner.Graph != null)
				{
					GH_ObjectResponse val = base.Owner.Container.RespondToMouseDown(sender, e);
					if ((int)val == 1)
					{
						m_dragmode = 1;
						sender.Cursor = Cursors.Default;
						return (GH_ObjectResponse)1;
					}
				}
				return base.RespondToMouseDown(sender, e);
			}

			public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
			{ 
				if (e.Button == MouseButtons.Left)
				{
					if (m_dragmode == 1)
					{
						Owner.TriggerAutoSave();
						Owner.RecordUndoEvent("Graph Change");
						m_dragmode = 2;
					}
					if (m_dragmode == 2 && base.Owner.Graph != null)
					{
						GH_ObjectResponse val = base.Owner.Container.RespondToMouseMove(sender, e);
						if ((int)val <= 1 || (int)val == 3)
						{
							return (GH_ObjectResponse)3;
						}
					}
				}
				return base.RespondToMouseMove(sender, e);
			}

			public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
			{ 
				if (m_dragmode > 0 && base.Owner.Graph != null)
				{
					GH_ObjectResponse val = base.Owner.Container.RespondToMouseUp(sender, e);
					switch (val)
					{
						case GH_ObjectResponse.Ignore:
							m_dragmode = 1;
							sender.Cursor = Cursors.Default;
							return (GH_ObjectResponse)1;
						case GH_ObjectResponse.Capture:
							m_dragmode = 0;
							sender.Refresh();
							return (GH_ObjectResponse)2;
						case GH_ObjectResponse.Release:
							m_dragmode = 0;
							sender.Cursor = Cursors.Default;
							return (GH_ObjectResponse)3;
					}
				}
				return base.RespondToMouseUp(sender, e);
			}

			public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
			{ 
				if (base.Owner.Graph == null)
				{
					return (GH_ObjectResponse)3;
				}
				GH_GraphEditor val =  new GH_GraphEditor();
				val.SetGraph(base.Owner.Container);
				GH_WindowsFormUtil.CenterFormOnCursor((Form)(object)val, true);
				DialogResult dialogResult = val.ShowDialog(Instances.DocumentEditor);
				if (dialogResult == DialogResult.OK)
				{
					val.Graph.LockGrips = base.Owner.Container.LockGrips;
					base.Owner.Container = val.Graph;
				}
				((Component)(object)val).Dispose();
				return (GH_ObjectResponse)3;
			}
		}

		protected GH_GraphContainer container;

		protected SortedList<Guid, string> m_graph_history;

		protected double minValue = double.MaxValue;

		protected double maxValue = double.MinValue;

		public GH_GraphContainer Container
		{
			get
			{
				return container;
			}
			set
			{
				if (value == container)
				{
					return;
				}
				if (container != null)
				{
					container.GraphChanged -= GraphChanged;
					container = null;
				}
				if (value != null && value.Graph != null)
				{
					container = value;
					if (container != null)
					{
						container.GraphChanged += GraphChanged;
					}
					Attributes.PerformLayout();
					if (container != null)
					{
						container.PrepareForUse();
						container.OnGraphChanged(false);
					}
					else
					{
						GraphChanged(null, false);
					}
				}
			}
		}

		public IGH_Graph Graph
		{
			get
			{
				if (container == null)
				{
					return null;
				}
				return container.Graph;
			}
		}

		public List<double> Samples
		{
			get;
			private set;
		}

		public bool RemapToTarget
		{
			get;
			set;
		}

		public override Guid ComponentGuid => new Guid("e2996e6c-e067-42fa-8f44-2192c6763262");

		public override GH_Exposure Exposure => (GH_Exposure)32;

		protected override Bitmap Icon => Resources.RichGraphMapper;

		public RichGraphMapper()
			: base("Rich Graph Mapper", "Rich Graph", "Represents a numeric mapping function", "Math", "Util")
		{
			m_graph_history = new SortedList<Guid, string>();
		}

		public override void CreateAttributes()
		{
			base.m_attributes = (IGH_Attributes)(object)new RichGraphMapperAttributes(this);
		}

		private void GraphChanged(GH_GraphContainer sender, bool bIntermediate)
		{
			((GH_ActiveObject)this).ExpireSolution(true);
		}

		private void RecallGraph(IGH_Graph g)
		{
			if (g != null && m_graph_history.ContainsKey(g.GraphTypeID))
			{
				GH_Archive gH_Archive = new GH_Archive();
				if (gH_Archive.Deserialize_Xml(m_graph_history[g.GraphTypeID]))
				{
					gH_Archive.ExtractObject((GH_ISerializable)g, "graph");
				}
			}
		}

		private void RememberGraph(IGH_Graph g)
		{
			if (g == null)
			{
				return;
			}
			Guid graphTypeID = g.GraphTypeID;
			if (m_graph_history.ContainsKey(graphTypeID))
			{
				m_graph_history.Remove(graphTypeID);
			}
			GH_Archive gH_Archive = new GH_Archive();
			if (gH_Archive.AppendObject((GH_ISerializable)g, "graph"))
			{
				string value = gH_Archive.Serialize_Xml();
				if (!string.IsNullOrEmpty(value))
				{
					m_graph_history.Add(graphTypeID, value);
				}
			}
		}

		public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
		{
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f8: Expected O, but got Unknown
			//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cb: Expected O, but got Unknown
			base.AppendAdditionalMenuItems(menu);
			bool flag = false;
			if (Container != null)
			{
				flag = Container.LockGrips;
			}
			GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Locked", (EventHandler)Menu_LockClicked, Graph != null, flag);
			GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Default", (EventHandler)Menu_DefaultClicked, Graph != null, false);
			GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Remap", (EventHandler)Menu_RemapClicked, true, RemapToTarget).ToolTipText = "Remap output values to target domain";
			GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Draw domains", (EventHandler)Menu_DrawDomainsClicked, Graph != null, ((RichGraphMapperAttributes)(object)((GH_DocumentObject)this).Attributes).DrawDomainsTags);
			IList<GH_GraphProxy> graphProxies = Instances.ComponentServer.GraphProxies;
			ToolStripMenuItem toolStripMenuItem = GH_DocumentObject.Menu_AppendItem((ToolStrip)menu, "Graph types");
			GH_DocumentObject.Menu_AppendItem((ToolStrip)toolStripMenuItem.DropDown, "None", (EventHandler)Menu_NoGraphItemClicked, true, Graph == null);
			Guid a = Guid.Empty;
			InterpolatedGraph interpolatedGraph = new InterpolatedGraph();
			PolylineGraph polylineGraph = new PolylineGraph();
			if (Graph != null)
			{
				a = Graph.GraphTypeID;
			}
			foreach (GH_GraphProxy item in graphProxies)
			{
				if (!(item.GUID == ((GH_AbstractGraph)interpolatedGraph).GraphTypeID) && !(item.GUID == ((GH_AbstractGraph)polylineGraph).GraphTypeID))
				{
					ToolStripMenuItem toolStripMenuItem2 = GH_DocumentObject.Menu_AppendItem((ToolStrip)toolStripMenuItem.DropDown, item.Name, (EventHandler)Menu_GraphTypeItemClicked, item.Icon, true, a == item.GUID);
					toolStripMenuItem2.Tag = item.GUID;
				}
			}
			GH_GraphProxy val = (GH_GraphProxy)(object)new GH_GraphProxy((IGH_Graph)(object)interpolatedGraph, typeof(InterpolatedGraph));
			ToolStripMenuItem toolStripMenuItem3 = GH_DocumentObject.Menu_AppendItem((ToolStrip)toolStripMenuItem.DropDown, val.Name, (EventHandler)Menu_FreeFormGraphClicked, val.Icon, true, a == val.GUID);
			toolStripMenuItem3.Tag = 5;
			for (int i = 3; i <= 10; i++)
			{
				ToolStripMenuItem toolStripMenuItem4 = GH_DocumentObject.Menu_AppendItem((ToolStrip)toolStripMenuItem3.DropDown, val.Name + " " + i + " points", (EventHandler)Menu_FreeFormGraphClicked, val.Icon, true, false);
				toolStripMenuItem4.Tag = i;
			}
			val = (GH_GraphProxy)(object)new GH_GraphProxy((IGH_Graph)(object)polylineGraph, typeof(PolylineGraph));
			ToolStripMenuItem toolStripMenuItem5 = GH_DocumentObject.Menu_AppendItem((ToolStrip)toolStripMenuItem.DropDown, val.Name, (EventHandler)Menu_PolylineGraphClicked, val.Icon, true, a == val.GUID);
			toolStripMenuItem5.Tag = 5;
			for (int j = 3; j <= 10; j++)
			{
				ToolStripMenuItem toolStripMenuItem6 = GH_DocumentObject.Menu_AppendItem((ToolStrip)toolStripMenuItem5.DropDown, val.Name + " " + j + " points", (EventHandler)Menu_PolylineGraphClicked, val.Icon, true, false);
				toolStripMenuItem6.Tag = j;
			}
		}

		private void Menu_DefaultClicked(object sender, EventArgs e)
		{
			if (Graph != null)
			{
				IGH_Graph val = Instances.ComponentServer.EmitGraph(Graph.GraphTypeID);
				if (val != null)
				{
					val.PrepareForUse();
					Container.Graph = val;
					Container.OnGraphChanged(false);
				}
			}
		}

		private void Menu_GraphTypeItemClicked(object sender, EventArgs e)
		{
			ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
			Guid guid = (Guid)toolStripMenuItem.Tag;
			IGH_Graph val = Instances.ComponentServer.EmitGraph((Guid)toolStripMenuItem.Tag);
			if (val != null)
			{
				if (Graph != null)
				{
					RememberGraph(Graph);
				}
				val.PrepareForUse();
				RecallGraph(val);
				GH_GraphContainer val2 = Container;
				Container = null;
				if (val2 == null)
				{
					val2 = (GH_GraphContainer)(object)new GH_GraphContainer(val);
				}
				else
				{
					val2.Graph = val;
				}
				Container = val2;
			}
		}

		private void Menu_FreeFormGraphClicked(object sender, EventArgs e)
		{
			ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
			int countPt = (int)toolStripMenuItem.Tag;
			IGH_Graph val = (IGH_Graph)(object)new InterpolatedGraph(countPt);
			if (val != null)
			{
				if (Graph != null)
				{
					RememberGraph(Graph);
				}
				val.PrepareForUse();
				GH_GraphContainer val2 = Container;
				Container = null;
				if (val2 == null)
				{
					val2 = (GH_GraphContainer)(object)new GH_GraphContainer(val);
				}
				else
				{
					val2.Graph = val;
				}
				Container = val2;
			}
		}

		private void Menu_PolylineGraphClicked(object sender, EventArgs e)
		{
			ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
			int countPt = (int)toolStripMenuItem.Tag;
			IGH_Graph val = (IGH_Graph)(object)new PolylineGraph(countPt);
			if (val != null)
			{
				if (Graph != null)
				{
					RememberGraph(Graph);
				}
				val.PrepareForUse();
				GH_GraphContainer val2 = Container;
				Container = null;
				if (val2 == null)
				{
					val2 = (GH_GraphContainer)(object)new GH_GraphContainer(val);
				}
				else
				{
					val2.Graph = val;
				}
				Container = val2;
			}
		}

		private void Menu_Bezier2GraphClicked(object sender, EventArgs e)
		{
			ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
			IGH_Graph val = new Bezier2Graph();
			if (val != null)
			{
				if (Graph != null)
				{
					RememberGraph(Graph);
				}
				val.PrepareForUse();
				RecallGraph(val);
				GH_GraphContainer val2 = Container;
				Container = null;
				if (val2 == null)
				{
					val2 = (GH_GraphContainer)(object)new GH_GraphContainer(val);
				}
				else
				{
					val2.Graph = val;
				}
				Container = val2;
			}
		}

		private void Menu_LockClicked(object sender, EventArgs e)
		{
			if (Container != null)
			{
				Container.LockGrips = !Container.LockGrips;
				Instances.RedrawCanvas();
			}
		}

		private void Menu_NoGraphItemClicked(object sender, EventArgs e)
		{
			if (Graph != null)
			{
				RememberGraph(Graph);
				Container.Graph = null;
				GraphChanged(null, false);
			}
		}

		private void Menu_RemapClicked(object sender, EventArgs e)
		{
			RemapToTarget = !RemapToTarget;
			if (RemapToTarget)
			{
				Message = "Remap";
			}
			else
			{
				Message = string.Empty;
			}
			ExpireSolution(true);
		}

		private void Menu_DrawDomainsClicked(object sender, EventArgs e)
		{
			RichGraphMapperAttributes richGraphMapperAttributes = Attributes as RichGraphMapperAttributes;
			if (richGraphMapperAttributes != null)
			{
				richGraphMapperAttributes.DrawDomainsTags = !richGraphMapperAttributes.DrawDomainsTags;
				Instances.RedrawCanvas();
			}
		}

		protected override void RegisterInputParams(GH_InputParamManager pManager)
		{
			pManager.AddNumberParameter("Values", "V", "Input values", (GH_ParamAccess)1);
			pManager.AddIntervalParameter("Source", "S", "Source domain", (GH_ParamAccess)0, new Interval(0.0, 1.0));
			pManager.AddIntervalParameter("Target", "T", "Target domain", (GH_ParamAccess)0, new Interval(0.0, 1.0));
		}

		protected override void RegisterOutputParams(GH_OutputParamManager pManager)
		{
			pManager.AddNumberParameter("Values", "V", "Output values", (GH_ParamAccess)1);
		}

		protected override void BeforeSolveInstance()
		{
			base.BeforeSolveInstance();
			if (RemapToTarget && Graph != null)
			{
				minValue = double.MaxValue;
				maxValue = double.MinValue;
			}
		}

		protected override void AfterSolveInstance()
		{
			base.AfterSolveInstance();
			if (RemapToTarget && Graph != null)
			{
				GH_Structure<GH_Number> val = Params.Output[0].VolatileData as GH_Structure<GH_Number>;
				foreach (GH_Path path in val.Paths)
				{
					foreach (GH_Number item in val.get_Branch(path))
					{
						item.Value = Utils.Remap(item.Value, minValue, maxValue, container.Y0, container.Y1);
					}
				}
			}
		}

		protected override void SolveInstance(IGH_DataAccess DA)
		{
			Samples = null;
			List<GH_Number> list = new List<GH_Number>();
			if (!DA.GetDataList<GH_Number>(0, list))
			{
				return;
			}
			Interval unset = Interval.Unset;
			if (!DA.GetData<Interval>(1, ref unset))
			{
				return;
			}
			Interval unset2 = Interval.Unset;
			if (DA.GetData<Interval>(2, ref unset2))
			{
				Samples = new List<double>();
				List<GH_Number> list2 = new List<GH_Number>();
				if (Graph == null)
				{
					foreach (GH_Number item in list)
					{
						double num = item.Value;
						Samples.Add(num);
						if (RemapToTarget)
						{
							num = Utils.Remap(num, unset.T0, unset.T1, unset2.T0, unset2.T1);
						}
						list2.Add(new GH_Number(num));
					}
				}
				else
				{
					container.X0 = unset.T0;
					container.X1 = unset.T1;
					container.Y0 = unset2.T0;
					container.Y1 = unset2.T1;
					foreach (GH_Number item2 in list)
					{
						double value = item2.Value;
						Samples.Add(value);
						double num2 = container.ValueAt(value);
						if (double.IsNaN(num2))
						{
							((GH_ActiveObject)this).AddRuntimeMessage((GH_RuntimeMessageLevel)20, "Graph output contains #NaN values.");
						}
						else if (RemapToTarget)
						{
							if (num2 < minValue)
							{
								minValue = num2;
							}
							if (num2 > maxValue)
							{
								maxValue = num2;
							}
						}
						list2.Add((GH_Number)(object)new GH_Number(num2));
					}
				}
				DA.SetDataList(0, (IEnumerable)list2);
			}
		}

		public string SaveState()
		{
			if (container == null)
			{
				return "null";
			}
			GH_LooseChunk gH_LooseChunk = new GH_LooseChunk("graph");
			container.Write((GH_IWriter)gH_LooseChunk);
			return gH_LooseChunk.Serialize_Xml();
		}

		public void LoadState(string state)
		{
			Container = null;
			if (state.Equals("null", StringComparison.OrdinalIgnoreCase))
			{
				ExpireSolution(false);
				return;
			}
			GH_LooseChunk gH_LooseChunk = new GH_LooseChunk("graph");
			gH_LooseChunk.Deserialize_Xml(state);
			GH_GraphContainer val = new GH_GraphContainer(null);
			if (val.Read(gH_LooseChunk))
			{
				Container = val;
				ExpireSolution(false);
			}
		}

		public override bool Write(GH_IWriter writer)
		{
			if (!base.Write(writer))
			{
				return false;
			}
			writer.SetBoolean("Remap", RemapToTarget);
			if (container != null)
			{
				GH_IWriter gH_IWriter = writer.CreateChunk("LocalGraph");
				return container.Write(gH_IWriter);
			}
			return true;
		}

		public override bool Read(GH_IReader reader)
		{
			Container = null;
			if (!base.Read(reader))
			{
				return false;
			}
			RemapToTarget = reader.GetBoolean("Remap");
			if (RemapToTarget)
			{
				Message = "Remap";
			}
			else
			{
				Message = string.Empty;
			}
			GH_IReader gH_IReader = reader.FindChunk("LocalGraph");
			if (gH_IReader != null)
			{
				GH_GraphContainer val = new GH_GraphContainer(null);
				if (!val.Read(gH_IReader))
				{
					return false;
				}
				Container = val;
			}
			return true;
		}
	}
}
