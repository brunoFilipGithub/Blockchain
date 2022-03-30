using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;


namespace Blockchain.Blockchain
{
    public class Block : IEquatable<Block>
    {
        public string hash { get { return Crypto.ComputeSHA256(index.ToString() + previousBlockHash + timestamp.ToString() + data); } }
        public int index;
        public string previousBlockHash;
        public DateTime timestamp;
        public List<LandTransaction> data;

        public Block()
        {

        }

        public Block(int index, string previousBlockHash, DateTime timestamp, List<LandTransaction> data)
        {
            this.index = index;
            this.previousBlockHash = previousBlockHash;
            this.timestamp = timestamp;
            this.data = data;
        }

        public Block(int index, string previousBlockHash, DateTime timestamp, LandTransaction data)
        {
            this.index = index;
            this.previousBlockHash = previousBlockHash;
            this.timestamp = timestamp;

            this.data = new List<LandTransaction>();
            this.data.Add(data);
        }

        public bool Equals(Block other)
        {
            if (other is null)
                return false;

            return this.hash == other.hash;
        }

        public override bool Equals(object obj) => Equals(obj as Block);
        public override int GetHashCode() => (hash).GetHashCode();
    }
}
