using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Util;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace RepDecay.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class GetRepostsController : ControllerBase {
		private readonly ILogger m_Logger;

		public GetRepostsController(ILogger<GetRepostsController> logger) {
			m_Logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> Get(string id) {
			if (string.IsNullOrWhiteSpace(id)) {
				return BadRequest();
			}


			if (!System.IO.File.Exists(Path.Combine(Program.MatStoragePath, id + ".xml"))) {
				using var client = new HttpClient();
				client.DefaultRequestHeaders.Add("User-Agent", Program.UserAgent);
				var doc = new HtmlDocument();
				HttpResponseMessage result = await client.GetAsync($"https://ruqqus.com/post/{id}");
				doc.LoadHtml(await result.Content.ReadAsStringAsync());
				HtmlNode metaImage = doc.DocumentNode.SelectNodes("//html/head/meta[@property=\"og:image\"]").FirstOrDefault();
				HttpResponseMessage downloadImage = await client.GetAsync(metaImage.GetAttributeValue("content", null));
				if (downloadImage.Content.Headers.ContentType.MediaType == "image/jpeg" ||
					downloadImage.Content.Headers.ContentType.MediaType == "image/png") {
					using Stream downloadStream = await downloadImage.Content.ReadAsStreamAsync();
					await Util.SaveMatsForImage(id, downloadStream);
				}
			}

			return new JsonResult(DuplicateImages(id));
		}

		private IEnumerable<string> DuplicateImages(string duplicateOf) {
			var stw = new Stopwatch();

			using Mat imageMat = new Mat();
			//using FileStorage fs = new FileStorage("C:/temp/mats.xml", FileStorage.Mode.Read);
			stw.Start();
			Program.FileStore["mat_" + duplicateOf].ReadMat(imageMat);

			ConcurrentBag<string> results = new ConcurrentBag<string>();
			using var matcher = new FlannBasedMatcher(new KdTreeIndexParams(5), new SearchParams(50));

			ParallelEnumerable.ForAll(Directory.GetFiles(Program.MatStoragePath).AsParallel(), filename => {
				filename = Path.GetFileNameWithoutExtension(filename);
				if (filename != duplicateOf) {
					using Mat otherImageMat = new Mat();
					Program.FileStore["mat_" + filename].ReadMat(otherImageMat);

					using var matches = new VectorOfVectorOfDMatch();
					matcher.KnnMatch(imageMat, otherImageMat, matches, 2);

					// These constants have been stolen from https://github.com/magamig/duplicate_images_finder
					const float DistanceModifier = 0.3f;
					const int MinMatches = 50;

					int matchCount = 0;
					for (int i = 0; i < matches.Size; i++) {
						if (matches[i][0].Distance < DistanceModifier * matches[i][1].Distance) {
							matchCount++;
							if (matchCount >= MinMatches) {
								results.Add(filename);
								break;
							}
						}
					}
				}
			});
			stw.Stop();
			m_Logger.LogInformation((stw.ElapsedMilliseconds / 1000f).ToString("#.###"));
			return results;
		}
	}
}
