using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ExcelReaderUtility
{
    public static List<string[]> ReadCSV(string filePath)
    {
        List<string[]> data = new List<string[]>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV file not found at path: {filePath}");
            return data;
        }

        using (StreamReader sr = new StreamReader(filePath))
        {
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] values = line.Split(',');
                data.Add(values);
            }
        }

        return data;
    }
}