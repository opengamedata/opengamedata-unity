# opengamedata-unity

Unity package for logging with Field Day's OpenGameData servers.

## Version Log

1. Initial version
2. Add reusable survey (19 May 2021)
3. Firebase Analytics integration; Support for event constants; performance improvements (21 Sept 2022)
4. Support for `game_state` and `user_data` parameters; support for sending arbitrary json as `event_data` (13 Jan 2023)
5. Updated survey support (4 April 2023)
6. Support for `game_state` and `user_data` as arbitrary json (14 May 2024)
7. Support for mirroring events to a separate endpoint (21 May 2024)

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
- `OGDSurvey`: handles survey loading and display management
- `SurveyData`: outlines the data schema for survey data
- `SurveyPanel`: controller for the survey popup
- `SurveyQuestionDisplay`: controller for the individual questions in a survey popup

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
    m_Logger.EventParamJson("subObject", "{\"someSubObjectField\":5}");
m_Logger.SubmitEvent(); // this will then submit the event.
```

You can also write this using an `EventScope` object.

```csharp
using(EventScope evt = m_Logger.NewEvent("eventName")) {
    evt.Param("param1", 2); // you can call .Param on the EventScope directly.
    evt.Param("anotherParam", 4.5f);
    evt.Param("someStringParam", "blah");
    evt.Param("isDoingStuff", true);
    evt.Json("subObject", "{\"someSubObjectField\":5}");
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
    m_Logger.GameStateParamJson("shared2", "{\"somethingElse\":5}");
m_Logger.SubmitGameState();
```

You can also using a `GameStateScope` similar to how the `EventScope` functions.

```csharp
using(GameStateScope scope = m_Logger.OpenGameState()) {
    scope.Param("anotherName", "some-job");
    scope.Param("shared1", 467);
    scope.Json("shared2", "{\"somethingElse\":5}");
}
```

Finally, there is a `GameState` method to set the `game_state` to an arbitrary JSON-formatted string.

```csharp
m_Logger.GameState("{\"param1\":502}");
```

### User Data

The shared `user_data` parameter behaves similarly to the `game_state` parameter, with
a nearly identical syntax, swapping `GameState` for `UserData` in method names.

```csharp
m_Logger.OpenUserData()
m_Logger.BeginUserData()
m_Logger.UserDataParam(...)
m_Logger.SubmitUserData()
m_Logger.UserData(...)
```

### Limitations

Valid parameter types in OpenGameData are integer types, floating point values, bools, strings,
and `StringBuilder` instances.

The default maximum size of the event parameters for a single event is 4096 characters.
The default maximum size for `game_state` and `user_data` is 2048 characters each.
You can reconfigure these maximum sizes by passing a `OGDLog.MemoryConfig` into the `OGDLog` constructor.

### Firebase Analytics

To send events to a Firebase Analytics project, pass a `FirebaseConsts` object, or a JSON string for a `FirebaseConsts` object, to `OGDLog.UseFirebase()`.
This is marked as `[Serializable]` in Unity, so including it as a serialized field on a `MonoBehaviour` or `ScriptableObject` is possible.

This will initialize Firebase Analytics logging. Once Firebase Analytics has finished initializing, future events will be logged to it.

**Note**: Shared parameters such as `game_state` and `user_data` will not be uploaded while Firebase Analytics is initializing. It is recommended to wait for the `OGDLog.IsReady()`
method on your logger instance to return `true` before setting that shared data. 

### Firebase Analytics on Mobile

Logging to Firebase for Android or iOS will require an additional Unity package. Follow the guide [here](https://firebase.google.com/docs/unity/setup) to set that up.

**Note**: On Android, Google Play services checking is already handled by `OGDLog`.

### Mirroring

You can mirror your OpenGameData events to a secondary endpoint by calling `OGDLog.ConfigureMirroring()` with the given endpoint url as a string. You can optionally pass in an app id to use as an override for the endpoint.

## Debugging

`OGDLog.SetDebug()` can be called to set the logger's debug flag. If set, all requests and responses associated with OpenGameData event logging will be logged to the console.

## Survey

A `Survey` is a series of questions asked to the player at certain moments of the game (as defined by the developer). These questions are organized into `Pages`, which are displayed to the player in succession. Once the survey is completed, the results are logged using an `OGDLog` instance to the `survey_submitted` event.

### Survey Data

Survey packages are `.json` files. Survey packages must follow the following schema:
```json

{
    "package_config_id": "some-survey-package-id", // A unique package identifier
    "surveys": [ // an array of survey objects
        {
            "display_event_id": "some-event-id", // event id used by the game to reference this survey
            "header": "A Survey Header", // text to display at the top of the survey,
            "pages": [ // array of survey pages
                {
                    "items": [ // array of survey questions
                        {
                            "prompt": "This is a survey question. Can you understand it?", // the displayed question text
                            "responses": [ "Yes", "No", "What?" ] // array of possible responses for the player to pick between
                        }
                    ]
                }
            ]
        }
    ]
}
```

An example asset is provided [here](https://github.com/opengamedata/opengamedata-unity/blob/main/Assets/Example/survey_example.json).

### OGDSurvey

An instance of `OGDSurvey` must be created in order to properly handle survey display and logging. This can be instantiated using the following code:

`OGDSurvey surveyManager = new OGDSurvey(mySurveyPanelPrefab, myOGDLogInstance)`

This requires a reference to a `SurveyPanel` prefab.

From there, a survey package can be loaded using the following methods:

```csharp
surveyManager.LoadSurveyPackage(mySurveyPackageInstance); // from a SurveyPackage directly
surveyManager.LoadSurveyPackageFromString(someJsonString); // from a JSON string)
surveyManager.LoadSurveyPackageFromString(someTextAsset); // from a TextAsset containing JSON contents
```

To display a survey for the given `display_event_id`, you can call one of these variants of the `DisplaySurvey` method:

```csharp
surveyManager.TryDisplaySurvey("my_event_id"); // this will attempt to display the survey with the given event id and return whether or not there was a corresponding survey to display
surveyManager.DisplaySurvey("my_event_id", ResumeGame); // this will display a survey and invoke the given callback when the survey is completed. it will invoke the callback immediately if no survey is present
yield return surveyManager.DisplaySurveyAndWait("my_event_id"); // for use in coroutines. this will attempt to display the correct survey, and will proceed once the survey is completed (or if no survey was found)
```

Note that you can only have one survey displayed at a time. To cancel the currently displayed survey, discarding its results in the process, call `surveyManager.CancelSurvey()`.

### SurveyPanel Prefab

Within the `Survey/_Assets` subfolder of the package, there is a `DefaultSurvey` prefab. This is a basic, but fully functional, survey panel. To customize it, you can create a Prefab Variant of this prefab, bringing it into your own project and allowing you to customize images, fonts, and the like. It's recommended you also create variants of the `SurveyQuestion` and `SurveyResponseToggle` prefabs for further visual customization.

Additional scripts can be attached to these prefabs to configure various animation callbacks in the `SurveyPanel` script. These include:

```csharp
mySurveyPanel.OnLoaded // callback for when survey data is available, prior to displaying the first page
mySurveyPanel.OpenPageAnim // if set, this coroutine will execute when a new page is being displayed
mySurveyPanel.ClosePageAnim // if set, this coroutine will execute when a new page is queued up to transition the current page out
mySurveyPanel.FinishAnim // if set, this coroutine will execute upon the final survey page being submitted
mySurveyPanel.OnFinished // callback for when the survey is completed and closed
mySurveyPanel.OnNextButtonState // callback for when the next/finish button is completed
```

Note: Survey prefabs must make use of `TextMeshPro` text elements to be compatible.

## Updating

To update the local package, run the following command:

`$ openupm add com.fieldday.opengamedata-unity`

## Removal

[This resource](https://gist.github.com/myusuf3/7f645819ded92bda6677) provides a method for untracking the submodule and removing the necessary files.

Remember to also remove the dependency in the project's `manifest.json` file.
