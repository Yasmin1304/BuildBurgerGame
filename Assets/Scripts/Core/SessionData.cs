
public static class SessionData
{   
    //public static string ParticipantNumber;
    public static string ParticipantCode;
    public static string ParticipantId;
    public static string SessionId;
    public static GameMode SelectedGameMode = GameMode.Letters;
    public static int RequestedStartLevelIndex = -1;

    public static void ResetForNewSession()
    {
        ParticipantCode = string.Empty;
        ParticipantId = string.Empty;
        SessionId = string.Empty;
        RequestedStartLevelIndex = -1;
        SelectedGameMode = GameMode.Letters;
    }
}
