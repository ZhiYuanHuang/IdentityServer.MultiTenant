using System;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Finbuckle.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer.MultiTenant.Domain;
using IdentityServer.MultiTenant.Framework.Const;

namespace IdentityServer.MultiTenant.MultiStrategy
{
    public class ExHostStrategy : IMultiTenantStrategy
    {
        //internal static class Constants
        //{
        //    public static int TenantIdMaxLength = 64;
        //    public static string TenantToken = "__tenant__";
        //}

        private readonly string regex;

        public ExHostStrategy(string template) {
            // New in 2.1, match whole domain if just "__tenant__".
            if (template == MulTenantConstants.TenantToken) {
                template = template.Replace(MulTenantConstants.TenantToken, @"(?<identifier>.+)");
            } else {
                // Check for valid template.
                // Template cannot be null or whitespace.
                if (string.IsNullOrWhiteSpace(template)) {
                    throw new MultiTenantException("Template cannot be null or whitespace.");
                }
                // Wildcard "*" must be only occur once in template.
                if (Regex.Match(template, @"\*(?=.*\*)").Success) {
                    throw new MultiTenantException("Wildcard \"*\" must be only occur once in template.");
                }
                // Wildcard "*" must be only token in template segment.
                if (Regex.Match(template, @"\*[^\.]|[^\.]\*").Success) {
                    throw new MultiTenantException("\"*\" wildcard must be only token in template segment.");
                }
                // Wildcard "?" must be only token in template segment.
                if (Regex.Match(template, @"\?[^\.]|[^\.]\?").Success) {
                    throw new MultiTenantException("\"?\" wildcard must be only token in template segment.");
                }

                template = template.Trim().Replace(".", @"\.");
                string wildcardSegmentsPattern = @"(\.[^\.]+)*";
                string singleSegmentPattern = @"[^\.]+";
                if (template.Substring(template.Length - 3, 3) == @"\.*") {
                    template = template.Substring(0, template.Length - 3) + wildcardSegmentsPattern;
                }

                wildcardSegmentsPattern = @"([^\.]+\.)*";
                template = template.Replace(@"*\.", wildcardSegmentsPattern);
                template = template.Replace("?", singleSegmentPattern);
                template = template.Replace(MulTenantConstants.TenantToken, @"(?<identifier>[^\.]+)");
            }

            this.regex = $"^{template}$";
        }

        public async Task<string?> GetIdentifierAsync(object context) {
            if (!(context is HttpContext httpContext))
                throw new MultiTenantException(null,
                    new ArgumentException($"\"{nameof(context)}\" type must be of type HttpContext", nameof(context)));

            var host = httpContext.Request.Host;

            if (host.HasValue == false)
                return null;

            if(System.Net.IPAddress.TryParse(host.Host,out _)) {
                return null;
            }

            string? identifier = null;

            var match = Regex.Match(host.Host, regex,
                RegexOptions.ExplicitCapture,
                TimeSpan.FromMilliseconds(100));

            if (match.Success) {
                identifier = match.Groups["identifier"].Value;

                var contextSystem= httpContext.RequestServices.GetRequiredService<ContextSystem>();

                contextSystem.SystemDomain = match.Value.Substring(identifier.Length+1);

            }

            return await Task.FromResult(identifier);
        }
    }

    
}
