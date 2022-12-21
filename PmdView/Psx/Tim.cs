using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Psx {
	public enum PixelMode {
		Bpp4,
		Bpp8,
		Bpp16,
		Bpp24,
		Mixed
	}


	
	public class Tim {
		public PixelMode pixelMode;
		public bool hasClut;
		
		public ushort[,] pixelData;
		public ushort[,] clutData;

		public int clutX, clutY, clutWidth, clutHeight;
		public int picX, picY, picWidth, picHeight;
		public int RealWidth {
			get {
				switch(pixelMode) {
					case PixelMode.Bpp4: return picWidth * 4;
					case PixelMode.Bpp8: return picWidth * 2;
					default: return picWidth;
				}
			}
		}

		public string? name;

		public ushort GetTpagValue() {
			return (ushort)((((picX) / 64) & 15) |
				((((picY) / 256) & 1) << 4) |
				(((0) & 3) << 5) |
				((((ushort)pixelMode) & 3) << 7));
		}

		public Tim(BinaryReader reader) {
			Console.WriteLine($"Loading tim {(reader.BaseStream as FileStream)?.Name}");
			//Read ID
			if (reader.ReadChar() != 0x10) { //Read tag (1)
				throw new InvalidDataException("Tim tag isn't 0x10");
			}
			if(reader.ReadChar() != 0) { //Read version (1)
				throw new InvalidDataException("Tim version is unknown");
			}
			if(reader.ReadUInt16() != 0) { //Read padding (2)
				throw new InvalidDataException("Tim ID padding is non-zero");
			}
			
			var flags = reader.ReadUInt32();//Read flag (4)
			pixelMode = (PixelMode)(flags & 3);
			if(pixelMode > PixelMode.Bpp16) {
				throw new NotImplementedException($"24bpp/mixed tims are not implemented");
			}
			hasClut = (flags & 0b1000) == 0b1000;
			if((flags & 0b11111111_11111111_11111111_11110000) != 0) { //Data in padding space?
				throw new InvalidDataException("Tim has data in flag padding space");
			}

			//Read clut, if there is one
			if(hasClut) {
				clutData = ReadBitMap(reader, out clutX, out clutY, out clutWidth, out clutHeight, PixelMode.Bpp16);
				switch(pixelMode) {
					case PixelMode.Bpp4:
						if(clutWidth != 16) {
							Console.WriteLine($"Tim is 4bpp and clut width is {clutWidth}");
						}
						break;
					case PixelMode.Bpp8:
						if (clutWidth != 256) {
							Console.WriteLine($"Tim is 8bpp and clut width is {clutWidth}");
						}
						break;
				}
				if(clutHeight != 1) {
					Console.WriteLine($"Tim clut height is {clutHeight}");
				}
			}

			//Read pixel data
			pixelData = ReadBitMap(reader, out picX, out picY, out picWidth, out picHeight, in pixelMode);
		}

		ushort[,] ReadBitMap(BinaryReader reader, out int x, out int y, out int width, out int height, in PixelMode pixelMode) {
			var len = reader.ReadInt32(); //Data WITH HEADER len in bytes

			x = reader.ReadUInt16(); //Framebuffer X
			y = reader.ReadUInt16(); //Framebuffer Y

			width = reader.ReadUInt16();
			height = reader.ReadUInt16();

			ushort[,] data = new ushort[width, height];

			for(var yy = 0; yy < height; yy++) {
				for (var xx = 0; xx < width; xx++) {
					data[xx, yy] = reader.ReadUInt16();
				}
			}
			
			return data;
		}

		public TimBlitter ToFramebuffer() {
			TimBlitter blitter = new(RealWidth, picHeight, false);
			blitter.BlitTim(this);
			return blitter;
		}

		public static Tim FromStream(Stream stream) {
			using(BinaryReader reader = new(stream)) {
				return new(reader);
			}
		}

		public static Tim FromBytes(in byte[] data) {
			using(MemoryStream stream = new(data)) {
				return FromStream(stream);
			}
		}
	}
}