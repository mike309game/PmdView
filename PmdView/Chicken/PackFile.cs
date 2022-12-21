using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PmdView.Chicken {
	public enum PfType {
		Texture = 0x31,
		Model = 0x32,
	}
	public class PackFile : IDisposable {
		public PfType type;
		public List<PackFile.File> files;

		public class File {
			public byte[] data;
			public string filename;

			public File(BinaryReader reader, long dataBlockOffset) {
				var filename = reader.ReadChars(0x10);
				if(filename[0x0F] != 0) { //Beeg filename
					var posHex = (reader.BaseStream.Position - 0x10).ToString("X8");
					throw new InvalidDataException($"Packfile has file at 0x{posHex} with no null terminator");
				}
				this.filename = new string(filename).Replace("\0", string.Empty); //isn't csharp just *wonderful*?
				var dataLen = reader.ReadInt32();
				var dataOffset = reader.ReadUInt32(); //Offset from start of data block

				var posBackup = reader.BaseStream.Position;
				reader.BaseStream.Position = dataBlockOffset + dataOffset;
				data = reader.ReadBytes(dataLen);
				reader.BaseStream.Position = posBackup;
			}
		}

		public PackFile(BinaryReader reader) {
			var type = reader.ReadUInt16();
			if(type != 0x31 && type != 0x32) { //Anomaly, or perhaps invalid file
				var typeHex = type.ToString("X2");
				throw new InvalidDataException($"Got unknown packfile type 0x{typeHex}");
			}
			this.type = (PfType)type;

			var fileCount = reader.ReadUInt16();
			var fileBlockSize = reader.ReadUInt32(); //figured out this is actually the file block size???

			var headerSize = reader.ReadUInt32(); //Header size, NOT counting file info block
			if (headerSize != 0x14) { //Very likely anomaly
				var sizeHex = headerSize.ToString("X4");
				throw new InvalidDataException($"Packfile header size isn't 0x14, got 0x{sizeHex}");
			}
			var dataSize = reader.ReadUInt32(); //Size of data block
			var firstDataAddr = reader.ReadUInt32();
			if(firstDataAddr != (fileBlockSize + 0x14 & (~3))) { //Anomaly
				var fileBlockSizeHex = fileBlockSize.ToString("X4");
				var firstDataAddrHex = firstDataAddr.ToString("X4");
				throw new InvalidDataException($"Packfile anomaly, fileBlockSize 0x{fileBlockSizeHex}, firstDataAddr 0x{firstDataAddrHex}");
			}

			files = new();
			for(var i = 0; i < fileCount; i++) {
				files.Add(new File(reader, firstDataAddr));
			}
		}

		public static PackFile FromStream(Stream stream) {
			using(BinaryReader reader = new(stream)) {
				return new(reader);
			}
		}

		public static PackFile FromFile(string path) {
			using(FileStream stream = new(path, FileMode.Open)) {
				return FromStream(stream);
			}
		}

		public void Dump(string path, bool prefixIndex) {
			for (var i = 0; i < files.Count; i++) {
				string fname = $"{path}{Path.DirectorySeparatorChar}{(prefixIndex ? (i.ToString() + "_") : null)}{files[i].filename}";
				using (var stream = new FileStream(fname, FileMode.Create)) {
					stream.Write(files[i].data);
					stream.Close();
					stream.Dispose();
				}
			}
		}

		public void Dispose() {
			files.Clear();
		}
	}
}
