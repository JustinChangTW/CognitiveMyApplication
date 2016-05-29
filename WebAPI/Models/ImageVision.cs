using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebAPI.Models
{
    public class ImageVision
    {
        private const string ServiceHost = "https://api.projectoxford.ai/vision/v1.0";
        private const string AnalyzeQuery = "analyze";
        private static string VISION_API_Key = System.Configuration.ConfigurationManager.AppSettings["VISION_API_Key"];   //Project Oxford

        public Stream ImageStream;

        public ImageVision(Stream imageStream)
        {
            this.ImageStream = imageStream;
        }

        public async Task<string> Compute()
        {
            List<VisualFeature> features = new List<VisualFeature>();
            features.Add(VisualFeature.Description);
            //features.Add(VisualFeature.Color);
            //features.Add(VisualFeature.Adult);
            //features.Add(VisualFeature.Faces);
            AnalysisResult anaResult = await AnalyzeImageAsync(this.ImageStream, features);
            return anaResult.Description.Captions[0].Text;
        }

        private CamelCasePropertyNamesContractResolver _defaultResolver = new CamelCasePropertyNamesContractResolver();

        public async Task<AnalysisResult> AnalyzeImageAsync(Stream imageStream, IEnumerable<VisualFeature> visualFeatures = null, IEnumerable<string> details = null)
        {
            return await AnalyzeImageAsync<Stream>(imageStream, visualFeatures, details);
        }

        private async Task<AnalysisResult> AnalyzeImageAsync<T>(T body, IEnumerable<VisualFeature> visualFeatures, IEnumerable<string> details)
        {
            var requestUrl = new StringBuilder(ServiceHost).Append('/').Append(AnalyzeQuery).Append("?");
            requestUrl.Append(string.Join("&", new List<string>
            {
                VisualFeaturesToString(visualFeatures),
                DetailsToString(details),
                "subscription-key" + "=" + VISION_API_Key
            }
            .Where(s => !string.IsNullOrEmpty(s))));

            var request = WebRequest.Create(requestUrl.ToString());

            return await this.SendAsync<T, AnalysisResult>("POST", body, request);
        }

        private async Task<TResponse> SendAsync<TRequest, TResponse>(string method, TRequest requestBody, WebRequest request, Action<WebRequest> setHeadersCallback = null)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException("request");
                }

                request.Method = method;
                if (null == setHeadersCallback)
                {
                    this.SetCommonHeaders(request);
                }
                else
                {
                    setHeadersCallback(request);
                }

                if (requestBody is Stream)
                {
                    request.ContentType = "application/octet-stream";
                }

                var asyncState = new WebRequestAsyncState()
                {
                    RequestBytes = this.SerializeRequestBody(requestBody),
                    WebRequest = (HttpWebRequest)request,
                };

                var continueRequestAsyncState = await Task.Factory.FromAsync<Stream>(
                                                    asyncState.WebRequest.BeginGetRequestStream,
                                                    asyncState.WebRequest.EndGetRequestStream,
                                                    asyncState,
                                                    TaskCreationOptions.None).ContinueWith<WebRequestAsyncState>(
                                                       task =>
                                                       {
                                                           var requestAsyncState = (WebRequestAsyncState)task.AsyncState;
                                                           if (requestBody != null)
                                                           {
                                                               using (var requestStream = task.Result)
                                                               {
                                                                   if (requestBody is Stream)
                                                                   {
                                                                       (requestBody as Stream).CopyTo(requestStream);
                                                                   }
                                                                   else
                                                                   {
                                                                       requestStream.Write(requestAsyncState.RequestBytes, 0, requestAsyncState.RequestBytes.Length);
                                                                   }
                                                               }
                                                           }

                                                           return requestAsyncState;
                                                       });

                var continueWebRequest = continueRequestAsyncState.WebRequest;
                var response = await Task.Factory.FromAsync<WebResponse>(
                                            continueWebRequest.BeginGetResponse,
                                            continueWebRequest.EndGetResponse,
                                            continueRequestAsyncState);

                return this.ProcessAsyncResponse<TResponse>(response as HttpWebResponse);
            }
            catch (Exception e)
            {
                this.HandleException(e);
                return default(TResponse);
            }
        }

        /// <summary>
        /// Processes the asynchronous response.
        /// </summary>
        /// <typeparam name="T">Type of response.</typeparam>
        /// <param name="webResponse">The web response.</param>
        /// <returns>The response.</returns>
        private T ProcessAsyncResponse<T>(HttpWebResponse webResponse)
        {
            using (webResponse)
            {
                if (webResponse.StatusCode == HttpStatusCode.OK ||
                    webResponse.StatusCode == HttpStatusCode.Accepted ||
                    webResponse.StatusCode == HttpStatusCode.Created)
                {
                    if (webResponse.ContentLength != 0)
                    {
                        using (var stream = webResponse.GetResponseStream())
                        {
                            if (stream != null)
                            {
                                if (webResponse.ContentType == "image/jpeg" ||
                                    webResponse.ContentType == "image/png")
                                {
                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        stream.CopyTo(ms);
                                        return (T)(object)ms.ToArray();
                                    }
                                }
                                else
                                {
                                    string message = string.Empty;
                                    using (StreamReader reader = new StreamReader(stream))
                                    {
                                        message = reader.ReadToEnd();
                                    }

                                    JsonSerializerSettings settings = new JsonSerializerSettings();
                                    settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                                    settings.NullValueHandling = NullValueHandling.Ignore;
                                    settings.ContractResolver = this._defaultResolver;

                                    return JsonConvert.DeserializeObject<T>(message, settings);
                                }
                            }
                        }
                    }
                }
            }

            return default(T);
        }

        /// <summary>
        /// Set request content type.
        /// </summary>
        /// <param name="request">Web request object.</param>
        private void SetCommonHeaders(WebRequest request)
        {
            request.ContentType = "application/json";
        }

        /// <summary>
        /// Serialize the request body to byte array.
        /// </summary>
        /// <typeparam name="T">Type of request object.</typeparam>
        /// <param name="requestBody">Strong typed request object.</param>
        /// <returns>Byte array.</returns>
        private byte[] SerializeRequestBody<T>(T requestBody)
        {
            if (requestBody == null || requestBody is Stream)
            {
                return null;
            }
            else
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                settings.ContractResolver = this._defaultResolver;

                return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBody, settings));
            }
        }

        /// <summary>
        /// Process the exception happened on rest call.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        private void HandleException(Exception exception)
        {
            WebException webException = exception as WebException;
            if (webException != null && webException.Response != null)
            {
                if (webException.Response.ContentType.ToLower().Contains("application/json"))
                {
                    Stream stream = null;

                    try
                    {
                        stream = webException.Response.GetResponseStream();
                        if (stream != null)
                        {
                            string errorObjectString;
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                stream = null;
                                errorObjectString = reader.ReadToEnd();
                            }

                            ClientError errorCollection = JsonConvert.DeserializeObject<ClientError>(errorObjectString);
                            if (errorCollection != null)
                            {
                                throw new ClientException
                                {
                                    Error = errorCollection,
                                };
                            }
                        }
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Dispose();
                        }
                    }
                }
            }

            throw exception;
        }

        /// <summary>
        /// This class is used to pass on "state" between each Begin/End call
        /// It also carries the user supplied "state" object all the way till
        /// the end where is then hands off the state object to the
        /// WebRequestCallbackState object.
        /// </summary>
        internal class WebRequestAsyncState
        {
            /// <summary>
            /// Gets or sets request bytes of the request parameter for http post.
            /// </summary>
            public byte[] RequestBytes { get; set; }

            /// <summary>
            /// Gets or sets the HttpWebRequest object.
            /// </summary>
            public HttpWebRequest WebRequest { get; set; }

            /// <summary>
            /// Gets or sets the request state object.
            /// </summary>
            public object State { get; set; }
        }

        private string VisualFeaturesToString(string[] features)
        {
            return (features == null || features.Length == 0)
                ? ""
                : "visualFeatures=" + string.Join(",", features);
        }

        private string VisualFeaturesToString(IEnumerable<VisualFeature> features)
        {
            return VisualFeaturesToString(features?.Select(feature => feature.ToString()).ToArray());
        }

        private string DetailsToString(IEnumerable<string> details)
        {
            return (details == null || details.Count() == 0)
                ? ""
                : "details=" + string.Join(",", details);
        }        
    }
}