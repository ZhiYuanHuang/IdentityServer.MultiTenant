using mvc=Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IdentityServer.MultiTenant.Quickstart
{
    [Route("{__tenant__=}/{controller=Home}/{action=Index}")]
    [Authorize(Policy = "sysManagePolicy")]
    public class ManagementController : mvc.Controller
    {
        public IActionResult Index() {
            return View();
        }
    }
}
