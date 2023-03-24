using ImagetotextWeb.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System.Diagnostics;
using System.Xml.Linq;

namespace ImagetotextWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


       

        private async Task<string> ConvertSpeechtoText(string v)
        {
            string convertedspeech = string.Empty;
            try
            {
                var stopRecognition = new TaskCompletionSource<int>();

                var speechConfig = SpeechConfig.FromSubscription("d2c0a2c9085045a3996ac9aa92ae3e3b", "eastus");
                speechConfig.SpeechRecognitionLanguage = "en-US";

                speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "1000");  //silence duration
                var audioConfig = AudioConfig.FromWavFileInput(v);
                AudioConfig.From
                using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
                Console.WriteLine("Reading audio file ...");
                speechRecognizer.Recognizing += (s, e) =>
                {
                    // Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                };


                speechRecognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        convertedspeech = e.Result.Text;
                        //Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        convertedspeech = "NOMATCH: Speech could not be recognized";
                        //Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                speechRecognizer.Canceled += (s, e) =>
                {

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                speechRecognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n  Session stopped event.");
                    stopRecognition.TrySetResult(0);
                };

                await speechRecognizer.StartContinuousRecognitionAsync();

                // Waits for completion. Use Task.WaitAny to keep the task rooted.
                Task.WaitAny(new[] { stopRecognition.Task });
            }
            catch (Exception ex )
            {
                convertedspeech = ex.Message;
                
            }
            
            return convertedspeech;
        }

        [HttpPost]
        public async Task<IActionResult> FileUpload()
        {
            var file = Request.Form.Files[0];
            string path = "";
            string iscopied = string.Empty;
            string convertedspeech = string.Empty;
            try
            {
                if (file.Length > 0)
                {
                    string filename = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Upload"));
                    path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Upload"));

                    using (var filestream = new FileStream(Path.Combine(path, filename), FileMode.Create))
                    {
                        await file.CopyToAsync(filestream);
                    }
                    iscopied = "file uploaded succesfully";
                    convertedspeech = await ConvertSpeechtoText(Path.Combine(path, filename));
                }
                else
                {
                    iscopied ="file not found";
                }
            }
            catch (Exception ex )
            {
                iscopied=ex.Message;
            }

            return Json(new { message = iscopied,convertedspeech=convertedspeech });
        }

        
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}