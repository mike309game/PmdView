using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace PmdView.Psx {
	public static class SVECTOR {
		public static Vector3 Read(BinaryReader reader) {
			Vector3 vec = new();
			vec.X = reader.ReadInt16();
			vec.Y = reader.ReadInt16();
			vec.Z = reader.ReadInt16();
			var padding = reader.ReadInt16();
			if (padding != 0) {
				throw new InvalidDataException("SVECTOR padding isn't 0");
			}
			return vec;
		}
	}
}
