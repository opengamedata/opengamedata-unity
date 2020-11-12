# opengamedata-unity
Unity submodule for logging with OpenGameData servers.

## Setup
To add the submodule to a project, run the following command from the project's root directory:

`$ git submodule add https://github.com/fielddaylab/opengamedata-unity`

Then, add the `FieldDay.Unity` namespace to the assembly definition file for the namespace where logging will be added.

An instance of `SimpleLog` can be created with the following format:

`SimpleLog slog = new SimpleLog(myAppId, myAppVersion, myQueryParams)`

- `myAppId`: an identifier for this app within the database (ex. 'AQUALAB')
- `myAppVersion`: the current version of the app for all logging events
- `myQueryParams`: if specified, finds a given player id

In order to log an event, the data must be contained by a `LogEvent` instance. The `LogEvent` constructor takes the following arguments:

- `data`: a `<string, string>` dictionary of data values for the given event
- `category`: an enum value to represent the given event type

Once a `LogEvent` object is constructed with the given data, it can then be passed into `SimpleLog` with the `Log` function, and the data will be logged to the database.
