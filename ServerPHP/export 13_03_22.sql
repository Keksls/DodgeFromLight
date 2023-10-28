-- phpMyAdmin SQL Dump
-- version 4.9.10
-- https://www.phpmyadmin.net/
--
-- Hôte : db5006237699.hosting-data.io
-- Généré le : Dim 13 mars 2022 à 20:44
-- Version du serveur : 5.7.36-log
-- Version de PHP : 7.0.33-0+deb9u12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de données : `dbs5212226`
--

DELIMITER $$
--
-- Procédures
--
CREATE DEFINER=`o5212226`@`%` PROCEDURE `DeleteAccount` (IN `AccountID` INT(11))  NO SQL DELETE FROM Accounts WHERE ID = AccountID$$

CREATE DEFINER=`o5212226`@`%` PROCEDURE `DeleteAccountScores` (IN `AccountID` INT(11))  NO SQL DELETE FROM Scores WHERE UserID = AccountID$$

CREATE DEFINER=`o5212226`@`%` PROCEDURE `DeleteAccountVotes` (IN `AccountID` INT(11))  NO SQL DELETE FROM Votes WHERE UserID = AccountID$$

CREATE DEFINER=`o5212226`@`%` PROCEDURE `GetDayMapStar` ()  NO SQL INSERT INTO DayStarMap (`MapID`, `Date`)
SELECT ID, CURRENT_DATE() FROM `Maps` WHERE State = 0 ORDER BY Likes DESC, NbDownloads DESC, Dislikes ASC, RAND() DESC LIMIT 3$$

CREATE DEFINER=`o5212226`@`%` PROCEDURE `UpdateBadMaps` ()  NO SQL UPDATE `Maps` SET `State`=1 WHERE  NbVotes >= 30 AND Dislikes / Likes >= 0.33 AND State = 0 AND ID NOT IN (SELECT MapID FROM DayStarMap)$$

DELIMITER ;

-- --------------------------------------------------------

--
-- Structure de la table `DayStarMap`
--

