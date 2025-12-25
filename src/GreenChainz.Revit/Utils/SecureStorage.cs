using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace GreenChainz.Revit.Utils
{
    public static class SecureStorage
    {
        private const string RegistryKeyPath = @"Software\GreenChainz\RevitPlugin";
        private const string TokenKeyName = "AuthToken";
        private const string EmailKeyName = "UserEmail";
        private const string CreditsKeyName = "UserCredits";

        // Optional entropy for additional security (unique to the application)
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GreenChainz-Revit-Plugin-Salt");

        public static void SaveToken(string token)
        {
            try
            {
                byte[] plaintext = Encoding.UTF8.GetBytes(token);
                byte[] ciphertext = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
                string encryptedToken = Convert.ToBase64String(ciphertext);

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    key.SetValue(TokenKeyName, encryptedToken);
                }
            }
            catch (Exception ex)
            {
                // Log error or handle it
                throw new Exception("Failed to save token securely.", ex);
            }
        }

        public static string LoadToken()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        string encryptedToken = key.GetValue(TokenKeyName) as string;
                        if (!string.IsNullOrEmpty(encryptedToken))
                        {
                            byte[] ciphertext = Convert.FromBase64String(encryptedToken);
                            byte[] plaintext = ProtectedData.Unprotect(ciphertext, Entropy, DataProtectionScope.CurrentUser);
                            return Encoding.UTF8.GetString(plaintext);
                        }
                    }
                }
            }
            catch
            {
                // Token might be corrupted or decryption failed (e.g. password changed)
                ClearToken();
            }
            return null;
        }

        public static void ClearToken()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
            {
                if (key != null)
                {
                    key.DeleteValue(TokenKeyName, false);
                }
            }
        }

        public static void SaveUserInfo(string email, int credits)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                if (email != null) key.SetValue(EmailKeyName, email);
                key.SetValue(CreditsKeyName, credits);
            }
        }

        public static string LoadUserEmail()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(EmailKeyName) as string;
            }
        }

        public static int LoadUserCredits()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                object val = key?.GetValue(CreditsKeyName);
                if (val is int i) return i;
                return 0;
            }
        }
    }
}
