-- Additive upgrade only. Back up and review DATABASE_UPGRADE.md first.
-- Preconditions checked by GuestCheckoutSchema: existing InnoDB Orders, signed int Id,
-- and no UserId foreign key. Do not rename existing tables or reset the database.
CREATE TABLE `GuestCheckouts` (
  `OrderId` int NOT NULL,
  `Reference` varchar(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `CheckoutKeyHash` char(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `RequestHash` char(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `ContactJson` longtext CHARACTER SET utf8mb4 NOT NULL,
  PRIMARY KEY (`OrderId`),
  UNIQUE KEY `IX_GuestCheckouts_Reference` (`Reference`),
  UNIQUE KEY `IX_GuestCheckouts_CheckoutKeyHash` (`CheckoutKeyHash`),
  CONSTRAINT `FK_GuestCheckouts_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
