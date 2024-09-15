
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.GameData.Characters;
using StardewValley.Quests;
using StardewValley.Triggers;


namespace CustomIntroductions
{
    internal class CohortQuestData
    {
        public string Id { get; set; } = "Error Cohort Quest Id";
        public string Title { get; set; } = "Error Cohort Quest Title";
        public string Description { get; set; } = "Error Cohort Quest Description";
        public List<string> NextQuests { get; set; } = new();
        public int MoneyReward { get; set; } = 0;
        public string RewardDescription { get; set; } = "-1";
        public bool CanBeCancelled { get; set; } = false;
        public List<string> Characters { get; set; } = new();

        public string FormQuestEntry()
        {
            string joinedNextQuests = string.Join<string>(" ", NextQuests);
            return $"Cohort/{Title}/{Description}/./{joinedNextQuests}/{MoneyReward}/{RewardDescription}/{CanBeCancelled}";
        }

        public SocializeQuest FormQuest()
        {
            SocializeQuest cohortQ = new();

            int validCount = 0;
            foreach (string charaId in Characters)
            {
                if (Game1.characterData.TryGetValue(charaId, out CharacterData? _))
                {
                    cohortQ.whoToGreet.Add(charaId);
                    validCount++;
                }
            }
            cohortQ.total.Value = validCount;
            cohortQ.objective.Value = new DescriptionElement("Strings\\StringsFromCSFiles:SocializeQuest.cs.13802", cohortQ.total.Value - cohortQ.whoToGreet.Count, cohortQ.total.Value);

            cohortQ.id.Value = Id;
            cohortQ.questTitle = Title;
            cohortQ.questDescription = Description;
            cohortQ.currentObjective = ".";
            foreach (string nextQ in NextQuests)
            {
                string nextQuest = nextQ;
                if (nextQuest.StartsWith('h'))
                {
                    if (!Game1.IsMasterGame)
                    {
                        nextQuest = nextQuest.Substring(1);
                    }
                }
                cohortQ.nextQuests.Add(nextQuest);
            }
            cohortQ.showNew.Value = true;
            cohortQ.moneyReward.Value = MoneyReward;
            cohortQ.rewardDescription.Value = (MoneyReward == -1) ? null : RewardDescription;
            cohortQ.canBeCancelled.Value = CanBeCancelled;

            return cohortQ;
        }
    }

    internal sealed class ModEntry : Mod
    {
        public const string CohortQuestAsset = "mushymato.CustomIntroductions/CohortQuest";
        public static Dictionary<string, CohortQuestData> CohortQuests = new();
        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetReady += OnAssetReady;

            TriggerActionManager.RegisterAction($"{ModManifest.UniqueID}_AddCohortQuest", AddCohortQuest);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(CohortQuestAsset))
            {
                e.LoadFrom(
                    () => new Dictionary<string, CohortQuestData>(),
                    AssetLoadPriority.Exclusive
                );
            }
            if (e.Name.IsEquivalentTo("Data/Quests"))
            {
                // Neccessary to add cohort quest as real quest entry, otherwise quest name defaults to the daily quest
                e.Edit((IAssetData asset) =>
                {
                    IDictionary<string, string> questData = asset.AsDictionary<string, string>().Data;
                    foreach (CohortQuestData cohortQuest in CohortQuests.Values)
                    {
                        questData.Add(new(cohortQuest.Id, cohortQuest.FormQuestEntry()));
                    }
                });
            }
        }

        private void OnAssetReady(object? sender, AssetReadyEventArgs e)
        {
            if (e.Name.IsEquivalentTo(CohortQuestAsset))
                Helper.GameContent.InvalidateCache("Data/Quests");
            CohortQuests = Helper.GameContent.Load<Dictionary<string, CohortQuestData>>(CohortQuestAsset);
        }

        public static bool AddCohortQuest(string[] args, TriggerActionContext context, out string error)
        {
            if (!ArgUtility.TryGet(args, 1, out string questId, out error, allowBlank: false))
                return false;

            if (CohortQuests.TryGetValue(questId, out CohortQuestData? cohortQuest) && !Game1.player.hasQuest(questId))
            {
                Game1.player.questLog.Add(cohortQuest.FormQuest());
            }

            return true;
        }
    }
}