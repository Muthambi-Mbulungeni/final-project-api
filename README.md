# Strategies Migration Platform

This repository contains the core application suite for the Strategies Migration Platform. It encompasses the .NET 10 backend API, the web client and the mobile application.

## Repository Structure

* Strategies.Api: Core backend service handling authentication, case management, document storage and AI triage.
* strategiesweb: Web interface for clients and staff.
* strategiesmobile: Mobile client application.

## Prerequisites

You must have the following installed to work on this project.

* .NET 10 SDK
* PostgreSQL database
* Node.js

## Backend Setup

Navigate to the Strategies.Api directory. 

Configure your local environment properly. Add a valid PostgreSQL connection string to the DefaultConnection property in your appsettings.json file. You must also supply a secure JWT key to enable authentication. Do not commit your secret keys to version control.

Execute the entity framework migrations to generate the database schema. Run dotnet ef database update from the API directory.

Start the application by running dotnet run.

## Quality Assurance

The Strategies.Api.Tests directory contains the automated test suite. Run dotnet test to execute it. 

You are required to verify your work. All tests must pass before any code is considered for integration. Broken builds and untested logic will be rejected.
