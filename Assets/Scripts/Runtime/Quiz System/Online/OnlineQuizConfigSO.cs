using UnityEngine;

[CreateAssetMenu(fileName = "OnlineQuizConfig", menuName = "Shivance Games/Online Quiz Config")]
public class OnlineQuizConfigSO : ScriptableObject
{
    public string quizCatalogCustomDataId = "cyberladder_quizzes";
    public string manifestCustomId = "cyberladder_quiz_manifest";
    public string quizPackCustomIdPrefix = "cyberladder_quiz_pack_";
    public bool useOnlineQuizSystem = true;
    public bool useLocalQuizFallback = true;
    public bool useLocalJsonCache = true;
    public bool hideInactivePacksInGame = true;
    public string defaultCategoryName = "General";
    public bool allowArchivedTournamentQuizPacks = false;
    public bool loadFullPackOnSelection = true;
    public bool loadFullPackOnGameStart = true;
    public string localCacheFolderName = "OnlineQuizCache";
}
