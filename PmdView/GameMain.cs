using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using PmdView.Psx;
using Microsoft.Xna.Framework.Input;
using ImGuiNET;
using PmdView.Chicken;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Xml.Xsl;

namespace PmdView {
	internal class GameMain : Game {
		GraphicsDeviceManager gdm;

		ImGuiRenderer imRenderer;
		ImGuiIOPtr io;

		string pmdPath = string.Empty;
		string tpfPath = string.Empty;
		XnaPmd mdl;
		Tim[] tims;
		int mdlFrame;

		RenderTarget2D vram;
		SpriteBatch sbatch;

		VramMgr vramMgr = new();

		Vector2 vramOffset = new();

		public void LoadTpf(string path) {
			path = path.Trim('"');
			using (var file = new FileStream(path, FileMode.Open)) {
				using (var reader = new BinaryReader(file)) {
					PackFile pf = new(reader);
					tims = new Tim[pf.files.Count];
					for(var i = 0; i < tims.Length; i++) {
						MemoryStream stream = new(pf.files[i].data);
						BinaryReader timReader = new(stream);
						Tim tim = new(timReader);
						stream.Dispose();
						timReader.Dispose();
						vramMgr.BlitTim(tim);
						tims[i] = tim;
					}
				}
			}
			tpfPath = string.Empty;
			vram.SetData<uint>(vramMgr.framebuffer);
		}

		public void LoadPmd(string path) {
			path = path.Trim('"');
			mdl?.Dispose();
			using(var file = new FileStream(path, FileMode.Open)) {
				using(var reader = new BinaryReader(file)) {
					Pmd pmd;
					try {
						pmd = new(reader, 2);
					} catch(Exception) {
						Console.WriteLine("Pmd loading failed with double buffer prims, trying without");
						reader.BaseStream.Position = 0;
						pmd = new(reader, 1);
					}
					mdlFrame = 0;
					mdl = new(in pmd, GraphicsDevice);
					mdl.tims = tims;
				}
			}
			pmdPath = string.Empty;
		}

		public GameMain() {
			gdm = new(this) {
				SynchronizeWithVerticalRetrace = true
			};
			IsMouseVisible = true;
			IsFixedTimeStep = true; //unlimited framerate
			Window.AllowUserResizing = true;
		}


		float yaw = 0;
		float pitch = 0;
		Vector3 mdlScale = new(1);
		bool canControlCam = true;
		bool canToggleCam = true;
		Vector3 camPos = new();

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.Black);

			sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

			sbatch.Draw(vram, vramOffset, Color.White);
			sbatch.End();

			//Vector3 camPos = new(MathF.Cos((float)gameTime.TotalGameTime.TotalSeconds) * 4, MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds) * 4, 128);

			//Vector3 cameraPosition = new Vector3(1130.0f, 130.0f, 1130.0f);
			//Vector3 cameraTarget = new Vector3(0.0f, 0.0f, 0.0f); // Look back at the origin
			
			Vector3 cameraTarget = new Vector3(0.0f, 0, 0.0f); // Look back at the origin
			Vector3 cameraPosition = new Vector3(
				(float)Math.Cos(gameTime.TotalGameTime.TotalSeconds) * 100,
				200,
				(float)Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 100);

			if (canControlCam) {
				yaw -= io.MouseDelta.X;
				pitch -= io.MouseDelta.Y;
				pitch = Math.Clamp(pitch, -90, 90);
			}

