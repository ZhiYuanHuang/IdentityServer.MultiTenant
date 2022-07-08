-- MySQL dump 10.13  Distrib 8.0.24, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: ids_sample
-- ------------------------------------------------------
-- Server version	5.7.34-0ubuntu0.18.04.1

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiresourceclaims`
--

DROP TABLE IF EXISTS `apiresourceclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiresourceclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(200) NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ApiResourceClaims_ApiResourceId` (`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceClaims_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiresourceclaims`
--

LOCK TABLES `apiresourceclaims` WRITE;
/*!40000 ALTER TABLE `apiresourceclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `apiresourceclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiresourceproperties`
--

DROP TABLE IF EXISTS `apiresourceproperties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiresourceproperties` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) NOT NULL,
  `Value` varchar(2000) NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ApiResourceProperties_ApiResourceId` (`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceProperties_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiresourceproperties`
--

LOCK TABLES `apiresourceproperties` WRITE;
/*!40000 ALTER TABLE `apiresourceproperties` DISABLE KEYS */;
/*!40000 ALTER TABLE `apiresourceproperties` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiresources`
--

DROP TABLE IF EXISTS `apiresources`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiresources` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `Name` varchar(200) NOT NULL,
  `DisplayName` varchar(200) DEFAULT NULL,
  `Description` varchar(1000) DEFAULT NULL,
  `AllowedAccessTokenSigningAlgorithms` varchar(100) DEFAULT NULL,
  `ShowInDiscoveryDocument` tinyint(1) NOT NULL,
  `Created` datetime(6) NOT NULL,
  `Updated` datetime(6) DEFAULT NULL,
  `LastAccessed` datetime(6) DEFAULT NULL,
  `NonEditable` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `IX_ApiResources_Name` (`Name`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiresources`
--

