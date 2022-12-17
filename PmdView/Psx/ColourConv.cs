using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Psx {
	public static class ColourConv {
		public static uint ARGB1555toARGB32(ushort value) {
			uint colour;
			colour =
				(uint)(
				(((((value & 0b0_00000_00000_11111) >>  0) * 255 + 15) / 31) << 0) |
				(((((value & 0b0_00000_11111_00000) >>  5) * 255 + 15) / 31) << 8) |
				(((((value & 0b0_11111_00000_00000) >> 10) * 255 + 15) / 31) << 16)
			);

			if ((value & 0b1_00000_00000_00000) == 0b1_00000_00000_00000) { //Semi trans?
				//colour.A = 0;
			} else if (value == 0) { //Completely blank pixel?
				//colour.A = 0;
			} else {
				//colour.A = 255; //Default, visible
				colour |= (0xFF000000);
			}
			return colour;
		}
		/*
		 * BGRA5551:
		 * 0bBBBBBGGG_GGRRRRRA
		 * ARGB1555:
		 * 0bARRRRRGG_GGGBBBBB
		 */
		public static ushort ARGB1555toBGRA5551(ushort value) {
			return (ushort) (
				((value & 0b10000000_00000000) >> 15) | //Semi trans bit
				((value & 0b00000000_00011111) << 11) | //Blue
				((value & 0b00000011_11100000) << 1) | //Green
				((value & 0b01111100_00000000) >> 9) //Red
				);
		}
	}
}
