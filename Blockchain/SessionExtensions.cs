using Blockchain.Blockchain;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blockchain
{
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
            
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }

        public static bool IsUserLoggedIn(this ISession session)
        {
            if (session.GetString("loggedUser") != null)
            {
                return true;
            }
            else return false;
        }

        public static bool IsUserAuthority(this ISession session)
        {
            if (session.GetObjectFromJson<AuthorityUser>("loggedUser").authorityUserLoginKeyHash != null)
            {
                return true;
            }
            else return false;
        }

        public static List<LandTransaction> GetRequestsForLoggedUser(this ISession session)
        {
            if (session.IsUserLoggedIn())
            {
                List<LandTransaction> transactions = Helper.GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);

                if (transactions != null)
                {
                    List<LandTransaction> pendingUsersTransactions = transactions.FindAll(transaction => transaction.landOwnerHash == session.GetLoggedUserHash() && transaction.transactionStatus == Status.Request);

                    if(pendingUsersTransactions.Count == 0 )
                    {
                        pendingUsersTransactions = null;
                    }

                    return pendingUsersTransactions;
                }
                else return null;
            }
            else return null;
        }

        public static string GetLoggedUserHash(this ISession session)
        {
            string userHash;

            if (session.IsUserAuthority())
            {
                userHash = session.GetObjectFromJson<AuthorityUser>("loggedUser").authorityUserLoginKeyHash;
            }
            else
            {
                userHash = session.GetObjectFromJson<User>("loggedUser").userLoginKeyHash;
            }

            return userHash;
        }

    }
}
