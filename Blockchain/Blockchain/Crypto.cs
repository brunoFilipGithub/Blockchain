using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Asn1.Pkcs;

namespace Blockchain.Blockchain
{
    public class Crypto
    {
		public static AsymmetricCipherKeyPair GenerateRandomKeyPair()
		{
			var rsaKeyPairGen = new RsaKeyPairGenerator();
			rsaKeyPairGen.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
			return rsaKeyPairGen.GenerateKeyPair(); ;
		}

		public static bool VerifySignature(string sourceData, byte[] signature, RsaKeyParameters publicKey)
		{
			byte[] tmpSource = Encoding.ASCII.GetBytes(sourceData);

			ISigner signClientSide = SignerUtilities.GetSigner(PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id);
			signClientSide.Init(false, publicKey);
			signClientSide.BlockUpdate(tmpSource, 0, tmpSource.Length);

			return signClientSide.VerifySignature(signature);
		}

		public static byte[] SignData(string sourceData, RsaKeyParameters privateKey)
		{
			byte[] tmpSource = Encoding.ASCII.GetBytes(sourceData);

			ISigner sign = SignerUtilities.GetSigner(PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id);
			sign.Init(true, privateKey);
			sign.BlockUpdate(tmpSource, 0, tmpSource.Length);
			return sign.GenerateSignature();
		}

		public static string ComputeSHA256(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
