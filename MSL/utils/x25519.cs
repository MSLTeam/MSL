using Chaos.NaCl;
using System;
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

    public static class PublicKeyBoxCompat
    {
        /// <summary>
        /// 解密数据
        /// </summary>
        public static byte[] Open(byte[] cipherText, byte[] nonce, byte[] privateKey, byte[] publicKey)
        {
            if (cipherText == null) throw new ArgumentNullException(nameof(cipherText));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));

            if (nonce.Length != 24) throw new ArgumentException("Nonce must be 24 bytes", nameof(nonce));
            if (privateKey.Length != 32) throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));
            if (publicKey.Length != 32) throw new ArgumentException("Public key must be 32 bytes", nameof(publicKey));
            if (cipherText.Length < 16) throw new ArgumentException("CipherText too short", nameof(cipherText));

            // 计算共享密钥
            byte[] sharedKey = new byte[32];

            // 使用ArraySegment包装参数
            var sharedKeySegment = new ArraySegment<byte>(sharedKey);
            var privateKeySegment = new ArraySegment<byte>(privateKey);
            var publicKeySegment = new ArraySegment<byte>(publicKey);

            // 密钥交换
            MontgomeryCurve25519.KeyExchange(
                sharedKeySegment,
                publicKeySegment,
                privateKeySegment
            );

            // 准备解密
            byte[] message = new byte[cipherText.Length - 16]; // 减去认证标签大小

            // 使用ArraySegment进行转换
            var messageSegment = new ArraySegment<byte>(message);
            var cipherTextSegment = new ArraySegment<byte>(cipherText);
            var nonceSegment = new ArraySegment<byte>(nonce);

            // 解密
            bool success = XSalsa20Poly1305.TryDecrypt(
                messageSegment,
                cipherTextSegment,
                sharedKeySegment,
                nonceSegment
            );

            if (!success)
            {
                throw new CryptographicException("验证失败：消息可能被篡改或密钥错误");
            }

            return message;
        }

        /// <summary>
        /// 加密数据
        /// </summary>
        public static byte[] Create(byte[] message, byte[] nonce, byte[] privateKey, byte[] publicKey)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));
            if (publicKey == null) throw new ArgumentNullException(nameof(publicKey));

            if (nonce.Length != 24) throw new ArgumentException("Nonce must be 24 bytes", nameof(nonce));
            if (privateKey.Length != 32) throw new ArgumentException("Private key must be 32 bytes", nameof(privateKey));
            if (publicKey.Length != 32) throw new ArgumentException("Public key must be 32 bytes", nameof(publicKey));

            // 计算共享密钥
            byte[] sharedKey = new byte[32];

            // 使用ArraySegment包装参数
            var sharedKeySegment = new ArraySegment<byte>(sharedKey);
            var privateKeySegment = new ArraySegment<byte>(privateKey);
            var publicKeySegment = new ArraySegment<byte>(publicKey);

            // 密钥交换
            MontgomeryCurve25519.KeyExchange(
                sharedKeySegment,
                publicKeySegment,
                privateKeySegment
                
            );

            // 准备加密后的密文（包含验证标签）
            byte[] cipherText = new byte[message.Length + 16]; // 加上认证标签大小

            // 使用ArraySegment进行转换
            var messageSegment = new ArraySegment<byte>(message);
            var cipherTextSegment = new ArraySegment<byte>(cipherText);
            var nonceSegment = new ArraySegment<byte>(nonce);

            // 加密
            XSalsa20Poly1305.Encrypt(
                cipherTextSegment,
                messageSegment,
                sharedKeySegment,
                nonceSegment
            );

            return cipherText;
        }

        /// <summary>
        /// 生成随机nonce
        /// </summary>
        public static byte[] GenerateNonce()
        {
            byte[] nonce = new byte[24];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }
            return nonce;
        }
    }
}
