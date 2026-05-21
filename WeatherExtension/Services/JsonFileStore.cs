// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BaldBeardedBuilder.WeatherExtension;
using Microsoft.CommandPalette.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal class JsonFileStore<T> where T : class
{
	private readonly string _filePath;
	private readonly JsonTypeInfo<List<T>> _jsonTypeInfo;
	private readonly string _entityName;
	private List<T> _items = [];
	private readonly Lock _sync = new();

	public JsonFileStore(string filePath, JsonTypeInfo<List<T>> jsonTypeInfo, string entityName)
	{
		_filePath = filePath;
		_jsonTypeInfo = jsonTypeInfo;
		_entityName = entityName;
		LoadFromFile();
	}

	public List<T> GetAll()
	{
		lock (_sync)
		{
			return new List<T>(_items);
		}
	}

	public bool Any(Func<T, bool> predicate)
	{
		lock (_sync)
		{
			return _items.Any(predicate);
		}
	}

	public bool Add(T item, Func<T, bool> duplicateCheck)
	{
		lock (_sync)
		{
			if (_items.Any(duplicateCheck))
			{
				return false;
			}

			_items.Add(item);
			SaveToFile();
			return true;
		}
	}

	public bool Remove(Func<T, bool> predicate)
	{
		lock (_sync)
		{
			var toRemove = _items.FirstOrDefault(predicate);
			if (toRemove == null)
			{
				return false;
			}

			_items.Remove(toRemove);
			SaveToFile();
			return true;
		}
	}

	/// <summary>
	/// Replaces the entire stored list with the supplied items. Returns
	/// true when the contents differ from what was stored, so callers can
	/// decide whether to fire change notifications.
	/// </summary>
	public bool ReplaceAll(IEnumerable<T> items)
	{
		lock (_sync)
		{
			var newList = items.ToList();
			if (newList.Count == _items.Count && _items.SequenceEqual(newList))
			{
				return false;
			}

			_items = newList;
			SaveToFile();
			return true;
		}
	}

	private void LoadFromFile()
	{
		try
		{
			if (File.Exists(_filePath))
			{
				var json = File.ReadAllText(_filePath);
				var items = JsonSerializer.Deserialize(json, _jsonTypeInfo);
				if (items != null)
				{
					_items = items;
				}
			}
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to load {_entityName}: {ex.Message}");
		}
	}

	private void SaveToFile()
	{
		try
		{
			var json = JsonSerializer.Serialize(_items, _jsonTypeInfo);
			File.WriteAllText(_filePath, json);
		}
		catch (Exception ex)
		{
			WeatherLogger.LogToHost(
				MessageState.Error,
				$"Failed to save {_entityName}: {ex.Message}");
		}
	}
}
