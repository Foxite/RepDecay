using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Flann;

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

			IEnumerable<string> images;
			try {
				images = DuplicateImages(await ImageStore.Instance.GetDataAsync(id, async () => {
					using var client = new HttpClient();
					client.DefaultRequestHeaders.Add("User-Agent", Program.UserAgent);
					var doc = new HtmlDocument();
					HttpResponseMessage result = await client.GetAsync($"https://ruqqus.com/post/{id}");
					doc.LoadHtml(await result.Content.ReadAsStringAsync());
					HtmlNode metaImage = doc.DocumentNode.SelectNodes("//html/head/meta[@property=\"og:image\"]").FirstOrDefault();
					HttpResponseMessage downloadImage = await client.GetAsync(metaImage.GetAttributeValue("content", null));
					if (downloadImage.Content.Headers.ContentType.MediaType == "image/jpeg" ||
						downloadImage.Content.Headers.ContentType.MediaType == "image/png") {
						return await downloadImage.Content.ReadAsStreamAsync();
					} else {
						throw new InvalidDataException();
					}
				}));
			} catch (InvalidDataException) {
				return UnprocessableEntity();
			}

			return new JsonResult(images);
		}

		private IEnumerable<string> DuplicateImages(ImageData imageData) {
			var stw = new Stopwatch();

			stw.Start();

			ConcurrentBag<string> results = new ConcurrentBag<string>();
			using var matcher = new FlannBasedMatcher(new IndexParams(), new SearchParams());

			ParallelEnumerable.ForAll(ImageStore.Instance.AsParallel(), otherImageData => {
				DMatch[][] matches = matcher.KnnMatch(imageData.Features, otherImageData.Features, 2);

				// These constants have been stolen from https://github.com/magamig/duplicate_images_finder
				const float DistanceModifier = 0.3f;
				const int MinMatches = 50;

				int matchCount = 0;
				for (int i = 0; i < matches.Length; i++) {
					if (matches[i][0].Distance < DistanceModifier * matches[i][1].Distance) {
						matchCount++;
						if (matchCount >= MinMatches) {
							results.Add(otherImageData.Posts[0].Id); // TODO multiple posts
							break;
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
