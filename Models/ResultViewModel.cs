namespace BSEBAnnualResultsMVC.Models
{
    // Main ViewModel passed to the View
    public class ResultViewModel
    {
        public StudentSummary Student { get; set; }
        public List<SubjectDetail> CompulsorySubjects { get; set; }
        public List<SubjectDetail> ElectiveSubjects { get; set; }
        public List<SubjectDetail> AdditionalSubjects { get; set; }
        public List<SubjectDetail> VocationalSubjects { get; set; }
    }
    // Student summary (replaces Table[0] from SP)
    public class StudentSummary
    {
        public string? BsebUniqueID { get; set; }
        public string? NameoftheCandidate { get; set; }
        public string? FathersName { get; set; }
        public string? CollegeName { get; set; }
        public string? RollCode { get; set; }
        public string? RollNo { get; set; }
        public string? RegistrationNo { get; set; }
        public string? FACULTY { get; set; }
        public string? TotalAggregateMarkinNumber { get; set; }
        public string? TotalAggregateMarkinWords { get; set; }
        public string? DIVISION { get; set; }
        public int? IsCCEMarks { get; set; }
    }

    // Subject row (replaces Table[1..4] from SP)
    public class SubjectDetail
    {
        public string? Sub { get; set; }
        public int? MaxMark { get; set; }
        public int? PassMark { get; set; }
        public string? Theory { get; set; }
        public string OB_PR { get; set; }
        public string? GRC_THO { get; set; }
        public string? GRC_PR { get; set; }
        public string? TOT_SUB { get; set; }
        public string? CCEMarks { get; set; }
        public string? SubjectGroupName { get; set; }
        public string? Dist { get; set; }
    }
}
