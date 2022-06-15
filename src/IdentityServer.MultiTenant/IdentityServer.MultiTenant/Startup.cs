// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Finbuckle.MultiTenant;
using IdentityServer.MultiTenant.Data;
using IdentityServer.MultiTenant.Domain;
using IdentityServer.MultiTenant.Framework.Const;
using IdentityServer.MultiTenant.Framework.Utils;
using IdentityServer.MultiTenant.Middleware;
using IdentityServer.MultiTenant.Models;
using IdentityServer.MultiTenant.MultiStrategy;
using IdentityServer.MultiTenant.Repository;
using IdentityServer.MultiTenant.Service;
using IdentityServer.MultiTenant.TenantStore;
using IdentityServer4;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant
{
    public class Startup
    {
        public IWebHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Environment = environment;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // uncomment, if you want to add an MVC-based UI
            //services.AddControllersWithViews();
            services.AddControllersWithViews(options => {
                options.Filters.Add<GlobalExceptionFilter>();
            });

            services.AddDistributedMemoryCache();

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var emptyConnectionString = Configuration.GetConnectionString("EmptyDefaultConnection");
            var publishHost = Configuration["PublishHost"];

            var sysIdsConnStr = Configuration.GetConnectionString("SysIdsConnection");

            services.AddMultiTenant<ExtendTenantInfo>()
                .WithStore<IdsMulTenantStoreV1<ExtendTenantInfo>>(ServiceLifetime.Scoped, (provider) => {
                    var cache = provider.GetRequiredService<IDistributedCache>();
                    var tenantRepo = provider.GetRequiredService<TenantRepository>();
                    return new IdsMulTenantStoreV1<ExtendTenantInfo>(tenantRepo, provider.GetRequiredService<ContextSystem>(), sysIdsConnStr, cache, MulTenantConstants.TenantToken, TimeSpan.FromSeconds(3));
                })
                .WithStrategy<ExHostStrategy>(ServiceLifetime.Singleton, "__tenant__.*")  //$"__tenant__.{publishHost}"
                                                                                          //.WithConfigurationStore()
                                                                                          //.WithStore
                .WithRouteStrategy()
                //.WithInMemoryStore(options => { 
                //     options.IsCaseSensitive = true;
                //    options.Tenants.Add(new ExtendTenantInfo { Id = System.Guid.NewGuid().ToString(), Identifier = "test1", Name = "testTenant1", EncryptedIdsConnectionString = emptyConnectionString });
                //    options.Tenants.Add(new ExtendTenantInfo { Id = System.Guid.NewGuid().ToString(), Identifier = "test2", Name = "testTenant3",EncryptedIdsConnectionString= emptyConnectionString });
                //     options.Tenants.Add(new ExtendTenantInfo { Id = System.Guid.NewGuid().ToString(), Identifier = "test3", Name = "testTenant3",EncryptedIdsConnectionString=emptyConnectionString });
                // })
                .WithPerTenantOptions<JwtBearerOptions>((o,tenantinfo)=> {
                    string publishHost = Configuration.GetValue<string>("PublishHost");
                    int publishPort = Configuration.GetValue<int>("PublishPort");
                    o.Authority = $"http://{tenantinfo.Identifier}.{publishHost}:{publishPort}";// "http://localhost:5000";
                    o.RequireHttpsMetadata = false;
                });
           
            services.AddDbContext<AspNetAccountDbContext>((provider,options) => {
                var contextTenant = provider.GetRequiredService<ContextTenant>();
                string realIdsConnStr = string.Empty;
                if (contextTenant.TenantInfo == null) {
                    realIdsConnStr = emptyConnectionString;
                } else {
                    realIdsConnStr = contextTenant.TenantInfo.ConnectionString;
                    if (string.IsNullOrEmpty(realIdsConnStr)) {
                        var encryptService = provider.GetRequiredService<EncryptService>();
                        string encryptedIdsConnStr = encryptService.Decrypt_Aes(contextTenant.TenantInfo.EncryptedIdsConnectionString);
                        realIdsConnStr = encryptedIdsConnStr;
                    }

                    if (string.IsNullOrEmpty(realIdsConnStr)) {
                        realIdsConnStr = emptyConnectionString;
                    }
                }

                options.UseDbConn(realIdsConnStr);
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AspNetAccountDbContext>()
                .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer(options => {
                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                //options.EmitStaticAudienceClaim = true;

                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://identityserver4.readthedocs.io/en/latest/topics/resources.html
                options.EmitStaticAudienceClaim = true;
                options.InputLengthRestrictions.Scope = 2000;

                options.Authentication.CookieSameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            })
                //.AddInMemoryIdentityResources(Config.IdentityResources)
                //.AddInMemoryApiScopes(Config.ApiScopes)
                //.AddInMemoryClients(Config.Clients)
                .AddConfigurationStore(options => {
                    options.ResolveDbContextOptions = (provider, builder) => {
                        var contextTenant = provider.GetRequiredService<ContextTenant>();
                        string realIdsConnStr = string.Empty;
                        if (contextTenant.TenantInfo == null) {
                            realIdsConnStr = emptyConnectionString;
                        } else {
                            realIdsConnStr = contextTenant.TenantInfo.ConnectionString;
                            if (string.IsNullOrEmpty(realIdsConnStr)) {
                                var encryptService = provider.GetRequiredService<EncryptService>();
                                string encryptedIdsConnStr = encryptService.Decrypt_Aes(contextTenant.TenantInfo.EncryptedIdsConnectionString);
                                realIdsConnStr = encryptedIdsConnStr;
                            }

                            if (string.IsNullOrEmpty(realIdsConnStr)) {
                                realIdsConnStr = emptyConnectionString;
                            }

                        }

                        builder.UseDbConn(realIdsConnStr);
                    };
                })
                .AddOperationalStore(options => {
                    options.ResolveDbContextOptions = (provider, builder) => {
                        var contextTenant = provider.GetRequiredService<ContextTenant>();
                        string realIdsConnStr = string.Empty;
                        if (contextTenant.TenantInfo == null) {
                            realIdsConnStr = emptyConnectionString;
                        } else {
                            realIdsConnStr = contextTenant.TenantInfo.ConnectionString;
                            if (string.IsNullOrEmpty(realIdsConnStr)) {
                                var encryptService = provider.GetRequiredService<EncryptService>();
                                string encryptedIdsConnStr = encryptService.Decrypt_Aes(contextTenant.TenantInfo.EncryptedIdsConnectionString);
                                realIdsConnStr = encryptedIdsConnStr;
                            }

                            if (string.IsNullOrEmpty(realIdsConnStr)) {
                                realIdsConnStr = emptyConnectionString;
                            }

                        }

                        builder.UseDbConn(realIdsConnStr);
                    };
                })
                .AddAspNetIdentity<ApplicationUser>()
                .AddProfileService<ProfileService>();
                ;

            services.AddSingleton<ICorsPolicyService>((container) => {
                var logger = container.GetRequiredService<ILogger<DefaultCorsPolicyService>>();
                return new DefaultCorsPolicyService(logger) {
                    AllowAll = true,
                };
            });

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();

            services.AddAuthorization(options => {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("Bearer")
                .RequireAuthenticatedUser().Build();

                //options.AddPolicy("sysManagePolicy", builder => {

                //    builder.AddAuthenticationSchemes("Bearer");
                //    builder.RequireAuthenticatedUser();
                //    builder.RequireClaim("aud", "idsmul");
                //    builder.RequireScope("idsmul.manage");

                //});

                options.AddPolicy("sysManagePolicy", builder => {

                    //builder.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
                    builder.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme);
                    builder.RequireAuthenticatedUser();
                    builder.RequireRole(MulTenantConstants.SysAdminRole);

                });

                options.AddPolicy("manageTenantPolicy", builder => {
                    builder.AddAuthenticationSchemes("Bearer");
                    builder.RequireAuthenticatedUser();
                    builder.RequireClaim("aud", "idsmul");
                    builder.RequireScope("idsmul.addtenant");
                });


                //("sysPolicy", builder => {
                //    builder.AddAuthenticationSchemes("Bearer");
                //    builder.RequireAuthenticatedUser();
                //    //builder.RequireClaim("aud", "sysmanage");
                //    //builder.RequireScope("sysmanage.resourcemanage");
                //});
            });

            services//.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                //.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts => {
                //    opts.AccessDeniedPath = "/sys/Account/Login";
                //    opts.Cookie.HttpOnly = true;
                //    opts.LoginPath = "/sys/Account/Login";


                //    opts.Cookie.Name = "sdfdss";
                //    opts.Cookie.IsEssential = true;
                //    opts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                //    //opts.Cookie.Name=
                //    //opts.Cookie.Name = IdentityServerConstants.DefaultCookieAuthenticationScheme;
                //    //opts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                //    //opts.Cookie.SecurePolicy=Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
                //})
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts => {
                    opts.RequireHttpsMetadata = false;
                    string publishHost = Configuration.GetValue<string>("PublishHost");
                    int publishPort = Configuration.GetValue<int>("PublishPort");
                    opts.Authority = $"http://{publishHost}:{publishPort}";// "http://localhost:5000";
                });

            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options => {
                options.AccessDeniedPath = "/sys/Account/Login";
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/sys/Account/Login";
               
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            });

            services.AddCors(options => {
                options.AddPolicy("default", policy => {
                    policy.AllowAnyOrigin();
                    policy.AllowAnyHeader();
                    policy.AllowAnyMethod();

                    //policy.AllowCredentials();
                });
            });

            services.AddScoped<ContextTenant>();
            services.AddScoped<ContextSystem>();
            services.AddDbUtil(o => {
                o.MasterConnStr = Configuration.GetConnectionString("TenantStoreMaster");
                o.SlaveConnStr = Configuration.GetConnectionString("TenantStoreSlave");
            });
            services.AddTransient<TenantRepository>();
            services.AddTransient<DbServerRepository>();
            services.AddSingleton<MysqlDbOperaService>();
            services.AddSingleton<SqliteDbOperaService>();
            services.AddSingleton<ITenantDbOperation>(provider => { 
                if(string.CompareOrdinal(Configuration["SampleDb:DbType"], "mysql")==0) {
                    return provider.GetRequiredService<MysqlDbOperaService>();
                } else {
                    return provider.GetRequiredService<SqliteDbOperaService>();
                }
            });
            services.AddSingleton<EncryptService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // uncomment if you want to add MVC
            app.UseStaticFiles();
            //app.UseRouting();

            app.UseRouting();
            app.UseCors("default");

            app.UseMultiTenant();
            app.UseMiddleware<ContextTenantMiddleware>();
            app.UseIdentityServer();

            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute("default", "api/{controller=Home}/{action=Index}");
                //endpoints.MapControllerRoute("sys", "{__tenant__=}/{controller=Home}/{action=Index}");
                //endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
