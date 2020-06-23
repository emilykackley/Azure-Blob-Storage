using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;

namespace AzureStorageLibrary.UnitTests
{
    
    [TestClass]
    public class AzureStorageUnitTests
    {
        private readonly string _connectionString = "DefaultEndpointsProtocol=https;AccountName=<accountname>;AccountKey=<accountkey>;";

        [TestMethod]
        [TestCategory("Container")]
        public void TestStorageContainer_Successful()
        {
            string message = "";
            try
            {
                AzureStorage storageTest = new AzureStorage(_connectionString, "containerTestSuccess");
                storageTest.DeleteBlobContainer();
            }
            catch(Exception ex)
            {
                message = ex.Message;
            }
            Assert.AreEqual(message, "");
 
        }

        [TestMethod]
        [TestCategory("Container")]
        public void TestStorageContainer_Unsuccessful()
        {
            string message = "";
            try
            {
                AzureStorage testContainer1 = new AzureStorage("NotRealConnectionString", "TestContainer1");
                testContainer1.DeleteBlobContainer();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            Assert.AreEqual(message, "Settings must be of the form \"name=value\".");

            message = "";
            string badAccountName = "DefaultEndpointsProtocol=https;AccountName=notrealaccount;AccountKey=key;";
            try
            {
                AzureStorage testContainer2 = new AzureStorage(badAccountName, "TestContainer2");
                testContainer2.DeleteBlobContainer();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            Assert.AreEqual(message, "The remote name could not be resolved: 'notrealaccount.blob.core.windows.net'");

            message = "";
            string badAccountKey = "DefaultEndpointsProtocol=https;AccountName=accountname;AccountKey=notRealAccountKey;";
            try
            {
                AzureStorage testContainer3 = new AzureStorage(badAccountKey, "TestContainer3");
                testContainer3.DeleteBlobContainer();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            Assert.AreEqual(message, "The remote server returned an error: (403) Forbidden."); 
        }

        [TestMethod]
        [TestCategory("Container")]
        public void TestDeleteContainer_Successful()
        {
            AzureStorage storageTest = new AzureStorage(_connectionString, "Delete Container");
            storageTest.DeleteBlobContainer();
            Assert.IsFalse(storageTest.CheckStorageConnection());
        }

        [TestMethod]
        [TestCategory("Container")]
        public void CheckStorageConnection_Successful()
        {
            AzureStorage storageTest = new AzureStorage(_connectionString, "storage connection");
            Assert.IsTrue(storageTest.CheckStorageConnection());
            storageTest.DeleteBlobContainer();
        }
                
        [TestMethod]
        [TestCategory("Upload")]
        public void TestUploadBlob_Successful()
        {
            string result = null;
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }
            
            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            try
            {
                storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }
            Assert.IsNull(result);

            storageTest.DeleteBlobContainerContents();
            File.Delete(uploadPath);
        }

        [TestMethod]
        [TestCategory("Upload")]
        public void TestUploadBlob_Fail()
        {
            string result = "";
            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            try
            {
                storageTest.UploadBlob("Not a real path", "directory", "UploadUser", "Comments");
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }

            Assert.AreEqual(result, "File does not exist");
            storageTest.DeleteBlobContainerContents();
            
        }

        [TestMethod]
        [TestCategory("Download")]
        public void TestDownload_Successful()
        {
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string downloadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string result = null;
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");

            var blobName = @"directory/" + Path.GetFileName(uploadPath);

            try
            {
                storageTest.DownloadBlob(blobName, downloadPath);
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }

            Assert.IsNull(result);

            bool isEqual = FileCompare(uploadPath, downloadPath);
            Assert.IsTrue(isEqual);

            storageTest.DeleteBlobContainerContents();
            File.Delete(uploadPath);
            File.Delete(downloadPath);
        }

        [TestMethod]
        [TestCategory("Download")]
        public void TestDownload_UnSuccessful()
        {
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string downloadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string result = null;
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");

            var blobName = @"directory/notRealBlobName";

            try
            {
                storageTest.DownloadBlob(blobName, downloadPath);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            Assert.AreEqual(result, "The remote server returned an error: (404) Not Found.");

            bool isEqual = FileCompare(uploadPath, downloadPath);
            Assert.IsFalse(isEqual);

            storageTest.DeleteBlobContainerContents();
            File.Delete(uploadPath);
            File.Delete(downloadPath);
        }

        [TestMethod]
        [TestCategory("Delete")]
        public void TestDeleteBlob_Successful()
        {
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string downloadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string result = null;
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");

            var blobName = @"directory/" + Path.GetFileName(uploadPath);
            try
            {
                storageTest.DeleteBlob(blobName, "Delete User");
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }
            Assert.IsNull(result);

            result = null;
            try
            {
                storageTest.DownloadBlob(blobName, downloadPath);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            Assert.AreEqual(result, "The remote server returned an error: (404) Not Found.");

            bool isEqual = FileCompare(uploadPath, downloadPath);
            Assert.IsFalse(isEqual);

            storageTest.DeleteBlobContainerContents();
            File.Delete(uploadPath);
        }

        [TestMethod]
        [TestCategory("Delete")]
        public void TestDeleteBlob_Unsuccessful()
        {
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string result = null;
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");

            var blobName = "NotRealBlobName";
            try
            {
                storageTest.DeleteBlob(blobName, "Delete User");
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            Assert.AreEqual(result, "The remote server returned an error: (404) Not Found.");

            storageTest.DeleteBlobContainerContents();
            File.Delete(uploadPath);
        }

        [TestMethod]
        [TestCategory("Recover")]
        public void TestRecoverBlob_Successful()
        {
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string result = null;
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");

            var blobName = @"directory/" + Path.GetFileName(uploadPath);

            storageTest.DeleteBlob(blobName, "Delete User");

            try
            {
                storageTest.RecoverBlob(blobName, "Recover User");
            }

            catch (Exception ex)
            {
                result = ex.Message;
            }

            Assert.IsNull(result);

            storageTest.DeleteBlobContainerContents();
            File.Delete(uploadPath);
        }

        [TestMethod]
        [TestCategory("Recover")]
        public void TestRecoverBlob_Unsuccessful()
        {
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string result = null;
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();

            var blobName = @"directory/" + Path.GetFileName(uploadPath);

            try
            {
                storageTest.RecoverBlob(blobName, "Recover User");
            }

            catch (Exception ex)
            {
                result = ex.Message;
            }

            Assert.AreEqual(result, "The remote server returned an error: (404) Not Found.");

            storageTest.DeleteBlobContainerContents();
            File.Delete(uploadPath);
        }

        [TestMethod]
        [TestCategory("RetreiveActive")]
        public void TestRetreiveActiveBlobs_Successful()
        {
            string result = null;
            string uploadPath1 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath1))
            {
                sw.WriteLine("Test file");
            }
            string uploadPath2 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath2))
            {
                sw.WriteLine("Test file");
            }
            string uploadPath3 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath3))
            {
                sw.WriteLine("Test file");
            }
            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();

