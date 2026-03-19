using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BSEBAnnualResultsMVC.Models
{
    [Table("EXAM_FinalPublishedResult")]
    public class ExamFinalPublishedResult
    {
        [Key]
        public long? PK_FinalPublishedResultId { get; set; }
        public string? RollCode { get; set; }
        public string? RollNumber { get; set; }
        public string? StudentFullName { get; set; }
        public string? FatherName { get; set; }
        public string? CollegeName { get; set; }
        public string? Faculty { get; set; }
        public string? RegistrationNo { get; set; }
        public string? Stu_UniqueId { get; set; }
        public string? DOB { get; set; }

        // Marks fields
        public string? SubjectPaperName { get; set; }
        public int? FMark { get; set; }
        public int? PMarks { get; set; }
        public int? TotalTheoryMarks { get; set; }
        public int? PRObtainedMarks { get; set; }
        public string? SubjectTotal { get; set; }
        public string? AbsentTh { get; set; }
        public string? AbsentPr { get; set; }
        public int? TheoryGraceMarks { get; set; }
        public int? PracticalGraceMarks { get; set; }
        public string? SubjectGroupName { get; set; }

        // Fixed tinyint mapping
        public byte? SubjectDisplayOrder { get; set; }
        public string? PassWithGrace { get; set; }
        public string? CategoryName { get; set; }
        public string? IsImproved { get; set; }
        public bool? IsSwappedT { get; set; }
        public string? IsSwapped { get; set; }  // varchar(10) in DB
        public string? Dist { get; set; }

        // Fixed CCEMarks mapping
        public int? CCEMarks { get; set; }

        // Summary fields
        public int? TotalMarks { get; set; }
        public string? TotalMarksInWords { get; set; }
        public string? Division { get; set; }
        public string? IsDivisionGrace { get; set; }
        public string? DivisionGraceMarks { get; set; }
        public string? PassedUnderRegulation { get; set; }
        public string? ExamType { get; set; }
        public string? ImprovementRemark { get; set; }
    }
}