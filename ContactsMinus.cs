using CloudX.Shared;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ContactsMinus
{
    public class ContactsMinus : NeosMod
    {
        public override string Name => "ContactsMinus";
        public override string Author => "Psychpsyo";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/Psychpsyo/NeosContactsMinus";

        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> modActive = new ModConfigurationKey<bool>("Set Hosted Worlds to Contacts-", "This sets every world you are currently hosting to Contacts-.", () => false);

        private static ModConfiguration config;
        public override void OnEngineInit()
        {
            config = GetConfiguration();

            Harmony harmony = new Harmony("Psychpsyo.ContactsMinus");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(World), "VerifyJoinRequest")]
        class ContactsChecker {
            static async Task<JoinGrant> Postfix(Task<JoinGrant> __result, World __instance, JoinRequestData joinRequest)
            {
                if (!config.GetValue(modActive))
                    return await __result;

                // this is the Contacts+ (SessionAccessLevel.FriendsOfFriends) checks from the original function, just inverted.
                if (string.IsNullOrEmpty(joinRequest.userID))
                    return await __result;
                if (__instance.Engine.Cloud.Friends.IsFriend(joinRequest.userID))
                    return JoinGrant.Deny("Only non-contacts of users in the session are allowed to join\n(World is set to Contacts-)");
                string id1 = __instance.Session.RequestContactCheckKey(joinRequest.connection).Result;
                if (!OneTimeVerificationKey.IsValidId(id1))
                    return await __result;
                CheckContactData data = new CheckContactData();
                data.OwnerId = joinRequest.userID;
                data.VerificationKey = id1;
                data.Contacts = new List<string>();
                List<FrooxEngine.User> list = new List<FrooxEngine.User>();
                __instance.GetUsers(list);
                foreach (FrooxEngine.User user in list)
                {
                    if (!user.IsHost && user.UserID != null)
                        data.Contacts.Add(user.UserID);
                }
                if (__instance.Engine.Cloud.CheckContact(data).Result.Entity)
                    return JoinGrant.Deny("Only non-contacts of users in the session are allowed to join\n(World is set to Contacts-)");

                return await __result;
            }
        }
    }
}
