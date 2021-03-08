using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace RichedGraphMapper
{
    public class Plugin : GH_AssemblyInfo
    {
		public override string Name => "RichedGraphMapper"; 
		public override Bitmap Icon => null; 
		public override string Description => "Extended Graph Mapper"; 
		public override Guid Id => new Guid("6ffcbd5d-525a-4a15-948e-4c777cbffd9a"); 
		public override string AuthorName => "Daniel Gonzalez Abalde"; 
		public override string AuthorContact => "https://discord.gg/zZvZX4yuBt"; 
		public override string AssemblyVersion => "1.0.0";
		 
	}
}
