# IdentityServer.MultiTenant

多租户独立数据库模式的identityserver

## 关于IdentityServer.MultiTenant

基于[IdentityServer4](https://github.com/IdentityServer/IdentityServer4)和[Finbuckle.MultiTenant](https://github.com/Finbuckle/Finbuckle.MultiTenant)开发的支持多租户认证鉴权的identityserver站点，支持部分 [Serverless](https://github.com/topics/serverless) 特性 

支持以下特性：

1. 多个租户共享数据库datasource、隔离数据架构database
2. 可扩展数据库服务器实例池，创建租户database自动选择当前占用最少的数据库服务实例
3. 创建租户，自动创建分配该租户的identityserver数据库，无需运维手动创建。