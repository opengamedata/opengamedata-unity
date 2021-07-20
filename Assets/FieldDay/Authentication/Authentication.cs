using UnityEngine;

public class Authentication : MonoBehaviour
{
    private string m_SaveCode = "SaveCode";
    private string m_PlayerName = "PlayerName";
    private string m_ClassCode = "ClassCode";
    
    #region Accessors

    // set when new game is clicked / continue clicked and player enters their code
    public string SaveCode { get { return m_SaveCode; } set { m_SaveCode = value; } }
    // set when player enters for new game
    public string PlayerName { get { return m_PlayerName; } set { m_PlayerName = value; } }
    // set when loading from class URL
    public string ClassCode { get { return m_ClassCode; } set { m_ClassCode = value; } }

    #endregion // Accessors

    private string GenerateSaveCode()
    {
        // call id gen API, set m_SaveCode, return to display in UI
        return m_SaveCode;
    }

    private void SendPlayerData(TextAsset playerData, string saveCode)
    {
        // send save data for anon player with save code
    }

    private void SendStudentData(TextAsset playerData, string saveCode, string playerName, string classCode)
    {
        // send save data for a student with name and class code
    }

    private TextAsset LoadPlayerData(string saveCode)
    {
        // get save data for a given code from DB
        return null;
    }
}
