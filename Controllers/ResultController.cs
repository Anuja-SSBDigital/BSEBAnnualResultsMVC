using BSEBAnnualResultsMVC.Models;
using BSEBAnnualResultsMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BSEBAnnualResultsMVC.Controllers
{
    public class ResultController : Controller
    {
        private readonly ResultService _service;
        private readonly ILogger<ResultController> _logger;

        // ✅ Inject service + logger
        public ResultController(ResultService service, ILogger<ResultController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: /Result/Index
        public ActionResult Index()
        {
            _logger.LogInformation("Index page loaded at {Time}", DateTime.Now);

            if (TempData["IsResultAfterScrutiny"] != null)
            {
                ViewBag.IsResultAfterScrutiny = TempData["IsResultAfterScrutiny"];
                ViewBag.ResultAfterScrutinyRemarks = TempData["ResultAfterScrutinyRemarks"];
                _logger.LogInformation("Scrutiny remarks displayed on Index page.");
            }

            return View();
        }

        // POST: /Result/GetResult
        [HttpPost]
        public ActionResult GetResult(string rollcode, string rollno)
        {
            _logger.LogInformation("GetResult called with RollCode: {RollCode}, RollNo: {RollNo}", rollcode, rollno);

            try
            {
                if (string.IsNullOrWhiteSpace(rollcode) || string.IsNullOrWhiteSpace(rollno))
                {
                    _logger.LogWarning("GetResult: Missing input.");
                    TempData["SwalType"] = "warning";
                    TempData["SwalTitle"] = "Missing Input";
                    TempData["SwalMessage"] = "Please enter both Roll Code and Roll Number.";
                    return RedirectToAction("Index");
                }

                ResultViewModel result = _service.GetResult(rollcode.Trim(), rollno.Trim());

                if (result == null)
                {
                    _logger.LogWarning("GetResult: No result found for RollCode: {RollCode}, RollNo: {RollNo}", rollcode, rollno);
                    TempData["SwalType"] = "error";
                    TempData["SwalTitle"] = "Not Found";
                    TempData["SwalMessage"] = "No result found for the given Roll Code and Roll Number.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("GetResult: Result found for RollCode: {RollCode}, RollNo: {RollNo}, Student: {StudentName}",
                    rollcode, rollno, result.Student?.NameoftheCandidate);

                // ✅ Log BEFORE serialization to catch crash point
                _logger.LogInformation("GetResult: Attempting JSON serialization...");

                string json;
                try
                {
                    json = JsonSerializer.Serialize(result);
                    _logger.LogInformation("GetResult: Serialization successful. JSON length: {Length}", json.Length);
                }
                catch (Exception serEx)
                {
                    _logger.LogError(serEx, "GetResult: JSON serialization FAILED for RollCode: {RollCode}, RollNo: {RollNo}", rollcode, rollno);
                    TempData["SwalType"] = "error";
                    TempData["SwalTitle"] = "Server Error";
                    TempData["SwalMessage"] = "Failed to process result data. Please try again.";
                    return RedirectToAction("Index");
                }

                TempData["Result"] = json;

                if (result.Student?.IsResultAfterScrutiny == true)
                {
                    _logger.LogInformation("GetResult: Scrutiny result for RollCode: {RollCode}. Remarks: {Remarks}",
                        rollcode, result.Student.ResultAfterScrutinyRemarks);
                    TempData["IsResultAfterScrutiny"] = true;
                    TempData["ResultAfterScrutinyRemarks"] = result.Student.ResultAfterScrutinyRemarks;
                }

                _logger.LogInformation("GetResult: Redirecting to ShowResult...");
                return RedirectToAction("ShowResult");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetResult: Unhandled exception for RollCode: {RollCode}, RollNo: {RollNo}", rollcode, rollno);
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "Server Error";
                TempData["SwalMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Result/ShowResult
        public ActionResult ShowResult()
        {
            _logger.LogInformation("ShowResult page loaded at {Time}", DateTime.Now);

            try
            {
                var json = TempData["Result"] as string;

                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("ShowResult: No result data found in TempData. Redirecting to Index.");
                    return RedirectToAction("Index");
                }

                // ✅ Deserialize back to ResultViewModel
                var result = JsonSerializer.Deserialize<ResultViewModel>(json);

                _logger.LogInformation("ShowResult: Displaying result for Student: {StudentName}, RollNo: {RollNo}",
                    result?.Student?.NameoftheCandidate, result?.Student?.RollNo);

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ShowResult: Unhandled exception while loading result page.");

                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "Server Error";
                TempData["SwalMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction("Index");
            }
        }
    }
}