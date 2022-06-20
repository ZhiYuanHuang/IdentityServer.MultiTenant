using mvc=Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.EntityFramework.DbContexts;

namespace IdentityServer.MultiTenant.Quickstart
{
    [Route("{__tenant__=}/{controller=Home}/{action=Index}")]
    [Authorize(Policy = "sysManagePolicy")]
    public class ManagementController : mvc.Controller
    {
        ConfigurationDbContext _configurationDbContext;
        public ManagementController(ConfigurationDbContext configurationDbContext) {
            _configurationDbContext = configurationDbContext;
        }
        public IActionResult Index() {
            return View();
        }

        public IActionResult Clients() {
            var clientList= _configurationDbContext.Clients.ToList();
            return View(clientList);
        }
    }
}
