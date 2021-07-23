using System;
using UnityEngine;
using UnityEngine.Networking;

public static class Authentication
{
    // Called once player hits start to display save code and cache somewhere
    public static void GenerateSaveCode(Action<string> completeCallback, Action<string> errorCallback)
    {
        // UnityWebRequest request = UnityWebRequest.Get("url");
        // AsyncOperation requestHandle = request.SendWebRequest();
        // requestHandle.completed += (async) => {
        //     if (request.isNetworkError || request.isHttpError)
        //     {
        //         errorCallback(request.error);
        //     }
        //     else
        //     {
        //         completeCallback(request.downloadHandler.text);
        //     }
        // };
        completeCallback("SaveCode");
    }

    // Send player data to server, use callbacks to check if post request errored or completed properly
    public static void SendPlayerData(string playerData, string saveCode, string playerName, string classCode, Action<string> completeCallback, Action<string> errorCallback)
    {
        // UnityWebRequest request = UnityWebRequest.Post("url", "data");
        // AsyncOperation requestHandle = request.SendWebRequest();
        // requestHandle.completed += (async) => {
        //     if (request.isNetworkError || request.isHttpError)
        //     {
        //         errorCallback(request.error);
        //     }
        //     else
        //     {
        //         completeCallback("Save complete");
        //     }
        // };
        completeCallback("Save complete");
    }

    // Get player data from server, return with complete callback
    public static void LoadPlayerData(string saveCode, string classCode, Action<string> completeCallback, Action<string> errorCallback)
    {
        // UnityWebRequest request = UnityWebRequest.Get("url");
        // AsyncOperation requestHandle = request.SendWebRequest();
        // requestHandle.completed += (async) => {
        //     if (request.isNetworkError || request.isHttpError)
        //     {
        //         errorCallback(request.error);
        //     }
        //     else
        //     {
        //         completeCallback(request.downloadHandler.text);
        //     }
        // };
        completeCallback("");
    }
}
