using GH_IO.Serialization;
using GH_IO.Types;
using Grasshopper.Kernel.Graphs;
using Rhino.Geometry;
using RichedGraphMapper;
using RichedGraphMapper.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace RichedGraphMapper
{ 

	public class SigmoidGraph : GH_AbstractGraph
	{
		public double e; 
		private double t0; 
		private double t1;

		public override Guid GraphTypeID => new Guid("d221fdb4-a2a5-4861-a206-f7ac8f91f9cb");

		public override Image Icon_16x16 => Resources.SigmoidGraph_16x16;

		public override bool IsValid => true;

		public SigmoidGraph() : base("Sigmoid", "Sigmoid curve evaluator")
		{
			e = 0.15;
			t0 = -100.0;
			t1 = 100.0;
		}

		public override double ValueAt(double t)
		{
			double x = GetE();
			double y = t * (t1 - t0) + t0;
			double num = 1.0 / (1.0 + Math.Pow(x, y));
			if (e > 0.5)
			{
				num = 1.0 - num;
			}
			return Remap(num);
		}

		protected double GetE()
		{
			double num = e;
			num = ((!(e <= 0.5)) ? ((e - 0.5) / 0.5) : (1.0 - e / 0.5));
			num = Math.Pow(num, 0.25);
			return Math.Min(num, 0.999999);
		}

		protected double Remap(double t)
		{
			double x = GetE();
			double num = 1.0 / (1.0 + Math.Pow(x, t0));
			double num2 = 1.0 / (1.0 + Math.Pow(x, t1));
			return (t - num) / (num2 - num);
		}

		protected override void CreateGrips()
		{ 
			base.ClearGrips();
			base.AddGrip((GH_GraphGrip)(object)new GH_GraphGrip(e, 0.3, (GH_GripConstraint)1));
		}

		protected override GH_AbstractGraph CreateDerivedDuplicate()
		{
			return new SigmoidGraph
			{
				e = e
			};
		}

		protected override void UpdateEquation()
		{
			e = base.Grips[0].X;
		}

		public override bool Write(GH_IWriter writer)
		{
			writer.SetDouble("e", e);
			writer.SetDouble("t0", t0);
			writer.SetDouble("t1", t1);
			return true;
		}

		public override bool Read(GH_IReader reader)
		{
			e = reader.GetDouble("e");
			t0 = reader.GetDouble("t0");
			t1 = reader.GetDouble("t1");
			return true;
		}
	}

	public class PolylineGraph : GH_AbstractGraph
	{
		protected Polyline curve;

		protected Point3d[] pts;

		protected int count;

		public override Guid GraphTypeID => new Guid("e4f68fba-e0ba-4bbb-98aa-96ecb7db5d4e");

		public override Image Icon_16x16 => Resources.PolylineGraph_16x16;

		public override bool IsValid => true;

		public PolylineGraph() : base("Polyline", "Polyline curve evaluator")
		{
			count = 5;
			CreatePoints();
			curve = Curve();
		}

		public PolylineGraph(int CountPt) : base("Polyline", "Polyline curve evaluator")
		{
			count = Math.Max(3, CountPt);
			CreatePoints();
			curve = Curve();
		}

		public virtual Polyline Curve()
		{ 
			if (curve == null)
			{
				curve = (Polyline)(object)new Polyline(pts);
			}
			return curve;
		}

		protected virtual void CreatePoints()
		{
			pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				double num = (double)i / ((double)count - 1.0);
				pts[i] = new Point3d(num, num, 0.0);
			}
		}

		public override double ValueAt(double t)
		{
			if (t <= 0.0)
			{
				return pts[0].Y;
			}
			if (t >= 1.0)
			{
				return pts[count - 1].Y;
			}
			if (curve == null)
			{
				curve = Curve();
			}
			return GH_AbstractGraph.IntersectionEvaluate(curve.ToNurbsCurve(), t);
		}

		protected override void CreateGrips()
		{
			if (pts == null)
			{
				CreatePoints();
			}
			base.ClearGrips();
			for (int i = 0; i < pts.Length; i++)
			{
				base.AddGrip(new GH_GraphGrip(pts[i].X, pts[i].Y, (GH_GripConstraint)((i == 0 || i == pts.Length - 1) ? 2 : 0)));
			}
		}

		protected override GH_AbstractGraph CreateDerivedDuplicate()
		{
			PolylineGraph polylineGraph = new PolylineGraph();
			polylineGraph.pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				polylineGraph.pts[i] = pts[i];
			}
			return polylineGraph;
		}

		public override void ClearCaches()
		{
			if (curve != null)
			{
				curve = null;
			}
		}

		protected override void UpdateEquation()
		{
			base.ClearCaches();
			for (int i = 0; i < base.Grips.Count; i++)
			{
				pts[i] = new Point3d(base.Grips[i].X, base.Grips[i].Y, 0.0);
			}
		}

		public override PointF[] GDI_GraphPath(RectangleF reg)
		{
			return GH_AbstractGraph.CurveToPointFArray(Curve().ToNurbsCurve(), reg);
		}

		public override bool Write(GH_IWriter writer)
		{
			writer.SetInt32("count", count);
			for (int i = 0; i < count; i++)
			{
				Point3d val = pts[i];
				writer.SetPoint3D("pt", i, new GH_Point3D(val.X, val.Y, val.Z));
			}
			return true;
		}

		public override bool Read(GH_IReader reader)
		{
			count = reader.GetInt32("count");
			pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				GH_Point3D point3D = reader.GetPoint3D("pt", i);
				pts[i] = new Point3d(point3D.x, point3D.y, point3D.z);
			}
			return true;
		}
	}

	public class InterpolatedGraph : GH_AbstractGraph
	{
		protected Curve curve;

		protected Point3d[] pts;

		protected int count;

		public override Guid GraphTypeID => new Guid("19f04c28-60bd-4807-9308-c961b1233cea");

		public override Image Icon_16x16 => Resources.InterpolatedGraph_16x16;

		public override bool IsValid => true;

		public InterpolatedGraph() : base("Interpolated", "Interpolated curve evaluator")
		{
			count = 5;
			CreatePoints();
			curve = Curve();
		}

		public InterpolatedGraph(int CountPt) : base("Interpolated", "Interpolated curve evaluator")
		{
			count = Math.Max(3, CountPt);
			CreatePoints();
			curve = Curve();
		}

		public virtual Curve Curve()
		{
			if (curve == null)
			{
				Vector3d val = pts[1] - pts[0];
				Vector3d val2 = pts[count - 1] - pts[count - 2];
				curve = Rhino.Geometry.Curve.CreateInterpolatedCurve(pts, 3, (CurveKnotStyle)2, val, val2);
			}
			return curve;
		}

		protected virtual void CreatePoints()
		{
			pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				double num = (double)i / ((double)count - 1.0);
				pts[i] = new Point3d(num, num, 0.0);
			}
		}

		public override double ValueAt(double t)
		{
			if (t <= 0.0)
			{
				return pts[0].Y;
			}
			if (t >= 1.0)
			{
				return pts[count - 1].Y;
			}
			if (curve == null)
			{
				curve = Curve();
			}
			return GH_AbstractGraph.IntersectionEvaluate(curve, t);
		}

		protected override void CreateGrips()
		{
			if (pts == null)
			{
				CreatePoints();
			}
			base.ClearGrips();
			for (int i = 0; i < pts.Length; i++)
			{
				base.AddGrip(new GH_GraphGrip(pts[i].X, pts[i].Y, (GH_GripConstraint)((i == 0 || i == pts.Length - 1) ? 2 : 0)));
			}
		}

		protected override GH_AbstractGraph CreateDerivedDuplicate()
		{
			InterpolatedGraph interpolatedGraph = new InterpolatedGraph();
			interpolatedGraph.pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				interpolatedGraph.pts[i] = pts[i];
			}
			return interpolatedGraph;
		}

		public override void ClearCaches()
		{
			if (curve != null)
			{
				curve.Dispose();
				curve = null;
			}
		}

		protected override void UpdateEquation()
		{
			base.ClearCaches();
			for (int i = 0; i < base.Grips.Count; i++)
			{
				pts[i] = new Point3d(base.Grips[i].X, base.Grips[i].Y, 0.0);
			}
		}

		public override PointF[] GDI_GraphPath(RectangleF reg)
		{
			return GH_AbstractGraph.CurveToPointFArray(Curve(), reg);
		}

		public override bool Write(GH_IWriter writer)
		{
			writer.SetInt32("count", count);
			for (int i = 0; i < count; i++)
			{
				Point3d val = pts[i];
				writer.SetPoint3D("pt", i, new GH_Point3D(val.X,val.Y,val.Z));
			}
			return true;
		}

		public override bool Read(GH_IReader reader)
		{
			count = reader.GetInt32("count");
			pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				GH_Point3D point3D = reader.GetPoint3D("pt", i);
				pts[i] = new Point3d(point3D.x, point3D.y, point3D.z);
			}
			return true;
		}
	}

	public class Bezier2Graph : GH_AbstractGraph
	{
		protected Curve curve;

		protected Point3d[] pts;

		protected int count = 7;

		public override Guid GraphTypeID => new Guid("34afa8f2-fee6-4e3b-82da-b980ffeb87aa");

		public override Image Icon_16x16 => Resources.Bezier2Graph_16x16;

		public override bool IsValid => true;

		public Bezier2Graph() : base("Bezier2", "Double bezier curve evaluator")
		{
			count = 7;
			CreatePoints();
			curve = Curve();
		}

		public virtual Curve Curve()
		{if (curve == null)
			{
				Point3d[] array = new Point3d[4]
				{
				pts[0],
				pts[1],
				pts[2],
				pts[3]
				};
				Curve val = Rhino.Geometry.Curve.CreateControlPointCurve(array, 3);
				Point3d[] array2 = new Point3d[4]
				{
				pts[3],
				pts[4],
				pts[5],
				pts[6]
				};
				Curve val2 = Rhino.Geometry.Curve.CreateControlPointCurve(array2, 3);
				curve = Rhino.Geometry.Curve.JoinCurves(new Curve[2]
				{
				val,
				val2
				})[0];
			}
			return curve;
		}

		protected virtual void CreatePoints()
		{
			pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				double num = (double)i / ((double)count - 1.0);
				pts[i] = new Point3d(num, num, 0.0);
			}
			pts[1].X = 0.0;
			pts[1].Y = 0.3;
			pts[2].X = 0.2;
			pts[2].Y = 0.5;
			pts[4].X = 0.8;
			pts[4].Y = 0.5;
			pts[count - 2].X = 1.0;
			pts[count - 2].Y = 0.7;
		}

		public override double ValueAt(double t)
		{
			if (t <= 0.0)
			{
				return  pts[0].Y;
			}
			if (t >= 1.0)
			{
				return pts[count - 1].Y;
			}
			if (curve == null)
			{
				curve = Curve();
			}
			return GH_AbstractGraph.IntersectionEvaluate(curve, t);
		}

		protected override void CreateGrips()
		{
			if (pts == null)
			{
				CreatePoints();
			}
			base.ClearGrips();
			for (int i = 0; i < pts.Length; i++)
			{
				base.AddGrip(new GH_GraphGrip(pts[i].X, pts[i].Y, (GH_GripConstraint)((i == 0 || i == pts.Length - 1) ? 2 : 0)));
			}
		}

		protected override GH_AbstractGraph CreateDerivedDuplicate()
		{
			Bezier2Graph bezier2Graph = new Bezier2Graph();
			bezier2Graph.pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				bezier2Graph.pts[i] = pts[i];
			}
			return bezier2Graph;
		}

		public override void ClearCaches()
		{
			if (curve != null)
			{
				curve.Dispose();
				curve = null;
			}
		}

		protected override void UpdateEquation()
		{
			base.ClearCaches();
			Point3d val = pts[2];
			Point3d val2 = pts[3];
			Point3d val3 = pts[4];
			for (int i = 0; i < base.Grips.Count; i++)
			{
				pts[i] = new Point3d(base.Grips[i].X, base.Grips[i].Y, 0.0);
			}
			if (pts[3] != val2)
			{
				Vector3d val4 = pts[3] - val2; 
				pts[2] += val4; 
				pts[4] += val4;
				base.Grips[2].X += val4.X;
				base.Grips[2].Y += val4.Y;
				base.Grips[4].X += val4.X;
				base.Grips[4].Y += val4.Y;
			}
			else if (pts[2] != val)
			{
				double num = val2.DistanceTo(val3);
				Vector3d val5 = val2 - pts[2];
				if (val5.IsZero)
				{
					val5 = pts[4] - val2;
				}
				val5.Unitize();
				val5 *= num;
				val3 = val2 + val5;
				pts[4] = val3;
				base.Grips[4].X = val3.X;
				base.Grips[4].Y = val3.Y;
			}
			else if (pts[4] != val3)
			{
				double num2 = val2.DistanceTo(val);
				Vector3d val6 = val2 - pts[4];
				if (val6.IsZero)
				{
					val6 = pts[2] - val2;
				}
				val6.Unitize();
				val6 *= num2;
				val = val2 + val6;
				pts[2] = val;
				base.Grips[2].X = val.X;
				base.Grips[2].Y = val.Y;
			}
		}

		public override PointF[] GDI_GraphPath(RectangleF reg)
		{
			return GH_AbstractGraph.CurveToPointFArray(Curve(), reg);
		}

		public override GH_GraphDrawInstruction Draw_PreRenderGrip(Graphics g, GH_GraphContainer cnt, int index)
		{
			if (index != 0)
			{
				return (GH_GraphDrawInstruction)0;
			}
			Pen pen = GH_GraphContainer.Render_GuidePen();
			PointF pt = cnt.ToRegionBox(base.Grips[0].Point);
			PointF pt2 = cnt.ToRegionBox(base.Grips[1].Point);
			PointF pt3 = cnt.ToRegionBox(base.Grips[2].Point);
			PointF pointF = cnt.ToRegionBox(base.Grips[3].Point);
			PointF pt4 = cnt.ToRegionBox(base.Grips[4].Point);
			PointF pt5 = cnt.ToRegionBox(base.Grips[5].Point);
			PointF pt6 = cnt.ToRegionBox(base.Grips[6].Point);
			Region clip = g.Clip;
			g.SetClip(cnt.Region);
			g.DrawLine(pen, pt, pt2);
			g.DrawLine(pen, pt3, pointF);
			g.DrawLine(pen, pointF, pt4);
			g.DrawLine(pen, pt5, pt6);
			g.ResetClip();
			g.SetClip(clip, CombineMode.Replace);
			pen.Dispose();
			return (GH_GraphDrawInstruction)0;
		}

		public override bool Write(GH_IWriter writer)
		{ 
			for (int i = 0; i < count; i++)
			{
				Point3d val = pts[i];
				writer.SetPoint3D("pt", i, new GH_Point3D(val.X,val.Y,val.Z));
			}
			return true;
		}

		public override bool Read(GH_IReader reader)
		{
			pts = new Point3d[count];
			for (int i = 0; i < count; i++)
			{
				GH_Point3D point3D = reader.GetPoint3D("pt", i);
				pts[i] = new Point3d(point3D.x, point3D.y, point3D.z);
			}
			return true;
		}
	}

	public class ArcGraph : GH_AbstractGraph
	{
		public double e;

		public override Guid GraphTypeID => new Guid("c5363e79-994b-4030-8e69-02cd00bf5982");

		public override Image Icon_16x16 => Resources.ArcGraph_16x16;

		public override bool IsValid => true;

		public ArcGraph() : base("Arc", "Arc curve evaluator")
		{
			e = 0.55;
		}

		public override double ValueAt(double t)
		{
			if (t <= 0.0 || t >= 1.0)
			{
				return (e < 0.5) ? 1 : 0;
			}
			double y = GetE();
			double value = t * 2.0 - 1.0;
			double num = Math.Sqrt(1.0 - Math.Pow(Math.Abs(value), y));
			if (e < 0.5)
			{
				num = 1.0 - num;
			}
			return num;
		}

		protected double GetE()
		{
			double num = e;
			num = ((!(e <= 0.5)) ? ((e - 0.5) / 0.5) : (1.0 - e / 0.5));
			num = num * 0.9 + 0.1;
			num = Math.Pow(num, 2.0);
			return num * 100.0;
		}

		protected override void CreateGrips()
		{ 
			base.ClearGrips();
			base.AddGrip(new GH_GraphGrip(0.3, e, (GH_GripConstraint)2));
		}

		protected override GH_AbstractGraph CreateDerivedDuplicate()
		{
			return new ArcGraph
			{
				e = e
			};
		}

		protected override void UpdateEquation()
		{
			e = base.Grips[0].Y;
		}

		public override bool Write(GH_IWriter writer)
		{
			writer.SetDouble("e", e);
			return true;
		}

		public override bool Read(GH_IReader reader)
		{
			e = reader.GetDouble("e");
			return true;
		}
	}
}
