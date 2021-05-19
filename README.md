# opengamedata-unity

Unity package for logging with Field Day's OpenGameData servers.

## Version Log

1. Initial version
2. Add reusable survey (5/19/21)

## Setup

Run the following command from the `Submodules` folder:

`$ git submodule add -b main https://github.com/opengamedata/opengamedata-unity.git`

Then, add the following line to the project's `manifest.json` dependencies:

`"com.fieldday.opengamedata-unity": "file:../Submodules/opengamedata-unity/Assets/FieldDay"`

[BeauUtil](https://github.com/BeauPrime/BeauUtil) is also included as a dependency.

## Contents

- `SimpleLog`: handles communication with the database using `UnityWebRequest`
- `SimpleLogUtils`: contains various helper functions for building data strings and imports JavaScript functions
- `CookieUtils`: a JavaScript plugin for handling cookies if the project is built for WebGL
- `LogEvent`: wrapper class for the data objects that get sent through `SimpleLog`

## Logging

An instance of `SimpleLog` can be created with the following format:

`SimpleLog slog = new SimpleLog(myAppId, myAppVersion, myQueryParams)`

- `myAppId`: an identifier for this app within the database (ex. "AQUALAB")
- `myAppVersion`: the current version of the app for all logging events
- `myQueryParams`: if specified, finds a given player id (see [`BeauUtil.QueryParams`](https://github.com/BeauPrime/BeauUtil/blob/master/Assets/BeauUtil/QueryParams.cs) for implementation)

In order to log an event, the data must be contained by a `LogEvent` object, which takes the following arguments:

- `data`: a `<string, string>` dictionary of data values for the given event
- `category`: an enum value to represent the given event type

`LogEvent` will format the data into a dictionary with the following key/value pairs:
- `event`: the event type, in this case will default to "CUSTOM"
- `event_custom`: the enum category for the logged event
- `event_data_complex`: a single JSON string containing the initial data passed into the `LogEvent` constructor

Once a `LogEvent` object is constructed with the given data, it can then be passed into `SimpleLog` with the `Log()` function.

## Debugging

`SimpleLog.Log()` can take in an optional boolean parameter `debug`, which defaults to false. If set to true, each log request will print the HTTP response code to the console.

## Survey

A survey asset can be created with the `.survey` file extension, and uses the following tags:

- `# defaultAnswers`: required and placed at the top of the file, lists default answers used for all questions
- `:: question.id`: the id associated with a given question
- `@answers`: optional, will override `defaultAnswers`

An example asset is provided [here](https://github.com/opengamedata/opengamedata-unity/blob/main/Assets/FieldDay/Survey/_Assets/sample.survey).

To use the `Survey` prefab, create a class which implements the `ISurveyHandler` interface, and call `Survey.Initialize(surveyAsset, surveyHandler)`. 

## Updating

To update the local package, run the following command:

`$ git submodule update --remote`

## Removal

[This resource](https://gist.github.com/myusuf3/7f645819ded92bda6677) provides a method for untracking the submodule and removing the necessary files.

Remember to also remove the dependency in the project's `manifest.json` file.
