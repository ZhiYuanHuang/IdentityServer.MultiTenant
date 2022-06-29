using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Quickstart
{
    public class ErrorViewModel
    {
        public ErrorViewModel() {
        }

        public ErrorViewModel(string error) {
            Error = error;
        }

        public string Error { get; set; }
    }
}
