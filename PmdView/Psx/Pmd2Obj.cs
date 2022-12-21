using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;



namespace PmdView.Psx {
	public static class Pmd2Obj {
		static void WriteMtl(string name, StringBuilder sb, int idx) {
			sb.AppendFormat("newmtl Mort_{1}_{0}\n", name, idx);
			sb.AppendLine(
@"d 1
illum 0
Ks 0.000 0.000 0.000
Kd 1.000 1.000 1.000"
			);
			sb.AppendFormat("map_Kd {1}_{0}.png\n", name, idx);
		}
		static void WriteVt(byte u, byte v, Tim tim, StringBuilder sb) {
			int width = tim.picWidth;
			switch(tim.pixelMode) {
				case PixelMode.Bpp4: width *= 4; break;
				case PixelMode.Bpp8: width *= 2; break;
			}			
			float un = (float)((u == width-1) ? (u+1) : u) / (float)width;
			float vn = 1 - (float)((v == (tim.picHeight-1)) ? (v+1) : v) / (float)tim.picHeight;
			sb.AppendFormat("vt {0} {1}\n", un, vn);
		}
		public static void WriteObjAndMtl(in Tim[] tims, in Pmd pmd, string outDir, int frame) {
			StringBuilder vSB = new(); //Vertex list sb
			StringBuilder vtSB = new(); //Texture list sb
			StringBuilder gpSB = new(); //Group sb
			StringBuilder mtlSB = new(); //MTL sb

			mtlSB.AppendLine("mtllib test.mtl");
			for(var i = 0; i < tims.Length; i++) {
				WriteMtl(tims[i].name ?? $"Tex{i}", mtlSB, i);
			}
			for(var i = 0; i < pmd.frameVertices.GetLength(1); i++) {
				var vert = pmd.frameVertices[frame, i];
				vSB.AppendFormat("v {0} {1} {2}\n", vert.X, vert.Y, vert.Z);
			}

			int texlistIdx = 1;
			int lastmat = -1;
			for (var i = 0; i < pmd.objects.Length; i++) {
				var obj = pmd.objects[i];
				gpSB.AppendFormat("g MortObj_{0}\n", i);
				for(var j = 0; j < obj.primgps.Length; j++) {
					var gp = obj.primgps[j];
					var type = gp.type;
					for(var k = 0; k < gp.packets.Length; k++) {
						var packet = gp.packets[k];
						var prim = packet.prim[0];
						if(!type.HasFlag(PrimGpType.NoTex)) { //has tex
							if(prim.tpage != lastmat) {
								gpSB.AppendFormat("usemtl Mort_{1}_{0}\n", tims[prim.tpage].name, prim.tpage);
								lastmat = prim.tpage;
							}
							/*WriteVt(prim.u0, prim.v0, tims[prim.tpage], vtSB);
							WriteVt(prim.u1, prim.v1, tims[prim.tpage], vtSB);
							WriteVt(prim.u2, prim.v2, tims[prim.tpage], vtSB);*/
							//texlistIdx += 3;
							if(type.HasFlag(PrimGpType.IsQuad)) {
								//WriteVt(prim.u3, prim.v3, tims[prim.tpage], vtSB);
								//texlistIdx++;
							}
							gpSB.Append("f");
							/*gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[0]+1, texlistIdx++);
							gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[1]+1, texlistIdx++);
							if(type.HasFlag(PrimGpType.IsQuad)) {
								gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[3]+1, texlistIdx++);
							}
							gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[2]+1, texlistIdx++);*/
							if (type.HasFlag(PrimGpType.IsQuad)) {
								WriteVt(prim.u3, prim.v3, tims[prim.tpage], vtSB);
								gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[3] + 1, texlistIdx++);
							}
							WriteVt(prim.u1, prim.v1, tims[prim.tpage], vtSB);
							gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[1] + 1, texlistIdx++);
							WriteVt(prim.u0, prim.v0, tims[prim.tpage], vtSB);
							gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[0] + 1, texlistIdx++);
							WriteVt(prim.u2, prim.v2, tims[prim.tpage], vtSB);
							gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[2] + 1, texlistIdx++);
							/*for(var l = 0; l < packet.vertsShared.Length; l++) {
								gpSB.AppendFormat(" {0}/{1}", packet.vertsShared[l], texlistIdx++);
							}*/
						} else {
							gpSB.Append("f");
							/*gpSB.AppendFormat(" {0}", packet.vertsShared[0]+1);
							gpSB.AppendFormat(" {0}", packet.vertsShared[1]+1);
							if(type.HasFlag(PrimGpType.IsQuad)) {
								gpSB.AppendFormat(" {0}", packet.vertsShared[3]+1);
							}
							gpSB.AppendFormat(" {0}", packet.vertsShared[2]+1);*/
							if (type.HasFlag(PrimGpType.IsQuad)) {
								gpSB.AppendFormat(" {0}", packet.vertsShared[3] + 1);
							}
							gpSB.AppendFormat(" {0}", packet.vertsShared[1] + 1);
							gpSB.AppendFormat(" {0}", packet.vertsShared[0] + 1);
							gpSB.AppendFormat(" {0}", packet.vertsShared[2] + 1);
							/*for (var l = 0; l < packet.vertsShared.Length; l++) {
								gpSB.AppendFormat(" {0}", packet.vertsShared[l]);
							}*/
						}
						gpSB.AppendLine();
					}
				}
			}

			StreamWriter wr = new(@"D:\DownloadFolder\Mort the Chicken (USA)\STAGE1\STAGE1\test.obj");
			
			wr.Write(vSB);
			wr.Write(vtSB);
			wr.Write(gpSB);
			wr.Flush();
			wr.Close();
			wr.Dispose();

			wr = new(@"D:\DownloadFolder\Mort the Chicken (USA)\STAGE1\STAGE1\test.mtl");
			wr.Write(mtlSB);
			wr.Flush();
			wr.Close();
			wr.Dispose();
		}
	}
}
