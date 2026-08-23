# PlayTimeStats - PotatoVN Plugin

A play time statistics plugin for PotatoVN, inspired by mobile "Screen Time" dashboards.

## Features

- **Time period switching**: Day / Week / Month tabs to switch data granularity
- **Horizontal bar chart**: Visualizes play time distribution across time segments
  - Day view: last 7 days (one bar per day)
  - Week view: last 4 weeks (one bar per week)
  - Month view: last 6 months (one bar per month)
- **Game ranking**: Lists all games sorted by cumulative play time, with icon, name, total time, and percentage
- **Data linkage**: Click a bar to filter the ranking list to that time segment
- **Multi-language**: Supports Chinese (Simplified), English, and Japanese
- **Dark theme**: Uses PotatoVN's theme resources for a consistent dark tech aesthetic

## Data Source

The plugin reads play time data from `Galgame.PlayedTime` (a `Dictionary<string, int>` mapping date strings to minutes) and `Galgame.TotalPlayTime` provided by the PotatoVN host application.

## Development

See the [PotatoVN plugin documentation](https://potatovn.net/development/client-plugin/quick-start.html) for more information about plugin development.