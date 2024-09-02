/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
IF NOT EXISTS (SELECT * FROM dbo.TEAM)
	BEGIN
	INSERT INTO Team(id, abbreviation, locationName, teamName, logoUri)
	VALUES(1, 'NJD', 'New Jersey', 'Devils', 'Devils.svg'),
	(2, 'NYI', 'New York', 'Islanders', 'Islanders.svg'),
	(3, 'NYR', 'New York', 'Rangers', 'Rangers.svg'),
	(4, 'PHI', 'Philadelphia', 'Flyers', 'Flyers.svg'),
	(5, 'PIT', 'Pittsburgh', 'Penguins', 'Penguins.svg'),
	(6, 'BOS', 'Boston', 'Bruins', 'Bruins.svg'),
	(7, 'BUF', 'Buffalo', 'Sabres', 'Sabres.svg'),
	(8, 'MTL', 'Montréal', 'Canadiens', 'Canadiens.svg'),
	(9, 'OTT', 'Ottawa', 'Senators', 'Senators.svg'),
	(10, 'TOR', 'Toronto', 'Maple Leafs', 'MapleLeafs.svg'),
	(11, 'ATL', 'Atlanta', 'Thrashers', 'Thrashers.svg'),
	(12, 'CAR', 'Carolina', 'Hurricanes', 'Hurricanes.svg'),
	(13, 'FLA', 'Florida', 'Panthers', 'Panthers.svg'),
	(14, 'TBL', 'Tampa Bay', 'Lightning', 'Lightning.svg'),
	(15, 'WSH', 'Washington', 'Capitals', 'Capitals.svg'),
	(16, 'CHI', 'Chicago', 'Blackhawks', 'Blackhawks.svg'),
	(17, 'DET', 'Detroit', 'Red Wings', 'RedWings.svg'),
	(18, 'NSH', 'Nashville', 'Predators', 'Predators.svg'),
	(19, 'STL', 'St. Louis', 'Blues', 'Blues.svg'),
	(20, 'CGY', 'Calgary', 'Flames', 'Flames.svg'),
	(21, 'COL', 'Colorado', 'Avalanche', 'Avalanche.svg'),
	(22, 'EDM', 'Edmonton', 'Oilers', 'Oilers.svg'),
	(23, 'VAN', 'Vancouver', 'Canucks', 'Canucks.svg'),
	(24, 'ANA', 'Anaheim', 'Ducks', 'Ducks.svg'),
	(25, 'DAL', 'Dallas', 'Stars', 'Stars.svg'),
	(26, 'LAK', 'Los Angeles', 'Kings', 'Kings.svg'),
	(27, 'PHX', 'Phoenix', 'Coyotes', 'PhoenixCoyotes.svg'),
	(28, 'SJS', 'San Jose', 'Sharks', 'Sharks.svg'),
	(29, 'CBJ', 'Columbus', 'Blue Jackets', 'BlueJackets.svg'),
	(30, 'MIN', 'Minnesota', 'Wild', 'Wild.svg'),
	(31, 'MNS', 'Minnesota', 'North Stars', 'NorthStars.svg'),
	(32, 'QUE', 'Quebec', 'Nordiques', 'Nordiques.svg'),
	(33, 'WIN', 'Winnipeg', 'Jets (1979)', 'Jets1979.svg'),
	(34, 'HFD', 'Hartford', 'Whalers', 'Whalers.svg'),
	(35, 'CLR', 'Colorado', 'Rockies', 'Rockies.svg'),
	(36, 'SEN', 'Ottawa', 'Senators (1917)', 'Senators1917.svg'),
	(37, 'HAM', 'Hamilton', 'Tigers', 'Tigers.svg'),
	(38, 'PIR', 'Pittsburgh', 'Pirates', 'Pirates.svg'),
	(39, 'QUA', 'Philadelphia', 'Quakers', 'Quakers.svg'),
	(40, 'DCG', 'Detroit', 'Cougars', 'Cougars.svg'),
	(41, 'MWN', 'Montreal', 'Wanderers', 'Wanderers.svg'),
	(42, 'QBD', 'Quebec', 'Bulldogs', 'Bulldogs.png'),
	(43, 'MMR', 'Montreal', 'Maroons', 'Maroons.svg'),
	(44, 'NYA', 'New York', 'Americans', 'Americans.svg'),
	(45, 'SLE', 'St. Louis', 'Eagles', 'Eagles.svg'),
	(46, 'OAK', 'Oakland', 'Seals', 'Seals.svg'),
	(47, 'AFM', 'Atlanta', 'Flames', 'AtlantaFlames.svg'),
	(48, 'KCS', 'Kansas City', 'Scouts', 'Scouts.svg'),
	(49, 'CBN', 'Cleveland', 'Barons', 'Barons.svg'),
	(50, 'DFL', 'Detroit', 'Falcons', 'Falcons.png'),
	(51, 'BRK', 'Brooklyn', 'Americans', 'BrooklynAmericans.png'),
	(52, 'WPG', 'Winnipeg', 'Jets', 'Jets.svg'),
	(53, 'ARI', 'Arizona', 'Coyotes', 'Coyotes.svg'),
	(54, 'VGK', 'Vegas', 'Golden Knights', 'GoldenKnights.svg'),
	(55, 'SEA', 'Seattle', 'Kraken', 'Kraken.svg'),
	(56, 'CSE', 'California', 'Golden Seals', 'GoldenSeals.svg'),
	(57, 'TAN', 'Toronto', 'Arenas', 'Arenas.svg'),
	(58, 'TSP', 'Toronto', 'St. Patricks', 'StPats.svg')
END