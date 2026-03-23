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
                bool exists = _db.FinalPublishedResults.Any(r => r.RollCode == rollCode && r.RollNumber == rollNo && r.IsActive == true);

                if (!exists) return null;

                // Fetch all rows for this student in ONE DB call
                var allRows = _db.FinalPublishedResults.Where(r => r.RollCode == rollCode && r.RollNumber == rollNo && r.IsActive == true).ToList(); // Single DB hit — no server load

                // Check CCEMarks (SP logic: IsCCEMarks)
                int isCCEMarks = allRows.Any(r => r.CCEMarks.HasValue && r.CCEMarks.Value != 0) ? 1 : 0;
                //int isCCEMarks = allRows.Any(r => r.CCEMarks != null && r.CCEMarks != "0" && r.CCEMarks != "") ? 1 : 0;

                // Get first row for student summary
                var first = allRows.First();

                // Build DIVISION string (SP's CONCAT CASE logic)
                string division = BuildDivision(first, allRows);
                //string division = BuildDivision(first);

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
                var compulsory = FilterSubjects(allRows, "1. अनिवार्य Compulsory", isVocational: false);
                var elective = FilterSubjects(allRows, "2. ऐच्छिक Elective", isVocational: false);
                var additional = FilterSubjects(allRows, "3. अतिरिक्त Additional", isVocational: false);
                var vocational = FilterSubjects(allRows, "4. Additional subject group Vocational (100 marks)", isVocational: true);

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

                throw new ApplicationException("Failed to load result data.", ex);

            }

        }
        //new division logic 
        private string BuildDivision(ExamFinalPublishedResult r, List<ExamFinalPublishedResult> allRows)
        {
            try
            {
                // MAX(PassedUnderRegulation) OVER (PARTITION BY RollCode, RollNumber)
                string passedUnderReg = allRows.Where(x => x.RollCode == r.RollCode && x.RollNumber == r.RollNumber).Select(x => x.PassedUnderRegulation).Where(x => !string.IsNullOrEmpty(x)).OrderByDescending(x => x).FirstOrDefault() ?? "";

                string regPart = !string.IsNullOrEmpty(passedUnderReg) ? " " + passedUnderReg : "";
                string gracePart = !string.IsNullOrEmpty(r.DivisionGraceMarks) ? " +" + r.DivisionGraceMarks : "";

                if (r.ExamType == "IMPROVEMENT" && r.IsTotalResultImproved == true)
                {
                    // WHEN ExamType = 'IMPROVEMENT' AND IsTotalResultImproved = 1
                    return $"{r.Division}{regPart}{gracePart} Improved".Trim();
                }
                else if (r.ExamType == "IMPROVEMENT" && (r.IsTotalResultImproved == false || r.IsTotalResultImproved == null))
                {
                    // WHEN ExamType = 'IMPROVEMENT' AND (IsTotalResultImproved = 0 OR IS NULL)
                    string regPartWrapped = !string.IsNullOrEmpty(passedUnderReg) ? $" ({passedUnderReg})" : "";
                    return $"Not Improved{regPartWrapped}".Trim();
                }
                else
                {
                    // ELSE — normal case
                    return $"{r.Division}{regPart}{gracePart}".Trim();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        // SP DIVISION CONCAT CASE logic moved to C#
        //private string BuildDivision(ExamFinalPublishedResult r)
        //{
        //    try
        //    {
        //        string divPart = (r.ImprovementRemark == "IMPROVED" || r.ImprovementRemark == null) ? r.Division : "";

        //        string gracePart = (!string.IsNullOrEmpty(r.DivisionGraceMarks)) ? " +" + r.DivisionGraceMarks : "";

        //        string improvePart = (r.ExamType == "IMPROVEMENT" && !string.IsNullOrEmpty(r.ImprovementRemark)) ? " " + r.ImprovementRemark : "";

        //        return $"{divPart} {r.PassedUnderRegulation}{gracePart}{improvePart}".Trim();
        //    }
        //    catch (Exception ex)
        //    {

        //        return string.Empty;
        //    }

        //}

        // SP's IIF and CONCAT TOT_SUB logic moved to C#
        private List<SubjectDetail> FilterSubjects(List<ExamFinalPublishedResult> allRows, string groupName, bool isVocational)
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
                    //TOT_SUB = BuildSubjectTotal(r),
                    // ✅ Choose correct TOT_SUB logic based on group // SP branch 4 logic
                    TOT_SUB = isVocational ? BuildSubjectTotalVocational(r) : BuildSubjectTotal(r), // SP branch 1/2/3 logic
                    CCEMarks = r.CCEMarks?.ToString(),
                    SubjectGroupName = r.SubjectGroupName,
                    Dist = r.Dist
                })
              .ToList();
            }
            catch (Exception ex)
            {

                return new List<SubjectDetail>();
            }

        }

        // SP's TOT_SUB CONCAT CASE logic
        private string BuildSubjectTotal(ExamFinalPublishedResult r)
        {
            try
            {
                string tot = r.SubjectTotal ?? "";
                // ✅ SP Branch 2: WHEN CategoryName = 'COMPARTMENTAL'
                // THEN CONCAT(SubjectTotal, ' ', ISNULL(IsCompartment, ''))
                if (string.Equals(r.CategoryName, "COMPARTMENTAL", StringComparison.OrdinalIgnoreCase))
                {
                    string compartment = !string.IsNullOrEmpty(r.IsCompartment) ? " " + r.IsCompartment : "";
                    return $"{tot}{compartment}".Trim();
                }
                string dist = (!string.IsNullOrEmpty(r.Dist)) ? " " + r.Dist : "";
                string grace = (!string.IsNullOrEmpty(r.PassWithGrace)) ? " " + r.PassWithGrace : "";

                //string improved = (r.CategoryName == "Improvement" && !string.IsNullOrEmpty(r.IsImproved)) ? " " + r.IsImproved : "";
                string improved = (string.Equals(r.CategoryName, "Improvement", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(r.IsImproved)) ? " " + r.IsImproved : "";
                string swapped = !string.IsNullOrEmpty(r.IsSwapped) ? " " + r.IsSwapped : "";
                //string swapped = (r.IsSwappedT == true && !string.IsNullOrEmpty(r.IsSwapped)) ? " " + r.IsSwapped : "";
                // If any of those exist, skip Dist (match SP commented-out logic)
                bool hasOverride = !string.IsNullOrEmpty(r.PassWithGrace) || (r.CategoryName == "Improvement" && !string.IsNullOrEmpty(r.IsImproved)) || (r.IsSwappedT == true && !string.IsNullOrEmpty(r.IsSwapped));
                string distFinal = hasOverride ? "" : dist;


                //if (grace == "*")
                //{
                return $"{tot}{distFinal}{grace}{improved}{swapped}".Trim();
                //}
                //else
                //{
                //    return $"{tot}{distFinal}{improved}{swapped}".Trim();
                //}
                //string swapped = (r.IsSwappedT == 1 && !string.IsNullOrEmpty(r.IsSwapped)) ? " " + r.IsSwapped : "";
                //return $"{tot}{grace}{improved}{swapped}";
            }
            catch (Exception ex)
            {

                return r.SubjectTotal ?? "";
            }

        }

        // ✅ SP Branch 4 logic — Vocational
        // SubjectTotal + CASE WHEN Grace>0 THEN '' ELSE Dist END
        private string BuildSubjectTotalVocational(ExamFinalPublishedResult r)
        {
            try
            {
                string tot = r.SubjectTotal ?? "";
                // ✅ SP Branch 2: WHEN CategoryName = 'COMPARTMENTAL'
                // THEN CONCAT(SubjectTotal, ' ', ISNULL(IsCompartment, ''))
                if (string.Equals(r.CategoryName, "COMPARTMENTAL", StringComparison.OrdinalIgnoreCase))
                {
                    string compartment = !string.IsNullOrEmpty(r.IsCompartment) ? " " + r.IsCompartment : "";
                    return $"{tot}{compartment}".Trim();
                }
                bool hasGrace = (r.TheoryGraceMarks.HasValue && r.TheoryGraceMarks.Value > 0) || (r.PracticalGraceMarks.HasValue && r.PracticalGraceMarks.Value > 0);

                // SP: CASE WHEN Grace>0 THEN '' ELSE Dist END
                string dist = hasGrace ? "" : (r.Dist ?? "");

                return $"{tot} {dist}".Trim();
            }
            catch (Exception ex)
            {

                return r.SubjectTotal ?? "";
            }
          
        }
    }
}
