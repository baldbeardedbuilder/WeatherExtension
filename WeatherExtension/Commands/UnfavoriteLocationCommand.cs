// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Commands;

internal sealed partial class UnfavoriteLocationCommand : InvokableCommand
{
	private readonly GeocodingResult _location;
	private readonly FavoritesManager _favoritesManager;

	public UnfavoriteLocationCommand(GeocodingResult location, FavoritesManager favoritesManager)
	{
		_location = location;
		_favoritesManager = favoritesManager;
		Name = Resources.unfavorite_command_name;
		Icon = new IconInfo("⭐");
	}

	public override string Id => "com.baldbeardedbuilder.cmdpal.weather.unfavorite";

	public override ICommandResult Invoke()
	{
		_favoritesManager.Unfavorite(_location);
		return CommandResult.KeepOpen();
	}
}