LOCK TABLES `apiresources` WRITE;
/*!40000 ALTER TABLE `apiresources` DISABLE KEYS */;
INSERT INTO `apiresources` VALUES (10,1,'test','测试',NULL,NULL,1,'2022-05-28 14:33:00.000000',NULL,NULL,0);
/*!40000 ALTER TABLE `apiresources` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiresourcescopes`
--

DROP TABLE IF EXISTS `apiresourcescopes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiresourcescopes` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Scope` varchar(200) NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ApiResourceScopes_ApiResourceId` (`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceScopes_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=94 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiresourcescopes`
--

LOCK TABLES `apiresourcescopes` WRITE;
/*!40000 ALTER TABLE `apiresourcescopes` DISABLE KEYS */;
INSERT INTO `apiresourcescopes` VALUES (93,'test.scope1',10);
/*!40000 ALTER TABLE `apiresourcescopes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiresourcesecrets`
--

DROP TABLE IF EXISTS `apiresourcesecrets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiresourcesecrets` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Description` varchar(1000) DEFAULT NULL,
  `Value` varchar(4000) NOT NULL,
  `Expiration` datetime(6) DEFAULT NULL,
  `Type` varchar(250) NOT NULL,
  `Created` datetime(6) NOT NULL,
  `ApiResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ApiResourceSecrets_ApiResourceId` (`ApiResourceId`) USING BTREE,
  CONSTRAINT `FK_ApiResourceSecrets_ApiResources_ApiResourceId` FOREIGN KEY (`ApiResourceId`) REFERENCES `apiresources` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiresourcesecrets`
--

LOCK TABLES `apiresourcesecrets` WRITE;
/*!40000 ALTER TABLE `apiresourcesecrets` DISABLE KEYS */;
/*!40000 ALTER TABLE `apiresourcesecrets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiscopeclaims`
--

DROP TABLE IF EXISTS `apiscopeclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiscopeclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(200) NOT NULL,
  `ScopeId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ApiScopeClaims_ScopeId` (`ScopeId`) USING BTREE,
  CONSTRAINT `FK_ApiScopeClaims_ApiScopes_ScopeId` FOREIGN KEY (`ScopeId`) REFERENCES `apiscopes` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiscopeclaims`
--

LOCK TABLES `apiscopeclaims` WRITE;
/*!40000 ALTER TABLE `apiscopeclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `apiscopeclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiscopeproperties`
--

DROP TABLE IF EXISTS `apiscopeproperties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiscopeproperties` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) NOT NULL,
  `Value` varchar(2000) NOT NULL,
  `ScopeId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ApiScopeProperties_ScopeId` (`ScopeId`) USING BTREE,
  CONSTRAINT `FK_ApiScopeProperties_ApiScopes_ScopeId` FOREIGN KEY (`ScopeId`) REFERENCES `apiscopes` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiscopeproperties`
--

LOCK TABLES `apiscopeproperties` WRITE;
/*!40000 ALTER TABLE `apiscopeproperties` DISABLE KEYS */;
/*!40000 ALTER TABLE `apiscopeproperties` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `apiscopes`
--

DROP TABLE IF EXISTS `apiscopes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `apiscopes` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `Name` varchar(200) NOT NULL,
  `DisplayName` varchar(200) DEFAULT NULL,
  `Description` varchar(1000) DEFAULT NULL,
  `Required` tinyint(1) NOT NULL,
  `Emphasize` tinyint(1) NOT NULL,
  `ShowInDiscoveryDocument` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `IX_ApiScopes_Name` (`Name`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `apiscopes`
--

LOCK TABLES `apiscopes` WRITE;
/*!40000 ALTER TABLE `apiscopes` DISABLE KEYS */;
INSERT INTO `apiscopes` VALUES (1,1,'test.scope1','测试scope',NULL,0,0,1);
/*!40000 ALTER TABLE `apiscopes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroleclaims`
--

DROP TABLE IF EXISTS `aspnetroleclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroleclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoleId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_AspNetRoleClaims_RoleId` (`RoleId`) USING BTREE,
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroleclaims`
--

LOCK TABLES `aspnetroleclaims` WRITE;
/*!40000 ALTER TABLE `aspnetroleclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetroleclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetroles`
--

DROP TABLE IF EXISTS `aspnetroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetroles` (
  `Id` varchar(255) NOT NULL,
  `Name` varchar(256) DEFAULT NULL,
  `NormalizedName` varchar(256) DEFAULT NULL,
  `ConcurrencyStamp` longtext,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `RoleNameIndex` (`NormalizedName`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetroles`
--

LOCK TABLES `aspnetroles` WRITE;
/*!40000 ALTER TABLE `aspnetroles` DISABLE KEYS */;
INSERT INTO `aspnetroles` VALUES ('9de8dbae-2628-45c5-b4cb-38aa39820af0','sys.admin','SYS.ADMIN','1e49e36b-1ad5-440a-9b7c-eda9e4b00c33');
/*!40000 ALTER TABLE `aspnetroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserclaims`
--

DROP TABLE IF EXISTS `aspnetuserclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` varchar(255) NOT NULL,
  `ClaimType` longtext,
  `ClaimValue` longtext,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_AspNetUserClaims_UserId` (`UserId`) USING BTREE,
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserclaims`
--

LOCK TABLES `aspnetuserclaims` WRITE;
/*!40000 ALTER TABLE `aspnetuserclaims` DISABLE KEYS */;
INSERT INTO `aspnetuserclaims` VALUES (1,'5ca57d20-947f-41e7-ade1-b9121e8ac5ad','name','Alice Smith'),(2,'5ca57d20-947f-41e7-ade1-b9121e8ac5ad','given_name','Alice'),(3,'5ca57d20-947f-41e7-ade1-b9121e8ac5ad','family_name','Smith'),(4,'5ca57d20-947f-41e7-ade1-b9121e8ac5ad','website','http://alice.com'),(5,'093f69a0-0d5d-4914-b045-755edfdceb3f','website','http://bob.com'),(6,'093f69a0-0d5d-4914-b045-755edfdceb3f','family_name','Smith'),(7,'093f69a0-0d5d-4914-b045-755edfdceb3f','given_name','Bob'),(8,'093f69a0-0d5d-4914-b045-755edfdceb3f','name','Bob Smith'),(9,'093f69a0-0d5d-4914-b045-755edfdceb3f','location','somewhere');
/*!40000 ALTER TABLE `aspnetuserclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserlogins`
--

DROP TABLE IF EXISTS `aspnetuserlogins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserlogins` (
  `LoginProvider` varchar(255) NOT NULL,
  `ProviderKey` varchar(255) NOT NULL,
  `ProviderDisplayName` longtext,
  `UserId` varchar(255) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`) USING BTREE,
  KEY `IX_AspNetUserLogins_UserId` (`UserId`) USING BTREE,
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserlogins`
--

LOCK TABLES `aspnetuserlogins` WRITE;
/*!40000 ALTER TABLE `aspnetuserlogins` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserlogins` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetuserroles`
--

DROP TABLE IF EXISTS `aspnetuserroles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetuserroles` (
  `UserId` varchar(255) NOT NULL,
  `RoleId` varchar(255) NOT NULL,
  PRIMARY KEY (`UserId`,`RoleId`) USING BTREE,
  KEY `IX_AspNetUserRoles_RoleId` (`RoleId`) USING BTREE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `aspnetroles` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetuserroles`
--

LOCK TABLES `aspnetuserroles` WRITE;
/*!40000 ALTER TABLE `aspnetuserroles` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetuserroles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusers`
--

DROP TABLE IF EXISTS `aspnetusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusers` (
  `Id` varchar(255) NOT NULL,
  `UserName` varchar(256) DEFAULT NULL,
  `NormalizedUserName` varchar(256) DEFAULT NULL,
  `Email` varchar(256) DEFAULT NULL,
  `NormalizedEmail` varchar(256) DEFAULT NULL,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `PasswordHash` longtext,
  `SecurityStamp` longtext,
  `ConcurrencyStamp` longtext,
  `PhoneNumber` longtext,
  `PhoneNumberConfirmed` tinyint(1) NOT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `LockoutEnd` datetime(6) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) NOT NULL,
  `AccessFailedCount` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `UserNameIndex` (`NormalizedUserName`) USING BTREE,
  KEY `EmailIndex` (`NormalizedEmail`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusers`
--

LOCK TABLES `aspnetusers` WRITE;
/*!40000 ALTER TABLE `aspnetusers` DISABLE KEYS */;
INSERT INTO `aspnetusers` VALUES ('093f69a0-0d5d-4914-b045-755edfdceb3f','bob','BOB','BobSmith@email.com','BOBSMITH@EMAIL.COM',1,'AQAAAAEAACcQAAAAED7DqQYA7iqfh5eiruCQFD9muMaUjfy7ZV7QqUHduCGnXw9Z1mnHBhEbnzjfjh0PjA==','F2NBWGWFKYLAGNQF3SMB3CWXRBVQ6NDL','0d694b62-b7fe-432c-8890-9c6844e033c7',NULL,0,0,NULL,1,0),('5ca57d20-947f-41e7-ade1-b9121e8ac5ad','alice','ALICE','AliceSmith@email.com','ALICESMITH@EMAIL.COM',1,'AQAAAAEAACcQAAAAEB8KLm3oBMR9o7N5lbJzXsJ50vDie7z57MfUrN0ZDVnCA9i6TPO8L7R/CUybDyJW9Q==','4RLS6BI47G7PVBIYLI77TAZQHH4WTSYC','2790969b-7b8e-4dbe-b9f0-d0f1a035e2a9',NULL,0,0,NULL,1,0);
/*!40000 ALTER TABLE `aspnetusers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `aspnetusertokens`
--

DROP TABLE IF EXISTS `aspnetusertokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `aspnetusertokens` (
  `UserId` varchar(255) NOT NULL,
  `LoginProvider` varchar(255) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`) USING BTREE,
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `aspnetusers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `aspnetusertokens`
--

LOCK TABLES `aspnetusertokens` WRITE;
/*!40000 ALTER TABLE `aspnetusertokens` DISABLE KEYS */;
/*!40000 ALTER TABLE `aspnetusertokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientclaims`
--

DROP TABLE IF EXISTS `clientclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(250) NOT NULL,
  `Value` varchar(250) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientClaims_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientClaims_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientclaims`
--

LOCK TABLES `clientclaims` WRITE;
/*!40000 ALTER TABLE `clientclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `clientclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientcorsorigins`
--

DROP TABLE IF EXISTS `clientcorsorigins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientcorsorigins` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Origin` varchar(150) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientCorsOrigins_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientCorsOrigins_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientcorsorigins`
--

LOCK TABLES `clientcorsorigins` WRITE;
/*!40000 ALTER TABLE `clientcorsorigins` DISABLE KEYS */;
/*!40000 ALTER TABLE `clientcorsorigins` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientgranttypes`
--

DROP TABLE IF EXISTS `clientgranttypes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientgranttypes` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `GrantType` varchar(250) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientGrantTypes_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientGrantTypes_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientgranttypes`
--

LOCK TABLES `clientgranttypes` WRITE;
/*!40000 ALTER TABLE `clientgranttypes` DISABLE KEYS */;
INSERT INTO `clientgranttypes` VALUES (1,'password',1);
/*!40000 ALTER TABLE `clientgranttypes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientidprestrictions`
--

DROP TABLE IF EXISTS `clientidprestrictions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientidprestrictions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Provider` varchar(200) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientIdPRestrictions_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientIdPRestrictions_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientidprestrictions`
--

LOCK TABLES `clientidprestrictions` WRITE;
/*!40000 ALTER TABLE `clientidprestrictions` DISABLE KEYS */;
/*!40000 ALTER TABLE `clientidprestrictions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientpostlogoutredirecturis`
--

DROP TABLE IF EXISTS `clientpostlogoutredirecturis`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientpostlogoutredirecturis` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PostLogoutRedirectUri` varchar(2000) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientPostLogoutRedirectUris_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientPostLogoutRedirectUris_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientpostlogoutredirecturis`
--

LOCK TABLES `clientpostlogoutredirecturis` WRITE;
/*!40000 ALTER TABLE `clientpostlogoutredirecturis` DISABLE KEYS */;
/*!40000 ALTER TABLE `clientpostlogoutredirecturis` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientproperties`
--

DROP TABLE IF EXISTS `clientproperties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientproperties` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) NOT NULL,
  `Value` varchar(2000) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientProperties_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientProperties_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientproperties`
--

LOCK TABLES `clientproperties` WRITE;
/*!40000 ALTER TABLE `clientproperties` DISABLE KEYS */;
/*!40000 ALTER TABLE `clientproperties` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientredirecturis`
--

DROP TABLE IF EXISTS `clientredirecturis`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientredirecturis` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RedirectUri` varchar(2000) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientRedirectUris_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientRedirectUris_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientredirecturis`
--

LOCK TABLES `clientredirecturis` WRITE;
/*!40000 ALTER TABLE `clientredirecturis` DISABLE KEYS */;
/*!40000 ALTER TABLE `clientredirecturis` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clients`
--

DROP TABLE IF EXISTS `clients`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clients` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `ClientId` varchar(200) NOT NULL,
  `ProtocolType` varchar(200) NOT NULL,
  `RequireClientSecret` tinyint(1) NOT NULL,
  `ClientName` varchar(200) DEFAULT NULL,
  `Description` varchar(1000) DEFAULT NULL,
  `ClientUri` varchar(2000) DEFAULT NULL,
  `LogoUri` varchar(2000) DEFAULT NULL,
  `RequireConsent` tinyint(1) NOT NULL,
  `AllowRememberConsent` tinyint(1) NOT NULL,
  `AlwaysIncludeUserClaimsInIdToken` tinyint(1) NOT NULL,
  `RequirePkce` tinyint(1) NOT NULL,
  `AllowPlainTextPkce` tinyint(1) NOT NULL,
  `RequireRequestObject` tinyint(1) NOT NULL,
  `AllowAccessTokensViaBrowser` tinyint(1) NOT NULL,
  `FrontChannelLogoutUri` varchar(2000) DEFAULT NULL,
  `FrontChannelLogoutSessionRequired` tinyint(1) NOT NULL,
  `BackChannelLogoutUri` varchar(2000) DEFAULT NULL,
  `BackChannelLogoutSessionRequired` tinyint(1) NOT NULL,
  `AllowOfflineAccess` tinyint(1) NOT NULL,
  `IdentityTokenLifetime` int(11) NOT NULL,
  `AllowedIdentityTokenSigningAlgorithms` varchar(100) DEFAULT NULL,
  `AccessTokenLifetime` int(11) NOT NULL,
  `AuthorizationCodeLifetime` int(11) NOT NULL,
  `ConsentLifetime` int(11) DEFAULT NULL,
  `AbsoluteRefreshTokenLifetime` int(11) NOT NULL,
  `SlidingRefreshTokenLifetime` int(11) NOT NULL,
  `RefreshTokenUsage` int(11) NOT NULL,
  `UpdateAccessTokenClaimsOnRefresh` tinyint(1) NOT NULL,
  `RefreshTokenExpiration` int(11) NOT NULL,
  `AccessTokenType` int(11) NOT NULL,
  `EnableLocalLogin` tinyint(1) NOT NULL,
  `IncludeJwtId` tinyint(1) NOT NULL,
  `AlwaysSendClientClaims` tinyint(1) NOT NULL,
  `ClientClaimsPrefix` varchar(200) DEFAULT NULL,
  `PairWiseSubjectSalt` varchar(200) DEFAULT NULL,
  `Created` datetime(6) NOT NULL,
  `Updated` datetime(6) DEFAULT NULL,
  `LastAccessed` datetime(6) DEFAULT NULL,
  `UserSsoLifetime` int(11) DEFAULT NULL,
  `UserCodeType` varchar(100) DEFAULT NULL,
  `DeviceCodeLifetime` int(11) NOT NULL,
  `NonEditable` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `IX_Clients_ClientId` (`ClientId`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clients`
--

LOCK TABLES `clients` WRITE;
/*!40000 ALTER TABLE `clients` DISABLE KEYS */;
INSERT INTO `clients` VALUES (1,1,'interactive','oidc',0,'交互客户端',NULL,NULL,NULL,0,1,0,1,0,0,0,NULL,1,NULL,1,1,300,NULL,1296000,300,NULL,2592000,1296000,1,0,1,0,1,1,0,'client_',NULL,'2021-11-16 12:28:50.569734',NULL,NULL,NULL,NULL,300,0);
/*!40000 ALTER TABLE `clients` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientscopes`
--

DROP TABLE IF EXISTS `clientscopes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientscopes` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Scope` varchar(200) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientScopes_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientScopes_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=261 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientscopes`
--

LOCK TABLES `clientscopes` WRITE;
/*!40000 ALTER TABLE `clientscopes` DISABLE KEYS */;
INSERT INTO `clientscopes` VALUES (259,'test.scope1',1),(260,'profile',1);
/*!40000 ALTER TABLE `clientscopes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `clientsecrets`
--

DROP TABLE IF EXISTS `clientsecrets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `clientsecrets` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Description` varchar(2000) DEFAULT NULL,
  `Value` varchar(4000) NOT NULL,
  `Expiration` datetime(6) DEFAULT NULL,
  `Type` varchar(250) NOT NULL,
  `Created` datetime(6) NOT NULL,
  `ClientId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_ClientSecrets_ClientId` (`ClientId`) USING BTREE,
  CONSTRAINT `FK_ClientSecrets_Clients_ClientId` FOREIGN KEY (`ClientId`) REFERENCES `clients` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `clientsecrets`
--

LOCK TABLES `clientsecrets` WRITE;
/*!40000 ALTER TABLE `clientsecrets` DISABLE KEYS */;
/*!40000 ALTER TABLE `clientsecrets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `devicecodes`
--

DROP TABLE IF EXISTS `devicecodes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `devicecodes` (
  `UserCode` varchar(200) NOT NULL,
  `DeviceCode` varchar(200) NOT NULL,
  `SubjectId` varchar(200) DEFAULT NULL,
  `SessionId` varchar(100) DEFAULT NULL,
  `ClientId` varchar(200) NOT NULL,
  `Description` varchar(200) DEFAULT NULL,
  `CreationTime` datetime(6) NOT NULL,
  `Expiration` datetime(6) NOT NULL,
  `Data` longtext NOT NULL,
  PRIMARY KEY (`UserCode`) USING BTREE,
  UNIQUE KEY `IX_DeviceCodes_DeviceCode` (`DeviceCode`) USING BTREE,
  KEY `IX_DeviceCodes_Expiration` (`Expiration`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `devicecodes`
--

LOCK TABLES `devicecodes` WRITE;
/*!40000 ALTER TABLE `devicecodes` DISABLE KEYS */;
/*!40000 ALTER TABLE `devicecodes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `dumpresult`
--

DROP TABLE IF EXISTS `dumpresult`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `dumpresult` (
  `Id` varchar(50) NOT NULL,
  `CreateTime` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `dumpresult`
--

LOCK TABLES `dumpresult` WRITE;
/*!40000 ALTER TABLE `dumpresult` DISABLE KEYS */;
/*!40000 ALTER TABLE `dumpresult` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `identityresourceclaims`
--

DROP TABLE IF EXISTS `identityresourceclaims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `identityresourceclaims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` varchar(200) NOT NULL,
  `IdentityResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_IdentityResourceClaims_IdentityResourceId` (`IdentityResourceId`) USING BTREE,
  CONSTRAINT `FK_IdentityResourceClaims_IdentityResources_IdentityResourceId` FOREIGN KEY (`IdentityResourceId`) REFERENCES `identityresources` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `identityresourceclaims`
--

LOCK TABLES `identityresourceclaims` WRITE;
/*!40000 ALTER TABLE `identityresourceclaims` DISABLE KEYS */;
/*!40000 ALTER TABLE `identityresourceclaims` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `identityresourceproperties`
--

DROP TABLE IF EXISTS `identityresourceproperties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `identityresourceproperties` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(250) NOT NULL,
  `Value` varchar(2000) NOT NULL,
  `IdentityResourceId` int(11) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `IX_IdentityResourceProperties_IdentityResourceId` (`IdentityResourceId`) USING BTREE,
  CONSTRAINT `FK_IdentityResourceProperties_IdentityResources_IdentityResourc~` FOREIGN KEY (`IdentityResourceId`) REFERENCES `identityresources` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `identityresourceproperties`
--

LOCK TABLES `identityresourceproperties` WRITE;
/*!40000 ALTER TABLE `identityresourceproperties` DISABLE KEYS */;
/*!40000 ALTER TABLE `identityresourceproperties` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `identityresources`
--

DROP TABLE IF EXISTS `identityresources`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `identityresources` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Enabled` tinyint(1) NOT NULL,
  `Name` varchar(200) NOT NULL,
  `DisplayName` varchar(200) DEFAULT NULL,
  `Description` varchar(1000) DEFAULT NULL,
  `Required` tinyint(1) NOT NULL,
  `Emphasize` tinyint(1) NOT NULL,
  `ShowInDiscoveryDocument` tinyint(1) NOT NULL,
  `Created` datetime(6) NOT NULL,
  `Updated` datetime(6) DEFAULT NULL,
  `NonEditable` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `IX_IdentityResources_Name` (`Name`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `identityresources`
--

LOCK TABLES `identityresources` WRITE;
/*!40000 ALTER TABLE `identityresources` DISABLE KEYS */;
/*!40000 ALTER TABLE `identityresources` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `persistedgrants`
--

DROP TABLE IF EXISTS `persistedgrants`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `persistedgrants` (
  `Key` varchar(200) NOT NULL,
  `Type` varchar(50) NOT NULL,
  `SubjectId` varchar(200) DEFAULT NULL,
  `SessionId` varchar(100) DEFAULT NULL,
  `ClientId` varchar(200) NOT NULL,
  `Description` varchar(200) DEFAULT NULL,
  `CreationTime` datetime(6) NOT NULL,
  `Expiration` datetime(6) DEFAULT NULL,
  `ConsumedTime` datetime(6) DEFAULT NULL,
  `Data` longtext NOT NULL,
  PRIMARY KEY (`Key`) USING BTREE,
  KEY `IX_PersistedGrants_Expiration` (`Expiration`) USING BTREE,
  KEY `IX_PersistedGrants_SubjectId_ClientId_Type` (`SubjectId`,`ClientId`,`Type`) USING BTREE,
  KEY `IX_PersistedGrants_SubjectId_SessionId_Type` (`SubjectId`,`SessionId`,`Type`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `persistedgrants`
--

LOCK TABLES `persistedgrants` WRITE;
/*!40000 ALTER TABLE `persistedgrants` DISABLE KEYS */;
/*!40000 ALTER TABLE `persistedgrants` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-07-04 14:46:25
