using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class VISIONController : Controller
    {
        [HttpPost]
        public async Task<ActionResult> Compute(System.Net.Http.HttpRequestMessage request)
        {
            string uploadFileName = Request.Files[0].FileName;
            if (string.IsNullOrEmpty(uploadFileName))
            {
                return Json(new { result = -1, message = "No Image File." });
            }
            else
            {
                try
                {
                    ImageVision imageV = new ImageVision(Request.Files[0].InputStream);
                    string caption = await imageV.Compute();
                    return Json(new { result = 0, message = caption });
                }
                catch (Exception ex)
                {
                    return Json(new { result = -1, message = ex.Message });
                }
            }
        }

        [HttpPost]
        public async Task ComputeWithVoiceReturn(System.Net.Http.HttpRequestMessage request)
        {
            string Lang = "en-US";
            string Text = "";

            string uploadFileName = Request.Files[0].FileName;
            if (string.IsNullOrEmpty(uploadFileName))
            {
                Text = "can't find upload media file";
            }
            else
            {
                try
                {
                    ImageVision imageV = new ImageVision(Request.Files[0].InputStream);
                    Text = await imageV.Compute();
                }
                catch (Exception ex)
                {
                    Text = ex.Message;
                }
            }

            TTSpeech tts = new TTSpeech(Text, Lang);
            byte[] voiceByte = tts.GetSpeech();

            if (tts.speechStream != null)
            {
                Response.Clear();
                Response.AddHeader("Content-Length", voiceByte.Length.ToString());
                Response.AddHeader("Content-Disposition", "attachment; filename=speech.wav");
                Response.OutputStream.Write(voiceByte, 0, voiceByte.Length);
                Response.Flush();
                Response.End();
            }
            else
            {
                Response.Clear();
                Response.Flush();
                Response.End();
            }
        }

        // GET: VISION
        public ActionResult Index()
        {
            return View();
        }
    }
}