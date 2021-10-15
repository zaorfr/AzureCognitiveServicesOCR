using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORIAzureCognitiveServices
{
    /// <summary>
    /// Object to identify matches
    /// </summary>
    public class MatchObject
    {
        public int Page { get; set; }
        public int Line { get; set; }
        public int Word { get; set; }
        public double Confidence { get; set; }
        public string Text { get; set; }

    }

    #region OCR request related objects definition
    /// <summary>
    /// OCR word
    /// </summary>
    public class WordOCR
    {
        public string boundingBox { get; set; }
        public string text { get; set; }
    }
    /// <summary>
    /// OCR Line
    /// </summary>
    public class LineOCR
    {
        public string boundingBox { get; set; }
        public List<WordOCR> words { get; set; }
    }
    /// <summary>
    /// OCR Region
    /// </summary>
    public class RegionOCR
    {
        public string boundingBox { get; set; }
        public List<LineOCR> lines { get; set; }
    }
    /// <summary>
    /// OCR Result
    /// </summary>
    public class ResultOCR
    {
        public string language { get; set; }
        public double textAngle { get; set; }
        public string orientation { get; set; }
        public List<RegionOCR> regions { get; set; }
        public string modelVersion { get; set; }
    }
    #endregion

    #region Read request related objects definition
   

    public class Word
    {
        public List<int> boundingBox { get; set; }
        public string text { get; set; }
        public double confidence { get; set; }
    }

    public class Line
    {
        public List<int> boundingBox { get; set; }
        public string text { get; set; }
       
        public List<Word> words { get; set; }
    }

    public class ReadResult
    {
        public int page { get; set; }
        public double angle { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string unit { get; set; }
        public List<Line> lines { get; set; }
    }

    public class AnalyzeResult
    {
        public string version { get; set; }
        public string modelVersion { get; set; }
        public List<ReadResult> readResults { get; set; }
    }

    public class ResultRead
    {
        public string status { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastUpdatedDateTime { get; set; }
        public AnalyzeResult analyzeResult { get; set; }
    }
    #endregion
}
