using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PmdView.Psx;
using System.Text;

namespace PmdView {
	internal class Program {
		static void Main(string[] args) {
			using(var game = new GameMain()) {
				game.Run();
			}
			
			
		}
	}
}