using PmdView.Chicken;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Psx {
	public static class Pmd2Ply {
		public static void WritePly(in Pmd pmd, string fname, string tpfFname) {
			List<Tim> tims = new();
			using(var tpfFile = File.OpenRead(tpfFname)) {
				using(var reader = new BinaryReader(tpfFile)) {
					using(var tpf = new PackFile(reader)) {
						foreach(var item in tpf.files) {
							using(var itemStream = new MemoryStream(item.data)) {
								using(var itemReader = new BinaryReader(itemStream)) {
									Tim tim = new(itemReader);
									
									switch (tim.pixelMode) {
										case PixelMode.Bpp4:
											tim.graphicRect.X *= 4;
											break;
										case PixelMode.Bpp8:
											tim.graphicRect.X *= 2;
											break;
									}
									tims.Add(tim);
								}
							}
						}
					}
				}
			}

			GC.Collect();

			List<Vector3> verts = new();
			List<int[]> faces = new();
			//List<Vector3[]> colours = new();
			List<Vector3> colours = new(); //temp for ply testing
			List<Vector2> uvs = new();
			int idxCounter = 0;

			foreach(var obj in pmd.objects) {
				foreach(var pg in obj.primgps) {
					foreach(var packet in pg.packets) {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
						var prim = packet.prim[0];
						var tim = tims[prim.tpage];
						//var tim = tims[0];
						prim.tpage = tim.GetTpagValue();
						if(!pg.type.HasFlag(PrimGpType.NoTex))
							Console.WriteLine(prim.tpage);

						int[] face = new int[pg.type.HasFlag(PrimGpType.IsQuad) ? 4 : 3];
						Vector3[] colour = new Vector3[face.Length];
						Vector2[] uv = new Vector2[face.Length];

						if (pg.type.HasFlag(PrimGpType.SharedVerts)) {
							verts.Add(pmd.frameVertices[0, packet.vertsShared[0]]);
							verts.Add(pmd.frameVertices[0, packet.vertsShared[1]]);
							verts.Add(pmd.frameVertices[0, packet.vertsShared[2]]);
						}

						face[0] = idxCounter++; colour[0] = new(packet.prim[0].r0, packet.prim[0].g0, packet.prim[0].b0); uv[0] = new(packet.prim[0].u0, packet.prim[0].v0);
						face[1] = idxCounter++; colour[1] = new(packet.prim[0].r1, packet.prim[0].g1, packet.prim[0].b1); uv[1] = new(packet.prim[0].u1, packet.prim[0].v1);
												
						if (pg.type.HasFlag(PrimGpType.IsQuad)) {
							if (pg.type.HasFlag(PrimGpType.SharedVerts)) {
								verts.Add(pmd.frameVertices[0, packet.vertsShared[3]]);
							}
							face[3] = idxCounter++; colour[3] = new(packet.prim[0].r3, packet.prim[0].g3, packet.prim[0].b3); uv[3] = new(packet.prim[0].u3, packet.prim[0].v3);
							
						}
						face[2] = idxCounter++; colour[2] = new(packet.prim[0].r2, packet.prim[0].g2, packet.prim[0].b2); uv[2] = new(packet.prim[0].u2, packet.prim[0].v2);

						{
							for(var i = 0; i < uv.Length; i++) {
								/*var xx = ((packet.prim[0].tpage & 0b1111)) * 64;
								var yy = (packet.prim[0].tpage & 0b10000) == 0b10000 ? 256 : 0;*/

								//I take quite a disliking towards the playstation's texture mapping
								uv[i].X += uv[i].X == (tim.graphicRect.Width - 1) ? 1 : 0;
								uv[i].Y += uv[i].Y == (tim.graphicRect.Height - 1) ? 1 : 0;

								var xx = tim.graphicRect.X;
								var yy = tim.graphicRect.Y;
								var bpp = (packet.prim[0].tpage & 0b110000000) >> 7;

								/*if(bpp == 0) {
									xx *= 4;
								} else if(bpp == 1) {
									xx *= 2;
								}*/
								uv[i].X = (xx + uv[i].X) / 4096;
								uv[i].Y = 1 - ((yy + uv[i].Y + (bpp == 0 ? 0 : 512)) / 1024);
							}
						}

						{
							colours.Add(colour[0]); uvs.Add(uv[0]);
							colours.Add(colour[1]); uvs.Add(uv[1]);

							colours.Add(colour[2]); uvs.Add(uv[2]);
							if (pg.type.HasFlag(PrimGpType.IsQuad)) {
								colours.Add(colour[3]); uvs.Add(uv[3]);
							}
						}

						/*Console.WriteLine($"{packet.prim[0].u0} {packet.prim[0].u1} {packet.prim[0].u2} {packet.prim[0].u3}");
						Console.WriteLine($"{packet.prim[0].v0} {packet.prim[0].v1} {packet.prim[0].v2} {packet.prim[0].v3}\n");*/

						faces.Add(face);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
					}
				}
			}

			var file = new StreamWriter(fname);

			file.WriteLine($@"ply
format ascii 1.0
comment bitch
element vertex {verts.Count}
property short y
property short z
property short x
property float s
property float t
property uchar red
property uchar green
property uchar blue
element face {faces.Count}
property list uchar uint vertex_indices
end_header");

			for(var i = 0; i < verts.Count; i++) {
				var vert = verts[i];
				var colour = colours[i];
				var uv = uvs[i];
				file.WriteLine($"{vert.X} {-vert.Y} {-vert.Z} {uv.X.ToString("G", CultureInfo.InvariantCulture)} {uv.Y.ToString("G", CultureInfo.InvariantCulture)} {colour.X} {colour.Y} {colour.Z}");
			}
			for(var i = 0; i < faces.Count; i++) {
				var face = faces[i];
				file.Write($"{face.Length} {face[0]} {face[1]} {face[2]}");
				if(face.Length == 4) {
					file.Write($" {face[3]}");
				}
				file.WriteLine();
			}
			file.Flush();
			file.Close();
		}
	}
}
