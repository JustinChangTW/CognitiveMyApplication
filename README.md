# CognitiveMyApplication
MyApplication is .NET Web API which offer below integrated services base on Microsoft Cognitive APIs:
- MyFamily
- VisionTell

Step 1: Login to Azure Portal with your subscription account

Step 2: Create a Resource Group to host this application, for example: CognitiveMyApplication

Step 3: Under CognitiveMyApplication, create two Cognitive APIs, one for FACE, another for SPEECH; get their account name and key.

Step 4: Open brower, access to URL(https://www.microsoft.com/cognitive-services/en-us/computer-vision-api), press "Get Start for Free" to subscribe VISION API.

Step 5: Open Visual Studio 2015, create a ASP.NET Web API project (MVC + Web API), and update web.config by reference sample.

Step 6: Copy All controller class, model class and view files from this GitHub repository to your application.

Step 7: Add below Package from Nuget Package Manager into your application:

     - Microsoft.ProjectOxford.Face
     - Microsoft.ProjectOxford.SpeechRecognition-x64
     - Microsoft.ProjectOxford.SpeechRecognition-x86
     - Microsoft.ProjectOxford.Vision
     
     

