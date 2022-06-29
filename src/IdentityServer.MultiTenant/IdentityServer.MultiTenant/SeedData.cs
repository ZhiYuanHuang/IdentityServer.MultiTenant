using IdentityModel;
using IdentityServer.MultiTenant.Data;
using IdentityServer.MultiTenant.Framework.Const;
using IdentityServer.MultiTenant.Framework.Utils;
using IdentityServer.MultiTenant.Models;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace IdentityServer.MultiTenant
{
    public class SeedData
    {
        public static void EnsureSeedData(string connectionString,IConfiguration config) {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var sysIdsConnStr = config.GetConnectionString("SysIdsConnection");
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddIdentityServer(options => {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
                options.EmitStaticAudienceClaim = true;
                options.InputLengthRestrictions.Scope = 2000;

                options.Authentication.CookieSameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            })
                .AddConfigurationStore(options => {
                    options.ConfigureDbContext = (builder) => {
                        builder.UseDbConn(sysIdsConnStr);
                    };
                })
                .AddOperationalStore(options => {
                    options.ConfigureDbContext = (builder) => {
                        builder.UseDbConn(sysIdsConnStr);
                    };
                });

            services.AddDbContext<AspNetAccountDbContext>(options => {
                options.UseDbConn(connectionString);
            });


            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AspNetAccountDbContext>()
                .AddDefaultTokenProviders();

            string sysAdminUserName=!string.IsNullOrEmpty(config["Sys:UserName"])? config["Sys:UserName"] : MulTenantConstants.SysAdminUserName;
            string sysAdminUserPwd = !string.IsNullOrEmpty(config["Sys:UserPwd"]) ? config["Sys:UserPwd"] : MulTenantConstants.SysAdminUserPwd;

            using (var serviceProvider = services.BuildServiceProvider()) {
                using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
                    //var idsContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                    //idsContext.Database.Migrate();

                    //var idsContext2 = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
                    //idsContext2.Database.Migrate();

                    var context = scope.ServiceProvider.GetService<AspNetAccountDbContext>();
                    //context.Database.Migrate();

                    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    var sysAdminRole= roleMgr.FindByNameAsync(MulTenantConstants.SysAdminRole).Result;
                    if (sysAdminRole == null) {
                        var createRoleResult= roleMgr.CreateAsync(new IdentityRole(MulTenantConstants.SysAdminRole)).Result;
                        if (!createRoleResult.Succeeded) {
                            throw new Exception(createRoleResult.Errors.First().Description);
                        }
                    }

                    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                    var sysAdmin = userMgr.FindByNameAsync(sysAdminUserName).Result;
                    if (sysAdmin == null) {
                        sysAdmin = new ApplicationUser { 
                            UserName=sysAdminUserName,
                            Email="SysAdmin@idsmul.com",
                            EmailConfirmed=true,
                        };
                        var result = userMgr.CreateAsync(sysAdmin, sysAdminUserPwd).Result;
                        if (!result.Succeeded) {
                            throw new Exception(result.Errors.First().Description);
                        }

                        result = userMgr.AddToRoleAsync(sysAdmin,MulTenantConstants.SysAdminRole).Result;
                        if (!result.Succeeded) {
                            throw new Exception(result.Errors.First().Description);
                        }
                        Log.Debug($"sysAdmin {sysAdminUserName} created");
                    } else {
                        Log.Debug($"{sysAdminUserName} already exists");
                    }

                    //var alice = userMgr.FindByNameAsync("alice").Result;
                   
                    //if (alice == null) {
                    //    alice = new ApplicationUser {
                    //        UserName = "alice",
                    //        Email = "AliceSmith@email.com",
                    //        EmailConfirmed = true,
                    //    };
                    //    var result = userMgr.CreateAsync(alice, "Pass123$").Result;
                    //    if (!result.Succeeded) {
                    //        throw new Exception(result.Errors.First().Description);
                    //    }

                    //    result = userMgr.AddClaimsAsync(alice, new Claim[]{
                    //        new Claim(JwtClaimTypes.Name, "Alice Smith"),
                    //        new Claim(JwtClaimTypes.GivenName, "Alice"),
                    //        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    //        new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                    //    }).Result;
                    //    if (!result.Succeeded) {
                    //        throw new Exception(result.Errors.First().Description);
                    //    }
                    //    Log.Debug("alice created");
                    //} else {
                    //    Log.Debug("alice already exists");
                    //}

                    //var bob = userMgr.FindByNameAsync("bob").Result;
                    //if (bob == null) {
                    //    bob = new ApplicationUser {
                    //        UserName = "bob",
                    //        Email = "BobSmith@email.com",
                    //        EmailConfirmed = true
                    //    };
                    //    var result = userMgr.CreateAsync(bob, "Pass123$").Result;
                    //    if (!result.Succeeded) {
                    //        throw new Exception(result.Errors.First().Description);
                    //    }

                    //    result = userMgr.AddClaimsAsync(bob, new Claim[]{
                    //        new Claim(JwtClaimTypes.Name, "Bob Smith"),
                    //        new Claim(JwtClaimTypes.GivenName, "Bob"),
                    //        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    //        new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                    //        new Claim("location", "somewhere")
                    //    }).Result;
                    //    if (!result.Succeeded) {
                    //        throw new Exception(result.Errors.First().Description);
                    //    }
                    //    Log.Debug("bob created");
                    //} else {
                    //    Log.Debug("bob already exists");
                    //}
                }
            }
        }
    }
}
