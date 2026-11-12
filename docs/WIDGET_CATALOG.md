# uWidgets Built-in Widget Catalog

uWidgets ships **11 modular widget assemblies** providing **26 distinct widget views** across time, productivity, weather, hardware monitoring, developer workflows, and finance.

---

## 1. Calendar (`Calendar.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **Date** | Current day of week, date, and month summary. | Small, Medium, Large |
| **Month** | Full monthly calendar grid with configurable first day of week. | Small, Large |
| **Countdown** | Days/hours countdown to a custom target event/date. | Small, Medium, Large |

## 2. Clock (`Clock.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **AnalogI** | Minimalist analog watch face with optional seconds hand. | Small, Large |
| **AnalogII** | Classic dial analog clock with hour markers. | Small, Large |
| **AnalogIII** | Modern geometric analog clock style. | Small, Large |
| **Digital** | Clean digital clock with 12/24-hour and seconds options. | Small, Medium, Large |
| **World** | Multi-city / timezone comparison clock. | Small, Medium, Large |
| **Pomodoro** | Interactive focus/break timer with session presets. | Small, Medium, Large |
| **Stopwatch** | Precision desktop stopwatch with start/pause/reset controls. | Small, Medium |

## 3. Development (`Development.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **Docker** | Displays Docker containers and running status via Docker Engine. | Medium, Large |
| **DevApps** | Detects active local development processes and tools. | Small, Medium, Large |
| **GitWorkingTree** | Shows branch, ahead/behind counts, working tree status, and quick-launch buttons for Editor, Terminal, and Explorer. | Small, Medium, Large |

## 4. Finance (`Finance.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **Crypto** | Live cryptocurrency prices and 24h change sparklines. | Small, Medium, Large |
| **Stocks** | Equity ticker prices and market movement indicators. | Small, Medium, Large |
| **Converter** | Interactive currency conversion calculator. | Medium, Large |

## 5. Media (`Media.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **NowPlaying** | Integrates with Windows media sessions to display track info, album art, and playback controls. | Small, Medium, Large |

## 6. Monitor (`Monitor.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **SingleMetric** | Dedicated gauge for CPU, RAM, Disk, Network, or Battery. | Small, Medium |
| **Dashboard** | Combined multi-metric system performance overview. | Medium, Large |
| **DeviceBatteries** | Tracks connected Bluetooth/peripheral device battery percentages with color-coded status and hover tooltips. | Small, Medium, Large |

## 7. Notes (`Notes.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **Note** | Desktop sticky note with editable title, timestamp, character count (Large), and inline editing. | Tiny, Small, Wide, Large |

## 8. Photos (`Photos.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **PhotoFrame** | Displays a chosen image or auto-rotating folder slideshow with optional caption overlay and clear error/empty guidance. | Tiny, Small, Wide, Large |

## 9. Reminders (`Reminders.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **List** | Interactive to-do checklist with custom list title, pending counters, completion toggles, and inline creation. | Tiny, Small, Wide, Large |

## 10. Shortcuts (`Shortcuts.dll`)
| SubType | Description | Sizes |
| --- | --- | --- |
| **Launcher** | Quick-launch grid of applications, URLs, or folders with custom icons stored in `CustomIcons`. | Small, Medium, Large |

## 11. Weather (`Weather.dll`)
Powered by Open-Meteo with offline cache fallback and retry cooldown handling:

| SubType | Description | Sizes |
| --- | --- | --- |
| **Forecast** | Current temperature, condition, hourly strip, and 7-day forecast with offline cache indicator & Retry action. | Tiny, Small, Wide, Large |
| **Temperature** | Ambient temperature range metric card. | Small |
| **UVIndex** | Current UV index gauge. | Small |
| **SunriseSunset** | Next sunrise or sunset time indicator. | Small |
| **Pressure** | Barometric surface pressure gauge. | Small |
| **AirQuality** | European AQI air quality index gauge. | Small |
