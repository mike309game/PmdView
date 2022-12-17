using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Psx {

	[Flags]
	public enum PrimGpType {
		IsQuad = 1 << 0,
		IsGourad = 1 << 1,
		NoTex = 1 <<  2,
		SharedVerts = 1 << 3,
		LightCalc = 1 << 4,
		NoCull = 1 << 5
	}
	public class Pmd {
		public class PrimPacket {
			//public PrimPacketType type;
			public Primitive[] prim;
			public Vector3[] vertsUnique;
			public uint[] vertsShared;
			public PrimPacket(int dupes) => prim = new Primitive[dupes];
		}

		public class PrimGp {
			public PrimGpType type;
			public PrimPacket[] packets;

			/*public Vector3 ReadSharedVert(BinaryReader reader, uint pos) {
				var oldPos = reader.BaseStream.Position;
				reader.BaseStream.Position = pos;

				var vec = ReadSVec(reader);

				reader.BaseStream.Position = oldPos;

				return vec;
			}*/

			public PrimGp(BinaryReader reader, uint gpPos, uint vertPos, int dupeBuffers) {
				var gpPtr = reader.ReadUInt32(); //Console.WriteLine($"Gp pointer is {gpPtr}");
				var oldPos = reader.BaseStream.Position;
				reader.BaseStream.Position = gpPtr;

				var packetNum = reader.ReadUInt16();
				type = (PrimGpType)reader.ReadUInt16();

				packets = new PrimPacket[packetNum];

				//Convert gp type to prim type
				PrimitiveType gpuType;
				gpuType = (PrimitiveType)(0b00100000 |
					(type.HasFlag(PrimGpType.NoTex) ? 0 : 0b00100) | //good job libgs
					(type.HasFlag(PrimGpType.IsQuad) ? 0b01000 : 0) |
					(type.HasFlag(PrimGpType.IsGourad) ? 0b10000 : 0)
				);

				for (var i = 0; i < packetNum; i++) {
					var packet = new PrimPacket(dupeBuffers);
					for (var j = 0; j < dupeBuffers; j++) {
						Primitive prim = new();
						prim.Deserialise(reader, gpuType);
						packet.prim[j] = prim;
						//Console.WriteLine($"Deserialised {pt}");
					}

					if (type.HasFlag(PrimGpType.SharedVerts)) { //is shared vertex
						packet.vertsShared = new uint[type.HasFlag(PrimGpType.IsQuad) ? 4 : 3];
						for(var j = 0; j < packet.vertsShared.Length; j++) {
							var value = reader.ReadUInt32();
							//Console.WriteLine(value % 8);
							packet.vertsShared[j] = value >> 3;
						}
					} else {
						packet.vertsUnique = new Vector3[type.HasFlag(PrimGpType.IsQuad) ? 4 : 3];
						for (var j = 0; j < packet.vertsUnique.Length; j++) {
							packet.vertsUnique[j] = SVECTOR.Read(reader);
						}
					}
					packets[i] = packet;
				}
				reader.BaseStream.Position = oldPos;
			}
		}

		public class Object {
			public PrimGp[] primgps;
			public Object(uint gpNum) => primgps = new PrimGp[gpNum];
		}

		public Object[] objects;
		public Vector3[,] frameVertices;
		public Pmd(BinaryReader reader, int dupeBuffers) {
			var vertsPerFrame = (ushort)(reader.ReadUInt16() & 0b00111111_11111111) >> 1; //i don't know what those bits indicate
			var unknown = reader.ReadUInt16();
			Console.WriteLine($"Pmd unknown var is 0x{unknown:X4}");

			var gpPos = reader.ReadUInt32();
			var vertPos = reader.ReadUInt32();

			var frames = (reader.BaseStream.Length - vertPos) / (vertsPerFrame << 3);
			Console.WriteLine($"Pmd has {frames} frames");

			frameVertices = new Vector3[frames, vertsPerFrame];

			reader.BaseStream.Position = vertPos;
			for(var frame = 0; frame < frames; frame++) {
				for(var vertice = 0; vertice < vertsPerFrame; vertice++) {
					//Console.WriteLine($"{reader.BaseStream.Position}, vert {vertice}, frame {frame}");
					frameVertices[frame, vertice] = SVECTOR.Read(reader);
				}
			}
			reader.BaseStream.Position = 12; //where we were last at

			var objNum = reader.ReadUInt32();
			objects = new Object[objNum];

			for(var i = 0; i < objNum; i++) { //Read objects
				var gpNum = reader.ReadUInt32();
				var obj = new Object(gpNum);
				for (var j = 0; j < gpNum; j++) {
					obj.primgps[j] = new(reader, gpPos, vertPos, dupeBuffers);
				}
				objects[i] = obj;
			}
		}
	}
}
