# opengamedata-unity
Unity submodule for logging with OpenGameData servers.

## Setup
If there isn't one already, create a `Submodules` directory within the project's root. To add the submodule to a project, run the following command from within the `Submodules` directory:

`$ git submodule add https://github.com/fielddaylab/opengamedata-unity`

Then, add a reference to `FieldDay` to the assembly definition file for any namespace where logging functions will be used. 

Note that this submodule includes the [BeauUtil](https://github.com/BeauPrime/BeauUtil) library as a dependency.

## Logging

An instance of `SimpleLog` can be created with the following format:

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
