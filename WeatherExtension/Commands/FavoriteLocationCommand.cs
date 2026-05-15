// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Commands;

internal sealed partial class FavoriteLocationCommand : InvokableCommand
{
	private readonly GeocodingResult _location;
	private readonly FavoritesManager _favoritesManager;

	public FavoriteLocationCommand(GeocodingResult location, FavoritesManager favoritesManager)
	{
		_location = location;
		_favoritesManager = favoritesManager;
		Name = Resources.favorite_command_name;
		Icon = new IconInfo("⭐");
	}

	public override string Id => "com.baldbeardedbuilder.cmdpal.weather.favorite";

	public override ICommandResult Invoke()
	{
		_favoritesManager.Favorite(_location);
		return CommandResult.KeepOpen();
	}
}
