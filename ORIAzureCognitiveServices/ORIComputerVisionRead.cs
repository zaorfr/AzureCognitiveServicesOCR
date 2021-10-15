using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ORIAzureCognitiveServices
{
    /// <summary>
    /// class that implements API call for Read & Get Read Results Azure computer vision:
    /// https://westus.dev.cognitive.microsoft.com/docs/services/computer-vision-v3-2/operations/5d986960601faab4bf452005
    /// https://westus.dev.cognitive.microsoft.com/docs/services/computer-vision-v3-2/operations/5d9869604be85dee480c8750
    /// </summary>
    public class ORIComputerVisionRead
    {
        #region definitions

        enum ReadResultStatus
        {
            notStarted  = 1,//The operation has not started.
            running     = 2,//The operation is being processed.
            failed      = 3,//The operation has failed.
            succeeded   = 4//The operation has succeeded If the status is succeeded, the response JSON will further include 'analyzeResult' containing the recognized text, organized as a hierarchy of pages of lines of words.
        };

        private string subscriptionKey;
        private string endpoint;
        private string apiversion;        
       
        public Boolean hasError;
        public string errorMsg;
        public ResultRead resultObject;
        public string ReadOperationID;


        List<MatchObject> MatchWords = new List<MatchObject>();
        List<MatchObject> ConstantWords = new List<MatchObject>();
        #endregion

        #region process request
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="_endpoint">endpoint URI</param>
        /// <param name="_subscriptionKey">Subscription key</param>
        /// <param name="_apiversion">api version</param>
        public ORIComputerVisionRead(string _endpoint, string _subscriptionKey, string _apiversion)
        {
            endpoint = _endpoint;
            subscriptionKey = _subscriptionKey;
            apiversion = _apiversion;
           

        }
        /// <summary>
        /// Runs Read request on filestream
        /// </summary>
        /// <param name="_fs">filestream to process</param>
        /// <returns>boolean of success</returns>
        public Boolean RunRead(FileStream _fs)
        {
            Boolean ret = false;
            string uriBase = $"{endpoint}vision/{apiversion}/read/analyze";
            try
            {
                HttpClient client = new HttpClient();                
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                HttpResponseMessage response;
                byte[] byteData = GetImageAsByteArray(_fs);
                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = client.PostAsync(uriBase, content).Result;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    string OperationLocation = "";
                    HttpHeaders headers = response.Headers;
                    IEnumerable<string> values;
                    if (headers.TryGetValues("Operation-Location", out values))
                    {
                        OperationLocation = values.First();
                        ReadOperationID = OperationLocation;                        
                    }
                    else
                    {
                        hasError = true;
                        errorMsg = $"Read result retrieval failed, document will not be processed";
                    }

                }
                else
                {
                    hasError = true;
                    errorMsg = $"Response status code {response.StatusCode} document will not be processed";
                }                    
            }
            catch (Exception e)
            {
                hasError = true;
                errorMsg = e.Message;
            }
            return ret;

        }
        public Boolean RunGetReadResults(string _OperationLocation)
        {
            Boolean ret = false;
            try
            {
                ResultRead r = new ResultRead();
                //while results are not in final status request results
                do
                {
                    r = getReadResults(_OperationLocation);
                }
                while ((r.status == ReadResultStatus.running.ToString() ||
                r.status == ReadResultStatus.notStarted.ToString()));

                if (r.status == ReadResultStatus.failed.ToString())
                {
                    hasError = true;
                    errorMsg = $"Read result retrieval failed for '{_OperationLocation}', document will not be processed";
                    ret = false;
                }
                else
                {
                    resultObject = r;
                    ret = true;
                }
            }
            catch(Exception ex)
            {
                hasError = true;
                errorMsg = $"Read result retrieval failed for '{_OperationLocation}' with error '{ex.Message}'";
                ret = false;
            }
            return ret;
        }
        /// <summary>
        /// performs request for results
        /// </summary>
        /// <param name="_resultUri">results URI returned by read request</param>
        /// <returns>ResultRead object</returns>
        public ResultRead getReadResults(string _resultUri)
        {
            HttpClient client = new HttpClient();
            ResultRead r;

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var response = client.GetAsync(_resultUri).Result;
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string contentString = response.Content.ReadAsStringAsync().Result;
                r = JsonConvert.DeserializeObject<ResultRead>(contentString);
                return r;
            }
            else
            {
                r = new ResultRead();
                r.status = ReadResultStatus.failed.ToString();
                return r;
            }
        }
        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(FileStream _fs)
        {

            // Read the file's contents into a byte array.
            BinaryReader binaryReader = new BinaryReader(_fs);
            return binaryReader.ReadBytes((int)_fs.Length);

        }
        #endregion

        #region process output
        /// <summary>
        /// returns the ReadOperationID that resulted to a Read API request
        /// </summary>
        /// <returns></returns>
        public string GetReadOperationID()
        {
            return ReadOperationID;
        }
        /// <summary>
        /// returns the results object
        /// </summary>
        /// <returns>object with results</returns>
        public ResultRead GetResultsOCRObject()
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
        /// returns number of pages in result
        /// </summary>
        /// <returns>number of pages in result</returns>
        public int GetResultNumPages()
        {
            int ret = 0;
            if (resultObject != null)
            {
                ret = resultObject.analyzeResult.readResults.Count;
            }
            return ret;
        }
        /// <summary>
        /// returns number of lines in a given page
        /// </summary>
        /// <param name="_page">page index</param>
        /// <returns>number of lines in page</returns>
        public int GetResultPageNumLines(int _page)
        {
            int ret = 0;
            if (resultObject != null)
            {
                if (resultObject.analyzeResult.readResults.Count >= _page)
                {
                    ret = resultObject.analyzeResult.readResults[_page].lines.Count;
                }
            }
            return ret;
        }
        /// <summary>
        /// Gets line text based on page and line index
        /// </summary>
        /// <param name="_page">page number</param>
        /// <param name="_line">linenumber</param>
        /// <returns></returns>
        public string GetLineText(int _page, int _line)
        {
            string ret = "";
            if (resultObject != null)
            {
                if (resultObject.analyzeResult.readResults.Count >= _page)
                {
                    if (resultObject.analyzeResult.readResults[_page].lines.Count >= _line)
                        ret = resultObject.analyzeResult.readResults[_page].lines[_line].text;
                }
            }
            return ret;
        }
        /// <summary>
        /// Get number of words in line
        /// </summary>
        /// <param name="_page">page number</param>
        /// <param name="_line">line number</param>
        /// <returns></returns>
        public int GetLineNumWords(int _page, int _line)
        {
            int ret = 0;
            if (resultObject != null)
            {
                if (resultObject.analyzeResult.readResults.Count >= _page)
                {
                    if (resultObject.analyzeResult.readResults[_page].lines.Count >= _line)
                        ret = resultObject.analyzeResult.readResults[_page].lines[_line].words.Count;
                }
            }
            return ret;
        }
        /// <summary>
        /// gets a particular word by possition from a page and line
        /// </summary>
        /// <param name="_page">page index</param>
        /// <param name="_line">line index</param>
        /// <param name="_word">word index</param>
        /// <returns>word</returns>
        public string GetWord(int _page, int _line, int _word)
        {
            string ret = "";
            if (resultObject != null)
            {
                if (resultObject.analyzeResult.readResults.Count >= _page)
                {
                    if (resultObject.analyzeResult.readResults[_page].lines.Count >= _line)
                    {
                        if (resultObject.analyzeResult.readResults[_page].lines[_line].words.Count >= _word)
                            ret = resultObject.analyzeResult.readResults[_page].lines[_line].words[_word].text;
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// returns confidence of word recognized
        /// </summary>
        /// <param name="_page">page index</param>
        /// <param name="_line">line index</param>
        /// <param name="_word">word index</param>
        /// <returns></returns>
        public double GetWordConfidence(int _page, int _line, int _word)
        {
            double ret = 0;
            if (resultObject != null)
            {
                if (resultObject.analyzeResult.readResults.Count >= _page)
                {
                    if (resultObject.analyzeResult.readResults[_page].lines.Count >= _line)
                    {
                        if (resultObject.analyzeResult.readResults[_page].lines[_line].words.Count >= _word)
                            ret = resultObject.analyzeResult.readResults[_page].lines[_line].words[_word].confidence;
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// Get first match for a pattern using regex will save firstMatchPageIndex & firstMatchPageIndex & firstMatchWordIndexif match found
        /// </summary>
        /// <param name="pattern">regular expression pattern</param>
        /// <returns>first match if fould or blank</returns>
        public string GetFirstMatchforPatternAllDoc(string pattern)
        {
            string ret = "";
            int pageIndex = 0;
            int lineIndex = 0;
            int wordindex = 0;
            try
            {
                foreach (ReadResult page in resultObject.analyzeResult.readResults)
                {
                    lineIndex = 0;
                    foreach (Line line in page.lines)
                    {
                        wordindex = 0;
                        foreach (Word word in line.words)
                        {
                            Regex rg = new Regex(pattern);
                            MatchCollection matchCollectionResult = rg.Matches(word.text);
                            if (matchCollectionResult.Count > 0)
                            {
                                ret = matchCollectionResult[0].Value;                                
                                break;
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
                hasError = true;
                errorMsg = ex.Message;
            }
            return ret;
        }
        /// <summary>
        /// get total number of match for pattern in all document
        /// </summary>
        /// <param name="pattern">regular expression</param>
        /// <returns>number of matches</returns>
        public int GetMatchCountforPatternAllDoc(string pattern)
        {
            int ret = 0;
            try
            {
                foreach (ReadResult page in resultObject.analyzeResult.readResults)
                {
                    foreach (Line line in page.lines)
                    {
                        foreach (Word word in line.words)
                        {
                            Regex rg = new Regex(pattern);
                            MatchCollection matchCollectionResult = rg.Matches(word.text);
                            ret += matchCollectionResult.Count;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                errorMsg = ex.Message;
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
                foreach (ReadResult page in resultObject.analyzeResult.readResults)
                {
                    lineIndex = 0;
                    foreach (Line line in page.lines)
                    {
                        wordindex = 0;
                        foreach (Word word in line.words)
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
                                m.Confidence = word.confidence;
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
                hasError = true;
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
                foreach (ReadResult page in resultObject.analyzeResult.readResults)
                {
                    lineIndex = 0;
                    foreach (Line line in page.lines)
                    {
                        wordindex = 0;
                        foreach (Word word in line.words)
                        {

                            if (word.text == _constant)
                            {
                                MatchObject m = new MatchObject();
                                m.Page = pageIndex;
                                m.Line = lineIndex;
                                m.Word = wordindex;
                                m.Text = word.text;
                                m.Confidence = word.confidence;
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
                hasError = true;
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
