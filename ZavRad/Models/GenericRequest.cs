namespace ZavRad.Models
{
    public class GenericRequest
    {
        public int Zoom { get; set; }
        public string ReferenceName { get; set; }
        public int Chunk { get; set; }
        public int ScreenSize { get; set; }
    }
}