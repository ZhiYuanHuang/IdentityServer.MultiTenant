/*
 Navicat Premium Data Transfer

 Source Server         : wsl_mysql
 Source Server Type    : MySQL
 Source Server Version : 50734
 Source Host           : localhost:3307
 Source Schema         : sys.identityserver.db

 Target Server Type    : MySQL
 Target Server Version : 50734
 File Encoding         : 65001

 Date: 29/06/2022 17:37:49
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for apiresourceclaims
-- ----------------------------
DROP TABLE IF EXISTS `apiresourceclaims`;
CREATE TABLE `apiresourceclaims`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ApiResourceClaims_ApiResourceId`(`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceClaims_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiresourceclaims
-- ----------------------------

-- ----------------------------
-- Table structure for apiresourceproperties
-- ----------------------------
DROP TABLE IF EXISTS `apiresourceproperties`;
CREATE TABLE `apiresourceproperties`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Value` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ApiResourceProperties_ApiResourceId`(`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceProperties_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiresourceproperties
-- ----------------------------

-- ----------------------------
-- Table structure for apiresources
-- ----------------------------
DROP TABLE IF EXISTS `apiresources`;
CREATE TABLE `apiresources`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `DisplayName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `AllowedAccessTokenSigningAlgorithms` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ShowInDiscoveryDocument` tinyint(1) NOT NULL,
  `Created` datetime(6) NOT NULL,
  `Updated` datetime(6) NULL DEFAULT NULL,
  `LastAccessed` datetime(6) NULL DEFAULT NULL,
  `NonEditable` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `IX_ApiResources_Name`(`Name`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 14 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiresources
-- ----------------------------
INSERT INTO `apiresources` VALUES (13, 1, 'idsmul', '多租户Ids', NULL, NULL, 1, '2021-11-16 12:28:51.597613', NULL, NULL, 0);

-- ----------------------------
-- Table structure for apiresourcescopes
-- ----------------------------
DROP TABLE IF EXISTS `apiresourcescopes`;
CREATE TABLE `apiresourcescopes`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Scope` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ApiResourceScopes_ApiResourceId`(`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceScopes_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 98 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiresourcescopes
-- ----------------------------
INSERT INTO `apiresourcescopes` VALUES (97, 'idsmul.createtenant', 13);

-- ----------------------------
-- Table structure for apiresourcesecrets
-- ----------------------------
DROP TABLE IF EXISTS `apiresourcesecrets`;
CREATE TABLE `apiresourcesecrets`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Value` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Expiration` datetime(6) NULL DEFAULT NULL,
  `Type` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Created` datetime(6) NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ApiResourceSecrets_ApiResourceId`(`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceSecrets_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiresourcesecrets
-- ----------------------------

-- ----------------------------
-- Table structure for apiscopeclaims
-- ----------------------------
DROP TABLE IF EXISTS `apiscopeclaims`;
CREATE TABLE `apiscopeclaims`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ScopeId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ApiScopeClaims_ScopeId`(`ScopeId`) USING BTREE,
  CONSTRAINT `FK_ApiScopeClaims_ApiScopes_ScopeId` FOREIGN KEY (`ScopeId`) REFERENCES `apiscopes` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiscopeclaims
-- ----------------------------

-- ----------------------------
-- Table structure for apiscopeproperties
-- ----------------------------
DROP TABLE IF EXISTS `apiscopeproperties`;
CREATE TABLE `apiscopeproperties`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Value` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ScopeId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ApiScopeProperties_ScopeId`(`ScopeId`) USING BTREE,
  CONSTRAINT `FK_ApiScopeProperties_ApiScopes_ScopeId` FOREIGN KEY (`ScopeId`) REFERENCES `apiscopes` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiscopeproperties
-- ----------------------------

-- ----------------------------
-- Table structure for apiscopes
-- ----------------------------
DROP TABLE IF EXISTS `apiscopes`;
CREATE TABLE `apiscopes`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `DisplayName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Required` tinyint(1) NOT NULL,
  `Emphasize` tinyint(1) NOT NULL,
  `ShowInDiscoveryDocument` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `IX_ApiScopes_Name`(`Name`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 40 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of apiscopes
-- ----------------------------
INSERT INTO `apiscopes` VALUES (39, 1, 'idsmul.createtenant', '创建租户', NULL, 0, 0, 1);

-- ----------------------------
-- Table structure for aspnetroleclaims
-- ----------------------------
DROP TABLE IF EXISTS `aspnetroleclaims`;
CREATE TABLE `aspnetroleclaims`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `ClaimValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_AspNetRoleClaims_RoleId`(`RoleId`) USING BTREE,
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of aspnetroleclaims
-- ----------------------------

-- ----------------------------
-- Table structure for aspnetroles
-- ----------------------------
DROP TABLE IF EXISTS `aspnetroles`;
CREATE TABLE `aspnetroles`  (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Name` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `NormalizedName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `RoleNameIndex`(`NormalizedName`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of aspnetroles
-- ----------------------------
INSERT INTO `aspnetroles` VALUES ('de63a8fa-c650-43cc-98b5-7fe0bd7c4ab9', 'sysadmin', 'SYSADMIN', 'e4988908-d3ba-4ea4-902b-6199582576a3');

-- ----------------------------
-- Table structure for aspnetuserclaims
-- ----------------------------
DROP TABLE IF EXISTS `aspnetuserclaims`;
CREATE TABLE `aspnetuserclaims`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClaimType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `ClaimValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_AspNetUserClaims_UserId`(`UserId`) USING BTREE,
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of aspnetuserclaims
-- ----------------------------

-- ----------------------------
-- Table structure for aspnetuserlogins
-- ----------------------------
DROP TABLE IF EXISTS `aspnetuserlogins`;
CREATE TABLE `aspnetuserlogins`  (
  `LoginProvider` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ProviderKey` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ProviderDisplayName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`LoginProvider`, `ProviderKey`) USING BTREE,
  INDEX `IX_AspNetUserLogins_UserId`(`UserId`) USING BTREE,
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of aspnetuserlogins
-- ----------------------------

-- ----------------------------
-- Table structure for aspnetuserroles
-- ----------------------------
DROP TABLE IF EXISTS `aspnetuserroles`;
CREATE TABLE `aspnetuserroles`  (
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `RoleId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`UserId`, `RoleId`) USING BTREE,
  INDEX `IX_AspNetUserRoles_RoleId`(`RoleId`) USING BTREE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of aspnetuserroles
-- ----------------------------
INSERT INTO `aspnetuserroles` VALUES ('7f21aab0-7075-4cd7-abcd-518bf0bb8c67', 'de63a8fa-c650-43cc-98b5-7fe0bd7c4ab9');

-- ----------------------------
-- Table structure for aspnetusers
-- ----------------------------
DROP TABLE IF EXISTS `aspnetusers`;
CREATE TABLE `aspnetusers`  (
  `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `UserName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Email` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `SecurityStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `PhoneNumber` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) NULL DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `UserNameIndex`(`NormalizedUserName`) USING BTREE,
  INDEX `EmailIndex`(`NormalizedEmail`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of aspnetusers
-- ----------------------------
INSERT INTO `aspnetusers` VALUES ('7f21aab0-7075-4cd7-abcd-518bf0bb8c67', 'SysAdmin', 'SYSADMIN', 'SysAdmin@idsmul.com', 'SYSADMIN@IDSMUL.COM', 1, 'AQAAAAEAACcQAAAAEGudUHBOgYVWGQccmX7I1LX/xQRdaiSFO46cmZcnL9XovJByWU0ZUGHhtnlq132jvg==', 'T5CVMLOIITEZ655UXO3PR4VJ5N35VT5P', '2bf3fb5a-9002-4f17-8630-725f0452610c', NULL, 0, 0, NULL, 1, 0);

-- ----------------------------
-- Table structure for aspnetusertokens
-- ----------------------------
DROP TABLE IF EXISTS `aspnetusertokens`;
CREATE TABLE `aspnetusertokens`  (
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `LoginProvider` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Value` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL,
  PRIMARY KEY (`UserId`, `LoginProvider`, `Name`) USING BTREE,
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of aspnetusertokens
-- ----------------------------

-- ----------------------------
-- Table structure for clientclaims
-- ----------------------------
DROP TABLE IF EXISTS `clientclaims`;
CREATE TABLE `clientclaims`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Value` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientClaims_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientClaims_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientclaims
-- ----------------------------

-- ----------------------------
-- Table structure for clientcorsorigins
-- ----------------------------
DROP TABLE IF EXISTS `clientcorsorigins`;
CREATE TABLE `clientcorsorigins`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Origin` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientCorsOrigins_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientCorsOrigins_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientcorsorigins
-- ----------------------------

-- ----------------------------
-- Table structure for clientgranttypes
-- ----------------------------
DROP TABLE IF EXISTS `clientgranttypes`;
CREATE TABLE `clientgranttypes`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `GrantType` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientGrantTypes_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientGrantTypes_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 14 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientgranttypes
-- ----------------------------
INSERT INTO `clientgranttypes` VALUES (13, 'client_credentials', 17);

-- ----------------------------
-- Table structure for clientidprestrictions
-- ----------------------------
DROP TABLE IF EXISTS `clientidprestrictions`;
CREATE TABLE `clientidprestrictions`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Provider` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientIdPRestrictions_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientIdPRestrictions_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientidprestrictions
-- ----------------------------

-- ----------------------------
-- Table structure for clientpostlogoutredirecturis
-- ----------------------------
DROP TABLE IF EXISTS `clientpostlogoutredirecturis`;
CREATE TABLE `clientpostlogoutredirecturis`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PostLogoutRedirectUri` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientPostLogoutRedirectUris_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientPostLogoutRedirectUris_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientpostlogoutredirecturis
-- ----------------------------

-- ----------------------------
-- Table structure for clientproperties
-- ----------------------------
DROP TABLE IF EXISTS `clientproperties`;
CREATE TABLE `clientproperties`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Value` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientProperties_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientProperties_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientproperties
-- ----------------------------

-- ----------------------------
-- Table structure for clientredirecturis
-- ----------------------------
DROP TABLE IF EXISTS `clientredirecturis`;
CREATE TABLE `clientredirecturis`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RedirectUri` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientRedirectUris_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientRedirectUris_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientredirecturis
-- ----------------------------

-- ----------------------------
-- Table structure for clients
-- ----------------------------
DROP TABLE IF EXISTS `clients`;
CREATE TABLE `clients`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `ClientId` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ProtocolType` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `RequireClientSecret` tinyint(1) NOT NULL,
  `ClientName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ClientUri` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `LogoUri` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `RequireConsent` tinyint(1) NOT NULL,
  `AllowRememberConsent` tinyint(1) NOT NULL,
  `AlwaysIncludeUserClaimsInIdToken` tinyint(1) NOT NULL,
  `RequirePkce` tinyint(1) NOT NULL,
  `AllowPlainTextPkce` tinyint(1) NOT NULL,
  `RequireRequestObject` tinyint(1) NOT NULL,
  `AllowAccessTokensViaBrowser` tinyint(1) NOT NULL,
  `FrontChannelLogoutUri` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `FrontChannelLogoutSessionRequired` tinyint(1) NOT NULL,
  `BackChannelLogoutUri` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `BackChannelLogoutSessionRequired` tinyint(1) NOT NULL,
  `AllowOfflineAccess` tinyint(1) NOT NULL,
  `IdentityTokenLifetime` int(11) NOT NULL,
  `AllowedIdentityTokenSigningAlgorithms` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `AccessTokenLifetime` int(11) NOT NULL,
  `AuthorizationCodeLifetime` int(11) NOT NULL,
  `ConsentLifetime` int(11) NULL DEFAULT NULL,
  `AbsoluteRefreshTokenLifetime` int(11) NOT NULL,
  `SlidingRefreshTokenLifetime` int(11) NOT NULL,
  `RefreshTokenUsage` int(11) NOT NULL,
  `UpdateAccessTokenClaimsOnRefresh` tinyint(1) NOT NULL,
  `RefreshTokenExpiration` int(11) NOT NULL,
  `AccessTokenType` int(11) NOT NULL,
  `EnableLocalLogin` tinyint(1) NOT NULL,
  `IncludeJwtId` tinyint(1) NOT NULL,
  `AlwaysSendClientClaims` tinyint(1) NOT NULL,
  `ClientClaimsPrefix` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `PairWiseSubjectSalt` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Created` datetime(6) NOT NULL,
  `Updated` datetime(6) NULL DEFAULT NULL,
  `LastAccessed` datetime(6) NULL DEFAULT NULL,
  `UserSsoLifetime` int(11) NULL DEFAULT NULL,
  `UserCodeType` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `DeviceCodeLifetime` int(11) NOT NULL,
  `NonEditable` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `IX_Clients_ClientId`(`ClientId`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 18 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clients
-- ----------------------------
INSERT INTO `clients` VALUES (17, 1, 'MyApp4', 'oidc', 1, 'ffddd', NULL, NULL, NULL, 0, 1, 0, 1, 0, 0, 0, NULL, 1, NULL, 1, 0, 300, NULL, 1296000, 300, NULL, 2592000, 1296000, 1, 0, 1, 0, 1, 0, 0, 'client_', NULL, '2022-06-20 09:53:47.411095', NULL, NULL, NULL, NULL, 300, 0);

-- ----------------------------
-- Table structure for clientscopes
-- ----------------------------
DROP TABLE IF EXISTS `clientscopes`;
CREATE TABLE `clientscopes`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Scope` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientScopes_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientScopes_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 270 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientscopes
-- ----------------------------
INSERT INTO `clientscopes` VALUES (269, 'idsmul.createtenant', 17);

-- ----------------------------
-- Table structure for clientsecrets
-- ----------------------------
DROP TABLE IF EXISTS `clientsecrets`;
CREATE TABLE `clientsecrets`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Description` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Value` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Expiration` datetime(6) NULL DEFAULT NULL,
  `Type` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Created` datetime(6) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_ClientSecrets_ClientId`(`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientSecrets_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 20 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of clientsecrets
-- ----------------------------
INSERT INTO `clientsecrets` VALUES (19, NULL, '4crYpN+ngBWgoMb3A2VCgO8+IQPzvSQ+0v4/F+eQNiw=', NULL, 'SharedSecret', '2022-06-23 02:43:11.181973', 17);

-- ----------------------------
-- Table structure for devicecodes
-- ----------------------------
DROP TABLE IF EXISTS `devicecodes`;
CREATE TABLE `devicecodes`  (
  `UserCode` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `DeviceCode` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `SubjectId` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `SessionId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ClientId` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `CreationTime` datetime(6) NOT NULL,
  `Expiration` datetime(6) NOT NULL,
  `Data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`UserCode`) USING BTREE,
  UNIQUE INDEX `IX_DeviceCodes_DeviceCode`(`DeviceCode`) USING BTREE,
  INDEX `IX_DeviceCodes_Expiration`(`Expiration`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of devicecodes
-- ----------------------------

-- ----------------------------
-- Table structure for identityresourceclaims
-- ----------------------------
DROP TABLE IF EXISTS `identityresourceclaims`;
CREATE TABLE `identityresourceclaims`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `IdentityResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_IdentityResourceClaims_IdentityResourceId`(`IdentityResourceId`) USING BTREE,
  CONSTRAINT `FK_IdentityResourceClaims_IdentityResources_IdentityResourceId` FOREIGN KEY (`IdentityResourceId`) REFERENCES `identityresources` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of identityresourceclaims
-- ----------------------------

-- ----------------------------
-- Table structure for identityresourceproperties
-- ----------------------------
DROP TABLE IF EXISTS `identityresourceproperties`;
CREATE TABLE `identityresourceproperties`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Value` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `IdentityResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_IdentityResourceProperties_IdentityResourceId`(`IdentityResourceId`) USING BTREE,
  CONSTRAINT `FK_IdentityResourceProperties_IdentityResources_IdentityResourc~` FOREIGN KEY (`IdentityResourceId`) REFERENCES `identityresources` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of identityresourceproperties
-- ----------------------------

-- ----------------------------
-- Table structure for identityresources
-- ----------------------------
DROP TABLE IF EXISTS `identityresources`;
CREATE TABLE `identityresources`  (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `DisplayName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Required` tinyint(1) NOT NULL,
  `Emphasize` tinyint(1) NOT NULL,
  `ShowInDiscoveryDocument` tinyint(1) NOT NULL,
  `Created` datetime(6) NOT NULL,
  `Updated` datetime(6) NULL DEFAULT NULL,
  `NonEditable` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `IX_IdentityResources_Name`(`Name`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of identityresources
-- ----------------------------

-- ----------------------------
-- Table structure for persistedgrants
-- ----------------------------
DROP TABLE IF EXISTS `persistedgrants`;
CREATE TABLE `persistedgrants`  (
  `Key` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `SubjectId` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `SessionId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ClientId` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `CreationTime` datetime(6) NOT NULL,
  `Expiration` datetime(6) NULL DEFAULT NULL,
  `ConsumedTime` datetime(6) NULL DEFAULT NULL,
  `Data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`Key`) USING BTREE,
  INDEX `IX_PersistedGrants_Expiration`(`Expiration`) USING BTREE,
  INDEX `IX_PersistedGrants_SubjectId_ClientId_Type`(`SubjectId`, `ClientId`, `Type`) USING BTREE,
  INDEX `IX_PersistedGrants_SubjectId_SessionId_Type`(`SubjectId`, `SessionId`, `Type`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of persistedgrants
-- ----------------------------

SET FOREIGN_KEY_CHECKS = 1;
