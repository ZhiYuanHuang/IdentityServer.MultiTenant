using Microsoft.AspNetCore.Authorization;
using mvc=Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.MultiTenant.Quickstart
{
    [AllowAnonymous]
    public class HomeController : mvc.Controller
    {
        public IActionResult Index() {
            return View();
        }
    }
}
