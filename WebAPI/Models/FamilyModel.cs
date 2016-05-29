using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WebAPI.Controllers;

namespace WebAPI.Models
{
    public class FamilyClass
    {
        public int Id;
        public string Name;
        public string FaceURL;
        public Guid FaceGUID;

        public FamilyClass(int id, string name, string faceURL)
        {
            this.Id = id;
            this.Name = name;
            this.FaceURL = faceURL;
        }
    }

    public class FamilyModel
    {
        public static List<FamilyClass> FamilyList = new List<FamilyClass>();
        public Stream GuestImageStream;
        public Guid guestGUID;
        public FaceServiceClient objFaceSrv;

        public FamilyModel(Stream guestFaceStream)
        {
            string FACE_API_Key = System.Configuration.ConfigurationManager.AppSettings["FACE_API_Key"];
            objFaceSrv = new FaceServiceClient(FACE_API_Key);
            GuestImageStream = guestFaceStream;
        }        

        async public Task<FamilyVerifyResult> Verification()
        {
            DateTime beginDT = DateTime.UtcNow;
            if (FamilyList.Count == 0)
            {
                FamilyList.Add(new FamilyClass(0, "Kevin", "http://kkcognitivestorage.blob.core.windows.net/homefaces/Family01.jpg"));
                FamilyList.Add(new FamilyClass(1, "Ashley", "http://kkcognitivestorage.blob.core.windows.net/homefaces/Family02.jpg"));
                FamilyList.Add(new FamilyClass(2, "Lynn", "http://kkcognitivestorage.blob.core.windows.net/homefaces/Family03.jpg"));

                await GetFamilyGUIDs();
            }

            guestGUID = await getGUID(GuestImageStream);


            string memberName = "";
            VerifyResult verifyResult = new VerifyResult();
            foreach (var family in FamilyList)
            {
                verifyResult = await objFaceSrv.VerifyAsync(family.FaceGUID, guestGUID);
                if (verifyResult.IsIdentical)
                {
                    memberName = family.Name;
                    break;
                }
            }            

            FamilyVerifyResult returnVerifyResult = new FamilyVerifyResult();
            returnVerifyResult.timespend = DateTime.UtcNow - beginDT;
            returnVerifyResult.Confidence = verifyResult.Confidence;
            returnVerifyResult.IsIdentical = verifyResult.IsIdentical;
            returnVerifyResult.memberName = memberName;
            return returnVerifyResult;
        }

        async private Task<bool> GetFamilyGUIDs()
        {
            for (int i = 0; i < FamilyList.Count; i++)
            {
                FamilyList[i].FaceGUID = await getGUID(FamilyList[i].FaceURL);
            }
            return true;
        }

        async private Task<Guid> getGUID(Stream faceSource)
        {
            Face[] objFace = await objFaceSrv.DetectAsync(faceSource);
            if (objFace.Length > 0)
                return objFace[0].FaceId;
            else
                throw new Exception("Fail on Detect Face Image");
        }

        async private Task<Guid> getGUID(string faceSource)
        {
            Face[] objFace = await objFaceSrv.DetectAsync(faceSource);
            if (objFace.Length > 0)
                return objFace[0].FaceId;
            else
                throw new Exception("Fail on Detect Face Image");
        }

    }
}