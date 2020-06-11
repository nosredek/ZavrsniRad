using System.Collections.Generic;

namespace ZavRad.Models
{
    public class SamFile
    {
        public string QueryName { get; set; }
        public string ReferenceName { get; set; }
        public string CigarString { get; set; }
        public int StartingPos { get; set; }
        public string Sequence { get; set; }
        public int Length { get; set; }

        public bool Reverse { get; set; }
        public IEnumerable<DrawExpression> DrawExpressions { get; set; }
    }
}