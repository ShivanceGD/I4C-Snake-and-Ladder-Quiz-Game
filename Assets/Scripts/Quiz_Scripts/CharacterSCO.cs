using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CharacterSCO", menuName = "CharacterSCO")]
public class CharacterSCO : ScriptableObject
{
    [Header("Female Characters")]
    public List<Sprite> FemaleCharacters_Icons = new List<Sprite>();
    //public List<string> FemaleCharacters_Names = new List<string>();
    [Header("Male Characters")]
    public List<Sprite> MaleCharacters_Icons = new List<Sprite>();
    //public List<string> MaleCharacters_Names = new List<string>();
    [Header("Anonymous")]
    public Sprite Anonymous_icon;
    public string Anonymous_Name;
    [Header("Common")]
    //public List<string> JobDescription = new List<string>();
    public List<Color> BGColor = new List<Color>();
    
}
