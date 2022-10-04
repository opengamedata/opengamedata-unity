# opengamedata-unity

Unity package for logging with Field Day's OpenGameData servers.

## Version Log

1. Initial version
2. Add reusable survey (19 May 2021)
3. Firebase Analytics integration; Support for event constants; performance improvements (21 Sept 2022)

## Setup

If you do not have `openupm-cli` interface installed, run the following command:

`$ npm install -g openupm-cli`

Run the following command from the root folder of your project:

`$ openupm add com.fieldday.opengamedata-unity`

## Contents

- `OGDLog`: handles communication with the database using `UnityWebRequest`
- `OGDLogUtils`: contains various helper functions for building data strings and generating UUIDs
- `CookieUtils`: a JavaScript plugin for handling cookies if the project is built for WebGL
- `LogEvent`: wrapper class for the data objects that get sent through `OGDLog`
- `OGDLogConsts`: wrapper struct for various app-level OpenGameData constants.
- `FirebaseConsts`: wrapper struct for Firebase initialization parameters.

## Logging

An instance of `OGDLog` can be created with the following format:

`OGDLog log = new OGDLog(myAppId, myAppVersion)`

- `myAppId`: an identifier for this app within the database (ex. "AQUALAB")
- `myAppVersion`: the current version of the app for all logging events

To send a user id along with every event, call `OGDLog.SetUserId(userId);`
To reset application-level constants, call `OGDLog.Initialize(appConsts);`

### Events

To log an event, you can do so in one of two ways.

You can do so with a series of function calls.

```csharp
m_Logger.BeginEvent("eventName");
    m_Logger.EventParam("param1", 2); // you can set integer parameters...
    m_Logger.EventParam("anotherParam", 4.5f) // floating point parameters...
    m_Logger.EventParam("someStringParam", "blah"); // string parameters...
    m_Logger.EventParam("isDoingStuff", true); // and boolean parameters
m_Logger.SubmitEvent(); // this will then submit the event.
```

You can also write this using an `EventScope` object.

```csharp
using(EventScope evt = m_Logger.NewEvent("eventName")) {
    evt.Param("param1", 2); // you can call .Param on the EventScope directly.
    evt.Param("anotherParam", 4.5f);
    evt.Param("someStringParam", "blah");
    evt.Param("isDoingStuff", true);
} // upon exiting this block, the event will be automatically submitted
```

### Events (Compability)

For backwards compatibility, you can send events using a `LogEvent` object, which takes the following arguments:

- `data`: a `<string, string>` dictionary of data values for the given event
- `category`: an enum or string value to represent the given event type

Once a `LogEvent` object is constructed with the given data, it can then be passed into `OGDLog` with the `Log()` function.
This will then log it using a sequence of calls similar to those listed in the previous section.

### Firebase Analytics

To send events to a Firebase Analytics project, pass a `FirebaseConsts` object, or a JSON string for a `FirebaseConsts` object, to `OGDLog.UseFirebase()`.
This is marked as `[Serializable]` in Unity, so including it as a serialized field on a `MonoBehaviour` or `ScriptableObject` is possible.

This will initialize Firebase Analytics logging. Once Firebase Analytics has finished initializing, future events will be logged to it.

### Firebase Analytics on Mobile

Logging to Firebase for Android or iOS will require an additional Unity package. Follow the guide [here](https://firebase.google.com/docs/unity/setup) to set that up.

**Note**: On Android, Google Play services checking is already handled by `OGDLog`.

## Debugging

`OGDLog.SetDebug()` can be called to set the logger's debug flag. If set, all requests and responses associated with OpenGameData event logging will be logged to the console.

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
