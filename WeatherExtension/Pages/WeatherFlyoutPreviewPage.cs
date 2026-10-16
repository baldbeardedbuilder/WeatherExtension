// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CmdPal.Ext.Weather.Models;
using Microsoft.CmdPal.Ext.Weather.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal sealed partial class WeatherFlyoutPreviewPage : ContentPage
{
    private readonly PreviewForm _form;

    public WeatherFlyoutPreviewPage(WeatherSettingsManager settings, WeatherFlyoutView view)
    {
        Id = $"com.baldbeardedbuilder.cmdpal.weather.flyout.preview.{view}";
        Name = WeatherFlyoutCatalog.Name(view);
        Title = Name;
        Icon = WeatherFlyoutCatalog.PreviewIcon(view);
        _form = new PreviewForm(settings, view);
    }

    public override IContent[] GetContent()
    {
        _form.Refresh();
        return [_form];
    }

    internal sealed partial class PreviewForm : FormContent
    {
        private readonly WeatherSettingsManager _settings;
        private readonly WeatherFlyoutView _view;

        public PreviewForm(WeatherSettingsManager settings, WeatherFlyoutView view)
        {
            _settings = settings;
            _view = view;
        }

        internal void Refresh()
        {
            var body = new JsonArray(FlyoutCard.Text(WeatherFlyoutCatalog.Name(_view), "large"),
                FlyoutCard.Text(WeatherFlyoutCatalog.Description(_view)),
                FlyoutCard.Text(FlyoutText.Get("Scope")),
                FlyoutCard.Text(FlyoutText.Format("CurrentDefault", WeatherFlyoutCatalog.Name(_settings.FlyoutView)), "small", true));
            try
            {
                var preview = Convert.ToBase64String(File.ReadAllBytes(WeatherFlyoutCatalog.PreviewPath(_view)));
                body.Add(FlyoutCard.Image($"data:image/png;base64,{preview}", WeatherFlyoutCatalog.Description(_view)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                WeatherLogger.LogToHost(MessageState.Error, $"Flyout preview load failed: {ex.Message}");
                body.Add(FlyoutCard.Text(FlyoutText.Get("PreviewUnavailable")));
            }

            body.Add(FlyoutCard.Text(FlyoutText.Get("HostSizing"), "small", true));
            TemplateJson = FlyoutCard.Document(body, new JsonArray(Action("apply", "UseView"), Action("cancel", "Cancel")));
        }

        public override CommandResult SubmitForm(string inputs, string data)
        {
            string? action;
            try
            {
                action = (JsonNode.Parse(data) as JsonObject)?["action"]?.GetValue<string>();
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                WeatherLogger.LogToHost(MessageState.Error, $"Invalid flyout selection action: {ex.Message}");
                return CommandResult.ShowToast(FlyoutText.Get("SaveFailed"));
            }

            if (action == "cancel")
            {
                return CommandResult.GoBack();
            }

            if (action != "apply")
            {
                WeatherLogger.LogToHost(MessageState.Error, "Unknown flyout selection action.");
                return CommandResult.ShowToast(FlyoutText.Get("SaveFailed"));
            }

            try
            {
                _settings.SetFlyoutView(_view);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                WeatherLogger.LogToHost(MessageState.Error, $"Flyout selection save failed: {ex.Message}");
                return CommandResult.ShowToast(FlyoutText.Get("SaveFailed"));
            }

            Refresh();
            return CommandResult.ShowToast(FlyoutText.Format("Saved", WeatherFlyoutCatalog.Name(_view)));
        }

        private static JsonObject Action(string id, string label) => new()
        {
            ["type"] = "Action.Submit", ["title"] = FlyoutText.Get(label),
            ["data"] = new JsonObject { ["action"] = id },
        };
    }
}
