using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RichedGraphMapper
{
	public static class Utils
	{
		public static double Remap(double v, double a, double b, double c, double d)
		{
			return (v - a) / (b - a) * (d - c) + c;
		}
	}
}
