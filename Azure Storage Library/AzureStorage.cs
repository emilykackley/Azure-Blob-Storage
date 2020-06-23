using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace AzureStorageLibrary
{
    class AzureStorage
    {
        readonly CloudBlobContainer blobContainer;
        //Keys for blob metadata
        private readonly string _blobNameKey;
        private readonly string _fileNameKey;
        private readonly string _blobStatusKey;
        private readonly string _fileCommentsKey;
        private readonly string _uploadDateKey;
        private readonly string _deleteDateKey;
        private readonly string _recoverDateKey;
        private readonly string _fileSizeKey;
        private readonly string _uploadKey;
        private readonly string _deleteKey;
        private readonly string _recoverKey;

        //Gets blob container for indicated sitename "containerName". Will create a new container if it does not
        //  already exist. Permissions currently set to "No Public Access".
        public AzureStorage(string connectionString, string containerName)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                //Container name must be lower case and have no spaces
                containerName = containerName.ToLower().Replace(" ", "");
                blobContainer = blobClient.GetContainerReference(containerName);
                blobContainer.CreateIfNotExists();
                //Set permissions to no public access
                var perm = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Off
                };

                blobContainer.SetPermissions(perm);

                //Set Metadata Keys
                _blobNameKey = "BlobName";
                _fileNameKey = "FileName";
                _blobStatusKey = "BlobStatus";
                _fileCommentsKey = "FileComments";
                _uploadDateKey = "UploadDate";
                _deleteDateKey = "DeleteDate";
                _recoverDateKey = "RecoverDate";
                _fileSizeKey = "FileSize";
                _uploadKey = "UploadedBy";
                _deleteKey = "DeletedBy";
                _recoverKey = "RecoveredBy";
            }
            catch(Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        //Check Storage Account Connection
        public bool CheckStorageConnection()
        {
            return blobContainer.Exists();
        }

        //Deletes storage container contents
        public void DeleteBlobContainer()
        {
            try
            {
                blobContainer.Delete();
            }
            catch(Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
        //Deletes storage container contents
        public void DeleteBlobContainerContents()
        {
            Parallel.ForEach(blobContainer.ListBlobs(useFlatBlobListing:true), x => ((CloudBlob)x).Delete(DeleteSnapshotsOption.IncludeSnapshots));
        }

        //Uploads files to Azure Storage Cloud in specified directories. Allows 10 simultaneous threads for uploading
        public void UploadBlob(string fileToUpload, string directory, string userID, string fileComments)
        {
            //ProgressBar progressBar = InitiateProgressBar("Uploading Files...", filesToUpload.Count);
            //int count = 1;
            BlobRequestOptions parallelThreadCountOptions = new BlobRequestOptions
            {
                ParallelOperationThreadCount = 10
            };
            if (File.Exists(fileToUpload))
            {
                long fileSize = new System.IO.FileInfo(fileToUpload).Length;
                byte[] fileContent = ReadFile(fileToUpload);
                string blobName = directory + @"\" + Path.GetFileName(fileToUpload);
                var blob = blobContainer.GetBlockBlobReference(blobName);


                try
                {
                    //Allow simultaneous I/O operations to help with large files
                    blob.UploadFromByteArray(fileContent, 0, fileContent.Length);

                    //Set file info for blob: blob name, file name, blob status, file comments, upload date/time, delete date/time,
                    //recover date/time, file size, uploaded by, deleted by, recovered by
                    blob.Metadata.Add(_blobNameKey, blob.Name);
                    blob.Metadata.Add(_fileNameKey, Path.GetFileName(fileToUpload));
                    blob.Metadata.Add(_blobStatusKey, "Active");
                    blob.Metadata.Add(_fileCommentsKey, fileComments);
                    blob.Metadata.Add(_uploadDateKey, DateTime.UtcNow.ToString());
                    blob.Metadata.Add(_deleteDateKey, "N/A");
                    blob.Metadata.Add(_recoverDateKey, "N/A");
                    blob.Metadata.Add(_fileSizeKey, fileSize.ToString());
                    blob.Metadata.Add(_uploadKey, userID);
                    blob.Metadata.Add(_deleteKey, "N/A");
                    blob.Metadata.Add(_recoverKey, "N/A");
                    blob.SetMetadata();

                    //Create a snapshot for blob versioning
                    blob.Snapshot();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message);
                }

            }
            else
            {
                throw new ArgumentException("File does not exist");
            }
        }

        //Downloads files from Azure Storage Cloud in specified directory. Allows 10 simultaneous IO operations
        public void DownloadBlob(string file, string downloadPath)
        {
            BlobRequestOptions parallelThreadCountOptions = new BlobRequestOptions
            {
                ParallelOperationThreadCount = 10
            };

            {
                try
                {
                    var blob = blobContainer.GetBlockBlobReference(file);
                    using (Stream fileStream = new FileStream(downloadPath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        blob.DownloadToStream(fileStream, null, parallelThreadCountOptions, null);
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message);
                }
            }
        }

        // Delete selected blob and snapshot
        public void DeleteBlob(string file, string userID)
        {

            BlobRequestOptions parallelThreadCountOptions = new BlobRequestOptions
            {
                ParallelOperationThreadCount = 10
            };

            try
            {
                var blob = blobContainer.GetBlockBlobReference(file);
                FileInfo attributes = ReadFileInfo(blob);
                blob.Metadata[_deleteDateKey] = DateTime.UtcNow.ToString();
                blob.Metadata[_deleteKey] = userID;
                blob.SetMetadata();
                blob.DeleteIfExists(DeleteSnapshotsOption.IncludeSnapshots, null, parallelThreadCountOptions, null);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        //Retrieves all deleted blobs in specified directory and allows the user to recover selected blobs
        public void RecoverBlob(string file, string userID)
        {
            BlobRequestOptions parallelThreadCountOptions = new BlobRequestOptions
            {
                ParallelOperationThreadCount = 10
            };

            try
            {
                var blob = blobContainer.GetBlockBlobReference(file);
                blob.Undelete(null, parallelThreadCountOptions, null);
                FileInfo attributes = ReadFileInfo(blob);
                blob.Metadata[_recoverDateKey] = DateTime.UtcNow.ToString();
                blob.Metadata[_recoverKey] = userID;
                blob.SetMetadata();
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        public List<FileInfo> RetrieveActiveBlobs(string directory)
        {
            List<FileInfo> files = new List<FileInfo>();
            try
            {
                CloudBlobDirectory blobDirectory = blobContainer.GetDirectoryReference(directory);
                IEnumerable<IListBlobItem> listBlobItems = blobDirectory.ListBlobs();
                foreach (IListBlobItem blob in listBlobItems)
                {
                    var blobItem = blob as CloudBlockBlob;
                    FileInfo fileInfo = ReadFileInfo(blobItem);
                    files.Add(fileInfo);
                }
            }
            catch(Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
            
            return files;
        }

        public List<string> RetrieveInactiveBlobs(string directory)
        {
            List<string> files = new List<string>();
            try
            {
                CloudBlobDirectory blobDirectory = blobContainer.GetDirectoryReference(directory);
                IEnumerable<IListBlobItem> listBlobItems = blobDirectory.ListBlobs(true, BlobListingDetails.Deleted, null, null);
                foreach (IListBlobItem blob in listBlobItems)
                {
                    var blobItem = blob as CloudBlockBlob;
                    files.Add(blobItem.Name);
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
            return files;
        }

        public List<FileInfo> RetrievePreviousBlobVersions(string blobName)
        {
            List<FileInfo> files = new List<FileInfo>();
            try
            {
                IList<IListBlobItem> snapshots = blobContainer.ListBlobs(null, true, BlobListingDetails.Snapshots).Where(x => ((CloudBlockBlob)x).IsSnapshot).ToList();
                foreach (var item in snapshots)
                {
                    CloudBlockBlob blob = item as CloudBlockBlob;
                    if (blob.Name == blobName)
                    {
                        files.Add(ReadFileInfo(blob));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }

            return files;
        }

        public FileInfo ReadFileInfo(CloudBlockBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                FileInfo attributes = new FileInfo
                {
                    BlobName = blob.Name,
                    FileName = blob.Metadata[_fileNameKey],
                    BlobStatus = blob.Metadata[_blobStatusKey],
                    FileComments = blob.Metadata[_fileCommentsKey],
                    UploadDate = blob.Metadata[_uploadDateKey],
                    DeleteDate = blob.Metadata[_deleteDateKey],
                    RecoverDate = blob.Metadata[_recoverDateKey],
                    FileSize = blob.Metadata[_fileSizeKey],
                    UploadedBy = blob.Metadata[_uploadKey],
                    DeletedBy = blob.Metadata[_deleteKey]
                };

                return attributes;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }

        //Read specified file byte by byte and return byte array
        private byte[] ReadFile(string filePath)
        {
            byte[] bytes;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                bytes = System.IO.File.ReadAllBytes(filePath);
                fs.Read(bytes, 0, System.Convert.ToInt32(fs.Length));
                fs.Close();
            }
            return bytes;
        }
    }
}
