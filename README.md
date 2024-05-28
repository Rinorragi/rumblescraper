# Rumble scraper

Small utility to scrape guild details from warcraftrumble.gg. Zero guarantees for working.

## Setup

You need .NET SDK to run this. [Download .NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

## How to run

On the directory where the script is run following with 123456789 of being your guild id.

```powershell
dotnet fsi .\scrape_goblins.fsx 123456789
```

## How to read data

The software creates `/data` directory and stores the results in txt file. You can run e.g. `cat .\data\123456789.txt` to read the data or open it with text editor.