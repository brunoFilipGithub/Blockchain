using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Blockchain.Blockchain;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using Blockchain.Models;

namespace Blockchain
{
    public enum ObjectType 
    {
        GenesisLandData,
        AuthorityUser,
        PendingLandTransactions,
        InVotingLandTransactions,
        User,
        Blockchain
    }
  
    public static class Helper
    {
        static Random random = new Random();

        public static List<T> GetObjectListFromJson<T>(ObjectType objectType)
        {
            string path = GetPath(objectType);
            List<T> list;

            if (File.Exists(path))
            {
                list = JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(path));
                return list;
            }
            else return null;

        }

        public static void WriteObjectListToJson<T>(List<T> list, ObjectType objectType)
        {
            string path = GetPath(objectType);
            string json = JsonConvert.SerializeObject(list, Formatting.Indented);

            System.IO.File.WriteAllText(path, json);
        }

        public static string GetBlockchainPath(string authorityUserLoginHash)
        {
            if (authorityUserLoginHash == "f47baffbe0414d1921e434bb687f63b7067cd08841b4422443200c0826111720")
            {
                return Directory.GetCurrentDirectory() + "/blockchain1/blockchain.json";
            }
            else if (authorityUserLoginHash == "8f5b0d96f685d770f54e24850ebb79a96909a8aeb851a52454a02cba350c7e1b")
            {
                return Directory.GetCurrentDirectory() + "/blockchain2/blockchain.json";
            }
            else if (authorityUserLoginHash == "65b287cf4f1fd0a04cea95cb1a59c8a95743a33c9259c5f59290ff207f5f33e0")
            {
                return Directory.GetCurrentDirectory() + "/blockchain3/blockchain.json";
            }
            else return string.Empty;
        }

        public static void RewriteBlockchains(Block newBlock)
        {
            List<AuthorityUser> authorityUsers = Helper.GetObjectListFromJson<AuthorityUser>(ObjectType.AuthorityUser);

            List<Block> currentBlockchain = GetObjectListFromJson<Block>(ObjectType.Blockchain);
            currentBlockchain.Add(newBlock);

            foreach (AuthorityUser authorityUser in authorityUsers)
            {
                string blockchainPath = GetBlockchainPath(authorityUser.authorityUserLoginKeyHash);

                string json = JsonConvert.SerializeObject(currentBlockchain, Formatting.Indented);
                System.IO.File.WriteAllText(blockchainPath, json);
            }
        }

        public static string[] GetAuthorityUserLoiginHashes()
        {
            List<string> result = new List<string>();

            result.Add("f47baffbe0414d1921e434bb687f63b7067cd08841b4422443200c0826111720");
            result.Add("8f5b0d96f685d770f54e24850ebb79a96909a8aeb851a52454a02cba350c7e1b");
            result.Add("65b287cf4f1fd0a04cea95cb1a59c8a95743a33c9259c5f59290ff207f5f33e0");
            
            return result.ToArray();
        }