CREATE TABLE `DayStarMap` (
  `ID` int(11) NOT NULL,
  `MapID` varchar(32) NOT NULL,
  `Date` date NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `DayStarMap`
--

INSERT INTO `DayStarMap` (`ID`, `MapID`, `Date`) VALUES
(59, '8dffa0fb56fa4e8fb523697630c616db', '2022-03-12'),
(60, '5d0a50a04d4f41b9a670a91cd816af74', '2022-03-12'),
(61, '2d178aa8717643aaa056d87bbb8150e0', '2022-03-12'),
(62, '1c32ac64668149288b067327eafbe5e3', '2022-03-12'),
(63, '6bb89b6ae9514d92bf2c79ac0ffb3bcc', '2022-03-12'),
(64, 'e3cdace510e441019ba9057fcc0fccc8', '2022-03-12'),
(65, 'dcc5b97989434c12ba94d164a3a01b7d', '2022-03-12'),
(66, 'db9d19b1ea5f46269a58fa255a5f56e8', '2022-03-12'),
(67, 'cc0d92f32c084c0d8f027aa5205b66e9', '2022-03-12'),
(68, '836645494ee64b519e3469f5dcdbe698', '2022-03-12'),
(69, 'f30bd59c4e5e468d9ed4aa7e61cf30cd', '2022-03-12'),
(70, '4d96e16b025143a48cc7932f66a04750', '2022-03-12'),
(71, '40abe1b2f8e8487d8a3aebe794a15807', '2022-03-13'),
(72, '79a7e71d5c47425c8e8543ddd2f63e01', '2022-03-13'),
(73, 'c3fc8db362ed4cdf8fb0a73982a59aa6', '2022-03-13');

-- --------------------------------------------------------

--
-- Structure de la table `MapCode`
--

CREATE TABLE `MapCode` (
  `CODE` varchar(6) NOT NULL,
  `MapID` varchar(32) NOT NULL,
  `WorkingUser` int(11) NOT NULL DEFAULT '-1',
  `LastWorkingPing` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `MapCode`
--

INSERT INTO `MapCode` (`CODE`, `MapID`, `WorkingUser`, `LastWorkingPing`) VALUES
('0271AF', '4c6a13c5317846f6bae85800eb2cf89c', -1, '2022-02-20 12:40:49'),
('0742A1', '4ebeb14f924e439a8347f06e3335b08c', -1, '0000-00-00 00:00:00'),
('165A3D', 'de389d1d1c084df0b845c7e1741246a3', -1, '2022-02-20 12:44:18'),
('19288A', 'a2dcf5f37c7a41ac97f4eb1805f9c2e7', -1, '0000-00-00 00:00:00'),
('2360E7', '443cbeee1a2344409a05204dd669bf83', -1, '2022-02-22 20:53:55'),
('26231D', 'dee411b5281d45fc9d8aca23f75fa28b', -1, '2022-02-20 12:44:40'),
('265EB7', '8749424e6be64c769d0c9fe333d1d8f1', -1, '2022-02-20 12:39:13'),
('27AA22', 'fde40f9f87df48fd95b6de7c1c773c9b', -1, '2022-02-22 21:00:33'),
('36456A', 'b9a317266a4d46cba94d92fe83eedbbb', -1, '2022-02-20 12:42:28'),
('39604F', '77dcadd50c844db98500fa40a0e32abe', -1, '2022-02-20 12:42:13'),
('48C5BC', 'c26e2633b5ff4078a4b3865742054b67', -1, '2022-02-22 21:05:37'),
('48EFC5', '28c7f5c3cd9f4199b125856d6e35f2d7', 1, '2022-02-20 12:45:10'),
('4A46CA', '6c7f6a5dcf9a4800a358737dd8d29e4c', -1, '2022-02-20 12:42:43'),
('4F6682', '66c947e4043a42eaa293219b6689d896', -1, '2022-02-20 12:39:21'),
('50727C', '0fa163ebab3c49fc8bbeddb97c815b56', -1, '2022-02-22 21:05:04'),
('55764A', 'd2f63173160f47069f7e6f63101344c6', 1, '2022-02-20 12:46:08'),
('5D73F2', '4fb0422412674fde93a60709a8c6d5ca', 1, '2022-02-20 12:45:59'),
('80E276', '432b32cd19c74ff5a215bd75005af3bf', -1, '2022-02-20 12:23:10'),
('874862', '672f1a7fb1b140a2aeac24c4adf841c1', -1, '2022-02-20 12:43:23'),
('888E82', 'cd8826727f924c459d2b1a3cf4cad7c6', 1, '2022-02-20 12:41:30'),
('8D85CA', '879af8a6d6544bc0b88544680d84852b', -1, '0000-00-00 00:00:00'),
('9347F0', 'c3fc8db362ed4cdf8fb0a73982a59aa6', 9, '2022-01-30 23:28:30'),
('A6CB56', 'e792dc241e67486c958a08f8b63137e6', -1, '2022-02-20 12:40:04'),
('A6E488', 'bb08b672165f4c3f8d50c32853cd4650', -1, '2022-02-20 12:39:55'),
('B514C0', 'b7f30362c0d8457dbc241f99f4f3d022', 1, '2022-02-20 12:47:12'),
('C82212', '4e4195111f114f2b9942c854df7e5e52', -1, '0000-00-00 00:00:00'),
('D2EA3F', '6837ba56420543b583819c1e41387da7', -1, '2022-02-20 12:41:20'),
('DF8FEF', 'a2401d49f35d400e9d83fa6c0bcf7012', -1, '2022-02-22 14:33:15'),
('E0008D', 'f30bd59c4e5e468d9ed4aa7e61cf30cd', -1, '0000-00-00 00:00:00'),
('F40C9C', '7c7b9f6048c342378f4bc52a51bf26f3', 1, '2022-02-20 12:44:32');

-- --------------------------------------------------------

--
-- Structure de la table `Maps`
--

CREATE TABLE `Maps` (
  `ID` varchar(32) NOT NULL,
  `Name` varchar(64) NOT NULL,
  `Author` int(11) NOT NULL,
  `UploadDate` date NOT NULL,
  `Width` int(3) NOT NULL,
  `Height` int(3) NOT NULL,
  `Likes` int(11) NOT NULL,
  `Dislikes` int(11) NOT NULL,
  `State` tinyint(1) NOT NULL,
  `NbDownloads` int(11) NOT NULL,
  `NbVotes` int(11) NOT NULL,
  `AlreadyFinished` tinyint(1) NOT NULL DEFAULT '0',
  `AuthorName` varchar(128) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `Maps`
--

INSERT INTO `Maps` (`ID`, `Name`, `Author`, `UploadDate`, `Width`, `Height`, `Likes`, `Dislikes`, `State`, `NbDownloads`, `NbVotes`, `AlreadyFinished`, `AuthorName`) VALUES
('0fa163ebab3c49fc8bbeddb97c815b56', 'Level 18', 9, '2022-01-30', 5, 6, 0, 0, 1, 10, 0, 1, 'sensi'),
('1c32ac64668149288b067327eafbe5e3', 'Lets sprint guys', 9, '2022-01-27', 15, 15, 8, 0, 0, 24, 4, 1, 'sensi'),
('28c7f5c3cd9f4199b125856d6e35f2d7', 'Level 17', 9, '2022-01-30', 5, 6, 0, 0, 1, 3, 0, 1, 'sensi'),
('29359345d77a428490cb8a550a10d1a5', 'Clyclops', 1, '2022-01-24', 0, 0, 3, 0, 0, 13, 3, 1, 'Keks'),
('2d178aa8717643aaa056d87bbb8150e0', 'GluTricks', 1, '2022-01-25', 0, 0, 9, 0, 0, 21, 5, 1, 'Keks'),
('40abe1b2f8e8487d8a3aebe794a15807', 'LightPath', 1, '2022-01-25', 0, 0, 4, 0, 0, 9, 4, 1, 'Keks'),
('432b32cd19c74ff5a215bd75005af3bf', 'test', 1, '2022-02-20', 8, 8, 0, 1, 0, 12, 1, 1, 'Keks'),
('443cbeee1a2344409a05204dd669bf83', 'Level 19', 9, '2022-01-30', 5, 6, 0, 0, 1, 4, 0, 1, 'sensi'),
('4c6a13c5317846f6bae85800eb2cf89c', 'Level 12', 9, '2022-01-30', 5, 6, 0, 0, 1, 3, 0, 1, 'sensi'),
('4d96e16b025143a48cc7932f66a04750', 'Epyleptik', 1, '2022-01-25', 0, 0, 4, 1, 0, 11, 5, 1, 'Keks'),
('4e4195111f114f2b9942c854df7e5e52', 'Level 26', 1, '2022-02-25', 6, 8, 0, 0, 1, 2, 0, 1, 'Keks'),
('4ebeb14f924e439a8347f06e3335b08c', 'Level 24', 1, '2022-02-25', 6, 8, 0, 0, 1, 1, 0, 1, 'Keks'),
('4fb0422412674fde93a60709a8c6d5ca', 'Level 20', 9, '2022-01-30', 5, 6, 0, 0, 1, 2, 0, 1, 'sensi'),
('5d0a50a04d4f41b9a670a91cd816af74', 'Welcome', 9, '2022-01-26', 5, 15, 10, 0, 0, 18, 6, 1, 'sensi'),
('66c947e4043a42eaa293219b6689d896', 'Level 9', 9, '2022-01-29', 5, 6, 0, 0, 1, 19, 0, 1, 'sensi'),
('672f1a7fb1b140a2aeac24c4adf841c1', 'Level 4', 9, '2022-01-30', 5, 6, 0, 0, 1, 3, 0, 1, 'sensi'),
('6837ba56420543b583819c1e41387da7', 'Level 13', 9, '2022-01-30', 5, 6, 0, 0, 1, 3, 0, 1, 'sensi'),
('6bb89b6ae9514d92bf2c79ac0ffb3bcc', 'Grappling', 1, '2022-01-25', 0, 0, 7, 0, 0, 12, 3, 1, 'Keks'),
('6c7f6a5dcf9a4800a358737dd8d29e4c', 'Level 6', 9, '2022-01-30', 5, 6, 0, 0, 1, 2, 0, 1, 'sensi'),
('77dcadd50c844db98500fa40a0e32abe', 'Level 15', 9, '2022-01-30', 5, 6, 0, 0, 1, 2, 0, 1, 'sensi'),
('79a7e71d5c47425c8e8543ddd2f63e01', 'SymeTrick', 1, '2022-01-25', 0, 0, 4, 0, 0, 8, 4, 1, 'Keks'),
('7c7b9f6048c342378f4bc52a51bf26f3', 'Level 1', 9, '2022-01-30', 5, 6, 0, 0, 1, 14, 0, 1, 'sensi'),
('836645494ee64b519e3469f5dcdbe698', 'Gluttons', 1, '2022-01-25', 0, 0, 4, 0, 0, 16, 4, 1, 'Keks'),
('8749424e6be64c769d0c9fe333d1d8f1', 'Level 8', 9, '2022-01-30', 5, 6, 0, 0, 1, 3, 0, 1, 'sensi'),
('879af8a6d6544bc0b88544680d84852b', 'Level 23', 1, '2022-02-25', 6, 8, 0, 0, 1, 2, 0, 1, 'Keks'),
('8dffa0fb56fa4e8fb523697630c616db', 'Easy1', 1, '2022-01-25', 0, 0, 14, 0, 0, 29, 6, 1, 'Keks'),
('921a92360f7c46b9a19dd88242d54021', 'Spiral', 1, '2022-01-25', 0, 0, 3, 0, 0, 11, 3, 1, 'Keks'),
('a2401d49f35d400e9d83fa6c0bcf7012', 'Level 5', 9, '2022-01-30', 5, 6, 0, 0, 1, 4, 0, 1, 'sensi'),
('a2dcf5f37c7a41ac97f4eb1805f9c2e7', 'Level 25 ', 1, '2022-02-25', 6, 8, 0, 0, 1, 3, 0, 1, 'Keks'),
('b7f30362c0d8457dbc241f99f4f3d022', 'Level 22', 9, '2022-01-30', 5, 6, 0, 0, 1, 4, 0, 1, 'sensi'),
('b9a317266a4d46cba94d92fe83eedbbb', 'Level 7', 9, '2022-01-30', 5, 6, 0, 0, 1, 6, 0, 1, 'sensi'),
('bb08b672165f4c3f8d50c32853cd4650', 'Level 10', 9, '2022-01-30', 5, 6, 0, 0, 1, 2, 0, 1, 'sensi'),
('c26e2633b5ff4078a4b3865742054b67', 'be patient', 9, '2022-01-28', 64, 64, 0, 0, 1, 155, 0, 1, 'sensi'),
('c3fc8db362ed4cdf8fb0a73982a59aa6', 'Grand canyon', 9, '2022-01-29', 10, 50, 3, 0, 0, 34, 3, 1, 'sensi'),
('c83a75240d9042979d23dd8a26b3a358', 'are u lucky', 1, '2022-01-27', 19, 19, 3, 0, 0, 12, 3, 1, 'Keks'),
('cc0d92f32c084c0d8f027aa5205b66e9', 'Dimseption', 1, '2022-01-25', 0, 0, 5, 0, 0, 16, 5, 1, 'Keks'),
('cd8826727f924c459d2b1a3cf4cad7c6', 'Level 14', 9, '2022-01-30', 5, 6, 0, 0, 1, 6, 0, 1, 'sensi'),
('ce99d6d3a3364f95b589bd05b2a6ce60', 'Labyrinthe', 9, '2022-01-27', 39, 39, 3, 0, 0, 10, 3, 1, 'sensi'),
('d2f63173160f47069f7e6f63101344c6', 'Level 21', 9, '2022-01-30', 5, 6, 0, 0, 1, 4, 0, 1, 'sensi'),
('d74e66d8e6fa4e77a8bb1777aaf32701', 'MapTooBad', 38, '2022-02-18', 2, 2, 0, 0, 1, 0, 0, 1, ''),
('db9d19b1ea5f46269a58fa255a5f56e8', 'Watch your steps', 1, '2022-02-20', 10, 20, 6, 0, 0, 4, 2, 1, 'Keks'),
('dcc5b97989434c12ba94d164a3a01b7d', 'NightWatch', 1, '2022-02-17', 16, 24, 6, 0, 0, 7, 2, 1, 'Keks'),
('de389d1d1c084df0b845c7e1741246a3', 'Level 2', 9, '2022-01-30', 5, 6, 0, 0, 1, 6, 0, 1, 'sensi'),
('dee411b5281d45fc9d8aca23f75fa28b', 'Level 3', 9, '2022-01-30', 5, 6, 0, 0, 1, 6, 0, 1, 'sensi'),
('e3cdace510e441019ba9057fcc0fccc8', '420', 1, '2022-01-27', 4, 20, 6, 0, 0, 18, 6, 1, 'Keks'),
('e792dc241e67486c958a08f8b63137e6', 'Level 11', 9, '2022-01-30', 5, 6, 0, 0, 1, 3, 0, 1, 'sensi'),
('f30bd59c4e5e468d9ed4aa7e61cf30cd', 'BombedIsland', 1, '2022-01-25', 0, 0, 4, 0, 0, 10, 4, 1, 'Keks'),
('fde40f9f87df48fd95b6de7c1c773c9b', 'Level 16', 9, '2022-01-30', 5, 6, 0, 0, 1, 3, 0, 1, 'sensi');

-- --------------------------------------------------------

--
-- Structure de la table `MonthStarMap`
--

CREATE TABLE `MonthStarMap` (
  `ID` int(11) NOT NULL,
  `MapID` varchar(32) NOT NULL,
  `Month` int(11) NOT NULL,
  `Year` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `MonthStarMap`
--

INSERT INTO `MonthStarMap` (`ID`, `MapID`, `Month`, `Year`) VALUES
(59, '8dffa0fb56fa4e8fb523697630c616db', 3, 2022),
(60, '5d0a50a04d4f41b9a670a91cd816af74', 3, 2022),
(61, '2d178aa8717643aaa056d87bbb8150e0', 3, 2022),
(62, '1c32ac64668149288b067327eafbe5e3', 3, 2022),
(63, '6bb89b6ae9514d92bf2c79ac0ffb3bcc', 3, 2022),
(64, 'e3cdace510e441019ba9057fcc0fccc8', 3, 2022),
(65, 'dcc5b97989434c12ba94d164a3a01b7d', 3, 2022),
(66, 'db9d19b1ea5f46269a58fa255a5f56e8', 3, 2022),
(67, 'cc0d92f32c084c0d8f027aa5205b66e9', 3, 2022),
(68, '836645494ee64b519e3469f5dcdbe698', 3, 2022);

-- --------------------------------------------------------

--
-- Structure de la table `Scores`
--

CREATE TABLE `Scores` (
  `ID` int(11) NOT NULL,
  `MapID` varchar(32) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Time` bigint(32) NOT NULL,
  `Turns` int(11) NOT NULL,
  `Date` date NOT NULL,
  `UserName` varchar(128) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `Scores`
--

INSERT INTO `Scores` (`ID`, `MapID`, `UserID`, `Time`, `Turns`, `Date`, `UserName`) VALUES
(18, '79a7e71d5c47425c8e8543ddd2f63e01', 9, 10775, 14, '2022-01-30', 'sensi'),
(19, '586cfd0add374eb883ffb730ef4bd1c2', 9, 8539, 18, '2022-01-26', 'sensi'),
(20, '6bb89b6ae9514d92bf2c79ac0ffb3bcc', 9, 13408, 24, '2022-01-27', 'sensi'),
(21, '8dffa0fb56fa4e8fb523697630c616db', 9, 14429, 22, '2022-01-27', 'sensi'),
(22, 'f30bd59c4e5e468d9ed4aa7e61cf30cd', 9, 13730, 39, '2022-01-27', 'sensi'),
(23, '4d96e16b025143a48cc7932f66a04750', 9, 1476, 1, '2022-01-27', 'sensi'),
(24, 'ab9b412f2af04b6d86a5c9ade67ff74b', 9, 4408, 10, '2022-01-27', 'sensi'),
(25, '836645494ee64b519e3469f5dcdbe698', 9, 4631, 8, '2022-01-27', 'sensi'),
(26, '2d178aa8717643aaa056d87bbb8150e0', 9, 7434, 28, '2022-01-27', 'sensi'),
(27, 'cc0d92f32c084c0d8f027aa5205b66e9', 9, 17786, 31, '2022-01-27', 'sensi'),
(28, '40abe1b2f8e8487d8a3aebe794a15807', 9, 4745, 16, '2022-01-30', 'sensi'),
(29, '6bb89b6ae9514d92bf2c79ac0ffb3bcc', 4, 72733, 62, '2022-01-26', 'Exocrow'),
(31, '8dffa0fb56fa4e8fb523697630c616db', 4, 34507, 78, '2022-01-26', 'Exocrow'),
(32, '836645494ee64b519e3469f5dcdbe698', 4, 3512, 9, '2022-01-26', 'Exocrow'),
(33, '586cfd0add374eb883ffb730ef4bd1c2', 4, 7342, 13, '2022-01-26', 'Exocrow'),
(34, '921a92360f7c46b9a19dd88242d54021', 9, 372318, 393, '2022-01-26', 'sensi'),
(35, '40abe1b2f8e8487d8a3aebe794a15807', 4, 5574, 16, '2022-01-26', 'Exocrow'),
(36, 'cc0d92f32c084c0d8f027aa5205b66e9', 4, 42032, 78, '2022-01-26', 'Exocrow'),
(37, '921a92360f7c46b9a19dd88242d54021', 4, 838701, 1008, '2022-01-26', 'Exocrow'),
(38, '2d178aa8717643aaa056d87bbb8150e0', 4, 12338, 31, '2022-01-26', 'Exocrow'),
(40, '79a7e71d5c47425c8e8543ddd2f63e01', 4, 85250, 123, '2022-01-26', 'Exocrow'),
(41, 'f30bd59c4e5e468d9ed4aa7e61cf30cd', 4, 15980, 40, '2022-01-26', 'Exocrow'),
(42, '5d0a50a04d4f41b9a670a91cd816af74', 9, 50791, 22, '2022-01-27', 'sensi'),
(43, '5d0a50a04d4f41b9a670a91cd816af74', 4, 31908, 23, '2022-01-26', 'Exocrow'),
(47, '29359345d77a428490cb8a550a10d1a5', 9, 8736, 13, '2022-01-27', 'sensi'),
(48, '1c32ac64668149288b067327eafbe5e3', 9, 8571, 22, '2022-01-27', 'sensi'),
(50, 'e3cdace510e441019ba9057fcc0fccc8', 9, 13715, 46, '2022-02-01', 'sensi'),
(51, 'c83a75240d9042979d23dd8a26b3a358', 9, 11114, 9, '2022-01-27', 'sensi'),
(56, 'ce99d6d3a3364f95b589bd05b2a6ce60', 9, 36276, 123, '2022-01-27', 'sensi'),
(57, 'c26e2633b5ff4078a4b3865742054b67', 9, 375195, 807, '2022-01-27', 'sensi'),
(59, 'c3fc8db362ed4cdf8fb0a73982a59aa6', 9, 30677, 95, '2022-02-02', 'sensi'),
(61, 'e3cdace510e441019ba9057fcc0fccc8', 4, 20751, 46, '2022-02-01', 'Exocrow'),
(62, 'ce99d6d3a3364f95b589bd05b2a6ce60', 4, 42085, 155, '2022-02-01', 'Exocrow'),
(63, '1c32ac64668149288b067327eafbe5e3', 4, 22134, 36, '2022-02-01', 'Exocrow'),
(64, 'c3fc8db362ed4cdf8fb0a73982a59aa6', 4, 42387, 95, '2022-02-02', 'Exocrow'),
(72, '2d178aa8717643aaa056d87bbb8150e0', 37, 8025, 30, '2022-02-17', ''),
(88, '8dffa0fb56fa4e8fb523697630c616db', 1, 4891, 23, '2022-02-18', 'Keks'),
(89, 'db9d19b1ea5f46269a58fa255a5f56e8', 1, 37405, 86, '2022-03-01', 'Keks'),
(90, 'dcc5b97989434c12ba94d164a3a01b7d', 1, 52704, 66, '2022-02-20', 'Keks'),
(91, '432b32cd19c74ff5a215bd75005af3bf', 1, 12327, 18, '2022-02-20', 'Keks'),
(92, 'db9d19b1ea5f46269a58fa255a5f56e8', 4, 108488, 130, '2022-02-21', 'Exocrow'),
(93, '5d0a50a04d4f41b9a670a91cd816af74', 1, 7266, 22, '2022-02-27', 'Keks'),
(94, '79a7e71d5c47425c8e8543ddd2f63e01', 1, 10153, 29, '2022-02-22', 'Keks'),
(95, 'f30bd59c4e5e468d9ed4aa7e61cf30cd', 1, 12506, 40, '2022-02-22', 'Keks'),
(96, 'c3fc8db362ed4cdf8fb0a73982a59aa6', 1, 34188, 80, '2022-02-22', 'Keks'),
(97, '6bb89b6ae9514d92bf2c79ac0ffb3bcc', 1, 11015, 24, '2022-03-01', 'Keks'),
(98, 'e3cdace510e441019ba9057fcc0fccc8', 43, 19240, 52, '2022-03-05', 'Datura'),
(99, 'dcc5b97989434c12ba94d164a3a01b7d', 43, 67535, 78, '2022-03-05', 'Datura'),
(100, '8dffa0fb56fa4e8fb523697630c616db', 43, 9229, 23, '2022-03-05', 'Datura'),
(101, 'cc0d92f32c084c0d8f027aa5205b66e9', 43, 32502, 43, '2022-03-05', 'Datura'),
(102, '5d0a50a04d4f41b9a670a91cd816af74', 43, 17442, 22, '2022-03-05', 'Datura'),
(103, '1c32ac64668149288b067327eafbe5e3', 1, 12556, 36, '2022-03-13', 'Keks');

-- --------------------------------------------------------

--
-- Structure de la table `Votes`
--

CREATE TABLE `Votes` (
  `MapID` varchar(32) NOT NULL,
  `UserID` int(11) NOT NULL,
  `VoteType` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `Votes`
--

INSERT INTO `Votes` (`MapID`, `UserID`, `VoteType`) VALUES
('6bb89b6ae9514d92bf2c79ac0ffb3bcc', 1, 0),
('2d178aa8717643aaa056d87bbb8150e0', 1, 0),
('79a7e71d5c47425c8e8543ddd2f63e01', 1, 0),
('40abe1b2f8e8487d8a3aebe794a15807', 1, 0),
('cc0d92f32c084c0d8f027aa5205b66e9', 1, 0),
('586cfd0add374eb883ffb730ef4bd1c2', 1, 0),
('836645494ee64b519e3469f5dcdbe698', 1, 0),
('4d96e16b025143a48cc7932f66a04750', 1, 0),
('8dffa0fb56fa4e8fb523697630c616db', 1, 0),
('921a92360f7c46b9a19dd88242d54021', 1, 0),
('ab9b412f2af04b6d86a5c9ade67ff74b', 1, 0),
('f30bd59c4e5e468d9ed4aa7e61cf30cd', 1, 0),
('29359345d77a428490cb8a550a10d1a5', 1, 0),
('79a7e71d5c47425c8e8543ddd2f63e01', 9, 0),
('586cfd0add374eb883ffb730ef4bd1c2', 9, 0),
('6bb89b6ae9514d92bf2c79ac0ffb3bcc', 9, 0),
('8dffa0fb56fa4e8fb523697630c616db', 9, 0),
('f30bd59c4e5e468d9ed4aa7e61cf30cd', 9, 0),
('4d96e16b025143a48cc7932f66a04750', 9, 0),
('ab9b412f2af04b6d86a5c9ade67ff74b', 9, 0),
('836645494ee64b519e3469f5dcdbe698', 9, 0),
('2d178aa8717643aaa056d87bbb8150e0', 9, 0),
('cc0d92f32c084c0d8f027aa5205b66e9', 9, 0),
('40abe1b2f8e8487d8a3aebe794a15807', 9, 0),
('6bb89b6ae9514d92bf2c79ac0ffb3bcc', 4, 0),
('4d96e16b025143a48cc7932f66a04750', 4, 0),
('8dffa0fb56fa4e8fb523697630c616db', 4, 0),
('836645494ee64b519e3469f5dcdbe698', 4, 0),
('586cfd0add374eb883ffb730ef4bd1c2', 4, 0),
('921a92360f7c46b9a19dd88242d54021', 9, 0),
('40abe1b2f8e8487d8a3aebe794a15807', 4, 0),
('cc0d92f32c084c0d8f027aa5205b66e9', 4, 0),
('921a92360f7c46b9a19dd88242d54021', 4, 0),
('2d178aa8717643aaa056d87bbb8150e0', 4, 0),
('ab9b412f2af04b6d86a5c9ade67ff74b', 4, 0),
('79a7e71d5c47425c8e8543ddd2f63e01', 4, 0),
('f30bd59c4e5e468d9ed4aa7e61cf30cd', 4, 0),
('5d0a50a04d4f41b9a670a91cd816af74', 9, 0),
('5d0a50a04d4f41b9a670a91cd816af74', 4, 0),
('5d0a50a04d4f41b9a670a91cd816af74', 1, 0),
('29359345d77a428490cb8a550a10d1a5', 9, 0),
('1c32ac64668149288b067327eafbe5e3', 9, 0),
('e3cdace510e441019ba9057fcc0fccc8', 1, 0),
('e3cdace510e441019ba9057fcc0fccc8', 9, 0),
('c83a75240d9042979d23dd8a26b3a358', 9, 0),
('1c32ac64668149288b067327eafbe5e3', 1, 0),
('c83a75240d9042979d23dd8a26b3a358', 1, 0),
('ce99d6d3a3364f95b589bd05b2a6ce60', 9, 0),
('c26e2633b5ff4078a4b3865742054b67', 9, 0),
('ce99d6d3a3364f95b589bd05b2a6ce60', 1, 0),
('c3fc8db362ed4cdf8fb0a73982a59aa6', 9, 0),
('c3fc8db362ed4cdf8fb0a73982a59aa6', 1, 0),
('e3cdace510e441019ba9057fcc0fccc8', 4, 0),
('ce99d6d3a3364f95b589bd05b2a6ce60', 4, 0),
('1c32ac64668149288b067327eafbe5e3', 4, 0),
('c3fc8db362ed4cdf8fb0a73982a59aa6', 4, 0),
('db9d19b1ea5f46269a58fa255a5f56e8', 1, 2),
('dcc5b97989434c12ba94d164a3a01b7d', 1, 2),
('432b32cd19c74ff5a215bd75005af3bf', 1, 1),
('db9d19b1ea5f46269a58fa255a5f56e8', 4, 0),
('e3cdace510e441019ba9057fcc0fccc8', 43, 0),
('dcc5b97989434c12ba94d164a3a01b7d', 43, 0),
('cc0d92f32c084c0d8f027aa5205b66e9', 43, 0),
('5d0a50a04d4f41b9a670a91cd816af74', 43, 0);

-- --------------------------------------------------------

--
-- Structure de la table `WeekStarMap`
--

CREATE TABLE `WeekStarMap` (
  `ID` int(11) NOT NULL,
  `MapID` varchar(32) NOT NULL,
  `Week` int(11) NOT NULL,
  `Year` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `WeekStarMap`
--

INSERT INTO `WeekStarMap` (`ID`, `MapID`, `Week`, `Year`) VALUES
(30, '8dffa0fb56fa4e8fb523697630c616db', 10, 2022),
(31, '5d0a50a04d4f41b9a670a91cd816af74', 10, 2022),
(32, '2d178aa8717643aaa056d87bbb8150e0', 10, 2022),
(33, '1c32ac64668149288b067327eafbe5e3', 10, 2022),
(34, '6bb89b6ae9514d92bf2c79ac0ffb3bcc', 10, 2022),
(37, 'e3cdace510e441019ba9057fcc0fccc8', 10, 2022),
(38, 'dcc5b97989434c12ba94d164a3a01b7d', 10, 2022),
(39, 'db9d19b1ea5f46269a58fa255a5f56e8', 10, 2022),
(40, 'cc0d92f32c084c0d8f027aa5205b66e9', 10, 2022),
(41, '836645494ee64b519e3469f5dcdbe698', 10, 2022);

-- --------------------------------------------------------

--
-- Structure de la table `WorkingMaps`
--

CREATE TABLE `WorkingMaps` (
  `ID` int(11) NOT NULL,
  `MapID` varchar(32) NOT NULL,
  `UserID` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

--
-- Déchargement des données de la table `WorkingMaps`
--

INSERT INTO `WorkingMaps` (`ID`, `MapID`, `UserID`) VALUES
(5, 'c26e2633b5ff4078a4b3865742054b67', 9),
(6, 'c26e2633b5ff4078a4b3865742054b67', 1),
(7, 'c3fc8db362ed4cdf8fb0a73982a59aa6', 9),
(8, '66c947e4043a42eaa293219b6689d896', 9),
(29, '7c7b9f6048c342378f4bc52a51bf26f3', 9),
(30, 'de389d1d1c084df0b845c7e1741246a3', 9),
(31, 'dee411b5281d45fc9d8aca23f75fa28b', 9),
(32, '672f1a7fb1b140a2aeac24c4adf841c1', 9),
(33, 'a2401d49f35d400e9d83fa6c0bcf7012', 9),
(34, '6c7f6a5dcf9a4800a358737dd8d29e4c', 9),
(35, 'b9a317266a4d46cba94d92fe83eedbbb', 9),
(36, '8749424e6be64c769d0c9fe333d1d8f1', 9),
(37, 'bb08b672165f4c3f8d50c32853cd4650', 9),
(38, 'e792dc241e67486c958a08f8b63137e6', 9),
(39, '4c6a13c5317846f6bae85800eb2cf89c', 9),
(40, '6837ba56420543b583819c1e41387da7', 9),
(41, 'cd8826727f924c459d2b1a3cf4cad7c6', 9),
(42, '77dcadd50c844db98500fa40a0e32abe', 9),
(43, 'fde40f9f87df48fd95b6de7c1c773c9b', 9),
(44, '28c7f5c3cd9f4199b125856d6e35f2d7', 9),
(45, '0fa163ebab3c49fc8bbeddb97c815b56', 9),
(46, '443cbeee1a2344409a05204dd669bf83', 9),
(47, '4fb0422412674fde93a60709a8c6d5ca', 9),
(48, 'd2f63173160f47069f7e6f63101344c6', 9),
(49, 'b7f30362c0d8457dbc241f99f4f3d022', 9),
(50, '66c947e4043a42eaa293219b6689d896', 1),
(54, '672f1a7fb1b140a2aeac24c4adf841c1', 1),
(55, 'a2401d49f35d400e9d83fa6c0bcf7012', 1),
(56, '6c7f6a5dcf9a4800a358737dd8d29e4c', 1),
(57, 'b9a317266a4d46cba94d92fe83eedbbb', 1),
(58, '8749424e6be64c769d0c9fe333d1d8f1', 1),
(59, 'bb08b672165f4c3f8d50c32853cd4650', 1),
(60, 'e792dc241e67486c958a08f8b63137e6', 1),
(61, '4c6a13c5317846f6bae85800eb2cf89c', 1),
(62, '6837ba56420543b583819c1e41387da7', 1),
(63, 'cd8826727f924c459d2b1a3cf4cad7c6', 1),
(64, '77dcadd50c844db98500fa40a0e32abe', 1),
(65, 'fde40f9f87df48fd95b6de7c1c773c9b', 1),
(66, '28c7f5c3cd9f4199b125856d6e35f2d7', 1),
(67, '0fa163ebab3c49fc8bbeddb97c815b56', 1),
(68, '443cbeee1a2344409a05204dd669bf83', 1),
(69, '4fb0422412674fde93a60709a8c6d5ca', 1),
(70, 'd2f63173160f47069f7e6f63101344c6', 1),
(71, 'b7f30362c0d8457dbc241f99f4f3d022', 1),
(72, '7c7b9f6048c342378f4bc52a51bf26f3', 1),
(73, 'de389d1d1c084df0b845c7e1741246a3', 1),
(74, 'dee411b5281d45fc9d8aca23f75fa28b', 1),
(78, 'f30bd59c4e5e468d9ed4aa7e61cf30cd', 1),
(79, '432b32cd19c74ff5a215bd75005af3bf', 1),
(81, '879af8a6d6544bc0b88544680d84852b', 1),
(82, '4ebeb14f924e439a8347f06e3335b08c', 1),
(83, 'a2dcf5f37c7a41ac97f4eb1805f9c2e7', 1),
(84, '4e4195111f114f2b9942c854df7e5e52', 1);

--
-- Index pour les tables déchargées
--

--
-- Index pour la table `DayStarMap`
--
ALTER TABLE `DayStarMap`
  ADD PRIMARY KEY (`ID`);

--
-- Index pour la table `MapCode`
--
ALTER TABLE `MapCode`
  ADD UNIQUE KEY `index_code` (`CODE`);

--
-- Index pour la table `Maps`
--
ALTER TABLE `Maps`
  ADD PRIMARY KEY (`ID`);

--
-- Index pour la table `MonthStarMap`
--
ALTER TABLE `MonthStarMap`
  ADD PRIMARY KEY (`ID`);

--
-- Index pour la table `Scores`
--
ALTER TABLE `Scores`
  ADD PRIMARY KEY (`ID`),
  ADD KEY `index_score_mapid` (`MapID`);

--
-- Index pour la table `WeekStarMap`
--
ALTER TABLE `WeekStarMap`
  ADD PRIMARY KEY (`ID`);

--
-- Index pour la table `WorkingMaps`
--
ALTER TABLE `WorkingMaps`
  ADD PRIMARY KEY (`ID`);

--
-- AUTO_INCREMENT pour les tables déchargées
--

--
-- AUTO_INCREMENT pour la table `DayStarMap`
--
ALTER TABLE `DayStarMap`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=74;

--
-- AUTO_INCREMENT pour la table `MonthStarMap`
--
ALTER TABLE `MonthStarMap`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=74;

--
-- AUTO_INCREMENT pour la table `Scores`
--
ALTER TABLE `Scores`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=104;

--
-- AUTO_INCREMENT pour la table `WeekStarMap`
--
ALTER TABLE `WeekStarMap`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=44;

--
-- AUTO_INCREMENT pour la table `WorkingMaps`
--
ALTER TABLE `WorkingMaps`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=85;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
