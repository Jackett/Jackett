using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website.Services;

namespace Website.Controllers
{
    public class DownloadController : Controller
    {
        // GET: Download
        public ActionResult Index()
        {
            ViewBag.Releases = DatabaseService.GetReleases();
            return View();
        }

        public ActionResult GetFile(string version, string file)
        {
            var filePath = Path.Combine(HttpRuntime.AppDomainAppPath, "App_data", "files", Path.GetFileName(version), Path.GetFileName(file));
            if(!System.IO.File.Exists(filePath))
                return new HttpNotFoundResult("Unknown download");

            DatabaseService.RecordDownload(version, file);

            return File(System.IO.File.ReadAllBytes(filePath), MimeMapping.GetMimeMapping(filePath), Path.GetFileName(filePath));
        }
    }
}