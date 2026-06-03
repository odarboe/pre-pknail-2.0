using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;


namespace PKLicense.UnitTests
{
    [TestFixture]
    public class StringEncryptionTest
    {
        [Test]
        public void TestPasswordEncryption()
        {
            string password = @"!this1smypassword$";
            string message = @"The quick brown fox jumped over the lazy dog";
            string encrypted = StringEncryption.SimpleEncryptWithPassword(message, password);
            string decrypted = StringEncryption.SimpleDecryptWithPassword(encrypted, password);
            if (decrypted.Equals(message)) { Assert.Pass(); } else { Assert.Fail(); }
        }

        [Test]
        public void TestByteArrayEncryption()
        {
            byte[] cryptKey = new byte[32];
            byte[] authKey = new byte[32];
            Random random = new Random();
            random.NextBytes(cryptKey);
            random.NextBytes(authKey);

            string cryptKeyEncoded = System.Convert.ToBase64String(cryptKey);
            int encLen = cryptKeyEncoded.Length;
            byte[] cryptKeyDecoded = System.Convert.FromBase64String(cryptKeyEncoded);

            string message = @"The quick brown fox jumped over the lazy dog";
            string encrypted = StringEncryption.SimpleEncrypt(message, cryptKeyDecoded, authKey);
            string decrypted = StringEncryption.SimpleDecrypt(encrypted, cryptKey, authKey);
            if (decrypted.Equals(message)) { Assert.Pass(); } else { Assert.Fail(); }            
        }
    }
}
