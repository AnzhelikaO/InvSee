using System;
using System.Collections.Generic;
using System.Linq;
using InvSee.Extensions;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace InvSee
{
	internal class Commands
	{
		private static readonly string _cp = TShockAPI.Commands.Specifier;

		public static void DoInvSee(CommandArgs args)
		{
			if (!Main.ServerSideCharacter)
			{
				args.Player.PluginErrorMessage("ServerSideCharacters must be enabled.");
				return;
			}
			PlayerInfo info = args.Player.GetPlayerInfo();
            #region Restore

            if (args.Parameters.Count < 1)
			{
                Restore(args, info);
                return;
			}

            #endregion
            #region Save

            string _0 = args.Parameters[0].ToLower();
            if ((_0 == "-s") || (_0 == "-save"))
            {
                Save(args, info);
                return;
            }

            #endregion
            Copy(args, info);
		}

        #region Restore

        private static void Restore(CommandArgs args, PlayerInfo info)
        {
            if (args.Player.Dead)
            {
                args.Player.PluginErrorMessage("You cannot restore your inventory while dead.");
                return;
            }
            if (info.Restore(args.Player))
            { args.Player.PluginErrorMessage("Inventory has been restored."); }
            else
            {
                args.Player.PluginInfoMessage("You are currently not seeing anyone's inventory.");
                args.Player.PluginInfoMessage($"Use '{_cp}invsee <player name>' to begin.");
            }
        }

        #endregion
        #region Save

        private static void Save(CommandArgs args, PlayerInfo info)
        {
            #region Checks

            if (!args.Player.HasPermission(Permissions.InvSeeSave))
            {
                args.Player.PluginErrorMessage("You don't have the permission to change player inventories!");
                return;
            }

            if (info.Backup == null)
            {
                args.Player.PluginErrorMessage("You are not copying any user!");
                return;
            }

            #endregion
            User user;
            List<TSPlayer> players = new List<TSPlayer>();
            #region GetUserAndPlayers

            if (info.CopyingUserID != -1)
            {
                user = TShock.Users.GetUserByID(info.CopyingUserID);
                if (user == null)
                {
                    args.Player.PluginErrorMessage("Invalid user!");
                    return;
                }
                players = TShock.Players.Where(p => ((p?.User != null)
                    && p.Active && (p.User.ID == user.ID))).ToList();
                args.Player.PluginInfoMessage($"Saved changes made to {user.Name}'s inventory.");
            }
            else
            {
                TSPlayer player = TShock.Players.ElementAtOrDefault(info.CopyingPlayerIndex);
                if (player == null)
                {
                    args.Player.PluginErrorMessage("Invalid player!");
                    return;
                }
                user = player.User;
                players = ((user == null)
                            ? new List<TSPlayer>() { player }
                            : TShock.Players.Where(p => ((p != null)
                               && p.Active && ((p.Index == player.Index)
                             || ((p.User?.ID ?? -1) == user.ID)))).ToList());
                TShock.Log.ConsoleInfo($"[Player change] {args.Player.Name} " +
                    $"has modified {player.Name}'s inventory.");
                args.Player.PluginInfoMessage($"Saved changes made to {player.Name}'s inventory.");
            }

            #endregion
            // We copy our character to make sure inventory is up to date before sending it.
            args.Player.PlayerData.CopyCharacter(args.Player);
            #region UpdateDatabase

            if (user != null)
            {
                try
                {
                    // Only replace inventory, ignore character looks.
                    PlayerData playerData = args.Player.PlayerData;

                    string query = @"UPDATE tsCharacter
									 SET Health = @0, MaxHealth = @1, Mana = @2, MaxMana = @3,
										 Inventory = @4
									 WHERE Account = @5;";
                    TShock.CharacterDB.database.Query(query, playerData.health, playerData.maxHealth,
                        playerData.mana, playerData.maxMana, string.Join("~", playerData.inventory),
                        info.CopyingUserID);
                    TShock.Log.ConsoleInfo($"[User change] {args.Player.Name} " +
                        $"has modified {user.Name}'s ({user.ID}) inventory.");
                }
                catch (Exception ex)
                {
                    args.Player.PluginErrorMessage("Unable to save the user's inventory.");
                    TShock.Log.Error(ex.ToString());
                    return;
                }
            }

            #endregion
            foreach (TSPlayer plr in players)
            { args.Player.PlayerData.RestoreCharacter(plr); }
        }

        #endregion
        #region Copy

        private static void Copy(CommandArgs args, PlayerInfo info)
        {
            string copyName = string.Join(" ", args.Parameters);
            PlayerData data;
            int userID = -1, playerIndex = -1;
            List<TSPlayer> players = TShock.Utils.FindPlayer(copyName);
            string name;
            #region User

            if (players.Count == 0)
            {
                if (!args.Player.HasPermission(Permissions.InvSeeUser))
                {
                    args.Player.PluginErrorMessage("You can't copy users!");
                    return;
                }

                User user = TShock.Users.GetUserByName(copyName);
                if (user == null)
                {
                    args.Player.PluginErrorMessage($"Invalid player or account '{copyName}'!");
                    return;
                }
                else
                {
                    data = TShock.CharacterDB.GetPlayerData(args.Player, user.ID);
                    userID = user.ID;
                    name = user.Name;
                }
            }

            #endregion
            #region Player

            else if (players.Count > 1)
            {
                TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                return;
            }
            else
            {
                players[0].PlayerData.CopyCharacter(players[0]);
                data = players[0].PlayerData;
                playerIndex = players[0].Index;
                name = players[0].Name;
            }

            #endregion
            #region Copy

            try
            {
                if (data == null)
                {
                    args.Player.PluginErrorMessage($"{name}'s data not found!");
                    return;
                }

                // Setting up backup data
                if (info.Backup == null)
                {
                    info.Backup = new PlayerData(args.Player);
                    info.Backup.CopyCharacter(args.Player);
                }
                
                info.CopyingUserID = userID;
                info.CopyingPlayerIndex = playerIndex;
                data.RestoreCharacter(args.Player);
                args.Player.PluginSuccessMessage($"Copied {name}'s inventory.");
            }
            catch (Exception ex)
            {
                // In case it fails, everything is restored
                info.Restore(args.Player);
                TShock.Log.ConsoleError(ex.ToString());
                args.Player.PluginErrorMessage("Something went wrong... restored your inventory.");
            }

            #endregion
        }

        #endregion
    }
}