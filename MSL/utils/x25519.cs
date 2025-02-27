using Chaos.NaCl;
using System.Security.Cryptography;

namespace MSL.utils
{
    public class X25519KeyPairGenerator
    {
        private SecureRandom random;

        public void Init(X25519KeyGenerationParameters parameters)
        {
            this.random = parameters.Random;
        }

        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            // 生成私钥
            byte[] privateKey = new byte[32];
            random.NextBytes(privateKey);

            // 按照X25519规范调整私钥
            privateKey[0] &= 248;
            privateKey[31] &= 127;
            privateKey[31] |= 64;

            // 使用Chaos.NaCl生成公钥
            byte[] publicKey = MontgomeryCurve25519.GetPublicKey(privateKey);

            var publicKeyParams = new X25519PublicKeyParameters(publicKey);
            var privateKeyParams = new X25519PrivateKeyParameters(privateKey);

            return new AsymmetricCipherKeyPair(publicKeyParams, privateKeyParams);
        }
    }

    public class X25519KeyGenerationParameters
    {
        public SecureRandom Random { get; }

        public X25519KeyGenerationParameters(SecureRandom random)
        {
            this.Random = random;
        }
    }

    public class SecureRandom
    {
        private readonly RNGCryptoServiceProvider rng;

        public SecureRandom()
        {
            rng = new RNGCryptoServiceProvider();
        }

        public void NextBytes(byte[] bytes)
        {
            rng.GetBytes(bytes);
        }
    }

    public class AsymmetricCipherKeyPair
    {
        public X25519PublicKeyParameters Public { get; }
        public X25519PrivateKeyParameters Private { get; }

        public AsymmetricCipherKeyPair(X25519PublicKeyParameters publicKey, X25519PrivateKeyParameters privateKey)
        {
            Public = publicKey;
            Private = privateKey;
        }
    }

    public class X25519PublicKeyParameters
    {
        private readonly byte[] keyData;

        public X25519PublicKeyParameters(byte[] data)
        {
            keyData = data;
        }

        public byte[] GetEncoded()
        {
            return keyData;
        }
    }

    public class X25519PrivateKeyParameters
    {
        private readonly byte[] keyData;

        public X25519PrivateKeyParameters(byte[] data)
        {
            keyData = data;
        }

        public byte[] GetEncoded()
        {
            return keyData;
        }
    }
}
