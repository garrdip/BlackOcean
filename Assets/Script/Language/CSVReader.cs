using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVReader : MonoBehaviour
{
    public static char delimiter = ',';
    public static Dictionary<string, string> ReadToDictionary(string csvFilePath)
    {
        int cnt = 0;
        Dictionary<string, string> dict = new Dictionary<string, string>();
        using (var reader = new StreamReader(csvFilePath))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();
                if (line == "") continue;
                string[] values = line.Split(delimiter);
                if(cnt == 0 && values[0] != "Language") 
                {
                    dict["Error"] = "error";
                    Debug.Log("This is not Language file");
                    return dict;
                }
                cnt++;
                dict[values[0]] = values[1];
            }
        }

        return dict;
    }
}