# opengamedata-unity
Unity package for logging with OpenGameData servers.

## Setup
If there isn't one already, create a `Submodules` directory within the project's root. 

To add the package to a project, run the following command from within the `Submodules` directory:

`$ git submodule add https://github.com/fielddaylab/opengamedata-unity.git`

Then, add a reference to `FieldDay` in the assembly definition file for any namespace where logging functions will be used. 

Note that this submodule includes the [BeauUtil](https://github.com/BeauPrime/BeauUtil) library as a dependency.

## Contents

This package includes the following classes:

- `SimpleLog`: handles communication with the database using `UnityWebRequest`
- `SimpleLogUtils`: contains various helper functions for building data strings and handling cookies
- `LogEvent`: wrapper class for the data objects that get sent through `SimpleLog`
- `CookieUtils`: a JavaScript plugin for handling cookies if the project is built for WebGL (found in `~/Assets/Plugins`)

## Logging

All communication with the database is handled through the `SimpleLog` class. An instance of `SimpleLog` can be created with the following format:

`SimpleLog slog = new SimpleLog(myAppId, myAppVersion, myQueryParams)`

- `myAppId`: an identifier for this app within the database (ex. "AQUALAB")
- `myAppVersion`: the current version of the app for all logging events
- `myQueryParams`: if specified, finds a given player id

In order to log an event, the data must be contained by a `LogEvent` object. The `LogEvent` constructor takes the following arguments:

- `data`: a `<string, string>` dictionary of data values for the given event
- `category`: an enum value to represent the given event type

`LogEvent` will format the data into a dictionary with the following key/value pairs:
- `event`: the event type, in this case will default to "CUSTOM"
- `event_custom`: the enum category for the logged event
- `event_data_complex`: a single JSON string containing the initial data passed into the `LogEvent` constructor

Once a `LogEvent` object is constructed with the given data, it can then be passed into `SimpleLog` with the `Log` function, and the data will be logged to the database.
