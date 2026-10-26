-- Explicit additive migration for the existing EnsureCreated database.
-- Apply only after a private full backup. No existing tables/rows are modified.
CREATE TABLE IF NOT EXISTS `ContactInquiries` (
 `Id` int NOT NULL AUTO_INCREMENT,
 `Reference` varchar(35) NOT NULL,
 `SubmissionKeyHash` varchar(64) NOT NULL,
 `RequestHash` varchar(64) NOT NULL,
 `FullName` varchar(120) NOT NULL,
 `Email` varchar(254) NOT NULL,
 `Phone` varchar(32) NULL,
 `Category` varchar(32) NOT NULL,
 `OrderReference` varchar(64) NULL,
 `Message` varchar(4000) NOT NULL,
 `UserId` varchar(255) NULL,
 `CreatedUtc` datetime(6) NOT NULL,
 PRIMARY KEY (`Id`),
 UNIQUE KEY `IX_ContactInquiries_Reference` (`Reference`),
 UNIQUE KEY `IX_ContactInquiries_SubmissionKeyHash` (`SubmissionKeyHash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
