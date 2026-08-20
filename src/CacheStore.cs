using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace GakumasSmartLauncher
{
    public sealed class CacheStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("GakumasSmartLauncher:cache:v1");
        private readonly string _cachePath;

        public CacheStore(string cachePath)
        {
            if (string.IsNullOrWhiteSpace(cachePath))
            {
                throw new ArgumentException("Cache path is required.", "cachePath");
            }

            _cachePath = cachePath;
        }

        public string CachePath
        {
            get { return _cachePath; }
        }

        public bool Exists
        {
            get { return File.Exists(_cachePath); }
        }

        public void Save(LaunchRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            record.Validate();
            var serializer = new JavaScriptSerializer();
            var plaintext = Encoding.UTF8.GetBytes(serializer.Serialize(record));
            byte[] protectedBytes = null;
            try
            {
                protectedBytes = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
                var directory = Path.GetDirectoryName(_cachePath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    throw new LauncherException("快取路徑無效。");
                }

                Directory.CreateDirectory(directory);
                var temporaryPath = Path.Combine(directory, Path.GetFileName(_cachePath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.WriteAllBytes(temporaryPath, protectedBytes);
                    if (File.Exists(_cachePath))
                    {
                        File.Replace(temporaryPath, _cachePath, null);
                    }
                    else
                    {
                        File.Move(temporaryPath, _cachePath);
                    }
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new LauncherException("Windows 無法保護啟動資料。", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new LauncherException("沒有權限寫入受保護的啟動資料。", ex);
            }
            catch (IOException ex)
            {
                throw new LauncherException("無法保存啟動資料，請稍後再試。", ex);
            }
            finally
            {
                Array.Clear(plaintext, 0, plaintext.Length);
                if (protectedBytes != null)
                {
                    Array.Clear(protectedBytes, 0, protectedBytes.Length);
                }
            }
        }

        public LaunchRecord Load()
        {
            if (!File.Exists(_cachePath))
            {
                return null;
            }

            byte[] protectedBytes = null;
            byte[] plaintext = null;
            try
            {
                protectedBytes = File.ReadAllBytes(_cachePath);
                plaintext = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                var serializer = new JavaScriptSerializer();
                var record = serializer.Deserialize<LaunchRecord>(Encoding.UTF8.GetString(plaintext));
                if (record == null)
                {
                    throw new LauncherException("啟動資料內容無效，請重新從 DMM 同步。");
                }

                record.Validate();
                return record;
            }
            catch (CryptographicException ex)
            {
                throw new LauncherException("啟動資料無法解密；它只能由建立時的 Windows 使用者讀取。", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new LauncherException("啟動資料已損壞，請重新從 DMM 同步。", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new LauncherException("沒有權限讀取受保護的啟動資料。", ex);
            }
            catch (IOException ex)
            {
                throw new LauncherException("無法讀取啟動資料，請稍後再試。", ex);
            }
            finally
            {
                if (protectedBytes != null)
                {
                    Array.Clear(protectedBytes, 0, protectedBytes.Length);
                }

                if (plaintext != null)
                {
                    Array.Clear(plaintext, 0, plaintext.Length);
                }
            }
        }
    }
}
