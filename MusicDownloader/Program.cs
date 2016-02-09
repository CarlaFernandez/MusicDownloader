using System;
using System.Globalization;
using System.Net;
using YoutubeExtractor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MusicDownloader {

    class Program {
        private const string ACCOUNT_KEY = "FHqqliSvc6usIrNa9VKYuvcPJ4ky3wyFJdO+vrzSoxg";

        static void Main(string[] args) {

            try {

                string videoURL = GetVideoURL(args[0]);
                DownloadAudio(videoURL);


            } catch (Exception e) {

                string innerMsg = (e.InnerException != null) ? e.InnerException.Message : String.Empty;
                Console.WriteLine("Exception: {0}\n{1}", e.Message, innerMsg);

            }


        }

        private static void DownloadAudio(string videoURL) {
            try {
                IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(videoURL);

                /*
                * We want the first extractable video with the highest audio quality.
                */
                VideoInfo video = videoInfos
                    .Where(info => info.CanExtractAudio)
                    .OrderByDescending(info => info.AudioBitrate)
                    .First();

                /*
                 * If the video has a decrypted signature, decipher it
                 */
                if (video.RequiresDecryption) {
                    DownloadUrlResolver.DecryptDownloadUrl(video);
                }

                /*
                 * Create the audio downloader.
                 * The first argument is the video where the audio should be extracted from.
                 * The second argument is the path to save the audio file.
                 */

                string pathUser = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string pathDownload = Path.Combine(pathUser, "Downloads");
                var audioDownloader = new AudioDownloader(video, Path.Combine(pathDownload, video.Title + video.AudioExtension));

                // Register the progress events. We treat the download progress as 85% of the progress and the extraction progress only as 15% of the progress,
                // because the download will take much longer than the audio extraction.
                audioDownloader.DownloadProgressChanged += (sender, args) => Console.WriteLine(args.ProgressPercentage * 0.85);
                audioDownloader.AudioExtractionProgressChanged += (sender, args) => Console.WriteLine(85 + args.ProgressPercentage * 0.15);

                /*
                 * Execute the audio downloader.
                 * For GUI applications note, that this method runs synchronously.
                 */
                audioDownloader.Execute();
                Console.WriteLine("SUCCESSFULL OPERATION");
            } catch (Exception e) {
                Console.WriteLine("ERROR: {0}\n{1}", e.Message, e.InnerException.Message);
            }



        }

        private static string GetVideoURL(string arg) {
            string query = arg;
            // Create a Bing container. 

            string rootUri = "https://api.datamarket.azure.com/Bing/Search";

            var bingContainer = new BingSearchContainer(new Uri(rootUri));

            string market = CultureInfo.CurrentCulture.Name;

            // Configure bingContainer to use your credentials. 

            bingContainer.Credentials = new NetworkCredential(ACCOUNT_KEY, ACCOUNT_KEY);



            // Build the query. 

            var videoQuery = bingContainer.Video(query, null, market, null, null, null, null, "Relevance");
            videoQuery = videoQuery.AddQueryOption("$top", 50);

            var videoResults = videoQuery.Execute();

            var videoURL = "";
            foreach (var result in videoResults) {
                if (result.MediaUrl.Contains("youtube")) {
                    videoURL = result.MediaUrl;
                    break;
                }

            }
            return videoURL;
        }
    }

}
