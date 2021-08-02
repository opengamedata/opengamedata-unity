using System;
using UnityEngine;
using UnityEngine.Networking;

public static class Authentication
{
    // Called once player hits start to display save code and cache somewhere
    public static void GenerateSaveCode(Action<string> completeCallback, Action<string> errorCallback)
    {
        UnityWebRequest request = UnityWebRequest.Get("https://fieldday-web.wcer.wisc.edu/wsgi-bin/opengamedata.wsgi/player/createID");
        AsyncOperation requestHandle = request.SendWebRequest();
        requestHandle.completed += (async) => {
            if (request.isNetworkError || request.isHttpError)
            {
                errorCallback(request.error);
            }
            else
            {
                completeCallback(request.downloadHandler.text);
            }
        };
    }

    // Send player data to server, use callbacks to check if post request errored or completed properly
    public static void SendPlayerData(string playerData, string saveCode, Action<string> completeCallback, Action<string> errorCallback)
    {
        UnityWebRequest request = UnityWebRequest.Post($"https://fieldday-web.wcer.wisc.edu/wsgi-bin/opengamedata.wsgi/player/{saveCode}/state", playerData);
        AsyncOperation requestHandle = request.SendWebRequest();
        requestHandle.completed += (async) => {
            if (request.isNetworkError || request.isHttpError)
            {
                errorCallback(request.error);
            }
            else
            {
                completeCallback(request.downloadHandler.text);
            }
        };
    }

    // Get player data from server, return with complete callback
    public static void LoadPlayerData(string saveCode, Action<string> completeCallback, Action<string> errorCallback)
    {
        UnityWebRequest request = UnityWebRequest.Get($"https://fieldday-web.wcer.wisc.edu/wsgi-bin/opengamedata.wsgi/player/{saveCode}/state");
        AsyncOperation requestHandle = request.SendWebRequest();
        requestHandle.completed += (async) => {
            if (request.isNetworkError || request.isHttpError)
            {
                errorCallback(request.error);
            }
            else
            {
                completeCallback(request.downloadHandler.text);
            }
        };
    }
}
