using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

namespace Imp.CitpSharp.FileSystemMediaServer
{
	internal class FileSystemMediaServerDevice : ICitpMediaServerDevice, IDisposable
	{
		private static readonly ImmutableHashSet<string> ImageFileExtensions = new[]
		{
			".png",
			".jpeg",
			".jpg",
			".bmp"
		}.ToImmutableHashSet();

		private static readonly ImmutableHashSet<string> MovieFileExtensions = new[]
		{
			".mov",
			".mp4",
			".m4v"
		}.ToImmutableHashSet();

		private FileSystemWatcher _watcher;

		private ImmutableDictionary<int, ImmutableDictionary<int, string>> _library = ImmutableDictionary<int, ImmutableDictionary<int, string>>.Empty;
		private ImmutableDictionary<MsexLibraryId, ElementLibrary> _citpLibrary = ImmutableDictionary<MsexLibraryId, ElementLibrary>.Empty;


		public FileSystemMediaServerDevice(Guid uuid, string peerName, string state, string productName,
			int productVersionMajor, int productVersionMinor, int productVersionBugfix, string libraryRootPath)
		{
			Uuid = uuid;
			PeerName = peerName;
			State = state;

			ProductName = productName;
			ProductVersionMajor = productVersionMajor;
			ProductVersionMinor = productVersionMinor;
			ProductVersionBugfix = productVersionBugfix;

			LibraryRootPath = libraryRootPath;



			_watcher = new FileSystemWatcher
			{
				Path = libraryRootPath,
				NotifyFilter = NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.LastWrite
				               | NotifyFilters.CreationTime | NotifyFilters.DirectoryName
			};

			_watcher.Changed += (s, e) => buildLibrary();
			_watcher.Deleted += (s, e) => buildLibrary();
			_watcher.Created += (s, e) => buildLibrary();
			_watcher.Renamed += (s, e) => buildLibrary();

			buildLibrary();
			_watcher.EnableRaisingEvents = true;
		}

		public string LibraryRootPath { get; }

		public Guid Uuid { get; }
		public string PeerName { get; }
		public string State { get; set; }

		public string ProductName { get; }
		public int ProductVersionMajor { get; }
		public int ProductVersionMinor { get; }
		public int ProductVersionBugfix { get; }

		public IImmutableSet<MsexVersion> SupportedMsexVersions =>
			new[]
			{
				MsexVersion.Version1_0,
				MsexVersion.Version1_1,
				MsexVersion.Version1_2
			}.ToImmutableHashSet();


		public IImmutableSet<MsexLibraryType> SupportedLibraryTypes =>
			new[]
			{
				MsexLibraryType.Media
			}.ToImmutableHashSet();

		public IImmutableSet<MsexImageFormat> SupportedThumbnailFormats =>
			new[]
			{
				MsexImageFormat.Rgb8,
				MsexImageFormat.Jpeg,
				MsexImageFormat.Png
			}.ToImmutableHashSet();

		public IImmutableList<ICitpMediaServerLayer> Layers { get; } = ImmutableList<ICitpMediaServerLayer>.Empty;

		public IImmutableDictionary<MsexLibraryId, ElementLibrary> ElementLibraries => _citpLibrary;

		public bool HasLibraryBeenUpdated { get; set; }



		public CitpImage GetVideoSourceFrame(int sourceId, CitpImageRequest request)
		{
			var buffer = new byte[request.FrameWidth * request.FrameHeight * 3];

			for (int i = 0; i < buffer.Length; i += 3)
				buffer[i] = 255;

			return new CitpImage(request, buffer, request.FrameWidth, request.FrameHeight);
		}

		public IImmutableSet<MsexImageFormat> SupportedStreamFormats =>
			new[]
			{
				MsexImageFormat.Rgb8,
				MsexImageFormat.Jpeg,
				MsexImageFormat.Png,
				MsexImageFormat.FragmentedJpeg,
				MsexImageFormat.FragmentedPng
			}.ToImmutableHashSet();

		public IImmutableDictionary<int, VideoSourceInformation> VideoSourceInformation => ImmutableDictionary<int, VideoSourceInformation>.Empty;


		public IImmutableList<ElementLibraryUpdatedInformation> GetLibraryUpdateInformation()
		{
			return ImmutableList<ElementLibraryUpdatedInformation>.Empty;
		}

		public CitpImage GetElementLibraryThumbnail(CitpImageRequest request, ElementLibraryInformation elementLibrary)
		{
			return null;
		}

