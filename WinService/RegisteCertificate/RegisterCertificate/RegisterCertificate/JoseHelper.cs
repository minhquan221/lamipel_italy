using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RegisterCertificate
{
    public class JoseHelper
    {
        private static JoseHelper _current = new JoseHelper();

        public static JoseHelper Current
        {
            get
            {
                return _current != null ? _current : new JoseHelper();
            }
        }

        public string CreateToken(string inputString, string privateRsaKey)
        {
            RSAParameters rsaParams;
            using (var tr = new StringReader(privateRsaKey))
            {
                var pemReader = new PemReader(tr);
                var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
                if (keyPair == null)
                {
                    throw new Exception("Could not read RSA private key");
                }
                var privateRsaParams = keyPair.Private as RsaPrivateCrtKeyParameters;
                rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                return Jose.JWT.Encode(inputString, rsa, Jose.JwsAlgorithm.RS256);
            }
        }

        public bool DecodeToken(ref string DecodeData, string token, string publicRsaKey)
        {
            try
            {
                DecodeData = string.Empty;
                RSAParameters rsaParams;
                using (var tr = new StringReader(publicRsaKey))
                {
                    var pemReader = new PemReader(tr);
                    var publicKeyParams = pemReader.ReadObject() as RsaKeyParameters;
                    if (publicKeyParams == null)
                    {
                        throw new Exception("Could not read RSA public key");
                    }
                    rsaParams = DotNetUtilities.ToRSAParameters(publicKeyParams);
                }
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(rsaParams);
                    // This will throw if the signature is invalid
                    DecodeData = Jose.JWT.Decode(token, rsa, Jose.JwsAlgorithm.RS256);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool Verify(string token, string key)
        {
            try
            {
                string[] parts = token.Split('.');
                string header = parts[0];
                string payload = parts[1];
                byte[] crypto = Base64UrlDecode(parts[2]);

                string headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
                JObject headerData = JObject.Parse(headerJson);

                string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
                JObject payloadData = JObject.Parse(payloadJson);

                var keyBytes = Convert.FromBase64String(key); // your key here

                AsymmetricKeyParameter asymmetricKeyParameter = PublicKeyFactory.CreateKey(keyBytes);
                RsaKeyParameters rsaKeyParameters = (RsaKeyParameters)asymmetricKeyParameter;
                RSAParameters rsaParameters = new RSAParameters();
                rsaParameters.Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned();
                rsaParameters.Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned();
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.ImportParameters(rsaParameters);
                SHA256 sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(parts[0] + '.' + parts[1]));

                RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm("SHA256");
                if (!rsaDeformatter.VerifySignature(hash, Convert.FromBase64String(parts[2])))
                {
                    //throw new ApplicationException(string.Format("Invalid signature"));
                    return false;
                }
                else
                    return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string Decode(string token, string key, bool verify = true)
        {
            try
            {
                string[] parts = token.Split('.');
                string header = parts[0];
                string payload = parts[1];
                byte[] crypto = Base64UrlDecode(parts[2]);

                string headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
                JObject headerData = JObject.Parse(headerJson);

                string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
                JObject payloadData = JObject.Parse(payloadJson);

                if (verify)
                {
                    var keyBytes = Convert.FromBase64String(key); // your key here

                    AsymmetricKeyParameter asymmetricKeyParameter = PublicKeyFactory.CreateKey(keyBytes);
                    RsaKeyParameters rsaKeyParameters = (RsaKeyParameters)asymmetricKeyParameter;
                    RSAParameters rsaParameters = new RSAParameters();
                    rsaParameters.Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned();
                    rsaParameters.Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned();
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.ImportParameters(rsaParameters);
                    SHA256 sha256 = SHA256.Create();
                    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(parts[0] + '.' + parts[1]));

                    RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                    rsaDeformatter.SetHashAlgorithm("SHA256");
                    if (!rsaDeformatter.VerifySignature(hash, Convert.FromBase64String(parts[2])))
                        throw new ApplicationException(string.Format("Invalid signature"));
                }

                return payloadData.ToString();
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        // from JWT spec
        private static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 1: output += "==="; break; // Three pad chars
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new System.Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }

        public string Sign(string payload, string privateKey)
        {
            List<string> segments = new List<string>();
            var header = new { alg = "RS256", typ = "JWT" };

            DateTime issued = DateTime.Now;
            DateTime expire = DateTime.Now.AddHours(10);

            byte[] headerBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(header, Formatting.None));
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            segments.Add(Base64UrlEncode(headerBytes));
            segments.Add(Base64UrlEncode(payloadBytes));

            string stringToSign = string.Join(".", segments.ToArray());

            byte[] bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

            byte[] keyBytes = Convert.FromBase64String(privateKey);

            var privKeyObj = Asn1Object.FromByteArray(keyBytes);
            var privStruct = RsaPrivateKeyStructure.GetInstance((Asn1Sequence)privKeyObj);

            ISigner sig = SignerUtilities.GetSigner("SHA256withRSA");

            sig.Init(true, new RsaKeyParameters(true, privStruct.Modulus, privStruct.PrivateExponent));

            sig.BlockUpdate(bytesToSign, 0, bytesToSign.Length);
            byte[] signature = sig.GenerateSignature();

            segments.Add(Base64UrlEncode(signature));
            return string.Join(".", segments.ToArray());
        }

        

        private static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }


    }
}
