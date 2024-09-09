namespace Services.NhlData.Mappers
{
	public static class MapPlayerBioResponseToName
	{
		/// <summary>
		/// Gets the name of a player from the player bio response
		/// </summary>
		/// <param name="playerBioResponse">The player bio response</param>
		/// <returns>The players name</returns>
		public static string Map(dynamic playerBioResponse)
		{
			var name = (string)playerBioResponse.data[0].skaterFullName;
			if (name == null || name == "")
				name = (string)playerBioResponse.data[0].goalieFullName;

			return name;
		}
	}
}
