using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Psx {
	public enum PrimitiveType : byte {
		PolyF3 = 0x20,
		PolyFT3 = 0x24,
		PolyG3 = 0x30,
		PolyGT3 = 0x34,
		PolyF4 = 0x28,
		PolyFT4 = 0x2c,
		PolyG4 = 0x38,
		PolyGT4 = 0x3c
	}
	public class Primitive {
		public PrimitiveType code;

		//Face colours
		public byte r0, g0, b0;
		public byte r1, g1, b1;
		public byte r2, g2, b2;
		public byte r3, g3, b3;

		//Face UVs
		public byte u0, v0;
		public byte u1, v1;
		public byte u2, v2;
		public byte u3, v3;

		public ushort clut;
		public ushort tpage;

		//////////////////////////////////////////////////////////////////////////////////////////////////////////

		public void Deserialise(BinaryReader reader, PrimitiveType type) {
			bool textured =		((byte)type & 0b00000100) != 0;
			bool quadrangle =	((byte)type & 0b00001000) != 0;
			bool gourad =		((byte)type & 0b00010000) != 0;

			ReadTag(reader);
			ReadColour(reader, ref r0, ref g0, ref b0); ReadCode(reader, type);
			ReadXY(reader);
			if (textured) {
				ReadUV(reader, ref u0, ref v0); ReadClut(reader);
			}
			if (gourad) {
				ReadColour(reader, ref r1, ref g1, ref b1); ReadPad8(reader);
			}
			ReadXY(reader);
			if (textured) {
				ReadUV(reader, ref u1, ref v1); ReadTpage(reader);
			}
			if (gourad) {
				ReadColour(reader, ref r2, ref g2, ref b2); ReadPad8(reader);
			}
			ReadXY(reader);
			if (textured) {
				ReadUV(reader, ref u2, ref v2); ReadPad16(reader);
			}
			if (quadrangle) {
				if (gourad) {
					ReadColour(reader, ref r3, ref g3, ref b3); ReadPad8(reader);
				}
				ReadXY(reader);
				if (textured) {
					ReadUV(reader, ref u3, ref v3); ReadPad16(reader);
				}
			}

			if (((uint)type & 0b00010000) == 0) {
				r1 = r0; g1 = g0; b1 = b0;
				r2 = r0; g2 = g0; b2 = b0;
				r3 = r0; g3 = g0; b3 = b0;
			}
		}

		//////////////////////////////////////////////////////////////////////////////////////////////////////////

		void ReadColour(BinaryReader reader, ref byte r, ref byte g, ref byte b) {
			r = reader.ReadByte();
			g = reader.ReadByte();
			b = reader.ReadByte();
		}

		void ReadUV(BinaryReader reader, ref byte u, ref byte v) {
			u = reader.ReadByte();
			v = reader.ReadByte();
		}

		void ReadXY(BinaryReader reader) {
			var x = reader.ReadUInt16();
			var y = reader.ReadUInt16();
			if(x != 0 || y != 0) {
				throw new InvalidDataException($"XY positions are non-blank ({x}, {y})");
			}
		}

		void ReadClut(BinaryReader reader) {
			clut = reader.ReadUInt16();
		}

		void ReadTpage(BinaryReader reader) {
			tpage = reader.ReadUInt16();
		}

		void ReadPad8(BinaryReader reader) {
			var value = reader.ReadByte();
			if(value != 0) {
				throw new InvalidDataException($"Padding isn't blank ({value})");
			}
		}

		void ReadPad16(BinaryReader reader) {
			var value = reader.ReadUInt16();
			if (value != 0) {
				throw new InvalidDataException($"Padding isn't blank ({value})");
			}
		}

		void ReadTag(BinaryReader reader) {
			var value = reader.ReadUInt32();
		}
		
		void ReadCode(BinaryReader reader, PrimitiveType desired) {
			var value = reader.ReadByte();
			if(value != (byte)desired) {
				throw new InvalidDataException($"Expected code was {(byte)desired}, got {value}");
			}
			code = desired;
		}
	}
}
