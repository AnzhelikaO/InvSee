#region Using
using Terraria;
using TShockAPI;
#endregion
namespace InvSee
{
	public class PlayerInfo
	{
		public const string KEY = "InvSee_Data";

		public PlayerData Backup { get; set; }
		public int CopyingUserID { get; set; }
		public int CopyingPlayerIndex { get; set; }
		#region Constructor

		public PlayerInfo()
		{
			Backup = null;
			CopyingUserID = CopyingPlayerIndex = -1;
		}

		#endregion
		#region Restore

		public bool Restore(bool ssc, TSPlayer player)
		{
			if (Backup == null)
                return false;
            if (!ssc)
            {
                Main.ServerSideCharacter = true;
                player.SendData(PacketTypes.WorldInfo);
            }
            Backup.RestoreCharacter(player);
            if (!ssc)
            {
                Main.ServerSideCharacter = false;
                player.SendData(PacketTypes.WorldInfo);
            }
            Backup = null;
			CopyingUserID = CopyingPlayerIndex = -1;
			return true;
		}

		#endregion
	}
}