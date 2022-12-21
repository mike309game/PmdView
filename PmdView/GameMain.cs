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
using RectpackSharp;
using System.Linq.Expressions;

namespace PmdView {
	internal class GameMain : Game {
		GraphicsDeviceManager gdm;

		ImGuiRenderer imRenderer;
		ImGuiIOPtr io;

		string pmdPath = string.Empty;
		string tpfPath = string.Empty;
		string pfPath = string.Empty;
		string pfDumpFolder = string.Empty;
		bool pfDumpPrefixIndex = true;

		string plyExportFolder = string.Empty;
		string objExportFolder = string.Empty;

		XnaPmd? mainMdl;
		PackFile? pf;
		TimBundle? timBundle;
		int mdlFrame;

		SpriteBatch sbatch;

		Vector2 vramOffset = new();

		public void LoadTpf(string path) {
			path = path.Trim('"');
			timBundle?.Dispose();
			timBundle = null;
			try {
				timBundle = TimBundle.FromTpf(path, GraphicsDevice);
				tpfPath = string.Empty;
			} catch(Exception e) {
				lastException = e;
				showErrorModal = true;
			}
		}

		public void LoadMainPmd(string path) {
			path = path.Trim('"');
			mainMdl?.Dispose();
			mainMdl = null;
			mdlFrame = 0;
			try {
				var pmd = Pmd.FromFile(path);
				mainMdl = new(in pmd, GraphicsDevice);
				pmdPath = string.Empty;
			} catch (Exception e) {
				mainMdl?.Dispose();
				lastException = e;
				showErrorModal = true;
			}
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
		bool showErrorModal = false;
		Exception lastException;
		int pfSelectedItem = -1;

		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.Black);

			if (timBundle?.texture is not null) {
				sbatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
				sbatch.Draw(timBundle.texture, vramOffset, Color.White);
				sbatch.End();
			}

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

			imRenderer.BeforeLayout(gameTime);

			if (timBundle is not null) {
				try {
					mainMdl?.Draw(mdlFrame, world * sc, view, proj, GraphicsDevice, in timBundle);
				} catch (Exception e) {
					lastException = e;
					showErrorModal = true;
					mainMdl?.Dispose();
					mainMdl = null;
				}
			}

			if (showErrorModal) {
				ImGui.OpenPopup("Error");
				showErrorModal = false;
			}
			bool dummy = true;
			
