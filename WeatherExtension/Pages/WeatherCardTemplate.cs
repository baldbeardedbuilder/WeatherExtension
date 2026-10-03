// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CmdPal.Ext.Weather.Pages;

internal static class WeatherCardTemplate
{
	internal const string Json = """
	{
	  "type": "AdaptiveCard",
	  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
	  "version": "1.5",
	  "body": [
	    { "type": "TextBlock", "text": "${locationName}", "size": "large", "weight": "bolder", "wrap": true },
	    { "type": "TextBlock", "text": "${status}", "$when": "${status != ''}", "wrap": true },
	    {
	      "type": "ColumnSet", "spacing": "medium",
	      "columns": [
	        {
	          "type": "Column", "width": "3", "verticalContentAlignment": "center",
	          "items": [
	            {
	              "type": "ColumnSet",
	              "columns": [
	                {
	                  "type": "Column", "width": "auto", "verticalContentAlignment": "center",
	                  "$when": "${currentImage != ''}",
	                  "items": [{ "type": "Image", "url": "${currentImage}", "$when": "${currentImage != ''}", "width": "64px", "height": "64px", "altText": "${currentCondition}" }]
	                },
	                {
	                  "type": "Column", "width": "stretch",
	                  "items": [
	                    { "type": "TextBlock", "text": "${currentTemp}", "size": "extraLarge", "weight": "bolder", "wrap": true },
	                    { "type": "TextBlock", "text": "${currentCondition}", "spacing": "small", "wrap": true },
	                    { "type": "TextBlock", "text": "${labels.feelsLike} ${feelsLike}", "size": "small", "spacing": "small", "isSubtle": true, "wrap": true }
	                  ]
	                }
	              ]
	            },
	            { "type": "TextBlock", "text": "${labels.highLow} ${todayHighLow}", "size": "small", "isSubtle": true, "wrap": true },
	            { "type": "TextBlock", "text": "${labels.wind} ${wind}", "size": "small", "spacing": "small", "isSubtle": true, "wrap": true },
	            { "type": "TextBlock", "text": "${labels.humidity} ${humidity}", "size": "small", "spacing": "small", "isSubtle": true, "wrap": true }
	          ]
	        },
	        {
	          "type": "Column", "width": "4", "spacing": "medium", "style": "emphasis", "verticalContentAlignment": "center",
	          "items": [
	            {
	              "type": "ColumnSet",
	              "columns": [
	                { "type": "Column", "width": "stretch", "items": [{ "type": "TextBlock", "text": "${labels.daylight}", "weight": "bolder", "wrap": true }] },
	                { "type": "Column", "width": "auto", "items": [{ "type": "TextBlock", "text": "${daylight.duration}", "size": "small", "horizontalAlignment": "right", "isSubtle": true, "wrap": true }] }
	              ]
	            },
	            { "type": "Image", "url": "${daylight.image}", "$when": "${daylight.image != ''}", "size": "stretch", "altText": "${daylight.altText}" },
	            { "type": "TextBlock", "text": "${daylight.status}", "size": "small", "isSubtle": true, "wrap": true },
	            {
	              "type": "ColumnSet", "spacing": "small",
	              "columns": [
	                {
	                  "type": "Column", "width": "stretch",
	                  "items": [
	                    { "type": "TextBlock", "text": "${daylight.sunrise}", "size": "small", "weight": "bolder", "wrap": true },
	                    { "type": "TextBlock", "text": "${labels.sunrise}", "size": "small", "spacing": "none", "isSubtle": true, "wrap": true }
	                  ]
	                },
	                {
	                  "type": "Column", "width": "stretch",
	                  "items": [
	                    { "type": "TextBlock", "text": "${daylight.localTime}", "size": "small", "horizontalAlignment": "center", "wrap": true },
	                    { "type": "TextBlock", "text": "${labels.localTime}", "size": "small", "spacing": "none", "isSubtle": true, "horizontalAlignment": "center", "wrap": true }
	                  ]
	                },
	                {
	                  "type": "Column", "width": "stretch",
	                  "items": [
	                    { "type": "TextBlock", "text": "${daylight.sunset}", "size": "small", "weight": "bolder", "horizontalAlignment": "right", "wrap": true },
	                    { "type": "TextBlock", "text": "${labels.sunset}", "size": "small", "spacing": "none", "isSubtle": true, "horizontalAlignment": "right", "wrap": true }
	                  ]
	                }
	              ]
	            }
	          ]
	        }
	      ]
	    },
	    { "type": "TextBlock", "text": "${labels.hours}", "weight": "bolder", "spacing": "medium", "wrap": true },
	    { "type": "TextBlock", "text": "${labels.forecastUnavailable}", "$when": "${count(hours) == 0}", "isSubtle": true, "wrap": true },
	    {
	      "type": "ColumnSet",
	      "$when": "${count(hours) > 0}",
	      "columns": [
	        {
	          "type": "Column", "width": "stretch", "$data": "${hours}",
	          "items": [
	            { "type": "TextBlock", "text": "${time}", "weight": "bolder", "horizontalAlignment": "center", "wrap": true },
	            { "type": "Image", "url": "${image}", "$when": "${image != ''}", "width": "32px", "height": "32px", "altText": "${condition}", "horizontalAlignment": "center", "spacing": "small" },
	            { "type": "TextBlock", "text": "${temperature}", "weight": "bolder", "horizontalAlignment": "center", "spacing": "small", "wrap": true },
	            { "type": "TextBlock", "text": "${rain}", "size": "small", "isSubtle": true, "horizontalAlignment": "center", "spacing": "small", "wrap": true }
	          ]
	        }
	      ]
	    },
	    { "type": "TextBlock", "text": "${labels.days}", "weight": "bolder", "spacing": "medium", "wrap": true },
	    { "type": "TextBlock", "text": "${labels.forecastUnavailable}", "$when": "${count(days) == 0}", "isSubtle": true, "wrap": true },
	    {
	      "type": "Container",
	      "items": [
	        {
	          "type": "ColumnSet", "$data": "${days}",
	          "columns": [
	            { "type": "Column", "width": "auto", "verticalContentAlignment": "center", "items": [{ "type": "TextBlock", "text": "${time}", "weight": "bolder", "wrap": true }] },
	            { "type": "Column", "width": "auto", "spacing": "small", "verticalContentAlignment": "center", "$when": "${image != ''}", "items": [{ "type": "Image", "url": "${image}", "$when": "${image != ''}", "width": "24px", "height": "24px", "altText": "${condition}" }] },
	            { "type": "Column", "width": "stretch", "spacing": "small", "verticalContentAlignment": "center", "items": [{ "type": "TextBlock", "text": "${condition}", "size": "small", "wrap": true }] },
	            { "type": "Column", "width": "auto", "verticalContentAlignment": "center", "items": [{ "type": "TextBlock", "text": "${rain}", "size": "small", "isSubtle": true, "wrap": true }] },
	            { "type": "Column", "width": "auto", "spacing": "medium", "verticalContentAlignment": "center", "items": [{ "type": "TextBlock", "text": "${temperature}", "size": "small", "horizontalAlignment": "right", "wrap": true }] }
	          ]
	        }
	      ]
	    }
	  ]
	}
	""";
}
