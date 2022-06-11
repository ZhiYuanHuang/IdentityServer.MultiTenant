using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Service
{
    public class EncryptService
    {
        private readonly Aes _AesInstance;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;
        public EncryptService(IConfiguration config) {
            string aesKey = config["AesKey"];

            _AesInstance = Aes.Create();
            _AesInstance.Key = Encoding.UTF8.GetBytes(aesKey);
            _AesInstance.Padding = PaddingMode.PKCS7;
            _AesInstance.Mode = CipherMode.ECB;
            _encryptor = _AesInstance.CreateEncryptor();
            _decryptor = _AesInstance.CreateDecryptor();
        }

        public string Encrypt_Aes(string plainText) {
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(plainText);
            byte[] resultArray = _encryptor.TransformFinalBlock(toEncryptArray,0,toEncryptArray.Length);
            return Convert.ToBase64String(resultArray,0,resultArray.Length);
        }

        public string Decrypt_Aes(string cipherText) {
            byte[] toEncryptArray = Convert.FromBase64String(cipherText);
            byte[] resultArray = _decryptor.TransformFinalBlock(toEncryptArray,0,toEncryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }
    }
}
