using Microsoft.Xna.Framework.Graphics;
using PmdView.Psx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RectpackSharp;
using PmdView.Chicken;

namespace PmdView {
	public class TimBundle : IDisposable {
		public Texture2D texture;
		public Tim[] tims;
		public PackingRectangle[] rects;
		public PackingRectangle bounds = new(0,0,2048,2048);
		public TimBundle(in Tim[] tims, GraphicsDevice gd) {
			this.tims = tims;
			rects = new PackingRectangle[tims.Length];
			for(var i = 0; i < rects.Length; i++) {
				rects[i] = new(0, 0, (uint)tims[i].RealWidth, (uint)tims[i].picHeight, i);
			}
			RectanglePacker.Pack(rects, out bounds);
			rects = rects.OrderBy(item => { return item.Id; }).ToArray();
			TimBlitter blitter = new((int)bounds.Width, (int)bounds.Height, false);
			for (var i = 0; i < rects.Length; i++) {
				blitter.BlitTim(tims[i], (int)rects[i].X, (int)rects[i].Y);
			}
			texture = new(gd, (int)bounds.Width, (int)bounds.Height);
			texture.SetData(blitter.framebuffer);
		}

		public static TimBundle FromTpf(string path, GraphicsDevice gd) {
			PackFile pf = PackFile.FromFile(path);
			Tim[] tims = new Tim[pf.files.Count];
			for(var i = 0; i < pf.files.Count; i++) {
				tims[i] = Tim.FromBytes(in pf.files[i].data);
				tims[i].name = pf.files[i].filename;
			}
			return new(in tims, gd);
		}

		public void Dump(string folder, GraphicsDevice gd) {
			for(var i = 0; i < tims.Length; i++) {
				var tim = tims[i];
				string path = $"{folder}{Path.DirectorySeparatorChar}{i}_{tim.name}";
				var blit = tim.ToFramebuffer();
				Texture2D tex = new(gd, tim.RealWidth, tim.picHeight);
				tex.SetData(blit.framebuffer);
				using(FileStream stream = new(path, FileMode.Create)) {
					tex.SaveAsPng(stream, tex.Width, tex.Height);
				}
				tex.Dispose();
			}
		}

		public void Dispose() {
			texture?.Dispose();
		}
	}
}