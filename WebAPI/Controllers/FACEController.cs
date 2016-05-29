using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    public class FamilyVerifyResult : VerifyResult
    {
        public string memberName;
        public TimeSpan timespend;
    }

    public class FACEController : Controller
    {
        [HttpPost]
        public async Task<ActionResult> Verification(System.Net.Http.HttpRequestMessage request)
        {
            string uploadFileName = Request.Files[0].FileName;
            if (string.IsNullOrEmpty(uploadFileName))
            {                
                return Json(new {result=-1, message="No Image File." });               
            }
            else
            {
                try
                {
                    FamilyModel faceDetect = new FamilyModel(Request.Files[0].InputStream);
                    FamilyVerifyResult result = await faceDetect.Verification();
                    return Json(new { result = 0, Identical = result.IsIdentical, Condidence = result.Confidence, member = result.memberName, timeSpend= result.timespend.TotalSeconds});                    
                }
                catch (Exception ex)
                {
                    return Json(new { result = -1, message=ex.Message });
                }
            }
        }

        [HttpPost]
        public async Task VerificationWithVoiceReturn(System.Net.Http.HttpRequestMessage request)
        {
            string Lang = "en-US";
            string Text = "";

            if (Request.Form["lang"] != null)
                Lang = Request.Form["lang"].ToString();

            string uploadFileName = Request.Files[0].FileName;
            if (string.IsNullOrEmpty(uploadFileName))
            {
                if (Lang == "zh-TW")
                    Text = "找不到上傳相片";
                else
                    Text = "can't find upload media file";
            }
            else
            {
                try
                {
                    FamilyModel faceDetect = new FamilyModel(Request.Files[0].InputStream);
                    FamilyVerifyResult result = await faceDetect.Verification();
                    if (result.IsIdentical)
                    {
                        if (Lang == "zh-TW")
                            Text = "你是家人, " + result.memberName + ". 信心指數 : " + result.Confidence;
                        else
                            Text = "You are family, " + result.memberName + ". Confidence : " + result.Confidence;
                    }
                    else
                    {
                        if (Lang == "zh-TW")
                            Text = "你不是家人";
                        else
                            Text = "You are Not family";
                    }
                }
                catch (Exception)
                {
                    if (Lang == "zh-TW")
                        Text = "無法辨識相片裡的臉孔";
                    else
                        Text = "Can't identify Image Face";
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

        // GET: FACE
        public ActionResult Index()
        {
            return View();
        }
    }
}