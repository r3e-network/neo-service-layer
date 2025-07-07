# Web Controllers

This directory should contain MVC controllers for web pages, not API controllers.

## Current Status

⚠️ **Cleanup Required**: This directory currently contains many API controllers that should be in the API project instead.

## Correct Structure

The Web project should only contain:
- `HomeController.cs` - For home page and general pages
- `ServicesController.cs` - For service demo pages
- `DocumentationController.cs` - For documentation pages

## API Controllers

All API controllers should be in the `NeoServiceLayer.Api` project at:
`/src/Api/NeoServiceLayer.Api/Controllers/`

## Migration Plan

1. Remove all `[ApiController]` decorated controllers from this directory
2. Ensure all API functionality is available in the API project
3. Keep only MVC controllers for rendering web pages
4. Update routes to use the API project endpoints

## Note

The Web project is for the user interface and should make HTTP calls to the API project for data, not implement API endpoints directly.