        public static void WriteNewPendingLandTransaction(LandTransaction landTransaction)
        {
            string path = GetPath(ObjectType.PendingLandTransactions);

            if (File.Exists(path))
            {
                List<LandTransaction> landTransactions = GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);
                landTransactions.Add(landTransaction);
                WriteObjectListToJson<LandTransaction>(landTransactions, ObjectType.PendingLandTransactions);
            }
            else
            {
                List<LandTransaction> landTransactions = new List<LandTransaction>() { landTransaction };
                WriteObjectListToJson<LandTransaction>(landTransactions, ObjectType.PendingLandTransactions);
            }
        }

        public static void WriteNewVotingLandTransaction(LandTransaction landTransaction)
        {
            string path = GetPath(ObjectType.InVotingLandTransactions);

            if (File.Exists(path))
            {
                List<LandTransaction> landTransactions = GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);
                landTransactions.Add(landTransaction);
                WriteObjectListToJson<LandTransaction>(landTransactions, ObjectType.PendingLandTransactions);
            }
            else
            {
                List<LandTransaction> landTransactions = new List<LandTransaction>() { landTransaction };
                WriteObjectListToJson<LandTransaction>(landTransactions, ObjectType.PendingLandTransactions);
            }
        }

        public static void RemoveAcceptedSignedTransactions(List<LandTransaction> signedTransactions)
        {
            List<LandTransaction> landTransactions = GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);

            foreach(LandTransaction signedTransaction in signedTransactions)
            {
                landTransactions.RemoveAll(transaction => transaction.transactionHash == signedTransaction.transactionHash);
            }

            WriteObjectListToJson<LandTransaction>(landTransactions, ObjectType.PendingLandTransactions);
        }

        public static List<LandTransaction> GetSignedTransactions()
        {
            List<LandTransaction> transactions = Helper.GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);

            if (transactions != null)
            {
                List<LandTransaction> pendingSignedTransactions = transactions.FindAll(transaction => transaction.transactionStatus == Status.Signed || transaction.transactionStatus == Status.Voting);

                if(pendingSignedTransactions.Count == 0)
                {
                    pendingSignedTransactions = null;
                }

                return pendingSignedTransactions;
            }
            else return null;
            
        }      

        public static int VoteNeedingTransactionsCount(string authorityUserLoginKeyHash)
        {
            int result = 0;

            List<LandTransaction> signedTransactions = Helper.GetSignedTransactions();
            List<LandTransaction> notVotedByThisAuthorityUser = new List<LandTransaction>();

            if (signedTransactions != null && signedTransactions.Count > 0)
            {
                foreach (LandTransaction landTransaction in signedTransactions)
                {
                    if (!landTransaction.authorityUsersThatVoted.Contains(Crypto.ComputeSHA256(authorityUserLoginKeyHash)))
                    {
                        notVotedByThisAuthorityUser.Add(landTransaction);
                    }
                }

                return notVotedByThisAuthorityUser.Count();
            }

            return result;
        }

        public static bool DoesLandRequestExist(LandModel model)
        {
            string path = GetPath(ObjectType.PendingLandTransactions);
            Land landType;
            Enum.TryParse(model.landId, out landType);

            if (File.Exists(path))
            {
                List<LandTransaction> landTransactions = GetObjectListFromJson<LandTransaction>(ObjectType.PendingLandTransactions);

                LandTransaction transactionExists = landTransactions.Find(element => element.landOwnerHash == model.landOwnerHash && element.landRequesterHash == model.landRequesterHash && (int)Enum.Parse(typeof(Land),element.landId) == (int)landType);
                if (transactionExists == null)
                {
                    return false;
                }
                else return true;
            }
            else
            {
                return false;
            }

        }

        public static string GetPath(ObjectType type)
        {
            string path = Directory.GetCurrentDirectory() + "/";

            switch (type)
            {
                case ObjectType.Blockchain:
                    {
                        path += "blockchain" + random.Next(1, 3) +"/blockchain.json";
                        break;
                    }
                case ObjectType.PendingLandTransactions:
                    {
                        path += "pendingTransactions.json";
                        break;
                    }
                case ObjectType.GenesisLandData:
                    {
                        path += "genesisBlockData.json";
                        break;
                    }
                case ObjectType.AuthorityUser:
                    {
                        path += "authorityUsers.json";
                        break;
                    }

                case ObjectType.User:
                    {
                        path += "users.json";
                        break;
                    }

                case ObjectType.InVotingLandTransactions:
                    {
                        path += "inVotingLandTransactions.json";
                        break;
                    }

                default:
                    {
                        path = null;
                        break;
                    }
            }

            return path;
        }        

        public static string GenerateUserLoginKey(string key)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string randomString  = new string(Enumerable.Repeat(chars, 15).Select(s => s[random.Next(s.Length)]).ToArray());

            return (ShuffleStringLetters(key, randomString)).Substring(0, 10);
        }

        private static string ShuffleStringLetters(string string1, string string2)
        {
            string temp = string1 + string2;
            char[] stringChar = temp.ToCharArray();

            int n = stringChar.Length;

            for (int i = 0; i < n; i++)
            {
                int r = i + random.Next(n - i);
                char t = stringChar[r];
                stringChar[r] = stringChar[i];
                stringChar[i] = t;
            }

            return new string(stringChar);
        }

        private static void AddUser(User user, string loginKey)
        {
            List<User> users = Helper.GetObjectListFromJson<User>(ObjectType.User);

            user.userLoginKeyHash = Crypto.ComputeSHA256(loginKey);
            users.Add(user);

            WriteObjectListToJson(users, ObjectType.User);
        }

        public static void SendKey(string userEmail, User user)
        {
            string loginKey = GenerateUserLoginKey(user.OIB);

            AddUser(user, loginKey);

            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);

            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new System.Net.NetworkCredential("noreplylandkeysender@gmail.com", "landkeysender");
            smtpClient.EnableSsl = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

            MailMessage mail = new MailMessage();


            mail.From = new MailAddress("noreplyLandKeySender@google.com", "Stranica zemljišta");
            mail.To.Add(new MailAddress(userEmail));

            mail.Subject = "Registracija";
            mail.IsBodyHtml = true;
            mail.Body = "Uspješno ste se registrirali. <br />Vaš ključ za prijavu je: " + loginKey + "<br />Obavezno ga držite tajnim.";

            smtpClient.Send(mail);
        }

        public static string NormalizeLandPoints(string points)
        {
            int xReductor;
            int yReductor;

            string[] landPoints = points.Split(" ");

            InitializeReductors(landPoints, out xReductor, out yReductor);

            string newPoints = string.Empty;

            for (int a = 0; a < landPoints.Length; a++)
            {
                string point = "";
                string[] landPointCurrent = landPoints[a].Split(",");

                point = (Convert.ToInt32(landPointCurrent[0]) - xReductor).ToString() + ",";
                point += (Convert.ToInt32(landPointCurrent[1]) - yReductor).ToString() + " ";

                newPoints += point;
            }

            return newPoints.Substring(0, newPoints.Length - 1);
        }

        private static void InitializeReductors(string[] landPoints, out int xReductor, out int yReductor)
        {
            int smallestXPoint = 0;
            int smallestYPoint = 0;

            for (int a = 0; a < landPoints.Length; a++)
            {
                string[] pointString = landPoints[a].Split(",");
                int pointX = Convert.ToInt32(pointString[0]);
                int pointY = Convert.ToInt32(pointString[1]);

                if (a == 0)
                {
                    smallestXPoint = pointX;
                    smallestYPoint = pointY;
                }
                else
                {
                    if (pointX < smallestXPoint)
                    {
                        smallestXPoint = pointX;
                    }

                    if (pointY < smallestYPoint)
                    {
                        smallestYPoint = pointY;
                    }
                }
            }

            xReductor = 0 + smallestXPoint - 5;
            yReductor = 0 + smallestYPoint - 5;
        }
    }
}
