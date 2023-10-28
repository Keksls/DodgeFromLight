<?php
	header('Content-type: text/html; charset=UTF-8');
	$host_name = 'db5006237699.hosting-data.io';
	$database = 'dbs5212226';
	$user_name = 'dbu2947541';
	$password = '8cqsn9qK!33zzupg';
	$db = new mysqli($host_name, $user_name, $password, $database);

	if ($db->connect_error)
	{
		die('La connexion au serveur MySQL a échoué: '. $db->connect_error .'');
	} 
	else 
	{
		if(isset($_GET["ProcessBadMaps"]))
		{
			$sql = "UPDATE `Maps` SET `State`=1, Likes = 0, Dislikes = 0, NbDownloads = 0, NbVotes = 0 WHERE NbVotes >= 30 AND Dislikes / Likes >= 0.33 AND State = 0 AND ID NOT IN (SELECT MapID FROM DayStarMap)";
			$db->query($sql);
			if($db->errno)
			{
				echo "Coult not process Bad Maps ".$db->error;
			}
			else
			{
				echo "OK";
			}
		}

		else if(isset($_GET["ProcessMapStarDay"]))
		{
			$sql = "INSERT INTO DayStarMap (`MapID`, `Date`) SELECT ID, CURRENT_DATE() FROM `Maps` WHERE State = 0 AND ID NOT IN (SELECT MapID FROM DayStarMap) ORDER BY Likes DESC, NbDownloads DESC, Dislikes ASC, RAND() DESC LIMIT 3";
			$db->query($sql);
			if($db->errno)
			{
				echo "Coult not add working Map ".$db->error;
			}
			else
			{
				echo "OK";
			}
		}
		
		else if(isset($_GET["ProcessMapStarWeek"]))
		{
			$sql = "INSERT INTO WeekStarMap (`MapID`, `Week`, `Year`) SELECT d.MapID, WEEK(CURRENT_DATE()), YEAR(CURRENT_DATE()) FROM `Maps` m, DayStarMap d WHERE m.ID = d.MapID AND m.ID NOT IN (SELECT MapID FROM WeekStarMap) AND d.Date BETWEEN DATE_SUB(CURRENT_DATE(), INTERVAL 7 DAY) AND CURRENT_DATE() ORDER BY m.Likes DESC, m.NbDownloads DESC, m.Dislikes ASC, RAND() DESC LIMIT 5";
			$db->query($sql);
			if($db->errno)
			{
				echo "Coult not add working Map ".$db->error;
			}
			else
			{
				echo "OK";
			}
		}
		
		else if(isset($_GET["ProcessMapStarMonth"]))
		{
			$sql = "INSERT INTO MonthStarMap (`MapID`, `Month`, `Year`) SELECT d.MapID, MONTH(CURRENT_DATE()), YEAR(CURRENT_DATE()) FROM `Maps` m, DayStarMap d WHERE m.ID = d.MapID AND m.ID NOT IN (SELECT MapID FROM MonthStarMap) AND d.Date BETWEEN DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY) AND CURRENT_DATE() ORDER BY m.Likes DESC, m.NbDownloads DESC, m.Dislikes ASC, RAND() DESC LIMIT 10";
			$db->query($sql);
			if($db->errno)
			{
				echo "Coult not add working Map ".$db->error;
			}
			else
			{
				echo "OK";
			}
		}

		else if(isset($_GET["GetMapsList"]))
		{
			$kw = $_GET["filter"];
			$myArray = array();
			$sql = "SELECT m.ID, m.Name, m.UploadDate, m.Width, m.Height, m.Likes, m.Dislikes, m.NbDownloads, m.AuthorName as Author FROM Maps m WHERE(m.AuthorName LIKE '%".$kw."%' OR m.Name LIKE '%".$kw."%') AND m.State = 0 ORDER BY LENGTH(m.Name), m.Name LIMIT 30";
			if ($result = $db->query($sql)) {

				while($row = $result->fetch_array(MYSQLI_ASSOC)) {
						$myArray[] = $row;
				}
				echo json_encode($myArray);
				$result->close();
			}
		}
		
		else if(isset($_GET["GetWorkingMapsList"]))
		{
			$userID = $_GET["userID"];
			$myArray = array();
			$sql = "SELECT m.ID, m.Name, m.UploadDate, m.Width, m.Height, m.Likes, m.Dislikes, m.NbDownloads, m.Author as AuthorID, m.AuthorName as Author, m.AlreadyFinished FROM Maps m, WorkingMaps w WHERE m.ID = w.MapID AND w.UserID = ".$userID." AND m.State = 1 ORDER BY LENGTH(m.Name), m.Name";
			if ($result = $db->query($sql)) {

				while($row = $result->fetch_array(MYSQLI_ASSOC)) {
						$myArray[] = $row;
				}
				echo json_encode($myArray);
				$result->close();
			}
			if($db->errno)
			{
				echo "can't get working maps ".$db->error;
				return false;
			}
		}

		else if(isset($_GET["DeleteMap"]))
		{
			$mapID = $_GET["mapID"];
			if(MapExist($mapID))
			{
				$query = "DELETE FROM `Maps` WHERE ID = '".$mapID."'";
				$db->query($query);
				if($db->errno)
				{
					echo "Coult not delete map ".$db->error;
				}
				else
				{
					$query = "DELETE FROM `WorkingMaps` WHERE MapID = '".$mapID."'";
					$db->query($query);
					
					$query = "DELETE FROM `Votes` WHERE MapID = '".$mapID."'";
					$db->query($query);
					
					$query = "DELETE FROM `Scores` WHERE MapID = '".$mapID."'";
					$db->query($query);
					
					$query = "DELETE FROM `MapCode` WHERE MapID = '".$mapID."'";
					$db->query($query);

					$url = './Maps/'.$mapID.'/';
					deleteDir($url);
					echo "OK";
				}
			}
			else
			{
				echo "map don't exists";
			}
		}

		else if(isset($_GET["AddWorkingMap"]))
		{
			$userID = $_GET["userID"];
			$mapID = $_GET["mapID"];
			
			$sql = "SELECT COUNT(*) FROM WorkingMaps WHERE MapID = '".$mapID."' AND UserID = ".$userID.";";
			$result = $db->query($sql);
			$row = $result->fetch_row();
			if($row[0] == 0)
			{
				global $db;
				$query = "INSERT INTO WorkingMaps (`MapID`, `UserID`) VALUES ('".$mapID."', ".$userID.");";
				$db->query($query);
				if($db->errno)
				{
					echo "Coult not add working Map ".$db->error;
				}
				else
				{
					echo "OK";
				}
			}
			else
			{
				echo "Already exist";
			}
		}

		else if(isset($_GET["DeleteWorkingMap"]))
		{
			$userID = $_GET["userID"];
			$mapID = $_GET["mapID"];
			
			$sql = "SELECT COUNT(*) FROM WorkingMaps WHERE MapID = '".$mapID."' AND UserID = ".$userID.";";
			$result = $db->query($sql);
			$row = $result->fetch_row();
			if($row[0] != 0)
			{
				global $db;
				$query = "DELETE FROM WorkingMaps WHERE MapID = '".$mapID."' AND UserID = ".$userID.";";
				$db->query($query);
				if($db->errno)
				{
					echo "Coult not remove working Map ".$db->error;
				}
				else
				{
					echo "OK";
				}
			}
			else
			{
				echo "no working map to remove";
			}
		}

		else if(isset($_GET["GetMap"]))
		{
			$mapID = $_GET["mapID"];
			if(MapExist($mapID))
			{
				$sql = "SELECT m.ID, m.Name, m.UploadDate, m.Width, m.Height, m.Likes, m.Dislikes, m.NbDownloads, m.AuthorName as Author, m.Author as AuthorID, m.State FROM Maps m WHERE m.ID = '".$mapID."'";
				if ($res = $db->query($sql))
				{
					$ok = false;
					while($row = $res->fetch_array(MYSQLI_ASSOC))
					{
						$ok = true;
						echo json_encode($row);
						break;
					}
					if(!$ok)
					{
						echo "error getting map";
					}
				}
				else
				{
					echo "error getting map";
				}
			}
			else
			{
				echo "map don't exist";
			}
		}

		else if(isset($_GET["DownloadPreviewImage"]))
		{
			$id = $_GET["ID"];
			$url = './Maps/'.$id.'/preview.png';
			//Clear the cache
			//clearstatcache();
			if(file_exists($url)) {
				//Define header information
				header('Content-Description: File Transfer');
				header('Content-Type: application/octet-stream');
				header('Content-Disposition: attachment; filename="'.basename($url).'"');
				header('Content-Length: ' . filesize($url));
				header('Pragma: public');
				//Clear system output buffer
				flush();
				//Read the size of the file
				readfile($url, true);
			}
			else{
				echo "File path does not exist.";
			}
		}

		else if(isset($_GET["DownloadMap"]))
		{
			$id = $_GET["mapID"];
			$zip = new ZipArchive();
			$rndCode = strtoupper(substr(md5(uniqid(mt_rand(), true)) , 0, 16));
			$filename = "./Maps/$id".$rndCode.".zip";

			IncraseNbDownloads($id);
						
			$url = $filename;
			if(MapExist($id))
			{
				$sql = "SELECT m.ID, m.Name, m.UploadDate, m.Width, m.Height, m.Likes, m.Dislikes, m.NbDownloads, m.AuthorName as Author, m.Author as AuthorID, m.State FROM Maps m WHERE m.ID = '".$id."'";
				if ($res = $db->query($sql))
				{
					$ok = false;
					while($row = $res->fetch_array(MYSQLI_ASSOC))
					{
						$ok = true;
						$md = json_encode($row);

						
						if ($zip->open($filename, ZipArchive::CREATE)!==TRUE) {
							echo "can't create file $filename";
						}

						$gridPath = "./Maps/$id/grid.json";
						$previewPath = "./Maps/$id/preview.png";
						$mdPath = "./Maps/$id/metadata.json";
						$myfile = fopen($mdPath, "w") or die("Unable to open file!");
						fwrite($myfile, $md);
						fclose($myfile);

						$zip->addFile($gridPath, "grid.json");
						$zip->addFile($previewPath, "preview.png");
						$zip->addFile($mdPath, "metadata.json");
						$zip->close();

						//Define header information
						header('Content-Description: File Transfer');
						header('Content-Type: application/octet-stream');
						header('Content-Disposition: attachment; filename="'.basename($url).'"');
						header('Content-Length: ' . filesize($url));
						header('Pragma: public');
						//Clear system output buffer
						flush();
						//Read the size of the file
						readfile($url, true);
						unlink($filename);
						break;
					}
					if(!$ok)
					{
						echo "error getting map";
					}
				}
				else
				{
					echo "error getting map";
				}
			}
			else
			{
				echo "map don't exist";
			}
		}

		else if(isset($_GET["UpdateFinishState"]))
		{
			$id = $_GET["ID"];
			$finished = $_GET["finished"];
			$query = "UPDATE `Maps` set AlreadyFinished = ".$finished." WHERE ID = '".$id."'";
			$db->query($query);
			if($db->errno)
			{
				echo "Coult not add map ".$db->error;
			}
			else
			{
				echo "OK";
			}
		}

		else if(isset($_GET["UpdateState"]))
		{
			$id = $_GET["ID"];
			$state = $_GET["state"];
			$query = "UPDATE `Maps` set State = ".$state." WHERE ID = '".$id."'";
			$db->query($query);
			if($db->errno)
			{
				echo "Coult not add map ".$db->error;
			}
			else
			{
				echo "OK";
			}
		}

		else if(isset($_GET["UploadMap"]))
		{
			$id = $_GET["ID"];
			$name = $_GET["name"];
			$authorID = $_GET["authorID"];
			$width = $_GET["width"];
			$height = $_GET["height"];
			$state = $_GET["state"];
			$finished = $_GET["finished"];
			$ok = false;

			if(!MapExist($id))
			{
				$query = "INSERT INTO `Maps` (`ID`, `Name`, `Author`, `UploadDate`, `Width`, `Height`, `State`, AlreadyFinished) VALUES ('".$id."', '".$name."', ".$authorID.", NOW(), ".$width.", ".$height.", ".$state.", ".$finished.")";
				$db->query($query);
				if($db->errno)
				{
					echo "Coult not add map ".$db->error;
				}
				else
				{
					$ok = true;
				}
			}
			else
			{
				$ok = true;
			}

			if($ok)
			{
				if($state == 0) // want to submit a map to discovery mode
				{
					$ok = IsMapAlreadyFinished($id);
					if(!$ok)
					{
						echo "Map not validated. Please do at least one run before submit.";
					}
				}
			}

			if($ok)
			{
				$uploads_dir = './Maps/'.$id.'/';
				mkdir($uploads_dir, 0700);
				$zipName = "$uploads_dir/$id.zip";
				$tmp_name = $_FILES["file"]["tmp_name"];
				move_uploaded_file($tmp_name, $zipName);

				$zip = new ZipArchive;
				if ($zip->open($zipName) === TRUE) {
					$zip->extractTo($uploads_dir);
					$zip->close();
					echo 'OK';
				} else {
					echo 'Error unzip map.';
				}
				unlink($zipName);
				unlink($uploads_dir.'metadata.json');
			}
			else
			{
				echo "Fail to upload map.";
			}
		}
		
		else if(isset($_GET["UploadHub"]))
		{
			$id = $_GET["ID"];
			$ok = false;
			mkdir($uploads_dir, 0700);
			$gridName = "./Hubs/".$id.".json";
			$tmp_name = $_FILES["file"]["tmp_name"];
			move_uploaded_file($tmp_name, $gridName);
			echo "OK";
		}
		
		else if(isset($_GET["DownloadHub"]))
		{
			$id = $_GET["ID"];
			$filename = "./Hubs/".$id.".json";
			if(file_exists($filename))
			{
				//Define header information
				header('Content-Description: File Transfer');
				header('Content-Type: application/octet-stream');
				header('Content-Disposition: attachment; filename="'.basename($filename).'"');
				header('Content-Length: ' . filesize($filename));
				header('Pragma: public');
				flush();
				readfile($filename, true);
			}
			else
			{
				echo "hub don't exist";
			}
		}

		else if(isset($_GET["DownloadLobby"]))
		{
			$id = $_GET["ID"];
			$filename = "./Lobbies/".$id.".json";
			if(file_exists($filename))
			{
				//Define header information
				header('Content-Description: File Transfer');
				header('Content-Type: application/octet-stream');
				header('Content-Disposition: attachment; filename="'.basename($filename).'"');
				header('Content-Length: ' . filesize($filename));
				header('Pragma: public');
				flush();
				readfile($filename, true);
			}
			else
			{
				echo "Lobby don't exist";
			}
		}
		
		else if(isset($_GET["HasHub"]))
		{
			$id = $_GET["ID"];
			$filename = "./Hubs/".$id.".json";
			if(file_exists($filename))
			{
				echo "true";
			}
			else
			{
				echo "false";
			}
		}

		else if (isset($_GET["GetMapCode"]))
		{
			$mapID = $_GET["mapID"];
			// check if map Exist
			if(MapExist($mapID))
			{
				// get code if already exist
				$result = $db->query("SELECT COUNT(*) FROM `MapCode` WHERE MapID = '".$mapID."'");
				$row = $result->fetch_row();
				if($row[0] > 0) // code already exist
				{
					$result = $db->query("SELECT CODE FROM `MapCode` WHERE MapID = '".$mapID."'");
					$row = $result->fetch_row();
					echo $row[0];
				}
				else // code do not exist
				{
					$ok = false;
					while(!$ok) // generate new unique code
					{
						$code = strtoupper(substr(md5(uniqid(mt_rand(), true)) , 0, 6));
						if(!CodeExist($code))
						{
							if(InsertCode($code, $mapID))
							{
								echo($code);
							}
							$ok = true;
						}
					}
				}
			}
			else // map do not exist
			{
				echo "Map do not exist (ID:".$mapID.")";
			}
		}

		else if(isset($_GET["GetMapIDFromCode"]))
		{
			$code = $_GET["code"];
			if(CodeExist($code))
			{
				echo MapIDFromCode($code);
			}
			else
			{
				echo "Invalid CODE '".$code."'";
			}
		}

		else if(isset($_GET["IsMapLocked"]))
		{
			$mapID = $_GET["mapID"];
			if(IsMapLocked($mapID))
			{
				echo "true";
			}
			else
			{
				echo "false";
			}
		}

		else if(isset($_GET["WorkOnMap"]))
		{
			$mapID = $_GET["mapID"];
			$userID = $_GET["userID"];
			if(WorkOnMap($mapID, $userID))
			{
				echo "OK";
			}
		}

		else if(isset($_GET["StopWorkingOnMap"]))
		{
			$mapID = $_GET["mapID"];
			if(WorkOnMap($mapID, -1))
			{
				echo "OK";
			}
		}

		else if(isset($_GET["GetDiscoveryMapID"]))
		{
			$user = $_GET["userID"];
			$sql = "SELECT m.ID FROM Maps m WHERE (Likes < 100 OR NbDownloads >= (NbVotes * 0.1)) AND 
			(SELECT COUNT(d.ID) FROM DayStarMap d WHERE MapID = m.ID) = 0 AND
			(SELECT COUNT(*) FROM Votes WHERE MapID = m.ID AND UserID = ".$user.") = 0 AND 
			m.State = 0 ORDER BY RAND() LIMIT 1";

			if ($res = $db->query($sql))
			{
				$row = $res->fetch_row();
				if($row[0] == "")
				{
					$sql = "SELECT ID FROM Maps WHERE State = 0 ORDER BY RAND() LIMIT 1";
					if ($res = $db->query($sql))
					{
						$row = $res->fetch_row();
						echo $row[0];
					}
					else
					{
						echo "Error getting rand map.";
					}
				}
				else
				{
					echo $row[0];
				}
			}
			else
			{
				echo "Error getting rand map.";
			}
		}

		else if(isset($_GET["GetMap"]))
		{
			$id = $_GET["mapID"];
			$sql = "SELECT * FROM Maps WHERE ID = '".$id."'";
			if ($res = $db->query($sql))
			{
				$ok = false;
				while($row = $res->fetch_array(MYSQLI_ASSOC))
				{
					$ok = true;
					echo json_encode($row);
					break;
				}
				if(!$ok)
				{
					echo "Error getting map";
				}
			}
			else
			{
				echo "Error getting map";
			}
		}

		else if(isset($_GET["Vote"]))
		{
			$mapID = $_GET["mapID"];
			$userID = $_GET["userID"];
			$voteType = $_GET["type"];
			if(HasVote($mapID, $userID))
			{
				echo "User already vote for this map.";
			}
			else
			{
				if(Vote($mapID, $userID, $voteType))
				{
					echo "OK";
				}
			}
		}

		else if(isset($_GET["HasVote"]))
		{
			$mapID = $_GET["mapID"];
			$userID = $_GET["userID"];
			if(HasVote($mapID, $userID))
			{
				echo "true";
			}
			else
			{
				echo "false";
			}
		}

		else if(isset($_GET["GetVote"]))
		{
			$mapID = $_GET["mapID"];
			$userID = $_GET["userID"];
			if(HasVote($mapID, $userID))
			{
				echo GetVoteType($mapID, $userID);
			}
			else
			{
				echo "This user has not yet voted for this map.";
			}
		}

		else if(isset($_GET["GetDayMapStar"]))
		{
			$sql = "SELECT m.ID, m.Name, m.UploadDate, m.Width, m.Height, m.Likes, m.Dislikes, m.NbDownloads, m.AuthorName as Author FROM Maps m, `DayStarMap` day WHERE day.`Date` = CURRENT_DATE() AND day.MapID = m.ID";
			if ($result = $db->query($sql)) {

				while($row = $result->fetch_array(MYSQLI_ASSOC)) {
						$myArray[] = $row;
				}
				echo json_encode($myArray);
				$result->close();
			}
		}
		
		else if(isset($_GET["GetWeekMapStar"]))
		{
			$sql = "SELECT m.ID, m.Name, m.UploadDate, m.Width, m.Height, m.Likes, m.Dislikes, m.NbDownloads, m.AuthorName as Author FROM Maps m, `WeekStarMap` day WHERE day.`Week` = WEEK(CURRENT_DATE()) AND day.Year = YEAR(CURRENT_DATE()) AND day.MapID = m.ID";
			if ($result = $db->query($sql)) {

				while($row = $result->fetch_array(MYSQLI_ASSOC)) {
						$myArray[] = $row;
				}
				echo json_encode($myArray);
				$result->close();
			}
		}
		
		else if(isset($_GET["GetMonthMapStar"]))
		{
			$sql = "SELECT m.ID, m.Name, m.UploadDate, m.Width, m.Height, m.Likes, m.Dislikes, m.NbDownloads, m.AuthorName as Author FROM Maps m, `MonthStarMap` day WHERE day.`Month` = MONTH(CURRENT_DATE()) AND day.Year = YEAR(CURRENT_DATE()) AND day.MapID = m.ID";
			if ($result = $db->query($sql)) {

				while($row = $result->fetch_array(MYSQLI_ASSOC)) {
						$myArray[] = $row;
				}
				echo json_encode($myArray);
				$result->close();
			}
		}

		else if(isset($_GET["AddScore"]))
		{
			$mapID = $_GET["mapID"];
			$userID = $_GET["userID"];
			$userName = $_GET["userName"];
			$time = $_GET["time"];
			$turns = $_GET["turns"];

			if(!HasScore($mapID, $userID))
			{
				if(AddScore($mapID, $userID, $userName, $time, $turns))
				{
					echo "WINXP";
				}
			}
			else
			{
				if(UpdateScore($mapID, $userID, $time, $turns))
				{
					echo "OK";
				}
			}
		}

		else if(isset($_GET["GetScores"]))
		{
			$mapID = $_GET["mapID"];
			$limite = $_GET["limite"];
			GetScores($mapID, $limite);
		}

		$db->close();
	}

	function IsMapLocked($mapID)
	{
		global $db;
		$sql = "SELECT COUNT(*) FROM MapCode WHERE MapID = '".$mapID."' AND WorkingUser <> -1 AND (SELECT TIME_TO_SEC(TIMEDIFF(NOW(), LastWorkingPing))) <= 120;";
		$result = $db->query($sql);
		if($db->errno)
		{
			echo "Coult not Lock map ".$db->error;
			return false;
		}
		else{
			$row = $result->fetch_row();
			return $row[0] == 1;
		}
	}

	function WorkOnMap($mapID, $userID)
	{ 
		global $db;
		$result = $db->query("SELECT COUNT(*) FROM `MapCode` WHERE MapID = '".$mapID."'");
		$row = $result->fetch_row();
		if($row[0] > 0) // code already exist
		{
			$query = "UPDATE MapCode set `WorkingUser` = ".$userID.", LastWorkingPing = NOW() WHERE MapID = '".$mapID."';";
			$db->query($query);
			if($db->errno)
			{
				echo "Coult not Lock map ".$db->error;
				return false;
			}
			else{
				return true;
			}
		}
		else
		{
			echo "don't need to lock this map";
			return false;
		}
	}

	function deleteDir($dirPath) {
		array_map('unlink', glob("$dirPath/*.*"));
		rmdir($dirPath);
	}

	function GetScores($mapID, $limite)
	{
		global $db;
		$sql = "SELECT UserName, UserID, Time, Turns FROM Scores WHERE MapID = '".$mapID."' ORDER BY Turns ASC, Time ASC LIMIT ".$limite.";";
		$myArray = array();
		if ($result = $db->query($sql)) {

			while($row = $result->fetch_array(MYSQLI_ASSOC)) {
					$myArray[] = $row;
			}
			echo json_encode($myArray);
		}
		if($db->errno)
		{
			echo "Error getting scores ".$db->error;
			return false;
		}
		else{
			return true;
		}
	}

	function HasScore($mapID, $userID)
	{
		global $db;
		$sql = "SELECT COUNT(*) FROM Scores WHERE MapID = '".$mapID."' AND UserID = ".$userID.";";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		return $row[0] > 0;
	}
	
	function UpdateScore($mapID, $userID, $time, $turns)
	{
		global $db;
		
		$sql = "SELECT `Time`, Turns FROM Scores WHERE MapID = '".$mapID."' AND UserID = ".$userID.";";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		$bestTime = $row[0];
		$bestTurns = $row[1];

		if($turns < $bestTurns || $turns == $bestTurns && $time < $bestTime)
		{
			$query = "UPDATE Scores SET `Time` = ".$time.", `Turns` = ".$turns.", `Date` = NOW() WHERE MapID = '".$mapID."' AND UserID = ".$userID.";";
			$db->query($query);
			if($db->errno)
			{
				echo "Coult not set Score ".$db->error;
				return false;
			}
			else{
				return true;
			}
		}
		else
		{
			return true;
		}
	}
	
	function AddScore($mapID, $userID, $userName, $time, $turns)
	{
		global $db;
		$query = "INSERT INTO Scores (`MapID`, `UserID`, `UserName`, `Time`, `Turns`, `Date`) VALUES ('".$mapID."', ".$userID.", '".$userName."', ".$time.", ".$turns.", NOW());";
		$db->query($query);
		if($db->errno)
		{
			echo "Coult not add Score ".$db->error;
			return false;
		}
		else{
			return true;
		}
	}

	function HasVote($mapID, $userID)
	{
		global $db;
		$sql = "SELECT COUNT(*) FROM Votes WHERE MapID = '".$mapID."' AND UserID = ".$userID."";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		return $row[0] > 0;
	}

	function IsMapAlreadyFinished($mapID)
	{
		global $db;
		$sql = "SELECT AlreadyFinished FROM Maps WHERE ID = '".$mapID."'";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		return $row[0] == 1;
	}

	function GetVoteType($mapID, $userID)
	{
		global $db;
		$sql = "SELECT VoteType FROM Votes WHERE MapID = '".$mapID."' AND UserID = ".$userID."";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		return $row[0];
	}

	function Vote($mapID, $userID, $voteType)
	{
		global $db;
		$query = "INSERT INTO Votes (`MapID`, `UserID`, `VoteType`) VALUES ('".$mapID."', ".$userID.", '".$voteType."');";
		$db->query($query);
		if($db->errno)
		{
			echo "Coult not Vote Map ".$db->error;
			return false;
		}
		else{
			if($voteType == 0)
			{
				if(Like($mapID, 1))
				{
					return IncraseNbVote($mapID);
				}
			}
			else if($voteType == 1)
			{
				if(Dislike($mapID))
				{
					return IncraseNbVote($mapID);
				}
			}
			else if($voteType == 2)
			{
				if(Like($mapID, 5))
				{
					return IncraseNbVote($mapID);
				}
			}
		}
	}

	function IncraseNbVote($mapID)
	{
		global $db;
		$sql = "SELECT NbVotes FROM Maps WHERE ID = '".$mapID."'";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		$nbLikes = $row[0] + 1;

		$query = "UPDATE Maps set `NbVotes` = ".$nbLikes." WHERE ID = '".$mapID."';";
		$db->query($query);
		if($db->errno)
		{
			echo "Coult not Vote Map ".$db->error;
			return false;
		}
		else{
			return true;
		}
	}

	function IncraseNbDownloads($mapID)
	{
		global $db;
		$sql = "SELECT NbDownloads FROM Maps WHERE ID = '".$mapID."'";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		$nbLikes = $row[0] + 1;

		$query = "UPDATE Maps set `NbDownloads` = ".$nbLikes." WHERE ID = '".$mapID."';";
		$db->query($query);
		if($db->errno)
		{
			echo "fail incrasing NbDownloads : ".$db->error;
			return false;
		}
		else{
			return true;
		}
	}

	function Like($mapID, $weight)
	{
		global $db;
		$sql = "SELECT Likes FROM Maps WHERE ID = '".$mapID."'";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		$nbLikes = $row[0] + $weight;

		$query = "UPDATE Maps set `Likes` = ".$nbLikes." WHERE ID = '".$mapID."';";
		$db->query($query);
		if($db->errno)
		{
			echo "Coult not Vote Map ".$db->error;
			return false;
		}
		else{
			return true;
		}
	}

	function Dislike($mapID)
	{
		global $db;
		$sql = "SELECT Dislikes FROM Maps WHERE ID = '".$mapID."'";
		$result = $db->query($sql);
		$row = $result->fetch_row();
		$nbLikes = $row[0] + 1;

		$query = "UPDATE Maps set `Dislikes` = ".$nbLikes." WHERE ID = '".$mapID."';";
		$db->query($query);
		if($db->errno)
		{
			echo "Coult not Vote Map ".$db->error;
			return false;
		}
		else{
			return true;
		}
	}

	function MapExist($mapID)
	{
		global $db;
		$result = $db->query("SELECT COUNT(*) FROM `Maps` WHERE ID = '".$mapID."'");
		$row = $result->fetch_row();
		return $row[0] > 0;
	}

	function CodeExist($code)
	{
		global $db;
		$result = $db->query("SELECT COUNT(*) FROM `MapCode` WHERE CODE = '".$code."'");
		$row = $result->fetch_row();
		return $row[0] > 0;
	}

	function InsertCode($code, $mapID)
	{
		global $db;
		$query = "INSERT INTO MapCode (`CODE`, `MapID`) VALUES ('".$code."', '".$mapID."');";
		$db->query($query);
		if($db->errno)
		{
			echo "Coult not create Map CODE ".$db->error;
			return false;
		}
		else{
			return true;
		}
	}

	function MapIDFromCode($code)
	{
		global $db;
		$result = $db->query("SELECT MapID FROM `MapCode` WHERE CODE = '".$code."'");
		$row = $result->fetch_row();
		return $row[0];
	}
?>