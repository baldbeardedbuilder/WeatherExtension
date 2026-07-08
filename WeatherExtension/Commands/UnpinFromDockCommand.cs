// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Commands;

internal sealed partial class UnpinFromDockCommand : InvokableCommand
{
	private readonly GeocodingResult _location;
	private readonly DockPinsManager _dockPinsManager;

	public UnpinFromDockCommand(GeocodingResult location, DockPinsManager dockPinsManager)
	{
		_location = location;
		_dockPinsManager = dockPinsManager;
		Name = Resources.unpin_from_dock_command_name;
		Icon = new IconInfo("\uE77A"); // Unpin icon
	}

	public override string Id => "com.baldbeardedbuilder.cmdpal.weather.unpinfromdock";

	public override ICommandResult Invoke()
	{
		_dockPinsManager.UnpinFromDock(_location);
		return CommandResult.KeepOpen();
	}
}
