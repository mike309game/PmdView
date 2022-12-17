using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PmdView {
	public static class Shenanigans {
		public static System.Numerics.Vector3 XnaToNumerics(Microsoft.Xna.Framework.Vector3 vecXna) {
			System.Numerics.Vector3 vecNum = new();
			vecNum.X = vecXna.X;
			vecNum.Y = vecXna.Y;
			vecNum.Z = vecXna.Z;
			return vecNum;
		}
		public static Microsoft.Xna.Framework.Vector3 NumericsToXna(System.Numerics.Vector3 vecNum) {
			Microsoft.Xna.Framework.Vector3 vecXna = new();
			vecXna.X = vecNum.X;
			vecXna.Y = vecNum.Y;
			vecXna.Z = vecNum.Z;
			return vecXna;
		}
	}
}
