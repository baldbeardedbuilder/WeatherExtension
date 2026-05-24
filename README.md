# Weather Extension for Microsoft Command Palette
<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-8-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

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

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/eneshenderson"><img src="https://avatars.githubusercontent.com/u/49413090?v=4?s=100" width="100px;" alt="Enes Hikmet Kayım"/><br /><sub><b>Enes Hikmet Kayım</b></sub></a><br /><a href="https://github.com/michaeljolley/WeatherExtension/commits?author=eneshenderson" title="Code">💻</a> <a href="https://github.com/michaeljolley/WeatherExtension/commits?author=eneshenderson" title="Tests">⚠️</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://baldbeardedbuilder.com/"><img src="https://avatars.githubusercontent.com/u/1228996?v=4?s=100" width="100px;" alt="Michael Jolley"/><br /><sub><b>Michael Jolley</b></sub></a><br /><a href="https://github.com/michaeljolley/WeatherExtension/commits?author=michaeljolley" title="Code">💻</a> <a href="https://github.com/michaeljolley/WeatherExtension/commits?author=michaeljolley" title="Tests">⚠️</a> <a href="#design-michaeljolley" title="Design">🎨</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/damianoschal-star"><img src="https://avatars.githubusercontent.com/u/275190590?v=4?s=100" width="100px;" alt="damianoschal-star"/><br /><sub><b>damianoschal-star</b></sub></a><br /><a href="https://github.com/michaeljolley/WeatherExtension/issues?q=author%3Adamianoschal-star" title="Bug reports">🐛</a> <a href="#userTesting-damianoschal-star" title="User Testing">📓</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Xqzmoi"><img src="https://avatars.githubusercontent.com/u/283920763?v=4?s=100" width="100px;" alt="Xqzmoi"/><br /><sub><b>Xqzmoi</b></sub></a><br /><a href="https://github.com/michaeljolley/WeatherExtension/issues?q=author%3AXqzmoi" title="Bug reports">🐛</a> <a href="#userTesting-Xqzmoi" title="User Testing">📓</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Codalexandre69200"><img src="https://avatars.githubusercontent.com/u/254201681?v=4?s=100" width="100px;" alt="Codalexandre69200"/><br /><sub><b>Codalexandre69200</b></sub></a><br /><a href="https://github.com/michaeljolley/WeatherExtension/issues?q=author%3ACodalexandre69200" title="Bug reports">🐛</a> <a href="#userTesting-Codalexandre69200" title="User Testing">📓</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/nyhtz"><img src="https://avatars.githubusercontent.com/u/69047113?v=4?s=100" width="100px;" alt="nyhtz"/><br /><sub><b>nyhtz</b></sub></a><br /><a href="https://github.com/michaeljolley/WeatherExtension/issues?q=author%3Anyhtz" title="Bug reports">🐛</a> <a href="#userTesting-nyhtz" title="User Testing">📓</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://darkpeak.dev"><img src="https://avatars.githubusercontent.com/u/2070987?v=4?s=100" width="100px;" alt="Andrew "AJ" Jones"/><br /><sub><b>Andrew "AJ" Jones</b></sub></a><br /><a href="https://github.com/michaeljolley/WeatherExtension/commits?author=andy-c-jones" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/niels9001"><img src="https://avatars.githubusercontent.com/u/9866362?v=4?s=100" width="100px;" alt="Niels Laute"/><br /><sub><b>Niels Laute</b></sub></a><br /><a href="#design-niels9001" title="Design">🎨</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
