using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace StorageContract
{
    [DisplayName("StorageContract")]
    [ManifestExtra("Author", "Neo Service Layer")]
    [ManifestExtra("Description", "Decentralized storage service")]
    [ManifestExtra("Version", "1.0.0")]
    public class StorageContract : SmartContract
    {
        private const byte DataPrefix = 0x01;
        private const byte MetadataPrefix = 0x02;
        private const byte AccessPrefix = 0x03;
        
        [DisplayName("FileStored")]
        public static event Action<UInt160, string, ulong> OnFileStored;

        [DisplayName("FileRetrieved")]
        public static event Action<UInt160, string> OnFileRetrieved;

        [DisplayName("FileDeleted")]
        public static event Action<UInt160, string> OnFileDeleted;

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                Runtime.Log("StorageContract deployed successfully");
            }
        }

        [DisplayName("storeFile")]
        public static string StoreFile(string fileName, string content, bool isPrivate)
        {
            var owner = Runtime.ExecutingScriptHash;
            var fileId = "file-" + Runtime.Time;
            var fileKey = ((ByteString)new byte[] { DataPrefix }).Concat(fileId);
            var metadataKey = ((ByteString)new byte[] { MetadataPrefix }).Concat(fileId);
            
            var fileData = new FileData
            {
                Id = fileId,
                FileName = fileName,
                Content = content,
                Owner = owner,
                IsPrivate = isPrivate,
                CreatedAt = Runtime.Time,
                Size = content.Length
            };

            Storage.Put(Storage.CurrentContext, fileKey, StdLib.Serialize(fileData));
            
            var metadata = new FileMetadata
            {
                Id = fileId,
                FileName = fileName,
                Owner = owner,
                IsPrivate = isPrivate,
                CreatedAt = Runtime.Time,
                Size = content.Length,
                AccessCount = 0
            };

            Storage.Put(Storage.CurrentContext, metadataKey, StdLib.Serialize(metadata));
            OnFileStored(owner, fileName, (ulong)content.Length);
            
            return fileId;
        }

        [DisplayName("retrieveFile")]
        public static string RetrieveFile(string fileId)
        {
            var requester = Runtime.ExecutingScriptHash;
            var fileKey = ((ByteString)new byte[] { DataPrefix }).Concat(fileId);
            var metadataKey = ((ByteString)new byte[] { MetadataPrefix }).Concat(fileId);
            
            var fileBytes = Storage.Get(Storage.CurrentContext, fileKey);
            if (fileBytes == null)
                return null;
            
            var fileData = (FileData)StdLib.Deserialize(fileBytes);
            
            // Check access permissions
            if (fileData.IsPrivate && fileData.Owner != requester)
            {
                // Check if requester has explicit access
                if (!HasAccess(fileId, requester))
                    return null;
            }
            
            // Update metadata
            var metadataBytes = Storage.Get(Storage.CurrentContext, metadataKey);
            if (metadataBytes != null)
            {
                var metadata = (FileMetadata)StdLib.Deserialize(metadataBytes);
                metadata.AccessCount++;
                Storage.Put(Storage.CurrentContext, metadataKey, StdLib.Serialize(metadata));
            }
            
            OnFileRetrieved(requester, fileData.FileName);
            return fileData.Content;
        }

        [DisplayName("deleteFile")]
        public static bool DeleteFile(string fileId)
        {
            var requester = Runtime.ExecutingScriptHash;
            var fileKey = ((ByteString)new byte[] { DataPrefix }).Concat(fileId);
            var metadataKey = ((ByteString)new byte[] { MetadataPrefix }).Concat(fileId);
            
            var fileBytes = Storage.Get(Storage.CurrentContext, fileKey);
            if (fileBytes == null)
                return false;
            
            var fileData = (FileData)StdLib.Deserialize(fileBytes);
            
            // Only owner can delete
            if (fileData.Owner != requester)
                return false;
            
            // Delete file and metadata
            Storage.Delete(Storage.CurrentContext, fileKey);
            Storage.Delete(Storage.CurrentContext, metadataKey);
            
            OnFileDeleted(requester, fileData.FileName);
            return true;
        }

        [DisplayName("grantAccess")]
        public static bool GrantAccess(string fileId, UInt160 user)
        {
            var owner = Runtime.ExecutingScriptHash;
            var fileKey = ((ByteString)new byte[] { DataPrefix }).Concat(fileId);
            var fileBytes = Storage.Get(Storage.CurrentContext, fileKey);
            
            if (fileBytes == null)
                return false;
            
            var fileData = (FileData)StdLib.Deserialize(fileBytes);
            
            // Only owner can grant access
            if (fileData.Owner != owner)
                return false;
            
            var accessKey = ((ByteString)new byte[] { AccessPrefix }).Concat(fileId).Concat(user);
            Storage.Put(Storage.CurrentContext, accessKey, 1);
            
            return true;
        }

        [DisplayName("revokeAccess")]
        public static bool RevokeAccess(string fileId, UInt160 user)
        {
            var owner = Runtime.ExecutingScriptHash;
            var fileKey = ((ByteString)new byte[] { DataPrefix }).Concat(fileId);
            var fileBytes = Storage.Get(Storage.CurrentContext, fileKey);
            
            if (fileBytes == null)
                return false;
            
            var fileData = (FileData)StdLib.Deserialize(fileBytes);
            
            // Only owner can revoke access
            if (fileData.Owner != owner)
                return false;
            
            var accessKey = ((ByteString)new byte[] { AccessPrefix }).Concat(fileId).Concat(user);
            Storage.Delete(Storage.CurrentContext, accessKey);
            
            return true;
        }

        [DisplayName("hasAccess")]
        public static bool HasAccess(string fileId, UInt160 user)
        {
            var accessKey = ((ByteString)new byte[] { AccessPrefix }).Concat(fileId).Concat(user);
            var accessBytes = Storage.Get(Storage.CurrentContext, accessKey);
            return accessBytes != null;
        }

        [DisplayName("getFileMetadata")]
        public static object GetFileMetadata(string fileId)
        {
            var metadataKey = ((ByteString)new byte[] { MetadataPrefix }).Concat(fileId);
            var metadataBytes = Storage.Get(Storage.CurrentContext, metadataKey);
            
            if (metadataBytes == null)
                return null;
            
            return StdLib.Deserialize(metadataBytes);
        }

        [DisplayName("getStorageStats")]
        public static object GetStorageStats()
        {
            // This would maintain global storage statistics
            // Simplified implementation
            return new StorageStats
            {
                TotalFiles = 100,
                TotalSize = 1000000,
                ActiveUsers = 50
            };
        }
    }

    public class FileData
    {
        public string Id;
        public string FileName;
        public string Content;
        public UInt160 Owner;
        public bool IsPrivate;
        public ulong CreatedAt;
        public int Size;
    }

    public class FileMetadata
    {
        public string Id;
        public string FileName;
        public UInt160 Owner;
        public bool IsPrivate;
        public ulong CreatedAt;
        public int Size;
        public int AccessCount;
    }

    public class StorageStats
    {
        public int TotalFiles;
        public long TotalSize;
        public int ActiveUsers;
    }
}