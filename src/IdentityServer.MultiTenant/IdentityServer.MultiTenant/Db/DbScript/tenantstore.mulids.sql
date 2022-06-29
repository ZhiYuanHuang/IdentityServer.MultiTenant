/*
 Navicat Premium Data Transfer

 Source Server         : wsl_mysql
 Source Server Type    : MySQL
 Source Server Version : 50734
 Source Host           : localhost:3307
 Source Schema         : tenantstore.mulids

 Target Server Type    : MySQL
 Target Server Version : 50734
 File Encoding         : 65001

 Date: 29/06/2022 17:37:37
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for dbserver
-- ----------------------------
DROP TABLE IF EXISTS `dbserver`;
CREATE TABLE `dbserver`  (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ServerHost` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ServerPort` int(11) NOT NULL,
  `UserName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Userpwd` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `CreatedDbCount` int(11) NULL DEFAULT NULL,
  `EnableStatus` int(11) NULL DEFAULT NULL,
  `EncryptUserpwd` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `idx_DbServer_ServerHost_serverport`(`ServerHost`, `ServerPort`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 9 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of dbserver
-- ----------------------------
INSERT INTO `dbserver` VALUES (5, '127.0.0.1', 3307, 'root', NULL, 2, 1, 'B2kkiVL6Denq52E34s1UTQ==');
INSERT INTO `dbserver` VALUES (7, '129.226.101.69', 13306, 'root', NULL, 0, 0, 'AkYIrVk6SXVUMu3Gcg8lCA==');
INSERT INTO `dbserver` VALUES (8, '172.16.0.118', 13360, 'root', NULL, -1, 1, 'B2kkiVL6Denq52E34s1UTQ==');

-- ----------------------------
-- Table structure for tenantdbserverref
-- ----------------------------
DROP TABLE IF EXISTS `tenantdbserverref`;
CREATE TABLE `tenantdbserverref`  (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TenantId` bigint(20) NULL DEFAULT NULL,
  `DbServerId` bigint(20) NULL DEFAULT NULL,
  `OldDbServerId` bigint(20) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `idx_TenantDbServerRef_tenantid`(`TenantId`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 79 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of tenantdbserverref
-- ----------------------------
INSERT INTO `tenantdbserverref` VALUES (31, 62, 5, 8);
INSERT INTO `tenantdbserverref` VALUES (73, 69, 5, 8);

-- ----------------------------
-- Table structure for tenantdomain
-- ----------------------------
DROP TABLE IF EXISTS `tenantdomain`;
CREATE TABLE `tenantdomain`  (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `TenantDomain` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `CreateTime` datetime NULL DEFAULT NULL,
  `EnableStatus` int(11) NULL DEFAULT NULL,
  `UpdateTime` datetime NULL DEFAULT NULL,
  `ParentDomainId` bigint(20) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `idx_tenantdomain_tenantdomain`(`TenantDomain`) USING BTREE,
  INDEX `idx_tenantdomain_ParentDomainId`(`ParentDomainId`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 4 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of tenantdomain
-- ----------------------------
INSERT INTO `tenantdomain` VALUES (3, 'idsmul.com', NULL, NULL, 1, '2022-06-21 11:47:09', NULL);

-- ----------------------------
-- Table structure for tenantinfo
-- ----------------------------
DROP TABLE IF EXISTS `tenantinfo`;
CREATE TABLE `tenantinfo`  (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `GuidId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Identifier` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `TenantDomainId` bigint(20) NOT NULL,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `ConnectionString` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `EncryptedIdsConnectionString` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `CreateTime` datetime NULL DEFAULT NULL,
  `Description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `EnableStatus` int(11) NULL DEFAULT NULL,
  `UpdateTime` datetime NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `idx_tenantinfo_Identifier_TenantDomainId`(`Identifier`, `TenantDomainId`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 70 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = DYNAMIC;

-- ----------------------------
-- Records of tenantinfo
-- ----------------------------
INSERT INTO `tenantinfo` VALUES (62, '84e341d49ff1445e846ca7f8f240f426', 'test1', 3, 'test1', NULL, 'E5Zh5P/L48F9sEIj8EXdkF62KfiZLj7FyEJvZqa02VUY13OBrCN98GucwGd17/Vkjfec7WRCTo47hT3WE40qrLumWJzyrhnS4sOmrK9HbmhjHJJnozrOZLfvXriUKi04SrqBJyf7/I+zGImQn/6nIA==', '2022-06-27 16:19:26', 'ddddd', 1, '2022-06-29 11:17:55');
INSERT INTO `tenantinfo` VALUES (69, 'bddc8c7767ab412ab7b30e068223f1d1', 'test2', 3, 'test2', NULL, 'E5Zh5P/L48F9sEIj8EXdkEiLhoAyqc/tcurVbXyQYMwY13OBrCN98GucwGd17/Vkjfec7WRCTo47hT3WE40qrLumWJzyrhnS4sOmrK9HbmhjHJJnozrOZLfvXriUKi04SrqBJyf7/I+zGImQn/6nIA==', '2022-06-29 11:12:42', 'ssss', 1, '2022-06-29 13:37:36');

SET FOREIGN_KEY_CHECKS = 1;
