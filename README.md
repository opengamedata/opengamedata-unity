# opengamedata-unity

Unity package for logging with Field Day's OpenGameData servers.

## Version Log

1. Initial version
2. Add reusable survey (19 May 2021)
3. Firebase Analytics integration; Support for event constants; performance improvements (21 Sept 2022)
4. Support for `game_state` and `user_data` parameters; support for sending arbitrary json as `event_data` (13 Jan 2023)
5. Updated survey support (4 April 2023)

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

To log an event, you can do so in one of three ways.

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

You can also specify the parameter JSON manually. This will allow you to
send an arbitrary JSON object, provided it is valid.

```csharp
m_Logger.Log("eventName", "{\"param1\":502}"); // send in a string
m_Logger.Log("eventName", OGDLogUtils.Stringify(mySerializableObject)) // you can also stringify objects serializable by Unity's default serializer.
```

### Events (Compability)

For backwards compatibility, you can send events using a `LogEvent` object, which takes the following arguments:

- `data`: a `<string, string>` dictionary of data values for the given event
- `category`: an enum or string value to represent the given event type

Once a `LogEvent` object is constructed with the given data, it can then be passed into `OGDLog` with the `Log()` function.
This will then log it using a sequence of calls similar to those listed in the previous section.

### Game State

To set the shared `game_state` parameter, you can do so in one of two ways.

```csharp
m_Logger.BeginGameState();
    m_Logger.GameStateParam("shared1", 2);
    m_Logger.GameStateParam("anotherName", "no-job");
m_Logger.SubmitGameState();
```

You can also using a `GameStateScope` similar to how the `EventScope` functions.

```csharp
using(GameStateScope scope = m_Logger.OpenGameState()) {
    scope.Param("anotherName", "some-job");
    scope.Param("shared1", 467);
}
```

### User Data

The shared `user_data` parameter behaves similarly to the `game_state` parameter, with
a nearly identical syntax, swapping `GameState` for `UserData` in method names.

```csharp
m_Logger.OpenUserData()
m_Logger.BeginUserData()
m_Logger.UserDataParam(...)
m_Logger.SubmitUserData()
```

### Limitations

Valid parameter types in OpenGameData are integer types, floating point values, strings,
and `StringBuilder` instances.

The maximum size of the event parameters for a single event is 4096 characters.
The maximum size for `game_state` and `user_data` is 2048 characters each.

### Firebase Analytics

To send events to a Firebase Analytics project, pass a `FirebaseConsts` object, or a JSON string for a `FirebaseConsts` object, to `OGDLog.UseFirebase()`.
This is marked as `[Serializable]` in Unity, so including it as a serialized field on a `MonoBehaviour` or `ScriptableObject` is possible.

This will initialize Firebase Analytics logging. Once Firebase Analytics has finished initializing, future events will be logged to it.

**Note**: Shared parameters such as `game_state` and `user_data` will not be uploaded while Firebase Analytics is initializing. It is recommended to wait for the `OGDLog.IsReady()`
method on your logger instance to return `true` before setting that shared data. 

### Firebase Analytics on Mobile

Logging to Firebase for Android or iOS will require an additional Unity package. Follow the guide [here](https://firebase.google.com/docs/unity/setup) to set that up.

**Note**: On Android, Google Play services checking is already handled by `OGDLog`.

## Debugging

`OGDLog.SetDebug()` can be called to set the logger's debug flag. If set, all requests and responses associated with OpenGameData event logging will be logged to the console.

## Survey

Survey packages are `.json` files.

An example asset is provided [here](https://github.com/opengamedata/opengamedata-unity/blob/main/Assets/Example/survey_example.json).

An instance of `OGDSurvey` must be created in order to properly handle survey display and logging.

(Survey documentation is in progress)

## Updating

To update the local package, run the following command:

`$ git submodule update --remote`

## Removal

[This resource](https://gist.github.com/myusuf3/7f645819ded92bda6677) provides a method for untracking the submodule and removing the necessary files.

Remember to also remove the dependency in the project's `manifest.json` file.
