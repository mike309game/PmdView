using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Psx {
	public class TimBlitter {
		public const int VRAMWIDTH = 4096;
		public const int VRAMHEIGHT = 1024;

		public int width;
		public int height;
		public bool isVram;
		public uint[] framebuffer;// = new uint[VRAMWIDTH * VRAMHEIGHT];

		public void ClearVram() {
			for(var yy = 0; yy < height; yy++) {
				for(var xx = 0; xx < width; xx++) {
					framebuffer[xx + yy * width] = 0;
				}
			}
		}

		public void BlitTim(Tim tim, int x = 0, int y = 0) {
			if(isVram) {
				x = tim.picX;
				y = tim.picY;
			}
			switch(tim.pixelMode) {
				case PixelMode.Bpp16:
					if (isVram) {
						y += 512;
						x += 2048;
					}
					for (var yy = 0; yy < tim.picHeight; yy++) {
						for (var xx = 0; xx < tim.picWidth; xx++) {
							framebuffer[xx + yy * width] = ColourConv.ARGB1555toARGB32(tim.pixelData[xx, yy]);
						}
					}
					break;
				case PixelMode.Bpp8:
					if (isVram) {
						y += 512;
						x *= 2;
					}
					for (var yy = 0; yy < tim.picHeight; yy++) {
						for (var xx = 0; xx < tim.picWidth; xx++) {
							var value = tim.pixelData[xx, yy];
							var pos = x + (xx * 2) + ((y + yy) * width);
							framebuffer[pos + 0] =		ColourConv.ARGB1555toARGB32(tim.clutData[(value & 0b00000000_11111111) >> 0, 0]);
							framebuffer[pos + 1] =		ColourConv.ARGB1555toARGB32(tim.clutData[(value & 0b11111111_00000000) >> 8, 0]);
						}
					}
					break;
				case PixelMode.Bpp4:
					if(isVram)
						x *= 4;
					for (var yy = 0; yy < tim.picHeight; yy++) {
						for (var xx = 0; xx < tim.picWidth; xx++) {
							var value = tim.pixelData[xx, yy];
							var pos = x + (xx * 4) + ((y + yy) * width);
							framebuffer[pos + 0] =		ColourConv.ARGB1555toARGB32(tim.clutData[(value & 0b0000_0000_0000_1111) >>  0, 0]);
							framebuffer[pos + 1] =		ColourConv.ARGB1555toARGB32(tim.clutData[(value & 0b0000_0000_1111_0000) >>  4, 0]);
							framebuffer[pos + 2] =		ColourConv.ARGB1555toARGB32(tim.clutData[(value & 0b0000_1111_0000_0000) >>  8, 0]);
							framebuffer[pos + 3] =		ColourConv.ARGB1555toARGB32(tim.clutData[(value & 0b1111_0000_0000_0000) >> 12, 0]);
						}
					}
					break;
			}
			if (isVram) {
				for (var yy = 0; yy < tim.clutHeight; yy++) {
					for (var xx = 0; xx < tim.clutWidth; xx++) {
						framebuffer[(xx + tim.clutX + 2048) + (yy + tim.clutY + 512) * width] = ColourConv.ARGB1555toARGB32(tim.clutData[xx, yy]);
					}
				}
			}
		}

		public TimBlitter(int width = VRAMWIDTH, int height = VRAMHEIGHT, bool isVram = true) {
			this.width = width;
			this.height = height;
			this.isVram = isVram;
			framebuffer = new uint[width * height];
			ClearVram();
		}
	}
}
