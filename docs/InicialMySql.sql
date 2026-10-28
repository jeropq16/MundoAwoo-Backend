CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Categorias` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Nombre` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Tipo` int NOT NULL,
    CONSTRAINT `PK_Categorias` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Usuarios` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `FechaRegistro` datetime(6) NOT NULL,
    CONSTRAINT `PK_Usuarios` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `ItemsDeAvatar` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `Nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CategoriaId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Ranura` int NOT NULL,
    `RecursoVisual` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_ItemsDeAvatar` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ItemsDeAvatar_Categorias_CategoriaId` FOREIGN KEY (`CategoriaId`) REFERENCES `Categorias` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Avatares` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UsuarioId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ColorFondo` char(7) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Avatares` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Avatares_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PerfilesUsuario` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UsuarioId` char(36) COLLATE ascii_general_ci NOT NULL,
    `NombreVisible` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Biografia` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_PerfilesUsuario` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PerfilesUsuario_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ItemsDesbloqueados` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `UsuarioId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ItemDeAvatarId` char(36) COLLATE ascii_general_ci NOT NULL,
    `FechaDesbloqueo` datetime(6) NOT NULL,
    CONSTRAINT `PK_ItemsDesbloqueados` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ItemsDesbloqueados_ItemsDeAvatar_ItemDeAvatarId` FOREIGN KEY (`ItemDeAvatarId`) REFERENCES `ItemsDeAvatar` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ItemsDesbloqueados_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ItemsEquipados` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `AvatarId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ItemDeAvatarId` char(36) COLLATE ascii_general_ci NOT NULL,
    `Ranura` int NOT NULL,
    CONSTRAINT `PK_ItemsEquipados` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ItemsEquipados_Avatares_AvatarId` FOREIGN KEY (`AvatarId`) REFERENCES `Avatares` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ItemsEquipados_ItemsDeAvatar_ItemDeAvatarId` FOREIGN KEY (`ItemDeAvatarId`) REFERENCES `ItemsDeAvatar` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Avatares_UsuarioId` ON `Avatares` (`UsuarioId`);

CREATE INDEX `IX_ItemsDeAvatar_CategoriaId` ON `ItemsDeAvatar` (`CategoriaId`);

CREATE INDEX `IX_ItemsDesbloqueados_ItemDeAvatarId` ON `ItemsDesbloqueados` (`ItemDeAvatarId`);

CREATE UNIQUE INDEX `IX_ItemsDesbloqueados_UsuarioId_ItemDeAvatarId` ON `ItemsDesbloqueados` (`UsuarioId`, `ItemDeAvatarId`);

CREATE UNIQUE INDEX `IX_ItemsEquipados_AvatarId_ItemDeAvatarId` ON `ItemsEquipados` (`AvatarId`, `ItemDeAvatarId`);

CREATE UNIQUE INDEX `IX_ItemsEquipados_AvatarId_Ranura` ON `ItemsEquipados` (`AvatarId`, `Ranura`);

CREATE INDEX `IX_ItemsEquipados_ItemDeAvatarId` ON `ItemsEquipados` (`ItemDeAvatarId`);

CREATE UNIQUE INDEX `IX_PerfilesUsuario_UsuarioId` ON `PerfilesUsuario` (`UsuarioId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261027214820_InicialMySql', '9.0.16');

COMMIT;
