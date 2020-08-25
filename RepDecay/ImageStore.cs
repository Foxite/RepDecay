using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace RepDecay {
	public class ImageStore : IEnumerable<ImageData> {
		private readonly Dictionary<string, ImageData> m_Store;

		public static ImageStore Instance { get; private set; }

		public ImageStore() {
			Instance = this;

			string[] files = Directory.GetFiles(Path.Combine("C:", "temp", "images"));
			m_Store = new Dictionary<string, ImageData>(files.Length);
			foreach (string path in files) {
				var data = new ImageData(new Mat(path), new List<float>());
				data.Posts.Add(new RuqqusPost(Path.GetFileNameWithoutExtension(path)));
				m_Store.Add(Path.GetFileNameWithoutExtension(path), data);
			}
		}

		public Task UpdateDataAsync(string id, ImageData data) {
			m_Store[id] = data;
			data.Features.SaveImage(Path.Combine("C:", "temp", "images", id + ".png"));
			return Task.CompletedTask;
		}

		public async Task<ImageData> GetDataAsync(string id, Func<Task<Stream>> getImageStream) {
			if (m_Store.TryGetValue(id, out ImageData ret)) {
				return ret;
			} else {
				ret = await ComputeDataAsync(getImageStream);
				await UpdateDataAsync(id, ret);
				return ret;
			}
		}

		private async Task<ImageData> ComputeDataAsync(Func<Task<Stream>> getImageStream) {
			Mat image;
			using (var stream = await getImageStream()) {
				image = Mat.FromStream(stream, ImreadModes.AnyColor);
			}

			var sift = SIFT.Create();
			var imagePoints = new VectorOfKeyPoint();
			Mat descriptors = new Mat();

			sift.DetectAndCompute(image, null, out var keypoints, descriptors, false);

			// TODO compute dominant hues

			return new ImageData(descriptors, new List<float>());
		}
		

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<ImageData> GetEnumerator() => m_Store.Values.GetEnumerator();
	}
}