            storageTest.UploadBlob(uploadPath1, "directory", "Upload User", "Comments");
            storageTest.UploadBlob(uploadPath2, "directory", "Upload User", "Comments");
            storageTest.UploadBlob(uploadPath3, "directory", "Upload User", "Comments");
            List<FileInfo> allBlobs = new List<FileInfo>();
            try
            {
                allBlobs = storageTest.RetrieveActiveBlobs("directory");
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }
            Assert.AreEqual(allBlobs.Count, 3);
            Assert.IsNull(result);

            File.Delete(uploadPath1);
            File.Delete(uploadPath2);
            File.Delete(uploadPath3);
            storageTest.DeleteBlobContainerContents();
        }

        [TestMethod]
        [TestCategory("RetreiveActive")]
        public void TestRetreiveActiveBlobs_Unsuccessful()
        {
            string result = null;
            string uploadPath1 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath1))
            {
                sw.WriteLine("Test file");
            }
            string uploadPath2 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath2))
            {
                sw.WriteLine("Test file");
            }
            string uploadPath3 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath3))
            {
                sw.WriteLine("Test file");
            }
            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            List<FileInfo> allBlobs = new List<FileInfo>();
        
            storageTest.UploadBlob(uploadPath1, "directory", "Upload User", "Comments");
            storageTest.UploadBlob(uploadPath2, "directory", "Upload User", "Comments");
            storageTest.UploadBlob(uploadPath3, "directory", "Upload User", "Comments");
            
            try
            {
                allBlobs = storageTest.RetrieveActiveBlobs("wrong directory");
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            Assert.AreEqual(allBlobs.Count, 0);
            Assert.IsNull(result);

            File.Delete(uploadPath1);
            File.Delete(uploadPath2);
            File.Delete(uploadPath3);
            storageTest.DeleteBlobContainerContents();
        }

        [TestMethod]
        [TestCategory("RetreiveInactive")]
        public void TestRetreiveInactiveBlobs_Successful()
        {
            string result = null;
            string uploadPath1 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath1))
            {
                sw.WriteLine("Test file");
            }
            string uploadPath2 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath2))
            {
                sw.WriteLine("Test file");
            }
            string uploadPath3 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            using (StreamWriter sw = File.CreateText(uploadPath3))
            {
                sw.WriteLine("Test file");
            }
            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();

            storageTest.UploadBlob(uploadPath1, "directory", "Upload User", "Comments");
            storageTest.UploadBlob(uploadPath2, "directory", "Upload User", "Comments");
            storageTest.UploadBlob(uploadPath3, "directory", "Upload User", "Comments");

            var blobName1 = @"directory/" + Path.GetFileName(uploadPath1);
            var blobName2 = @"directory/" + Path.GetFileName(uploadPath2);
            var blobName3 = @"directory/" + Path.GetFileName(uploadPath3);

            storageTest.DeleteBlob(blobName1, "Delete user");
            storageTest.DeleteBlob(blobName2, "Delete user");
            storageTest.DeleteBlob(blobName3, "Delete user");

            List<string> allBlobs = new List<string>();
            try
            {
                allBlobs = storageTest.RetrieveInactiveBlobs("directory");
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            Assert.IsTrue(allBlobs.Contains(@"directory/" + Path.GetFileName(uploadPath1)));
            Assert.IsTrue(allBlobs.Contains(@"directory/" + Path.GetFileName(uploadPath2)));
            Assert.IsTrue(allBlobs.Contains(@"directory/" + Path.GetFileName(uploadPath3)));
            Assert.IsNull(result);

            File.Delete(uploadPath1);
            File.Delete(uploadPath2);
            File.Delete(uploadPath3);
            storageTest.DeleteBlobContainerContents();
        }

        [TestMethod]
        [TestCategory("RetreiveInactive")]
        public void TestRetreiveInactiveBlobs_Unsuccessful()
        {
            string result = null;
            string uploadPath1 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();

            List<string> allBlobs = new List<string>();

            try
            {
                allBlobs = storageTest.RetrieveInactiveBlobs("not real directory");
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            Assert.AreEqual(allBlobs.Count, 0);
            allBlobs.Clear();

            try
            {
                allBlobs = storageTest.RetrieveInactiveBlobs("directory");
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            Assert.IsFalse(allBlobs.Contains(@"directory/" + Path.GetFileName(uploadPath1)));

            Assert.IsNull(result);

            File.Delete(uploadPath1);
            storageTest.DeleteBlobContainerContents();
        }

        [TestMethod]
        [TestCategory("RetreivePrevious")]
        public void TestRetreivePreviousBlobs_Successful()
        {
            string uploadPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".txt";
            string result = null;
            using (StreamWriter sw = File.CreateText(uploadPath))
            {
                sw.WriteLine("Test file");
            }

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");
            storageTest.UploadBlob(uploadPath, "directory", "UploadUser", "Comments");
            List<FileInfo> allBlobVersions = new List<FileInfo>();
            var blobName = @"directory/" + Path.GetFileName(uploadPath);
            try
            {
                allBlobVersions = storageTest.RetrievePreviousBlobVersions(blobName);
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }

            Assert.AreEqual(allBlobVersions.Count, 3);
            Assert.IsNull(result);

            File.Delete(uploadPath);
            storageTest.DeleteBlobContainerContents();
        }

        [TestMethod]
        [TestCategory("RetreivePrevious")]
        public void TestRetreivePreviousBlobs_Unsuccessful()
        {
            string result = null;

            AzureStorage storageTest = new AzureStorage(_connectionString, "test record");
            storageTest.DeleteBlobContainerContents();

            List<FileInfo> allBlobVersions = new List<FileInfo>();
            var blobName = "not real name";
            try
            {
                allBlobVersions = storageTest.RetrievePreviousBlobVersions(blobName);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            Assert.AreEqual(allBlobVersions.Count, 0);
            Assert.IsNull(result);

            storageTest.DeleteBlobContainerContents();
        }

        public bool FileCompare(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                        return false;
                }
                return true;
            }
            else
                return false;
        }
    }
}
