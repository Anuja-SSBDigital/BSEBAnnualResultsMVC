using BSEBAnnualResultsMVC.Models;
using BSEBAnnualResultsMVC.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BSEBAnnualResultsMVC.Controllers
{
    public class ResultController : Controller
    {
        private readonly ResultService _service;

        // ✅ Inject service
        public ResultController(ResultService service)
        {
            _service = service;
        }

        // GET: /Result/Index
        public ActionResult Index()
        {
            return View();
        }

        // POST: /Result/GetResult
        [HttpPost]
        public ActionResult GetResult(string rollcode, string rollno)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rollcode) || string.IsNullOrWhiteSpace(rollno))
                {
                    ViewBag.Error = "Please enter Roll Code and Roll Number.";
                    return View("Index");
                }


                ResultViewModel result = _service.GetResult(rollcode.Trim(), rollno.Trim());

                if (result == null)
                {
                    ViewBag.Error = "Invalid Login Details";
                    return View("Index");
                }

                // Store in TempData to pass to result page
                //TempData["Result"] = result;
                // ✅ Serialize to JSON string for TempData
                TempData["Result"] = JsonSerializer.Serialize(result);
                return RedirectToAction("ShowResult");
            }
            catch (Exception ex)
            {

                throw;
            }
          
        }

        // GET: /Result/ShowResult
        public ActionResult ShowResult()
        {
            try
            {
                var json = TempData["Result"] as string;

                if (string.IsNullOrEmpty(json))
                    return RedirectToAction("Index");

                // ✅ Deserialize back to ResultViewModel
                var result = JsonSerializer.Deserialize<ResultViewModel>(json);
                return View(result);
            }
            catch (Exception ex)
            {

                throw;
            }
           
        }
    }
}
