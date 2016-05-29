using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class SPEECHController : Controller
    {
        public void Text2Speech()
        {
            string Text = Request.QueryString["text"];
            string Lang = Request.QueryString["lang"];

            if (string.IsNullOrEmpty(Text) || string.IsNullOrEmpty(Lang))
            {
                Response.Clear();
                Response.Flush();
                Response.End();
                return;
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

        //[HttpPost]
        //public async Task<ActionResult> Speech2Text(System.Net.Http.HttpRequestMessage request)
        //{
        //    string uploadFileName = Request.Files[0].FileName;
        //    if (string.IsNullOrEmpty(uploadFileName))
        //    {
        //        return Json(new { result = -1, message = "No Voice File." });
        //    }
        //    else
        //    {
        //        try
        //        {
                    
        //            return Json(new { result = 0, Identical = result.IsIdentical, Condidence = result.Confidence, member = result.memberName });
        //        }
        //        catch (Exception ex)
        //        {
        //            return Json(new { result = -1, message = ex.Message });
        //        }
        //    }
        //}

        // GET: SPEECH
        public ActionResult Index()
        {
            return View();
        }
    }
}