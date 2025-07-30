using UnityEngine;

[CreateAssetMenu(fileName = "SelectedSiteData", menuName = "ScriptableObjects/SelectedSiteData", order = 1)]
public class SelectedSiteData : ScriptableObject
{
    public int siteIndex = -1;
    public string siteName = "";
}