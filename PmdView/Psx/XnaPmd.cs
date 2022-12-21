using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Psx {
	public class XnaPmd : IDisposable {
		public class Object {
			public class PrimGp {
				public VertexBuffer vb;
				public bool hasTex = false;
			}
			public bool show = true;
			public PrimGp[] gps;
			public int idx;

			public Object(int idx, in Pmd pmd) {
				this.idx = idx;
				gps = new PrimGp[pmd.objects[idx].primgps.Length];
				//gps.Initialize();
				for(var i = 0; i < gps.Length; i++) {
					gps[i] = new();
				}
			}

			public void GenMesh(in TimBundle bundle, in Pmd pmd, int frame, GraphicsDevice gd) {
				var obj = pmd.objects[idx];
				for (var i = 0; i < gps.Length; i++) {
					var gp = gps[i];
					gp.vb?.Dispose();
					var type = obj.primgps[i].type;
					gp.hasTex = !type.HasFlag(PrimGpType.NoTex);

					List<VertexPositionColorTexture> vertsTex = null;
					List<VertexPositionColor> vertsClr = null;

					if(gp.hasTex) {
						vertsTex = new();
					} else {
						vertsClr = new();
					}

					for (var j = 0; j < obj.primgps[i].packets.Length; j++) {
						var packet = obj.primgps[i].packets[j];
						var prim = packet.prim[0];

						var vec1 = Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[0]]);
						var col1 = new Color(prim.r0, prim.g0, prim.b0);
						var vec2 = Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[1]]);
						var col2 = new Color(prim.r1, prim.g1, prim.b1);
						var vec3 = Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[2]]);
						var col3 = new Color(prim.r2, prim.g2, prim.b2);
						Vector3 vec4 = Vector3.Zero;
						Color col4 = Color.White;
						if (type.HasFlag(PrimGpType.IsQuad)) {
							vec4 = Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[3]]);
							col4 = new Color(prim.r3, prim.g3, prim.b3);
						}
						if(gp.hasTex) {
							//var verts = new List<VertexPositionColorTexture>();
							var rect = bundle.rects[prim.tpage];
							Vector2 uvAddition = new(rect.X, rect.Y);
							Vector2[] uv = new Vector2[4];
							uv[0] = new Vector2(prim.u0, prim.v0);
							uv[1] = new Vector2(prim.u1, prim.v1);
							uv[2] = new Vector2(prim.u2, prim.v2);
							uv[3] = new Vector2(prim.u3, prim.v3);

							for (var k = 0; k < 4; k++) {
								uv[k].X = (uv[k].X == rect.Width - 1) ? uv[k].X + 1 : uv[k].X;
								uv[k].Y = (uv[k].Y == rect.Height - 1) ? uv[k].Y + 1 : uv[k].Y;
								uv[k] += uvAddition;
								uv[k].X /= bundle.bounds.Width;
								uv[k].Y /= bundle.bounds.Height;
							}

							VertexPositionColorTexture vert1 = new(vec1, col1, uv[0]);
							VertexPositionColorTexture vert2 = new(vec2, col2, uv[1]);
							VertexPositionColorTexture vert3 = new(vec3, col3, uv[2]);
							vertsTex.Add(vert1);
							vertsTex.Add(vert2);
							vertsTex.Add(vert3);
							if (type.HasFlag(PrimGpType.IsQuad)) {
								VertexPositionColorTexture vert4 = new(vec4, col4, uv[3]);
								vertsTex.Add(vert3);
								vertsTex.Add(vert4);
								vertsTex.Add(vert2);
							}
							//gp.vb = new(gd, typeof(VertexPositionColorTexture), verts.Count, BufferUsage.WriteOnly);
							//gp.vb.SetData<VertexPositionColorTexture>(verts.ToArray());
						} else {
							//var verts = new List<VertexPositionColor>();
							VertexPositionColor vert1 = new(vec1, col1);
							VertexPositionColor vert2 = new(vec2, col2);
							VertexPositionColor vert3 = new(vec3, col3);
							vertsClr.Add(vert1);
							vertsClr.Add(vert2);
							vertsClr.Add(vert3);
							if (type.HasFlag(PrimGpType.IsQuad)) {
								VertexPositionColor vert4 = new(vec4, col4);
								vertsClr.Add(vert3);
								vertsClr.Add(vert4);
								vertsClr.Add(vert2);
							}
							//gp.vb = new(gd, typeof(VertexPositionColor), verts.Count, BufferUsage.WriteOnly);
							//gp.vb.SetData<VertexPositionColor>(verts.ToArray());
						}
					}

					if(gp.hasTex) {
						gp.vb = new(gd, typeof(VertexPositionColorTexture), vertsTex.Count, BufferUsage.WriteOnly);
						gp.vb.SetData<VertexPositionColorTexture>(vertsTex.ToArray());
					} else {
						gp.vb = new(gd, typeof(VertexPositionColor), vertsClr.Count, BufferUsage.WriteOnly);
						gp.vb.SetData<VertexPositionColor>(vertsClr.ToArray());
					}
				}
			}
		}

		BasicEffect basicEffect;
		public Pmd pmd;
		public Tim[] tims;
		public VertexBuffer vb;
		int lastFrame = -1;
		public Object[] objs;

		/*void RegenMesh(int frame, GraphicsDevice gd) {
			vb?.Dispose();
			List<VertexPositionColorTexture> verts = new();
			foreach (var obj in pmd.objects) {
				foreach (var gp in obj.primgps) {
					foreach (var packet in gp.packets) {
						var prim = packet.prim[0];
						var tim = tims[prim.tpage];
						var timWidth = tim.picWidth;
						Vector2 uvAddition = new(tim.picX, tim.picY);
						if (tim.pixelMode == PixelMode.Bpp4) {
							uvAddition.X *= 4;
							timWidth *= 4;
						} else if (tim.pixelMode == PixelMode.Bpp8) {
							uvAddition.X *= 2;
							uvAddition.Y += 512;
							timWidth *= 4;
						}
						Vector2[] uv = new Vector2[4];
						uv[0] = new Vector2(prim.u0, prim.v0);
						uv[1] = new Vector2(prim.u1, prim.v1);
						uv[2] = new Vector2(prim.u2, prim.v2);
						uv[3] = new Vector2(prim.u3, prim.v3);


						for(var i = 0; i < 4; i++) {
							uv[i].X = (uv[i].X == timWidth - 1) ? uv[i].X + 1 : uv[i].X;
							uv[i].Y = (uv[i].Y == tim.picHeight - 1) ? uv[i].Y + 1 : uv[i].Y;
							uv[i] += uvAddition;
							uv[i].X /= 4096;
							uv[i].Y /= 1024;
						}
						

						VertexPositionColorTexture vert1 = new(Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[0]]), new Color(prim.r0, prim.g0, prim.b0), uv[0]);
						VertexPositionColorTexture vert2 = new(Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[1]]), new Color(prim.r1, prim.g1, prim.b1), uv[1]);
						VertexPositionColorTexture vert3 = new(Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[2]]), new Color(prim.r2, prim.g2, prim.b2), uv[2]);
						verts.Add(vert1);
						verts.Add(vert2);
						verts.Add(vert3);
						if (gp.type.HasFlag(PrimGpType.IsQuad)) {
							VertexPositionColorTexture vert4 = new(Shenanigans.NumericsToXna(pmd.frameVertices[frame, packet.vertsShared[3]]), new Color(prim.r3, prim.g3, prim.b3), uv[3]);
							verts.Add(vert3);
							verts.Add(vert4);
							verts.Add(vert2);
						}
					}
				}
			}
			vb = new(gd, typeof(VertexPositionColorTexture), verts.Count, BufferUsage.WriteOnly);
			vb.SetData<VertexPositionColorTexture>(verts.ToArray());
		}*/

		public XnaPmd(in Pmd pmd, GraphicsDevice gd) {
			basicEffect = new(gd);
			basicEffect.LightingEnabled = false;
			basicEffect.VertexColorEnabled = true;
			basicEffect.Alpha = 1f;
			basicEffect.TextureEnabled = true;
			this.pmd = pmd;
			objs = new Object[pmd.objects.Length];
			for (var i = 0; i < objs.Length; i++) {
				objs[i] = new(i, in pmd);
			}
		}

		public void Draw(int frame, Matrix world, Matrix view, Matrix projection, GraphicsDevice gd, in TimBundle bundle) {
			if(frame != lastFrame) {
				lastFrame = frame;
				//RegenMesh(frame, gd);
				foreach(var obj in objs) {
					obj.GenMesh(in bundle, in pmd, frame, gd);
				}
			}
			basicEffect.Projection = projection;
			basicEffect.View = view;
			basicEffect.World = world;
			//gd.SetVertexBuffer(vb);
			gd.RasterizerState = RasterizerState.CullNone;
			gd.DepthStencilState = DepthStencilState.Default;
			gd.SamplerStates[0] = SamplerState.PointWrap;
			basicEffect.Texture = bundle.texture;
			foreach(EffectPass pass in basicEffect.CurrentTechnique.Passes) {
				pass.Apply();
				//gd.DrawPrimitives(Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, 0, vb.VertexCount / 3);
				//bool doTex = true;
				foreach(var obj in objs) {
					if(obj.show) {
						foreach(var gp in obj.gps) {
							if(gp.hasTex != basicEffect.TextureEnabled) {
								basicEffect.TextureEnabled = gp.hasTex;
								pass.Apply();
							}
							gd.SetVertexBuffer(gp.vb);
							//pass.Apply();
							gd.DrawPrimitives(Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, 0, gp.vb.VertexCount / 3);
						}
					}
				}
			}
		}

		public void Dispose() {
			foreach(var obj in objs) {
				foreach(var gp in obj.gps) {
					gp.vb?.Dispose();
				}
			}
		}
	}
}