			Matrix rot = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(yaw), MathHelper.ToRadians(pitch), 0f);
			var keystate = Keyboard.GetState();
			if (canControlCam) {
				float mult = keystate.IsKeyDown(Keys.LeftShift) ? 8 : 1;
				if (keystate.IsKeyDown(Keys.W)) {
					camPos += rot.Forward * mult;
				}
				if (keystate.IsKeyDown(Keys.S)) {
					camPos += rot.Backward * mult;
				}
				if (keystate.IsKeyDown(Keys.A)) {
					camPos += rot.Left * mult;
				}
				if (keystate.IsKeyDown(Keys.D)) {
					camPos += rot.Right * mult;
				}
				if (keystate.IsKeyDown(Keys.E)) {
					camPos += rot.Up * mult;
				}
				if (keystate.IsKeyDown(Keys.Q)) {
					camPos += rot.Down * mult;
				}
			}
			if (keystate.IsKeyDown(Keys.Escape)) {
				if (canToggleCam) {
					canControlCam = !canControlCam;
					canToggleCam = false;
				}
			} else {
				canToggleCam = true;
			}

			Matrix view = Matrix.CreateLookAt(rot.Translation + camPos, rot.Forward + camPos, rot.Up);
			//Matrix view = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
			Matrix proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), GraphicsDevice.Viewport.AspectRatio, 1f, 16000f);
			Matrix world = Matrix.Identity;
			//Matrix sc = Matrix.CreateScale((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds) * 4);
			Matrix sc = Matrix.CreateScale(mdlScale);

			mdl?.Draw(mdlFrame, world * sc, view, proj, GraphicsDevice, ref vram);

			imRenderer.BeforeLayout(gameTime);
			ImGui.Begin("fart");
			ImGui.TextUnformatted($"yaw {yaw} pitch {pitch}");
			ImGui.SliderInt("Frame", ref mdlFrame, 0, mdl?.pmd.frameVertices.GetLength(0)-1 ?? 0);
			var scaleNumerics = Shenanigans.XnaToNumerics(mdlScale);
			ImGui.SliderFloat3("Model scale", ref scaleNumerics, -16, 16);
			mdlScale = Shenanigans.NumericsToXna(scaleNumerics);

			ImGui.InputText("Tpf", ref tpfPath, 512);
			ImGui.SameLine();
			if(ImGui.Button("Load")) {
				LoadTpf(tpfPath);
			}
			ImGui.InputText("Pmd", ref pmdPath, 512);
			ImGui.SameLine();
			if (ImGui.Button("Load##pmd")) {
				LoadPmd(pmdPath);
			}

			if (ImGui.CollapsingHeader("g")) {
				ImGui.Indent();
				if(ImGui.Button("Show all")) {
					for (var i = 0; i < mdl?.objs.Length; i++) {
						mdl.objs[i].show = true;
					}
				}
				for(var i = 0; i < mdl?.objs.Length; i++) {
					ImGui.PushID(i);
					ImGui.Checkbox($"Object {i}", ref mdl.objs[i].show);
					ImGui.SameLine();
					if (ImGui.Button("Solo")) {
						for(var j = 0; j < mdl.objs.Length; j++) {
							mdl.objs[j].show = j == i;
						}
					}
					ImGui.PopID();
				}
				ImGui.Unindent();
			}

			ImGui.End();
			imRenderer.AfterLayout();

			base.Draw(gameTime);
		}
		protected override void Update(GameTime gameTime) {
			//PutTim(test);
			if (io.MouseDown[2]) {
				vramOffset.X += io.MouseDelta.X;
				vramOffset.Y += io.MouseDelta.Y;
			}
			base.Update(gameTime);
		}
		protected override void Initialize() {
			
			base.Initialize();
		}
		protected override void LoadContent() {
			sbatch = new(GraphicsDevice);
			imRenderer = new(this);
			imRenderer.RebuildFontAtlas();
			io = ImGui.GetIO();

			var args = Environment.GetCommandLineArgs();

			vram = new(GraphicsDevice, VramMgr.VRAMWIDTH, VramMgr.VRAMHEIGHT, false, SurfaceFormat.Color, DepthFormat.None);
			vram.SetData<uint>(vramMgr.framebuffer);

			GC.Collect();

			base.LoadContent();
		}

		void PutBitMap(Texture2D texture, Rectangle rect, PixelMode pm) {
			
			GraphicsDevice.SetRenderTarget(vram);
			
			var batch = new SpriteBatch(GraphicsDevice);
			switch(pm) {
				case PixelMode.Bpp4:
					rect.X *= 4;
					break;
				case PixelMode.Bpp8:
					rect.X *= 2;
					rect.Y += 512;
					break;
				case PixelMode.Bpp16:
					rect.X += 1024 * 2;
					rect.Y += 512;
					break;

			}
			//rect = tim.graphicRect;
			/*rect.X += Mouse.GetState().X;
			rect.Y += Mouse.GetState().Y;*/

			batch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
			batch.Draw(texture, rect, Color.White);
			batch.End();
			batch.Dispose();
			
			GraphicsDevice.SetRenderTargets(null);
		}
	}
}
