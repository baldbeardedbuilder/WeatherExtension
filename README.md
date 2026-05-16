# Weather Extension for Microsoft Command Palette

[<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>](https://get.microsoft.com/installer/download/9N01D6BS9V98?referrer=appbadge) ![WinGet Package Version](https://img.shields.io/winget/v/BaldBeardedBuilder.WeatherforCommandPalette)


A weather extension for [Microsoft Command Palette](https://github.com/microsoft/PowerToys)
(PowerToys) that provides current conditions, hourly forecasts, and multi-day
forecasts right from your desktop.

<img width="1213" height="738" alt="image" src="https://github.com/user-attachments/assets/ad98a1b4-8c14-41c5-a397-4a8b3ff3c662" />

## Features

- **Current Weather** — Temperature, feels like, humidity, wind speed, and
  conditions for any location worldwide
- **Hourly Forecast** — View the remaining hours of the day with temperature,
  precipitation chance, and conditions
- **3-Day Forecast** — Quick glance at upcoming weather with highs, lows, and
  conditions
- **Dock Bands** — Pin your favorite locations to the Command Palette dock for
  at-a-glance weather cards
- **Postal Code Search** — Search by postal/ZIP code or city name
- **Configurable Units** — Fahrenheit/Celsius, mph/km/h, and adjustable update
  intervals

## Screenshots

### Search & Current Conditions

<img width="1209" height="739" alt="image" src="https://github.com/user-attachments/assets/4bc5ca9f-9aa3-47d0-91c5-24e4ef04f31e" />

### Dock Band Card

<img width="1112" height="669" alt="image" src="https://github.com/user-attachments/assets/57d4aba6-f788-4a79-b730-82ece6be2148" />

### Hourly Forecast

<img width="1206" height="743" alt="image" src="https://github.com/user-attachments/assets/3c6eaf9e-1761-4235-b240-8fd2830357cf" />

### Settings

<img width="1205" height="738" alt="image" src="https://github.com/user-attachments/assets/02f4d072-38cc-425a-800d-5e0ad14aaf33" />

## Installation

You can install Weather for Command Palette via:

- [Microsoft Store](https://apps.microsoft.com/detail/9N01D6BS9V98)
- Winget via `winget install BaldBeardedBuilder.WeatherforCommandPalette`
- [GitHub Releases](https://github.com/michaeljolley/WeatherExtension/releases)

---

## Getting Started

### Prerequisites

- Windows 10/11
- [PowerToys](https://github.com/microsoft/PowerToys) with Command Palette
  enabled
- .NET 9.0 SDK (for building from source)

## Data Sources

- Weather data provided by [Open-Meteo](https://open-meteo.com/) (free, no API
  key required)
- Geocoding by [Open-Meteo Geocoding API](https://geocoding-api.open-meteo.com/)
  with [Nominatim](https://nominatim.openstreetmap.org/) fallback for postal
  codes

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file
for details.
