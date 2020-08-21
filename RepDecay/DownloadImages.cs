using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Hsv = SixLabors.ImageSharp.ColorSpaces.Hsv;

namespace RepDecay {
	public static class DownloadImages {
		/// <summary>
		/// Downsize an image to be at most 500 pixels tall or wide, grayscale it, and save it as a bitmap.
		/// </summary>
		public static async Task ConvertImage(string name, Stream stream) {
			// Note: OpenCV supports webp images, but ImageSharp does not. However, it's being worked on. https://github.com/SixLabors/ImageSharp/issues/121
			// There's apparently a functional decoder for lossless and lossy images, which is all we need as we're converting them to bitmaps anyway.
			// TODO: Use the prerelease ImageSharp build with support for loading webp images
			using var image = await Image.LoadAsync<Rgb24>(stream);
			var converter = new ColorSpaceConverter();

			await Task.Run(() => {
				// Scale image to at most 500x500 (maintain aspect ratio)
				if (image.Width > 500 || image.Height > 500) {
					image.Mutate(ctx => {
						Size size;
						if (image.Width > image.Height) {
							size = new Size(500, image.Height / image.Width * 500);
						} else if (image.Width < image.Height) {
							size = new Size(image.Width / image.Height * 500, 500);
						} else {
							size = new Size(500, 500);
						}

						ctx.Resize(new ResizeOptions() {
							Mode = ResizeMode.Stretch,
							Size = size
						});
					});
				}

				// Greyscale image by setting the saturation of every pixel to zero
				for (int x = 0; x < image.Width; x++) {
					for (int y = 0; y < image.Height; y++) {
						Hsv pixel = converter.ToHsv(image[x, y]);
						image[x, y] = converter.ToRgb(new Hsv(0, 0, pixel.V));
					}
				}
			});

			// Save as bmp for fast loading
			await image.SaveAsBmpAsync(File.OpenWrite(Path.Combine(Program.ImageStoragePath, name)));

			// Save mat data
			using var cvImage = new Image<Gray, byte>(Path.Combine(Program.ImageStoragePath, name));
			using var imageMat = new Mat();
			await Task.Run(() => {
				using var sift = new Emgu.CV.XFeatures2D.SIFT();
				using var imagePoints = new VectorOfKeyPoint();

				sift.DetectAndCompute(cvImage, null, imagePoints, imageMat, false);
			});
			using FileStorage fs = new FileStorage(Path.Combine(Program.MatStoragePath, name + ".xml"), FileStorage.Mode.Write);
			fs.Write(imageMat, "mat");
		}
	}
}
