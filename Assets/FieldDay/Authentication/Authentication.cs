using UnityEngine;

public class Authentication : MonoBehaviour
{
    private string m_SaveCode;
    private string m_PlayerName;
    private string m_ClassCode;

    // TODO: may need to access these from AnalyticsService to send with log events?
    
    // set when new game is clicked / continue clicked and player enters their code
    public string SaveCode { get; set; }
    // set when player enters for new game
    public string PlayerName { get; set; }
    // set when loading from class URL
    public string ClassCode { get; set; }

    private string GetSaveCode()
    {
        // call id gen API, set m_SaveCode, return to display in UI
        return "";
    }

    // TODO: what format is player data saved as (JSON string, custom)?

    private void SendPlayerData(string playerData, string saveCode)
    {
        // send save data for anon player with save code
    }

    private void SendPlayerData(string playerData, string saveCode, string playerName, string classCode)
    {
        // send save data for a student with name and class code
    }

    private string LoadPlayerData(string saveCode)
    {
        // get save data for a given code from DB
        return "";
    }
}
