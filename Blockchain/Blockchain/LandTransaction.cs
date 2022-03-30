using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;

namespace Blockchain.Blockchain
{
    public enum Land
    {
        A1,
        A2,
        A3,
        A4,
        A5,
        A6,
        A7,
        A8,
        A9,
        A10,
        A11,
        A12,
        A13,
        A14,
        A15,
        A16,
        A17,
        A18,
        A19,
        A20,
        A21,
        A22,
        A23,
        A24,
        A25
    }

    public enum Status
    {
        Request,
        Signed,
        Voting,
        Verified
    }

    public class LandTransaction
    {
        public string transactionHash { get { return Crypto.ComputeSHA256(landOwnerHash + landRequesterHash + landId.ToString()); } }
        public string landOwnerHash { get; set; }
        public string landRequesterHash { get; set; }
        public string landId { get; set; }
        public Status transactionStatus { get; set; }
        public string location { get; set; }
        public DateTime timestamp { get; set; }
        public byte[] digitalSignature { get; set; }
        public byte[] publicKey { get; set; }
        public List<string> authorityUsersThatVoted { get; set; }
        public int votes { get; set; }

        public LandTransaction(string landOwnerHash, string landRequesterHash, string location, Land land)
        {
            this.landOwnerHash = landOwnerHash;
            this.landRequesterHash = landRequesterHash;
            this.landId = land.ToString();
            this.transactionStatus = Status.Request;
            this.location = location;

            this.digitalSignature = null;
            this.publicKey = null;
            this.authorityUsersThatVoted = new List<string>();
            this.votes = 0;

            timestamp = DateTime.Now;
        }


    }
}
