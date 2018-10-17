﻿#region Using
using Microsoft.Xna.Framework;
using TShockAPI;
#endregion
namespace InvSee.Extensions
{
	public static class TSPlayerExtensions
	{
		#region GetPlayerInfo

		public static PlayerInfo GetPlayerInfo(this TSPlayer player)
		{
			if (!player.ContainsData(PlayerInfo.KEY))
			{ player.SetData(PlayerInfo.KEY, new PlayerInfo()); }
			return player.GetData<PlayerInfo>(PlayerInfo.KEY);
		}

		#endregion
		#region PluginMessage

		public static void PluginMessage(this TSPlayer player, string message, Color color) =>
			player.SendMessage(PMain.Tag + message, color);
		public static void PluginErrorMessage(this TSPlayer player, string message) =>
			player.PluginMessage(message, Color.Red);
		public static void PluginInfoMessage(this TSPlayer player, string message) =>
			player.PluginMessage(message, Color.Yellow);
		public static void PluginSuccessMessage(this TSPlayer player, string message) =>
			player.PluginMessage(message, Color.Green);
		public static void PluginWarningMessage(this TSPlayer player, string message) =>
			player.PluginMessage(message, Color.OrangeRed);

		#endregion
	}
}