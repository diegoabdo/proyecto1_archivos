using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO;
using System.Linq;
using System.Xml;


public class FATFile
{
    public string Name { get; set; }
    public string FirstDataFilePath { get; set; }
    public bool IsInRecycleBin { get; set; } = false;
    public int TotalCharacters { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class DataBlock
{
    public string Data { get; set; }
    public string NextFilePath { get; set; }
    public bool EOF { get; set; }
}

public class FATSystem
{
    private const int BlockSize = 20;
    private const string FatDirectory = "FAT/";
    private const string DataDirectory = "Data/";

    private List<FATFile> fatTable = new List<FATFile>();

    public FATSystem()
    {
        Directory.CreateDirectory(FatDirectory);
        Directory.CreateDirectory(DataDirectory);
        LoadFatTable();
    }

    private void LoadFatTable()
    {
        if (File.Exists(FatDirectory + "FAT.json"))
        {
            string json = File.ReadAllText(FatDirectory + "FAT.json");
            fatTable = JsonConvert.DeserializeObject<List<FATFile>>(json);
        }
    }

    private void SaveFatTable()
    {
        string json = JsonConvert.SerializeObject(fatTable, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(FatDirectory + "FAT.json", json);
    }

    public void CreateFile(string fileName, string content)
    {
        var fatFile = new FATFile
        {
            Name = fileName,
            CreatedAt = DateTime.Now,
            TotalCharacters = content.Length
        };

        fatFile.FirstDataFilePath = WriteDataBlocks(content);
        fatTable.Add(fatFile);
        SaveFatTable();

        Console.WriteLine("Archivo creado correctamente.");
    }

    private string WriteDataBlocks(string content)
    {
        string firstFilePath = null;
        string previousFilePath = null;

        for (int i = 0; i < content.Length; i += BlockSize)
        {
            string blockData = content.Substring(i, Math.Min(BlockSize, content.Length - i));

            var dataBlock = new DataBlock
            {
                Data = blockData,
                EOF = (i + BlockSize >= content.Length)
            };

            string filePath = DataDirectory + Guid.NewGuid() + ".json";
            File.WriteAllText(filePath, JsonConvert.SerializeObject(dataBlock, Newtonsoft.Json.Formatting.Indented));

            if (firstFilePath == null)
                firstFilePath = filePath;

            if (previousFilePath != null)
            {
                var previousBlock = JsonConvert.DeserializeObject<DataBlock>(File.ReadAllText(previousFilePath));
                previousBlock.NextFilePath = filePath;
                File.WriteAllText(previousFilePath, JsonConvert.SerializeObject(previousBlock, Newtonsoft.Json.Formatting.Indented));
            }

            previousFilePath = filePath;
        }

        return firstFilePath;
    }

    public void ListFiles()
    {
        var files = fatTable.Where(f => !f.IsInRecycleBin).ToList();
        if (files.Count == 0)
        {
            Console.WriteLine("No hay archivos disponibles.");
            return;
        }

        for (int i = 0; i < files.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {files[i].Name} - {files[i].TotalCharacters} caracteres - Creado: {files[i].CreatedAt} - Modificado: {files[i].ModifiedAt}");
        }
    }

    public void OpenFile(int index)
    {
        if (index < 0 || index >= fatTable.Count || fatTable[index].IsInRecycleBin)
        {
            Console.WriteLine("Archivo no válido.");
            return;
        }

        var fatFile = fatTable[index];
        Console.WriteLine($"Archivo: {fatFile.Name}");
        Console.WriteLine($"Tamaño: {fatFile.TotalCharacters}");
        Console.WriteLine($"Creado: {fatFile.CreatedAt} ");
        Console.WriteLine($"Modificado: {fatFile.ModifiedAt}");

        string content = ReadFileContent(fatFile.FirstDataFilePath);
        Console.WriteLine("Contenido");
        Console.WriteLine(content);
    }

    private string ReadFileContent(string firstFilePath)
    {
        string content = "";
        string filePath = firstFilePath;

        while (!string.IsNullOrEmpty(filePath))
        {
            var dataBlock = JsonConvert.DeserializeObject<DataBlock>(File.ReadAllText(filePath));
            content += dataBlock.Data;
            filePath = dataBlock.NextFilePath;
        }

        return content;
    }

    public void ModifyFile(int index, string newContent)
    {
        if (index < 0 || index >= fatTable.Count || fatTable[index].IsInRecycleBin)
        {
            Console.WriteLine("Archivo no válido.");
            return;
        }

        var fatFile = fatTable[index];
        Console.WriteLine($"Modificando archivo: {fatFile.Name} \n");

        DeleteDataBlocks(fatFile.FirstDataFilePath);

        fatFile.FirstDataFilePath = WriteDataBlocks(newContent);
        fatFile.ModifiedAt = DateTime.Now;
        fatFile.TotalCharacters = newContent.Length;

        SaveFatTable();
        Console.WriteLine("Archivo modificado correctamente.");
    }

    private void DeleteDataBlocks(string firstFilePath)
    {
        string filePath = firstFilePath;

        while (!string.IsNullOrEmpty(filePath))
        {
            var dataBlock = JsonConvert.DeserializeObject<DataBlock>(File.ReadAllText(filePath));
            File.Delete(filePath);
            filePath = dataBlock.NextFilePath;
        }
    }

    public void DeleteFile(int index)
    {
        if (index < 0 || index >= fatTable.Count || fatTable[index].IsInRecycleBin)
        {
            Console.WriteLine("Archivo no válido.");
            return;
        }

        var fatFile = fatTable[index];
        fatFile.IsInRecycleBin = true;
        fatFile.DeletedAt = DateTime.Now;

        SaveFatTable();
        Console.WriteLine("Archivo movido a la Papelera de Reciclaje.");
    }

    public void RecoverFile(int index)
    {
        if (index < 0 || index >= fatTable.Count || !fatTable[index].IsInRecycleBin)
        {
            Console.WriteLine("Archivo no válido.");
            return;
        }

        var fatFile = fatTable[index];
        fatFile.IsInRecycleBin = false;
        fatFile.DeletedAt = null;

        SaveFatTable();
        Console.WriteLine("Archivo recuperado.");
    }

    public void ListRecycleBinFiles()
    {
        var recycleBinFiles = fatTable.Where(f => f.IsInRecycleBin).ToList();
        if (recycleBinFiles.Count == 0)
        {
            Console.WriteLine("No hay archivos en la papelera de reciclaje.");
            return;
        }

        for (int i = 0; i < recycleBinFiles.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {recycleBinFiles[i].Name} - {recycleBinFiles[i].TotalCharacters} caracteres - Eliminado: {recycleBinFiles[i].DeletedAt}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        FATSystem fatSystem = new FATSystem();
        bool exit = false;

        while (!exit)
        {
            Console.WriteLine("\n--- Menú FAT ---");
            Console.WriteLine("1. Crear un archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir un archivo");
            Console.WriteLine("4. Modificar un archivo");
            Console.WriteLine("5. Eliminar un archivo");
            Console.WriteLine("6. Recuperar un archivo");
            Console.WriteLine("7. Salir");
            Console.Write("Seleccione una opción: ");
            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    Console.Write("Nombre del archivo: ");
                    string fileName = Console.ReadLine();
                    Console.Write("Contenido del archivo: ");
                    string content = Console.ReadLine();
                    fatSystem.CreateFile(fileName, content);
                    break;
                case "2":
                    fatSystem.ListFiles();
                    break;
                case "3":
                    fatSystem.ListFiles();
                    Console.Write("Seleccione el archivo a abrir: ");
                    int openIndex = int.Parse(Console.ReadLine()) - 1;
                    fatSystem.OpenFile(openIndex);
                    break;
                case "4":
                    fatSystem.ListFiles();
                    Console.Write("Seleccione el archivo a modificar: ");
                    int modifyIndex = int.Parse(Console.ReadLine()) - 1;
                    Console.Write("Nuevo contenido: ");
                    string newContent = Console.ReadLine();
                    fatSystem.ModifyFile(modifyIndex, newContent);
                    break;
                case "5":
                    fatSystem.ListFiles();
                    Console.Write("Seleccione el archivo a eliminar: ");
                    int deleteIndex = int.Parse(Console.ReadLine()) - 1;
                    fatSystem.DeleteFile(deleteIndex);
                    break;
                case "6":
                    fatSystem.ListRecycleBinFiles();
                    Console.Write("Seleccione el archivo a recuperar: ");
                    int recoverIndex = int.Parse(Console.ReadLine()) - 1;
                    fatSystem.RecoverFile(recoverIndex);
                    break;
                case "7":
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }
}
