#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using InvSee.Extensions;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
#endregion
namespace InvSee
{
    internal class Commands
    {
        private static readonly string _cp = TShockAPI.Commands.Specifier;
        #region DoInvSee

        public static void DoInvSee(CommandArgs args)
        {
            bool ssc = Main.ServerSideCharacter;
            PlayerInfo info = args.Player.GetPlayerInfo();
            #region Restore

            if (args.Parameters.Count < 1)
            {
                Restore(ssc, args, info);
                return;
            }

            #endregion
            #region Save

            string _0 = args.Parameters[0].ToLower();
            if ((_0 == "-s") || (_0 == "-save"))
            {
                Save(ssc, args, info);
                return;
            }

            #endregion
            Copy(ssc, args, info);
        }

        #endregion
        #region Restore

        private static void Restore(bool ssc, CommandArgs args, PlayerInfo info)
        {
            if (args.Player.Dead)
            {
                args.Player.PluginErrorMessage("You cannot restore your inventory while dead.");
                return;
            }
            if (info.Restore(ssc, args.Player))
                args.Player.PluginErrorMessage("Inventory has been restored.");
            else
            {
                args.Player.PluginInfoMessage("You are currently not seeing anyone's inventory.");
                args.Player.PluginInfoMessage($"Use '{_cp}invsee <player name>' to begin.");
            }
        }

        #endregion
        #region Save

        private static void Save(bool ssc, CommandArgs args, PlayerInfo info)
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
            #region User

            if (info.CopyingUserID != -1)
            {
                UserAccount user = TShock.UserAccounts.GetUserAccountByID(info.CopyingUserID);
                if (user == null)
                {
                    args.Player.PluginErrorMessage("Invalid user!");
                    return;
                }
                // We copy our character to make sure inventory is up to date before sending it.
                args.Player.PlayerData.CopyCharacter(args.Player);
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
                        $"has modified {user.Name}'s inventory.");
                    args.Player.PluginInfoMessage($"Saved changes made to {user.Name}'s inventory.");
                }
                catch (Exception ex)
                {
                    args.Player.PluginErrorMessage("Unable to save the user's inventory.");
                    TShock.Log.Error(ex.ToString());
                    return;
                }
            }

            #endregion
            #region Player

            else
            {
                TSPlayer player = TShock.Players.ElementAtOrDefault(info.CopyingPlayerIndex);
                if (player == null)
                {
                    args.Player.PluginErrorMessage("Invalid player!");
                    return;
                }
                args.Player.PlayerData.CopyCharacter(args.Player);
                if (!ssc)
                {
                    Main.ServerSideCharacter = true;
                    player.SendData(PacketTypes.WorldInfo);
                }
                args.Player.PlayerData.RestoreCharacter(player);
                if (!ssc)
                {
                    Main.ServerSideCharacter = false;
                    player.SendData(PacketTypes.WorldInfo);
                }
                TShock.Log.ConsoleInfo($"[Player change] {args.Player.Name} " +
                    $"has modified {player.Name}'s inventory.");
                args.Player.PluginInfoMessage($"Saved changes made to {player.Name}'s inventory.");
            }

            #endregion
        }

        #endregion
        #region Copy

        private static void Copy(bool ssc, CommandArgs args, PlayerInfo info)
        {
            if (args.Player.PlayerData == null)
                args.Player.PlayerData = new PlayerData(args.Player);

            string name = string.Join(" ", args.Parameters);
            PlayerData data;
            int userID = -1, playerIndex = -1;
            List<TSPlayer> players = TSPlayer.FindByNameOrID(name);
            #region User

            if (players.Count == 0)
            {
                if (!args.Player.HasPermission(Permissions.InvSeeUser))
                {
                    args.Player.PluginErrorMessage("You can't copy users!");
                    return;
                }

                UserAccount user = TShock.UserAccounts.GetUserAccountByName(name);
                if (user == null)
                {
                    args.Player.PluginErrorMessage($"Invalid player or account '{name}'!");
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
                args.Player.SendMultipleMatchError(players.Select(p => p.Name));
                return;
            }
            else
            {
                if (players[0].PlayerData == null)
                    players[0].PlayerData = new PlayerData(players[0]);
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
                if (!ssc)
                {
                    Main.ServerSideCharacter = true;
                    args.Player.SendData(PacketTypes.WorldInfo);
                }
                data.RestoreCharacter(args.Player);
                if (!ssc)
                {
                    Main.ServerSideCharacter = false;
                    args.Player.SendData(PacketTypes.WorldInfo);
                }
                args.Player.PluginSuccessMessage($"Copied {name}'s inventory.");
            }
            catch (Exception ex)
            {
                // In case it fails, everything is restored
                info.Restore(ssc, args.Player);
                TShock.Log.ConsoleError(ex.ToString());
                args.Player.PluginErrorMessage("Something went wrong... restored your inventory.");
            }

            #endregion
        }

        #endregion
    }
}