		public CitpImage GetElementThumbnail(CitpImageRequest request, ElementLibraryInformation elementLibrary,
			ElementInformation element)
		{
			if (!_library.TryGetValue(elementLibrary.Id.LibraryNumber, out var localLibrary))
				return null;

			if (!localLibrary.TryGetValue(element.ElementNumber, out var filePath))
				return null;

			var extension = Path.GetExtension(filePath);

			Image<Rgba32> image;

			if (MovieFileExtensions.Contains(extension))
			{
				string outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + FileExtensions.Png);
				var task = Conversion.Snapshot(filePath, outputPath, TimeSpan.Zero).Start();

				task.Wait();

				if (!task.Result.Success)
					return null;

				// TODO: Remove this step when the jpeg bug in ImageSharp is fixed
				string outputPathNative = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "native" + FileExtensions.Png);
				System.Drawing.Image imageNative = System.Drawing.Image.FromFile(outputPath);
				imageNative.Save(outputPathNative);
				imageNative.Dispose();
				
				image = Image.Load(outputPathNative);

				File.Delete(outputPath);
				File.Delete(outputPathNative);

			}
			else if (ImageFileExtensions.Contains(extension))
			{
				image = Image.Load(filePath);
			}
			else
			{
				return null;
			}

			using (var ms = new MemoryStream())
			{
				image.Mutate(c => c.Resize(request.FrameWidth, request.FrameHeight));

				switch (request.Format)
				{
					case MsexImageFormat.Rgb8:

						var pixels = image.CloneAs<Rgb24>().GetPixelSpan();

						if (request.IsBgrOrder)
						{
							foreach (var p in pixels)
							{
								ms.WriteByte(p.B);
								ms.WriteByte(p.G);
								ms.WriteByte(p.R);
							}
						}
						else
						{
							foreach (var p in pixels)
							{
								ms.WriteByte(p.R);
								ms.WriteByte(p.G);
								ms.WriteByte(p.B);
							}
						}

						break;

					case MsexImageFormat.Png:
						image.SaveAsPng(ms);
						break;

					case MsexImageFormat.Jpeg:
						image.SaveAsJpeg(ms);
						break;

					default:
						return null;
				}

				var citpImage = new CitpImage(request, ms.ToArray(), image.Width, image.Height);

				image.Dispose();

				return citpImage;
			}
		}

		private void buildLibrary()
		{
			var pathRegex = new Regex("[0-9]{3}.+");

			var directories = Directory.GetDirectories(LibraryRootPath, "", SearchOption.TopDirectoryOnly)
				.Where(p => pathRegex.IsMatch(p))
				.Select(p => Tuple.Create(int.Parse(Path.GetFileName(p).Substring(0, 3)), p))
				.Where(t => t.Item1 >= 0 && t.Item1 <= 255);

			var updatedLibrary = new Dictionary<int, ImmutableDictionary<int, string>>();

			foreach (var dir in directories)
			{
				var files = Directory.GetFiles(dir.Item2, "", SearchOption.TopDirectoryOnly);

				var libraryFiles = new Dictionary<int, string>();

				foreach (var f in files)
				{
					if (!pathRegex.IsMatch(f))
						continue;

					int index = int.Parse(Path.GetFileNameWithoutExtension(f).Substring(0, 3));

					if (index < 0 || index > 255 || libraryFiles.ContainsKey(index))
						continue;

					libraryFiles.Add(index, f);
				}

				updatedLibrary.Add(dir.Item1, libraryFiles.ToImmutableDictionary());
			}

			_library = updatedLibrary.ToImmutableDictionary();

			_citpLibrary = _library.ToImmutableDictionary(p => MsexLibraryId.FromMsexV1LibraryNumber(p.Key),
				p => new ElementLibrary(MsexLibraryType.Media,
					new ElementLibraryInformation(MsexLibraryId.FromMsexV1LibraryNumber(p.Key),
						(byte)p.Key, (byte)p.Key, p.Key.ToString(), 0, (ushort)p.Value.Count, 0),
					p.Value.Select(e => new MediaInformation((byte)e.Key, (byte)e.Key, (byte)e.Key,
						Path.GetFileNameWithoutExtension(e.Value), File.GetLastWriteTime(e.Value),
						0, 0, 0, 0, 0))));
		}

		public void Dispose()
		{
			_watcher.Dispose();
		}
	}
}