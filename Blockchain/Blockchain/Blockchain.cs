using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Blockchain.Blockchain
{
    public static class Blockchain
    {
        public static List<Block> blockchain { get; set; }

        static Blockchain()
        {
            blockchain = new List<Block>();          

            InitializeBlockchain();
        }

        private static void InitializeBlockchain()
        {
            blockchain = Helper.GetObjectListFromJson<Block>(ObjectType.Blockchain);
        }

        public static void AddBlock(LandTransaction transaction)
        {
            int index = blockchain.IndexOf(blockchain.Last()) + 1;

            Block newBlock = new Block(index, blockchain.ElementAt(index - 1).hash, DateTime.Now, transaction);


            Save(newBlock);
        }

        public static void AddBlock(List<LandTransaction> transactions)
        {
            int index = blockchain.IndexOf(blockchain.Last()) + 1;

            Block newBlock = new Block(index, blockchain.ElementAt(index - 1).hash, DateTime.Now, transactions);

            Save(newBlock);
        }

        private static void Save(Block block)
        {
            if (IsValid())
            {
                Helper.RewriteBlockchains(block);
            }

            InitializeBlockchain();
        }

        public static bool IsValid()
        {
            if (blockchain.Count > 1)
            {
                for (int i = 1; i < blockchain.Count - 1; i++)
                {
                    Block currentBlock = blockchain.ElementAt(i);
                    Block prevBlock = blockchain.ElementAt(i - 1);

                    if (currentBlock.previousBlockHash != prevBlock.hash)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static List<LandTransaction> GetLatestLandDataList()
        {
            List<LandTransaction> result = new List<LandTransaction>();

            for(int i = blockchain.IndexOf(blockchain.Last()); i >= 0; i--)
            {
                if (result.Count() > 0)
                {
                    foreach (LandTransaction land in blockchain.ElementAt(i).data)
                    {
                        if (result.Any(element => element.landId == land.landId))
                        {
                            continue;
                        }
                        else
                        {
                            if (i != 0)
                            {
                                SetLandOwner(land);
                            }
                            result.Add(land);
                        }
                    }
                }
                else
                {
                    foreach (LandTransaction land in blockchain.ElementAt(i).data)
                    {
                        if (i != 0)
                        {
                            SetLandOwner(land);
                        }
                        result.Add(land);
                    }
                }
            }

            return result;

        }

        private static LandTransaction SetLandOwner(LandTransaction transaction)
        {
            if (transaction.landRequesterHash != "-")
            {
                transaction.landOwnerHash = transaction.landRequesterHash;
                transaction.landRequesterHash = "-";
            }

            return transaction;
        }

        private static string GetAuthorityUserThatHasBlockchainChange()
        {
            List<AuthorityUser> authorityUsers = Helper.GetObjectListFromJson<AuthorityUser>(ObjectType.AuthorityUser);

            foreach (AuthorityUser authorityUser in authorityUsers)
            {
                if (authorityUser.changeInBlockcahin == true)
                {
                    AuthorityUser authorityUserWithChange = authorityUser;

                    authorityUsers.Remove(authorityUser);
                    authorityUserWithChange.changeInBlockcahin = false;
                    authorityUsers.Add(authorityUserWithChange);
                    Helper.WriteObjectListToJson(authorityUsers, ObjectType.AuthorityUser);

                    return authorityUser.authorityUserLoginKeyHash;
                }
            }

            return string.Empty;
        }

        private static List<Block> GetBlockchain(string authorityUserLoginKeyHash)
        {
            string path = Helper.GetBlockchainPath(authorityUserLoginKeyHash);

            List<Block> blockchainResult = JsonConvert.DeserializeObject<List<Block>>(File.ReadAllText(path));

            return blockchainResult;
        }

        private static void UpdateBlockchainsIfChanged()
        {
            if (GetAuthorityUserThatHasBlockchainChange() != string.Empty)
            {
                List<AuthorityUser> authorityUsers = Helper. GetObjectListFromJson<AuthorityUser>(ObjectType.AuthorityUser);
                List<Block> newBlockchain = GetBlockchain(GetAuthorityUserThatHasBlockchainChange());

                authorityUsers.RemoveAll(authorityUser => authorityUser.authorityUserLoginKeyHash == GetAuthorityUserThatHasBlockchainChange());


                foreach (AuthorityUser authorityUser in authorityUsers)
                {
                    string path = Helper.GetBlockchainPath(authorityUser.authorityUserLoginKeyHash);
                    string json = JsonConvert.SerializeObject(newBlockchain, Formatting.Indented);
                    System.IO.File.WriteAllText(path, json);
                }

            }
        }

        private static void AnalyzeBlockchains()
        {
            List<AuthorityUser> authorityUsers = Helper.GetObjectListFromJson<AuthorityUser>(ObjectType.AuthorityUser);

            Dictionary<string, List<Block>> blockchains = new Dictionary<string, List<Block>>();

            foreach (AuthorityUser authorityUser in authorityUsers)
            {
                string chainPath = Helper.GetBlockchainPath(authorityUser.authorityUserLoginKeyHash);

                List<Block> chain = JsonConvert.DeserializeObject<List<Block>>(File.ReadAllText(chainPath));

                blockchains.Add(authorityUser.authorityUserLoginKeyHash, chain);
            }

            int number_of_blockchains = Helper.GetAuthorityUserLoiginHashes().Count();

            for (int i = 0; i < number_of_blockchains - 1; i++)
            {
                if (i != 0)
                {
                    List<Block> blockchainCurrent = new List<Block>();
                    List<Block> blockchainPrevious = new List<Block>();

                    blockchains.TryGetValue(Helper.GetAuthorityUserLoiginHashes().ElementAt(i), out blockchainCurrent);
                    blockchains.TryGetValue(Helper.GetAuthorityUserLoiginHashes().ElementAt(i - 1), out blockchainPrevious);


                    IEnumerable<Block> differences = blockchainCurrent.Except(blockchainPrevious);

                    if (differences.ToList().Count > 0)
                    {
                        if (i != (number_of_blockchains - 1))
                        {
                            // blockchainCurrent is not the last element 
                            IEnumerable<Block> differences2 = blockchainCurrent.Except(blockchainPrevious);
                        }
                        else
                        {
                            // it is the last element
                            continue;
                        }

                        continue;
                    }
                }
            }
        }
    }
}
