using BSEBAnnualResultsMVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace BSEBAnnualResultsMVC.Services
{
    public class ResultService
    {
        private readonly AppDbContext _db;

        // ✅ Inject DbContext
        public ResultService(AppDbContext db)
        {
            _db = db;
        }


        public ResultViewModel GetResult(string rollCode, string rollNo)
        {
            try
            {
                // Check if record exists (SP's EXISTS check)
                bool exists = _db.FinalPublishedResults.Any(r => r.RollCode == rollCode && r.RollNumber == rollNo);

                if (!exists) return null;

                // Fetch all rows for this student in ONE DB call
                var allRows = _db.FinalPublishedResults.Where(r => r.RollCode == rollCode && r.RollNumber == rollNo)
                    .ToList(); // Single DB hit — no server load

                // Check CCEMarks (SP logic: IsCCEMarks)
                int isCCEMarks = allRows.Any(r => r.CCEMarks.HasValue && r.CCEMarks.Value != 0) ? 1 : 0;
                //int isCCEMarks = allRows.Any(r => r.CCEMarks != null && r.CCEMarks != "0" && r.CCEMarks != "") ? 1 : 0;

                // Get first row for student summary
                var first = allRows.First();

                // Build DIVISION string (SP's CONCAT CASE logic)
                string division = BuildDivision(first);

                // Build TotalAggregateMarkinNumber
                string totalAgg = $"{first.TotalMarks} {first.IsDivisionGrace} ({first.TotalMarksInWords})";

                var student = new StudentSummary
                {
                    BsebUniqueID = first.Stu_UniqueId,
                    NameoftheCandidate = first.StudentFullName,
                    FathersName = first.FatherName,
                    CollegeName = first.CollegeName,
                    RollCode = first.RollCode,
                    RollNo = first.RollNumber,
                    RegistrationNo = first.RegistrationNo,
                    FACULTY = first.Faculty,
                    TotalAggregateMarkinNumber = totalAgg,
                    TotalAggregateMarkinWords = first.TotalMarksInWords,
                    DIVISION = division,
                    IsCCEMarks = isCCEMarks
                };

                // Filter by SubjectGroupName (SP's 4 SELECT queries)
                var compulsory = FilterSubjects(allRows, "1. अनिवार्य Compulsory");
                var elective = FilterSubjects(allRows, "2. ऐच्छिक Elective");
                var additional = FilterSubjects(allRows, "3. अतिरिक्त Additional");
                var vocational = FilterSubjects(allRows,"Additional subject group Vocational (100 marks)");

                return new ResultViewModel
                {
                    Student = student,
                    CompulsorySubjects = compulsory,
                    ElectiveSubjects = elective,
                    AdditionalSubjects = additional,
                    VocationalSubjects = vocational
                };
            }
            catch (Exception ex)
            {

                throw;
            }
          
        }

        // SP DIVISION CONCAT CASE logic moved to C#
        private string BuildDivision(ExamFinalPublishedResult r)
        {
            try
            {
                string divPart = (r.ImprovementRemark == "IMPROVED" || r.ImprovementRemark == null) ? r.Division : "";

                string gracePart = (!string.IsNullOrEmpty(r.DivisionGraceMarks)) ? " +" + r.DivisionGraceMarks : "";

                string improvePart = (r.ExamType == "IMPROVEMENT" && !string.IsNullOrEmpty(r.ImprovementRemark)) ? " " + r.ImprovementRemark : "";

                return $"{divPart} {r.PassedUnderRegulation}{gracePart}{improvePart}".Trim();
            }
            catch (Exception ex)
            {

                throw;
            }
         
        }

        // SP's IIF and CONCAT TOT_SUB logic moved to C#
        private List<SubjectDetail> FilterSubjects(List<ExamFinalPublishedResult> allRows, string groupName)
        {
            try
            {
                return allRows.Where(r => r.SubjectGroupName == groupName).OrderBy(r => r.SubjectDisplayOrder).Select(r => new SubjectDetail
                {
                    Sub = r.SubjectPaperName,
                    MaxMark = r.FMark,
                    PassMark = r.PMarks,

                    // IIF(AbsentTh='P', TotalTheoryMarks, AbsentTh)
                    Theory = r.AbsentTh == "P" ? r.TotalTheoryMarks?.ToString() : r.AbsentTh,

                    // IIF(AbsentPr='P', PRObtainedMarks, AbsentPr)
                    OB_PR = r.AbsentPr == "P" ? (r.PRObtainedMarks.HasValue ? r.PRObtainedMarks.ToString() : "") : r.AbsentPr,

                    // Grace marks — show empty if null/0
                    GRC_THO = (r.TheoryGraceMarks == null || r.TheoryGraceMarks == 0) ? "" : r.TheoryGraceMarks.ToString(),

                    GRC_PR = (r.PracticalGraceMarks == null || r.PracticalGraceMarks == 0) ? "" : r.PracticalGraceMarks.ToString(),

                    // CONCAT(SubjectTotal, PassWithGrace, IsImproved, IsSwapped)
                    TOT_SUB = BuildSubjectTotal(r),

                    CCEMarks = r.CCEMarks?.ToString(),
                    SubjectGroupName = r.SubjectGroupName
                })
              .ToList();
            }
            catch (Exception ex)
            {

                throw;
            }
          
        }

        // SP's TOT_SUB CONCAT CASE logic
        private string BuildSubjectTotal(ExamFinalPublishedResult r)
        {
            try
            {
                string tot = r.SubjectTotal ?? "";

                string grace = (!string.IsNullOrEmpty(r.PassWithGrace)) ? " " + r.PassWithGrace : "";

                string improved = (r.CategoryName == "Improvement" && !string.IsNullOrEmpty(r.IsImproved)) ? " " + r.IsImproved : "";

                string swapped = (r.IsSwappedT == true && !string.IsNullOrEmpty(r.IsSwapped)) ? " " + r.IsSwapped : "";

                //string swapped = (r.IsSwappedT == 1 && !string.IsNullOrEmpty(r.IsSwapped)) ? " " + r.IsSwapped : "";
                return $"{tot}{grace}{improved}{swapped}";
            }
            catch (Exception ex)
            {

                throw;
            }
         
        }
    }
}
