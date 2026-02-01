using LifeApp.Utility.Verbs;

namespace LifeApp.Utility.Interfaces.Verbs
{
    internal interface ILetterboxdSyncVerb
    {
        Task<int> Execute(LetterboxdSyncVerbOptions options);
    }
}