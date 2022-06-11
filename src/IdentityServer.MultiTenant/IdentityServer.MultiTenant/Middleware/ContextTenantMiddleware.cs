using Finbuckle.MultiTenant;
using IdentityServer.MultiTenant.Domain;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Middleware
{
    public class ContextTenantMiddleware
    {
        private readonly RequestDelegate _next;
        public ContextTenantMiddleware(RequestDelegate next) {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ContextTenant contextTenant) {
            //var clientClaim = context.User.Claims.FirstOrDefault(x => x.Type == "client_" + ClientExtraClaimTypeConst.ClientContextConst);
            //if (clientClaim != null) {
            //    string contextClientId = clientClaim.Value;
            //    contextClientDto.ContextClient = contextClientId;

            //}
            var tenantInfo = context.GetMultiTenantContext<ExtendTenantInfo>();
            if(tenantInfo==null || tenantInfo.TenantInfo==null) {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("tenant not exists!");
                return;
            }

            if (tenantInfo != null) {
                contextTenant.TenantInfo = tenantInfo.TenantInfo;
            }
            

            await _next(context);
        }
    }
}
