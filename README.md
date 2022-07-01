# IdentityServer.MultiTenant

多租户独立数据库模式的identityserver

## 关于IdentityServer.MultiTenant

基于[IdentityServer4](https://github.com/IdentityServer/IdentityServer4)和[Finbuckle.MultiTenant](https://github.com/Finbuckle/Finbuckle.MultiTenant)开发的支持多租户认证鉴权的identityserver站点，支持部分 [Serverless](https://github.com/topics/serverless) 特性 

支持以下特性：

1. 多个租户共享数据库datasource、隔离数据架构database
2. 可扩展数据库服务器实例池，创建租户database自动选择当前占用最少的数据库服务实例
3. 创建租户，自动创建分配该租户的identityserver数据库，无需运维手动创建。
4. 支持mysql、sqlite两种，其中默认使用sqlite，拉取代码后即可调试运行，方便快速上手开发。

## 原理

1. oauth 终结点（如 获取token '/connect/token'等）以http请求的 host 解析到对应的租户identityserver 数据，从而达到实现各租户的认证授权；
2. http请求的host形式为 {identifier}.{maindomain}，每个二级主域名 maindomain 下管理一批以 identifier 为标识的租户，例：test1.idsmul.com 将映射到 idsmul.com 域名下 test1 租户，test2.idsmul.com 解析到 idsmul.com 域名下的 test2 租户；
3. maindomain 也可为多级域名的形式，如：education.idsmul.com、Ecommerce.idsmul.com，由此细分为教育行业、电商行业，达到充分利用宝贵的通用二级域名。此时 test1.education.idsmul.com 将解析为 education.idsmul.com 下的test1租户，test1.Ecommerce.idsmul.com 将解析为 education.idsmul.com 下的test1租户，此两个租户为不同租户；
4. 系统有 sys.identityserver.db 后台管理数据库，为管理系统数据的identityserver数据库，由浏览器登录后可管理系统；
5. 数据库 tenantstore.mulids.sql，存储系统数据，如：域名 domain、数据库使用mysql时的数据库服务器池 DbServer、租户信息 tenantinfo (存储加密数据库链接)；
6. 数据库 emptyidentityserver.db，当解析不到租户时，映射到空的identityserver数据库；
7. 数据库 ids_sample，创建新的租户时，以此identityserver 模板数据库创建该租户的identityserver数据库；
8. 租户可由api接口或系统管理页面创建，其中api接口由 管理数据库 sys.identityserver.db 的 client_credentials 模式进行登录授权，管理页面提供管理功能，如：启用/删除租户、迁移数据库功能（使用于mysql）;
9. api接口的client 由系统管理页面创建，client的token包含 domain 等信息，经由api接口创建的租户则使用此 domain 域名；

## 事项

1. 地址信息：后台基地址为 {ip:port}/sys/，api基地址为 {ip:port}/manage/

## 后台管理功能截图

1. 创建 domain 信息

![创建 Domain ](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/domains.png)

2. 创建 client 信息

![创建 Client](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/clients.png)

3. 创建 db server 信息

![创建 db server](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/dbservers.png)

4.创建 tenant 信息

![创建 tenant](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/tenants.png)

5.迁移 tenant 数据库

![迁移 数据库](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/migrate.png)

## postman 示例截图

1. 租户 test1 用户获取token

![租户 test1 用户获取token](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/tenant1Token.png)

2. 租户 test2 用户获取token

![租户 test2 用户获取token](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/tenant2Token.png)

3. 使用租户 test1 用户的 token 访问租户 test1 的带验证接口，访问正常

![使用租户 test1 用户的 token 访问租户 test1 的待验证接口，访问正常](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/tenant1Verify.png)

4. 使用租户 test2 用户的 token 访问租户 test1 的带验证接口，访问返回未授权401

![使用租户 test2 用户的 token 访问租户 test1 的待验证接口，访问返回未授权401](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/tenant2Verify.png)

5. 系统 client 获取token

![系统 client 获取token](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/clientGetToken.png)

6. 系统 client 调用 api 接口创建租户

![系统 client 调用 api 接口创建租户](https://raw.githubusercontent.com/ZhiYuanHuang/IdentityServer.MultiTenant/develop/snap/clientCreateTenant.png)
