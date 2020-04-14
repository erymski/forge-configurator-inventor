using System;

namespace IoConfigDemo
{
    public class Project
    {
        public Project(string projectName) 
        {
            if(Name == string.Empty) {
                throw new Exception("Initializing Project with empty name");
            }

            Name = projectName; 
            OSSSourceModel = $"{ONC.projectsFolder}-{projectName}";
            OSSThumbnail = $"{ONC.cacheFolder}-{projectName}-original-thumbnail.svg";
            HrefThumbnail = "bike.png"; // temporary icon
        }

        static public Project FromObjectKey(string objectKey) {
            if(!objectKey.StartsWith($"{ONC.projectsFolder}-")) {
                throw new Exception("Initializing Project from invalid bucket key: " + objectKey);
            }

            var projectName = objectKey.Substring(ONC.projectsFolder.Length+1);
            return new Project(projectName);
        }

        public string Name { get; private set; }
        public string OSSSourceModel {get; private set; }
        public string OSSThumbnail { get; private set; }
        public string HrefThumbnail { get; private set; }

        public OSSObjectKeyProvider KeyProvider(string hash) => new OSSObjectKeyProvider(Name, hash);
    }
}