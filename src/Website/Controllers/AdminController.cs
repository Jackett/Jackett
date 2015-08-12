using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Website.Services;

namespace Website.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        public async Task<ActionResult> Sync()
        {
           
            await new DatabaseService().Sync();
            return View();
        }
    }
}