			if(ImGui.BeginPopupModal("Error", ref dummy, ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.TextUnformatted($"ERROR:\n{lastException?.ToString() ?? "Something nasty's going on or my code sucks (likely case)"}");
				if(ImGui.Button("OK")) {
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
			ImGui.Begin("Main", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
			
			if(ImGui.Button("Export main model to ply")) {
				try {
					for(var i = 0; i < mainMdl?.pmd.frameVertices.GetLength(0); i++) {
						string path = $"{plyExportFolder}{Path.DirectorySeparatorChar}Frame_{i}.ply";
						Pmd2Ply.WritePly(in mainMdl.pmd, path, in timBundle, i);
					}
					using (FileStream stream = new($"{plyExportFolder}{Path.DirectorySeparatorChar}Texture.png", FileMode.Create))
						timBundle.texture.SaveAsPng(stream, timBundle.texture.Width, timBundle.texture.Height);
				} catch(Exception e) {
					lastException = e;
					showErrorModal = true;
				}
			}
			ImGui.SameLine();
			ImGui.InputText("##imtired", ref plyExportFolder, 512);

			ImGui.TextUnformatted($"yaw {yaw} pitch {pitch}");
			ImGui.SliderInt("Frame", ref mdlFrame, 0, mainMdl?.pmd.frameVertices.GetLength(0)-1 ?? 0);
			var scaleNumerics = Shenanigans.XnaToNumerics(mdlScale);
			ImGui.SliderFloat3("Model scale", ref scaleNumerics, -16, 16);
			mdlScale = Shenanigans.NumericsToXna(scaleNumerics);

			ImGui.InputText("Texture bundle path (TPF)", ref tpfPath, 512);
			ImGui.SameLine();
			if(ImGui.Button("Load")) {
				LoadTpf(tpfPath);
			}
			ImGui.InputText("Main model (PMD)", ref pmdPath, 512);
			ImGui.SameLine();
			if (ImGui.Button("Load##pmd")) {
				LoadMainPmd(pmdPath);
			}

			//////////////////////////////////////////////////

			if (ImGui.CollapsingHeader("Object Visibility")) {
				ImGui.Indent();
				if(ImGui.Button("Show all")) {
					for (var i = 0; i < mainMdl?.objs.Length; i++) {
						mainMdl.objs[i].show = true;
					}
				}
				for(var i = 0; i < mainMdl?.objs.Length; i++) {
					ImGui.PushID(i);
					ImGui.Checkbox($"Object {i}", ref mainMdl.objs[i].show);
					ImGui.SameLine();
					if (ImGui.Button("Solo")) {
						for(var j = 0; j < mainMdl.objs.Length; j++) {
							mainMdl.objs[j].show = j == i;
						}
					}
					ImGui.PopID();
				}
				ImGui.Unindent();
			}

			//////////////////////////////////////////////////
			if(ImGui.CollapsingHeader("Packfile Viewer")) {
				ImGui.Indent();
				ImGui.TextUnformatted("Path to packfile");
				ImGui.InputText("##pfpath", ref pfPath, 512); ImGui.SameLine();
				if (ImGui.Button("Load##pfloadbt")) {
					pfPath = pfPath.Trim('"');
					pf?.Dispose();
					pfSelectedItem = -1;
					try {
						pf = PackFile.FromFile(pfPath);
					} catch (Exception e) {
						pf = null;
						lastException = e;
						showErrorModal = true;
					}
				}
				if (pf is not null) {
					if (ImGui.BeginListBox("##pflistbox", new(200f, 200f))) {
						for(var i = 0; i < pf.files.Count; i++) {
							bool isSelected = pfSelectedItem == i;
							ImGui.PushID(i);
							if (ImGui.Selectable(pf.files[i].filename, isSelected)) {
								pfSelectedItem = i;
							}
							ImGui.PopID();
							if(isSelected) {
								ImGui.SetItemDefaultFocus();
							}
						}
						ImGui.EndListBox();
					}
					ImGui.SameLine();
					ImGui.BeginGroup();
					if(ImGui.Button("Load as model")) {
						try {
							mdlFrame = 0;
							mainMdl?.Dispose();
							Pmd pmd = Pmd.FromBytes(in pf.files[pfSelectedItem].data);
							mainMdl = new(in pmd, GraphicsDevice);
						} catch(Exception e) {
							lastException = e;
							showErrorModal = true;
						}
					}
					ImGui.EndGroup();
					ImGui.InputText("##pfdumppath", ref pfDumpFolder, 512); ImGui.SameLine();
					if (ImGui.Button("Export files to folder")) {
						try {
							pfDumpFolder = pfDumpFolder.Trim('"');
							if (pfDumpFolder == string.Empty)
								throw new Exception("HEY JACKASS!!!!!! You don't want to dump files in the root of the program.\nInsert the path to a folder.");
							else
								pf.Dump(pfDumpFolder, pfDumpPrefixIndex);
						} catch (Exception e) {
							lastException = e;
							showErrorModal = true;
						}
					}
					ImGui.Checkbox("Prefix filename with index in packfile (RECOMMENDED)", ref pfDumpPrefixIndex);
				}
				ImGui.Unindent();
			}

			ImGui.End();
			imRenderer.AfterLayout();

			base.Draw(gameTime);
		}
		protected override void Update(GameTime gameTime) {
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

			GC.Collect();

			base.LoadContent();
		}
	}
}
