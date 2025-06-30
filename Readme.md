# ScraperDotNet

## Overview

ScraperDotNet is a web scraper built on the .NET platform. It's designed to navigate and extract web content for further analysing/processing. The application can save data to a SQL Server database or the file system, and leverages AI/LLM capabilities to analyze and understand website content. 

ScraperDotNet can handle complex websites built with JavaScript rendering, perform scrolling operations to get data that loads on the fly when you scroll down, capture screenshots, and save pages as PDFs. It can also download content that is attached to pages and served from FTP(S). 

It integrates with AI models to verify if the content on the page is valid.

## Installation

### Prerequisites

- .NET 8.0 or later (https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server 2022 or later (if you want to have it locally you can use a docker container https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-deployment?view=sql-server-ver16&pivots=cs1-bash or you can install a free Express edition: https://www.microsoft.com/en-ie/download/details.aspx?id=104781)
- Playwright with Chromium (https://playwright.dev/docs/intro)
- (optional) Ollama (for LLM features, https://ollama.com/download)
- (optional) Python (https://www.python.org/downloads/)

Note: if your machine doesn't have an NVidia graphic card, running an LLM on it might significantly slow it down or not run at all. If this is your case, you can disable using AI by removing the AI section from the appsettings.local.json file

### Setup

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/ScraperDotNet.git
   ```

2. Restore dependencies:
   ```
   dotnet restore
   ```

3. Configure your database connection string in `appsettings.local.json`

4. Build the project:
   ```
   dotnet build
   ```

5. (optional) Download a LLM

   Either (for NVidia graphic cards with at least 12GB VRAM):
   ```
   ollama pull gemma3:12b
   ```

   Or (for CPU inference or NVidia graphic cards with at least 4GB VRAM):
   ```
   ollama pull gemma3:4b
   ```

   Note: skip this step if you don't want to use AI

6. Create/update the database

   The app needs an SqlServer database (tested on SqlServer 2022 - it might work on a lower version but I never tested it). It uses Entity Framework Core to create the database and apply migrations. See https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli

## Running and Configuration

### Configuration

The application uses `appsettings.local.json` for configuration. Key settings include:

- **ConnectionStrings:ScraperContext**: SQL Server connection string
- **PageSaveLocation**: Directory to save downloaded content
- **Ai**: Optional section - if removed, the app won't use AI
- **Ai:OllamaModelName**: Name of the Ollama model to use (default: gemma3:12b). Change this to 'gemma3:4b' if you pulled the smaller model in the setup step.
- **Ai:OllamaEndpoint**: URL where Ollama is running (default: http://localhost:11434)
- **Browser:HideUI**: Whether to run the browser in headless mode or with visible user interface (default: false)
- **WaitForUserActionOnBlockedPages**: If a page contains a captcha or requires a user to log in, you can turn this setting to **true**. In this case, if a login page or captcha is spotted, you will be able to sign in. Otherwise the page will be skipped.

### Running the Application

1. Start the application:
   ```
   dotnet run
   ```

2. The interactive console provides the following commands:
   - A: Auto download all fresh pages
   - 1: Open a page by its Address.Id and save its source
   - U: Add a new address and open it
   - D: Domain download - traverse a website and download content within its domain
   - P: Save current page as PDF
   - S: Make a screenshot of the currently displayed page
   - I: Save the whole page as an image
   - O: Ask an Ollama model about an image
   - X: Extract links from downloaded pages and populate more addresses
   - Y: Extract links from a given page and populate addresses
   - Z: Extract links from a given page and populate addresses within domain
   - Arrow Down: Scroll down slightly
   - Page Down: Scroll down more
   - B: Keep scrolling down until page bottom
   - Esc: Exit

## Features

### Various content download&capture
- Download HTML of a page, including dynamic pages created with javascript 
- Take a screenshot of a visible area of a page
- Take a screenshot of the whole page (including the parts that don't fit into the browser's window)
- Convert a page to PDF
- Download an attachment (content that opens up a window and asks to be saved on a local drive)

### AI Classification
- let AI check if the page is showing valid content, if it's an error page if it is blocked by a captcha or requires a user to sign in

### Crawling
- Extract links from visited pages and carry on scraping
- Option to carry on visiting pages infinitely 
- Option to limit visited pages to a single domain
- Option to visit a single page

### Infinite Loop protection
- Once an address has been visited, it won't be downloaded again
