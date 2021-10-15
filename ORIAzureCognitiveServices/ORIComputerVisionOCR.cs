using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ORIAzureCognitiveServices
{
    /// <summary>
    /// class that implements API call for OCR Azure computer vision:
    /// https://westus.dev.cognitive.microsoft.com/docs/services/computer-vision-v3-2/operations/56f91f2e778daf14a499f20d
    /// </summary>
    public class ORIComputerVisionOCR
    {
        #region definitions
        private string subscriptionKey;
        private string endpoint;
        private string apiversion;
        private string language;
        

       
        public Boolean hasError;
        public string errorMsg;
        public ResultOCR resultObject;


        List<MatchObject> MatchWords = new List<MatchObject>();
        List<MatchObject> ConstantWords = new List<MatchObject>();
        #endregion

        #region process request
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_endpoint">Endpoint URI</param>
        /// <param name="_subscriptionKey">Subscription Key</param>
        /// <param name="_apiversion">Api version</param>
        
        
        public ORIComputerVisionOCR(string _endpoint, string _subscriptionKey, string _apiversion)
        {
            endpoint = _endpoint;
            subscriptionKey = _subscriptionKey;
            apiversion = _apiversion;
            language = "unk";//unk (AutoDetect)

        }

        /// <summary>
        /// Runs OCR request on filestream
        /// </summary>
        /// <param name="_fs">filestream to process</param>
        /// <returns>boolean of success</returns>
        public Boolean RunOCR(Stream _fs)
        {
            Boolean ret = false;
            string uriBase = $"{endpoint}vision/{apiversion}/ocr";
            try
            {
                HttpClient client = new HttpClient();
                // Request headers.
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                // Request parameters.                 
                string requestParameters = $"language={language}&detectOrientation=true";
                // Assemble the URI for the REST API method.
                string uri = $"{uriBase}?{requestParameters}";
                HttpResponseMessage response;
                byte[] byteData = GetImageAsByteArray(_fs);
                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = client.PostAsync(uri, content).Result;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string contentString = response.Content.ReadAsStringAsync().Result;                    
                    hasError = false;
                    resultObject = JsonConvert.DeserializeObject<ResultOCR>(contentString);
                    ret = true;                    
                }
                else
                {
                    hasError = true;
                    errorMsg = $"Read response status code '{response.StatusCode}', document will not be processed";
                }
            }
            catch (Exception e)
            {
                hasError = true;
                errorMsg = e.Message;
            }
            return ret;
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(Stream _fs)
        {
            // Read the file's contents into a byte array.
            BinaryReader binaryReader = new BinaryReader(_fs);
            return binaryReader.ReadBytes((int)_fs.Length);
        }
        #endregion

        #region process results
        /// <summary>
        /// returns the results object
        /// </summary>
        /// <returns>object with results</returns>
        public ResultOCR GetResultsOCRObject()
        {
            return resultObject;
        }
        /// <summary>
        /// returns read status Failure or success
        /// </summary>
        /// <returns>status</returns>
        public Boolean HasError()
        {
            return hasError;
        }
        /// <summary>
        /// retrieves error message
        /// </summary>
        /// <returns>error message</returns>
        public string GetErrorMsg()
        {
            return errorMsg;
        }
        /// <summary>
        /// returns number of regions in result
        /// </summary>
        /// <returns>number of pages in result</returns>
        public int GetResultNumRegions()
        {
            int ret = 0;
            if (resultObject != null)
            {
                ret = resultObject.regions.Count;
            }
            return ret;
        }
        /// <summary>
        /// returns number of lines in a given page
        /// </summary>
        /// <param name="_region">region index</param>
        /// <returns>number of lines in region</returns>
        public int GetResultRegionNumLines(int _region)
        {
            int ret = 0;
            if (resultObject != null)
            {
                if (resultObject.regions.Count >= _region)
                {
                    ret = resultObject.regions[_region].lines.Count;
                }
            }
            return ret;
        }
       /// <summary>
       /// get number of words for a given region and line
       /// </summary>
       /// <param name="_region"></param>
       /// <param name="_line"></param>
       /// <returns></returns>
        public int GetResultRegionLineNumWords(int _region, int _line)
        {
            int ret = 0;
            if (resultObject != null)
            {
                if (resultObject.regions.Count >= _region)
                {
                    if(resultObject.regions[_region].lines.Count >= _line)
                    {
                        ret = resultObject.regions[_region].lines[_line].words.Count;
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// gets a particular word by possition from a region and line
        /// </summary>
        /// <param name="_region">region index</param>
        /// <param name="_line">line index</param>
        /// <param name="_word">word index</param>
        /// <returns>word</returns>
        public string GetWord(int _region, int _line, int _word)
        {
            string ret = "";
            if (resultObject != null)
            {
                if (resultObject.regions.Count >= _region)
                {
                    if (resultObject.regions[_region].lines.Count >= _line)
                    {
                        if (resultObject.regions[_region].lines[_line].words.Count >= _word)
                            ret = resultObject.regions[_region].lines[_line].words[_word].text;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// populates array with strings for all matches in the document
        /// </summary>
        /// <param name="pattern">regular expression</param>   
        /// <returns>if at least one found</returns>
        public Boolean PopulateMatchListForPatternAllDoc(string pattern)
        {
            int pageIndex = 0;
            int lineIndex = 0;
            int wordindex = 0;
            Boolean ret = false;
            MatchWords = new List<MatchObject>();
            try
            {
                foreach (RegionOCR page in resultObject.regions)
                {
                    lineIndex = 0;
                    foreach (LineOCR line in page.lines)
                    {
                        wordindex = 0;
                        foreach (WordOCR word in line.words)
                        {
                            Regex rg = new Regex(pattern);
                            MatchCollection matchCollectionResult = rg.Matches(word.text);
                            if (matchCollectionResult.Count > 0)
                            {
                                MatchObject m = new MatchObject();
                                m.Page = pageIndex;
                                m.Line = lineIndex;
                                m.Word = wordindex;
                                m.Text = word.text;
                               
                                MatchWords.Add(m);
                                ret = true;
                            }
                            wordindex++;
                        }
                        lineIndex++;
                    }
                    pageIndex++;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            return ret;
        }
        /// <summary>
        /// Get number of matches for pattern
        /// </summary>
        /// <returns>num matches</returns>
        public int GetNumMatchesPattern()
        {
            int ret = 0;
            if (MatchWords != null)
                ret = MatchWords.Count;
            return ret;
        }
        /// <summary>
        /// Get match text by position for pattern
        /// </summary>
        /// <param name="_position">position in match list</param>
        /// <returns>string match text</returns>
        public string GetMatchTextPattern(int _position)
        {
            string ret = "";
            if (MatchWords != null)
            {
                if (MatchWords.Count >= _position)
                    ret = MatchWords[_position].Text;
            }
            return ret;
        }
        /// <summary>
        /// populates list of found matches based on a constant text
        /// </summary>
        /// <param name="_constant">text to find</param>
        /// <returns>if at least one found</returns>
        public Boolean PupulateMatchListForConstantsAllDoc(string _constant)
        {
            Boolean ret = false;
            int pageIndex = 0;
            int lineIndex = 0;
            int wordindex = 0;
            ConstantWords = new List<MatchObject>();
            try
            {
                foreach (RegionOCR page in resultObject.regions)
                {
                    lineIndex = 0;
                    foreach (LineOCR line in page.lines)
                    {
                        wordindex = 0;
                        foreach (WordOCR word in line.words)
                        {
                            if (word.text == _constant)
                            {
                                MatchObject m = new MatchObject();
                                m.Page = pageIndex;
                                m.Line = lineIndex;
                                m.Word = wordindex;
                                m.Text = word.text;
                               
                                ConstantWords.Add(m);
                                ret = true;
                            }
                            wordindex++;
                        }
                        lineIndex++;
                    }
                    pageIndex++;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            return ret;
        }

        /// <summary>
        /// Get number of matches for Constants
        /// </summary>
        /// <returns>num matches</returns>
        public int GetNumMatchesConstants()
        {
            int ret = 0;
            if (ConstantWords != null)
                ret = ConstantWords.Count;
            return ret;
        }
        /// <summary>
        /// Get match text by position for constants
        /// </summary>
        /// <param name="_position">position in match list</param>
        /// <returns>string match text</returns>
        public string GetMatchTextConstants(int _position)
        {
            string ret = "";
            if (ConstantWords != null)
            {
                if (ConstantWords.Count >= _position)
                    ret = ConstantWords[_position].Text;
            }
            return ret;
        }
        #endregion
    }
}
