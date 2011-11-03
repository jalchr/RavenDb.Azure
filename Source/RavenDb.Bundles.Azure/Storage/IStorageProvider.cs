using System.IO;

namespace RavenDb.Bundles.Azure.Storage
{
    public interface IStorageProvider
    {
        void            Initialize();
        DirectoryInfo   GetDirectoryForDatabase(string databaseName);
    }
}