-- Additive upgrade only. Back up the existing database before applying.
-- Orders, Products, Identity users, guest checkouts and carts are unchanged.
CREATE TABLE IF NOT EXISTS `CustomerProfiles` (
 `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
 `FullName` varchar(120) NOT NULL DEFAULT '', `DisplayName` varchar(60) NOT NULL DEFAULT '',
 `Phone` varchar(32) NOT NULL DEFAULT '', `PhoneRegion` varchar(2) NOT NULL DEFAULT 'PK',
 `Address` varchar(240) NOT NULL DEFAULT '', `City` varchar(100) NOT NULL DEFAULT '',
 `Region` varchar(100) NOT NULL DEFAULT '', `PostalCode` varchar(20) NOT NULL DEFAULT '',
 `Country` varchar(2) NOT NULL DEFAULT 'PK', `Avatar` longblob NULL, `PendingEmail` varchar(254) NULL,
 PRIMARY KEY (`UserId`), CONSTRAINT `FK_CustomerProfiles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
