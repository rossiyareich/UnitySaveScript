using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO.Compression;


public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager I;
	
	public string ArchiveFileExtension = ".save";
	public string UnitFileExtension = ".csave";
	public string MetadataFileExtension = ".meta";
	
    public event EventHandler<bool> OnSaveLoad;

    public List<string> archivesList { get; private set; }

    string rootPath;

    private void Awake()
    {
        rootPath = Application.persistentDataPath;
        I = this;
    }

    public bool TryRefreshIndex()
    {
        archivesList = Directory.GetFiles(rootPath).ToList();
        return archivesList.Any();
    }

    public IEnumerable<int> GetAvailableSlots(IEnumerable<string> archives) =>
        archives.Where(x => x.EndsWith(ArchiveFileExtension)).Select(x => int.Parse(x.Split('-', '.')[1]));

    IEnumerable<ISaveLoad> FindAllObjects()
    {
        foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var childrenInterfaces = rootGameObject.GetComponentsInChildren<ISaveLoad>();
            foreach (var childInterface in childrenInterfaces)
            {
                yield return childInterface;
            }
        }
    }

    public string GetFinalArchivePath(int saveSlot) =>
        Path.Combine(rootPath, $"save-{saveSlot}{ArchiveFileExtension}");

    public string GetFinalEntryName(ISaveLoad data) =>
        $"{data.GetGameObjectName()}-{data.GetDataTypeName()}{UnitFileExtension}";

    public void WriteArchiveMetadata(int saveSlot)
    {
        using (FileStream zipToOpen = new FileStream(GetFinalArchivePath(saveSlot), FileMode.Open))
        {
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                var metadataFileName = $"metadata{MetadataFileExtension}";
                var entry = archive.GetEntry(metadataFileName);
                var saveMetadata = new SaveMetadata();
                if (entry is null)
                {
                    entry = archive.CreateEntry(metadataFileName);
                    saveMetadata.CreatedAt = DateTime.UtcNow.ToString();
                }
                if (saveMetadata.CreatedAt is null)
                {
                    using (var stream = entry.Open())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            saveMetadata.CreatedAt = JsonUtility.FromJson<SaveMetadata>(reader.ReadToEnd()).CreatedAt;
                        }
                    }
                }

                entry.Delete();
                entry = archive.CreateEntry(metadataFileName);

                using (var stream = entry.Open())
                {
                    saveMetadata.LastSaved = DateTime.UtcNow.ToString();
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(JsonUtility.ToJson(saveMetadata));
                    }
                }
            }
        }
    }

    public bool CreateArchiveIfNotExists(int saveSlot)
    {
        if (!TryRefreshIndex() || !GetAvailableSlots(archivesList).Contains(saveSlot))
        {
            string rawDirPath = Path.Combine(rootPath, $"tempsave-{saveSlot}");
            Directory.CreateDirectory(rawDirPath);
            ZipFile.CreateFromDirectory(rawDirPath, GetFinalArchivePath(saveSlot));

            using (FileStream zipToOpen = new FileStream(GetFinalArchivePath(saveSlot), FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    foreach (var data in FindAllObjects())
                    {
                        archive.CreateEntry(GetFinalEntryName(data));
                    }
                }
            }

            Directory.Delete(rawDirPath);
            TryRefreshIndex();
            return true;
        }
        else
        {
            using (FileStream zipToOpen = new FileStream(GetFinalArchivePath(saveSlot), FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    var entriesName = archive.Entries.Select(x => x.FullName);
                    foreach (var data in FindAllObjects())
                    {
                        if (!entriesName.Contains(GetFinalEntryName(data)))
                        {
                            archive.CreateEntry(GetFinalEntryName(data));
                        }
                    }
                }
            }
        }

        return false;
    }

    public SaveMetadata GetSaveMetadata(int saveSlot)
    {
        SaveMetadata saveMetadata;

        using (FileStream zipToOpen = new FileStream(GetFinalArchivePath(saveSlot), FileMode.Open))
        {
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                var metadataFileName = $"metadata{MetadataFileExtension}";
                using (var stream = archive.GetEntry(metadataFileName).Open())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        saveMetadata = JsonUtility.FromJson<SaveMetadata>(reader.ReadToEnd());
                    }
                }
            }
        }

        return saveMetadata;
    }

    public void SaveAll(int saveSlot, out bool overwrote)
    {
        overwrote = !CreateArchiveIfNotExists(saveSlot);
        WriteArchiveMetadata(saveSlot);

        using (var zipToOpen = new FileStream(GetFinalArchivePath(saveSlot), FileMode.Open))
        {
            using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                foreach (var data in FindAllObjects())
                {
                    var entry = archive.GetEntry(GetFinalEntryName(data));
                    entry.Delete();
                    entry = archive.CreateEntry(GetFinalEntryName(data));

                    using (var stream = entry.Open())
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.Write(JsonUtility.ToJson(data.Save()));
                        }
                    }
                } 
            }
        }

        OnSaveLoad?.Invoke(this, true);
    }

    public void LoadAll(int saveSlot, out bool autoCreated)
    {
        autoCreated = CreateArchiveIfNotExists(saveSlot);

        if (autoCreated)
        {
            SaveAll(saveSlot, out _);
        }

        using (var zipToOpen = new FileStream(GetFinalArchivePath(saveSlot), FileMode.Open))
        {
            using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
            {
                foreach (var data in FindAllObjects())
                {
                    var entry = archive.GetEntry(GetFinalEntryName(data));
                    using (var stream = entry.Open())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var correctObject = Activator.CreateInstance(Type.GetType(data.GetDataTypeName()));
                            var loadedData = JsonUtility.FromJson(reader.ReadToEnd(), correctObject.GetType()) as ISaveLoadData;
                            loadedData ??= correctObject as ISaveLoadData;
                            data.Load(loadedData);
                        }
                    }
                }
            }
        }

        OnSaveLoad?.Invoke(this, false);
    }

}
