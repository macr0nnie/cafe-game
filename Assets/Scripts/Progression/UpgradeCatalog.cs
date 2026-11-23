using UnityEngine;

/// Everything the shop computer sells, in the order it lists them.
[CreateAssetMenu(menuName = "Cafe/Upgrade Catalog", fileName = "UpgradeCatalog")]
public class UpgradeCatalog : ScriptableObject
{
    public UpgradeDef[] All = new UpgradeDef[0];
}
